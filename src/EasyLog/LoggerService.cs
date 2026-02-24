using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;



namespace EasyLog
{
    public class LoggerService
    {
        private string _logDirectory;
        private string _stateFilePath;
        public static bool LogFormat; // true = JSON, false = XML

        private static readonly object _stateLock = new object();
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        // Configurable log target and server connection
        public static LogTarget CurrentLogTarget { get; set; } = LogTarget.Both;
        public static string ServerIp { get; set; } = "127.0.0.1";
        public static int ServerPort { get; set; } = 25549;
        public static bool IsServerConnected { get; private set; } = false;

        private static CancellationTokenSource _reconnectCts = new CancellationTokenSource();

        static LoggerService()
        {
            _ = Task.Run(ProcessLogQueueAsync);
        }
        public static void ForceReconnect()
        {
            var oldCts = Interlocked.Exchange(ref _reconnectCts, new CancellationTokenSource());
            oldCts.Cancel();
            oldCts.Dispose();
            IsServerConnected = false;
        }


        public static async Task<bool> CheckServerConnectionAsync(int timeoutMs = 500)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                using var client = new TcpClient();
                await client.ConnectAsync(ServerIp, ServerPort, cts.Token);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string greeting = await reader.ReadLineAsync(cts.Token);
                return greeting == "EASYSAVE_LOGSERVER";
            }
            catch
            {
                return false;
            }
        }

        public LoggerService(bool logFormat)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "Logs");
            _stateFilePath = Path.Combine(_logDirectory, "state.json");
            EnsureDirectoryExist();

            LogFormat = logFormat;
        }
        public void WriteDailyLog(DailyLog logEntry)
        {
            lock (_stateLock)
            {
                if (CurrentLogTarget == LogTarget.Local || CurrentLogTarget == LogTarget.Both)
                {
                    string dailyFileName = $"{DateTime.Now:yyyy-MM-dd}";
                    if (LogFormat == true)
                    {
                        WriteJson(logEntry, dailyFileName);
                    }
                    else
                    {
                        WriteXml(logEntry, dailyFileName);
                    }
                }

                // Enqueue for centralized server if target is Centralized or Both
                if (CurrentLogTarget == LogTarget.Centralized || CurrentLogTarget == LogTarget.Both)
                {
                    string serialized;
                    if (LogFormat)
                    {
                        var options = new JsonSerializerOptions { WriteIndented = false };
                        serialized = JsonSerializer.Serialize(logEntry, options);
                    }
                    else
                    {
                        var xmlSerializer = new XmlSerializer(typeof(DailyLog));
                        using var sw = new StringWriter();
                        xmlSerializer.Serialize(sw, logEntry);
                        serialized = sw.ToString().ReplaceLineEndings(string.Empty);
                    }
                    _logQueue.Enqueue(serialized);
                }
            }
        }

        public void UpdateStateLog(StateLog stateEntry)
        {
            lock (_stateLock)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(stateEntry, options);

                File.WriteAllText(_stateFilePath, jsonString);
            }
        }
        public void EnsureDirectoryExist()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private static async Task ProcessLogQueueAsync()
        {
            while (true)
            {
                var token = _reconnectCts.Token;
                try
                {
                    using (var client = new TcpClient())
                    {
                        // Use a linked token that also enforces a 5-second connection timeout.
                        // Without this, ConnectAsync waits for the OS TCP timeout (20-75s)
                        // when the remote host silently drops packets or a transparent proxy
                        // intercepts the connection.
                        using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                        {
                            connectCts.CancelAfter(TimeSpan.FromSeconds(5));
                            await client.ConnectAsync(ServerIp, ServerPort, connectCts.Token);
                        }
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true))
                        using (var writer = new StreamWriter(stream, Encoding.UTF8, -1, leaveOpen: true) { AutoFlush = true })
                        {
                            using var greetCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                            greetCts.CancelAfter(TimeSpan.FromSeconds(3));
                            string greeting = await reader.ReadLineAsync(greetCts.Token);
                            if (greeting != "EASYSAVE_LOGSERVER")
                                throw new IOException("Invalid server handshake.");

                            IsServerConnected = true;
                            while (!token.IsCancellationRequested)
                            {
                                if (_logQueue.TryDequeue(out string logData))
                                {
                                    try
                                    {
                                        await writer.WriteLineAsync(logData.AsMemory(), token);
                                    }
                                    catch
                                    {
                                        IsServerConnected = false;
                                        _logQueue.Enqueue(logData);
                                        throw;
                                    }
                                }
                                else
                                {
                                    // Detect if the remote server has closed the connection:
                                    // Poll returns true when data is available OR the connection is closed.
                                    // If true but Available == 0, the remote end has disconnected.
                                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                                    {
                                        IsServerConnected = false;
                                        throw new IOException("Server disconnected.");
                                    }
                                    await Task.Delay(50, token);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    IsServerConnected = false;
                }
                catch (Exception)
                {

                    IsServerConnected = false;

                    try { await Task.Delay(2000, _reconnectCts.Token); }
                    catch (OperationCanceledException) { }
                }
            }
        }

        private void WriteJson(DailyLog logEntry, string dailyFileName)
        {
            string fullPath = Path.Combine(_logDirectory, $"{dailyFileName}.json");
            List<DailyLog> logs = new List<DailyLog>();

            if (File.Exists(fullPath))
            {
                try
                {
                    string existingJson = File.ReadAllText(fullPath);
                    logs = JsonSerializer.Deserialize<List<DailyLog>>(existingJson) ?? new List<DailyLog>();
                }
                catch { logs = new List<DailyLog>(); }
            }

            logs.Add(logEntry);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(logs, options);
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(jsonString);
            }
        }

        private void WriteXml(DailyLog logEntry, string dailyFileName)
        {
            string fullPath = Path.Combine(_logDirectory, $"{dailyFileName}.xml");
            List<DailyLog> logs = new List<DailyLog>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<DailyLog>), new XmlRootAttribute("DailyLogs"));

            if (File.Exists(fullPath))
            {
                try
                {
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (stream.Length > 0)
                            logs = (List<DailyLog>)serializer.Deserialize(stream);
                    }
                }
                catch { logs = new List<DailyLog>(); }
            }

            logs.Add(logEntry);

            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    serializer.Serialize(stream, logs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XML Write Error: {ex.Message}");
            }
        }
    }
}
