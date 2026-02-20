using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.LogServer
{
    class Program
    {
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        private const int Port = 5000;
        private const string LogDirectory = "Logs";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting EasyLog Centralized Server...");

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            Task writerTask = ProcessLogQueueAsync();

            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Console.WriteLine($"Server listening on port {Port}...");

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
                Console.WriteLine($"Critical Server Error: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string logData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    _logQueue.Enqueue(logData);
                    Console.WriteLine($"Received log snippet from {client.Client.RemoteEndPoint}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client Error: {ex.Message}");
            }
            finally
            {
                client.Close();
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"File Write Error: {ex.Message}");
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
    }
}