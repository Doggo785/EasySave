using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;



namespace EasyLog
{
    public class LoggerService
    {
        private string _logDirectory;
        private string _stateFilePath;
        public static bool _logFormat; // true = JSON, false = XML

        private static readonly object _stateLock = new object();
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        // Configurable log target and server IP
        public static LogTarget CurrentLogTarget { get; set; } = LogTarget.Both;
        public static string ServerIp { get; set; } = "127.0.0.1";
        public static bool IsServerConnected { get; private set; } = false;

        static LoggerService()
        {
            _ = Task.Run(ProcessLogQueueAsync);
        }

        /// <summary>
        /// Tests TCP connectivity to the configured log server.
        /// Uses a CancellationToken to abort the connection attempt on timeout.
        /// </summary>
        public static async Task<bool> CheckServerConnectionAsync(int timeoutMs = 500)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                using var client = new TcpClient();
                await client.ConnectAsync(ServerIp, 5000, cts.Token);
                return client.Connected;
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

            _logFormat = logFormat;
        }
        public void WriteDailyLog(DailyLog logEntry)
        {
            lock (_stateLock)
            {
                // Write locally if target is Local or Both
                if (CurrentLogTarget == LogTarget.Local || CurrentLogTarget == LogTarget.Both)
                {
                    string dailyFileName = $"{DateTime.Now:yyyy-MM-dd}";
                    if (_logFormat == true)
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
                    var options = new JsonSerializerOptions { WriteIndented = false };
                    string jsonString = JsonSerializer.Serialize(logEntry, options);
                    _logQueue.Enqueue(jsonString);
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
                try
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(ServerIp, 5000);
                        IsServerConnected = true;
                        using (var stream = client.GetStream())
                        using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                        {
                            while (true)
                            {
                                if (_logQueue.TryDequeue(out string logData))
                                {
                                    try
                                    {
                                        await writer.WriteLineAsync(logData);
                                    }
                                    catch
                                    {
                                        IsServerConnected = false;
                                        // Re-enqueue the log data if writing fails
                                        _logQueue.Enqueue(logData);
                                        throw; // Break the inner loop to reconnect
                                    }
                                }
                                else
                                {
                                    await Task.Delay(50);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    IsServerConnected = false;
                    // Wait before trying to reconnect
                    await Task.Delay(2000);
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
