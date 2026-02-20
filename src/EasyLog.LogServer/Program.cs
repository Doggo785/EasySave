using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyLog.LogServer
{
    class Program
    {
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        private const int Port = 5000;
        private const string LogDirectory = "Logs";

        // Dashboard stats
        private static int _totalConnections = 0;
        private static int _logsReceived = 0;
        private static int _logsWritten = 0;
        private static int _errors = 0;
        private static DateTime _startTime;
        private static string _lastLog = "-";
        private static readonly object _consoleLock = new object();

        static async Task Main(string[] args)
        {
            _startTime = DateTime.Now;
            Console.Clear();
            Console.CursorVisible = false;

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
                _lastLog = $"Critical Server Error: {ex.Message}";
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            Interlocked.Increment(ref _totalConnections);
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                while (true)
                {
                    string logData = await reader.ReadLineAsync();
                    if (logData == null) break;

                    if (!string.IsNullOrWhiteSpace(logData))
                    {
                        _logQueue.Enqueue(logData);
                        Interlocked.Increment(ref _logsReceived);

                        string cleanLog = logData.Replace("\r", "").Replace("\n", " ");
                        _lastLog = cleanLog.Length > 60 ? cleanLog.Substring(0, 57) + "..." : cleanLog;
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

        private static async Task RenderDashboardAsync()
        {
            while (true)
            {
                lock (_consoleLock)
                {
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
                    Console.WriteLine(" Latest Log Snippet:");
                    Console.WriteLine($" {_lastLog,-58}");
                    Console.WriteLine("============================================================");
                }
                await Task.Delay(500);
            }
        }
    }
}