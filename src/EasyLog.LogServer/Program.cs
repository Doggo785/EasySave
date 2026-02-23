using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyLog.LogServer
{
    class Program
    {
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        private const int Port = 25549;
        private const string LogDirectory = "Logs";

        // Dashboard stats
        private static int _totalConnections = 0;
        private static int _logsReceived = 0;
        private static int _logsWritten = 0;
        private static int _errors = 0;
        private static DateTime _startTime;
        private static volatile LastLogInfo _lastLogInfo = new();
        private static readonly ConcurrentDictionary<string, int> _clientStats = new();
        private static readonly object _consoleLock = new object();

        private record LogEntry(
            string? TimeStamp,
            string? ClientId,
            string? JobName,
            string? SourceFile,
            string? TargetFile,
            long FileSize,
            double TransferTimeMs
        );

        private sealed class LastLogInfo
        {
            public string Time { get; init; } = "-";
            public string ClientId { get; init; } = "-";
            public string JobName { get; init; } = "-";
            public long FileSize { get; init; } = 0;
            public double TransferTimeMs { get; init; } = 0;
            public bool IsParsed { get; init; } = false;
            public string Raw { get; init; } = "-";
        }

        static async Task Main(string[] args)
        {
            _startTime = DateTime.Now;

            try
            {
                if (!Console.IsOutputRedirected)
                {
                    Console.Clear();
                    Console.CursorVisible = false;
                }
            }
            catch { /* Ignore console errors in Docker */ }

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            Task dashboardTask = RenderDashboardAsync();
            Task writerTask = ProcessLogQueueAsync();

            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // Fire and forget
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errors);
                _lastLogInfo = new LastLogInfo { Raw = $"Critical Server Error: {ex.Message}" };
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            Interlocked.Increment(ref _totalConnections);
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: -1, leaveOpen: true) { AutoFlush = true };
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);

                await writer.WriteLineAsync("EASYSAVE_LOGSERVER");

                while (true)
                {
                    string logData = await reader.ReadLineAsync();
                    if (logData == null) break;

                    if (!string.IsNullOrWhiteSpace(logData))
                    {
                        _logQueue.Enqueue(logData);
                        Interlocked.Increment(ref _logsReceived);

                        try
                        {
                            var entry = JsonSerializer.Deserialize<LogEntry>(logData);
                            if (entry != null)
                            {
                                _clientStats.AddOrUpdate(entry.ClientId ?? "Unknown", 1, (_, c) => c + 1);
                                string formattedTime = DateTime.TryParse(entry.TimeStamp, out var dt)
                                    ? dt.ToString("yyyy-MM-dd HH:mm:ss")
                                    : entry.TimeStamp ?? "-";
                                _lastLogInfo = new LastLogInfo
                                {
                                    Time = formattedTime,
                                    ClientId = entry.ClientId ?? "Unknown",
                                    JobName = entry.JobName ?? "-",
                                    FileSize = entry.FileSize,
                                    TransferTimeMs = entry.TransferTimeMs,
                                    IsParsed = true
                                };
                            }
                        }
                        catch
                        {
                            string clean = logData.Replace("\r", "").Replace("\n", " ");
                            _lastLogInfo = new LastLogInfo { Raw = clean.Length > 56 ? clean[..53] + "..." : clean };
                        }
                    }
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref _errors);
            }
            finally
            {
                client.Close();
                Interlocked.Decrement(ref _totalConnections);
            }
        }

        private static async Task ProcessLogQueueAsync()
        {
            while (true)
            {
                if (_logQueue.TryDequeue(out string logEntry))
                {

                    string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
                    string filePath = Path.Combine(LogDirectory, fileName);

                    try
                    {
                        await File.AppendAllTextAsync(filePath, logEntry + Environment.NewLine);
                        Interlocked.Increment(ref _logsWritten);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref _errors);
                        _logQueue.Enqueue(logEntry); // Try again to write log
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }

        private static DateTime _lastFallbackLog = DateTime.MinValue;

        private static async Task RenderDashboardAsync()
        {
            while (true)
            {
                lock (_consoleLock)
                {
                    try
                    {
                        if (!Console.IsOutputRedirected)
                        {
                            var snapshot = _lastLogInfo;
                            var topClients = _clientStats
                                .OrderByDescending(kv => kv.Value)
                                .Take(5)
                                .ToList();

                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine("============================================================");
                            Console.WriteLine("                 EASYLOG CENTRALIZED SERVER                 ");
                            Console.WriteLine("============================================================");
                            Console.WriteLine($" Uptime:          {DateTime.Now - _startTime:hh\\:mm\\:ss}");
                            Console.WriteLine($" Port:            {Port}");
                            Console.WriteLine("------------------------------------------------------------");
                            Console.WriteLine($" Active Connections: {_totalConnections,-10}");
                            Console.WriteLine($" Logs Received:     {_logsReceived,-10}");
                            Console.WriteLine($" Logs Written:      {_logsWritten,-10}");
                            Console.WriteLine($" Queue Size:        {_logQueue.Count,-10}");
                            Console.WriteLine($" Errors:            {_errors,-10}");
                            Console.WriteLine("------------------------------------------------------------");
                            Console.WriteLine(" Latest Entry:");
                            if (snapshot.IsParsed)
                            {
                                Console.WriteLine($"  Time:     {snapshot.Time,-48}");
                                Console.WriteLine($"  Client:   {snapshot.ClientId,-48}");
                                Console.WriteLine($"  Job:      {snapshot.JobName,-48}");
                                Console.WriteLine($"  {"Size: " + snapshot.FileSize + " B",-30}{"Transfer: " + snapshot.TransferTimeMs + " ms",-28}");
                            }
                            else
                            {
                                Console.WriteLine($"  {snapshot.Raw,-58}");
                                Console.WriteLine($"  {string.Empty,-58}");
                                Console.WriteLine($"  {string.Empty,-58}");
                                Console.WriteLine($"  {string.Empty,-58}");
                            }
                            Console.WriteLine("------------------------------------------------------------");
                            Console.WriteLine(" Client Activity:");
                            for (int i = 0; i < 5; i++)
                            {
                                if (i < topClients.Count)
                                    Console.WriteLine($"  {topClients[i].Key,-44}{topClients[i].Value,8} logs  ");
                                else if (i == 0)
                                    Console.WriteLine($"  {"No data yet",-58}");
                                else
                                    Console.WriteLine($"  {string.Empty,-58}");
                            }
                            Console.WriteLine("============================================================");
                        }
                        else
                        {
                            // For Docker environment
                            if ((DateTime.Now - _lastFallbackLog).TotalSeconds >= 10)
                            {
                                Console.WriteLine($"[EasyLog Server] Uptime: {DateTime.Now - _startTime:hh\\:mm\\:ss} | Active Conn: {_totalConnections} | Received: {_logsReceived} | Written: {_logsWritten} | Queue: {_logQueue.Count} | Errors: {_errors}");
                                _lastFallbackLog = DateTime.Now;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore console errors
                    }
                }
                await Task.Delay(500);
            }
        }
    }
}