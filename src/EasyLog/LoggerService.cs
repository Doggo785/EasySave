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
        private bool _logFormat; // true = JSON, false = XML

        private static readonly object _stateLock = new object();
        private static readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        static LoggerService()
        {
            _ = Task.Run(ProcessLogQueueAsync);
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

                string dailyFileName = $"{DateTime.Now:yyyy-MM-dd}";
                if (_logFormat)
                {
                    WriteJson(logEntry, dailyFileName);
                }
                else
                {
                    WriteXml(logEntry, dailyFileName);
                }

                var options = new JsonSerializerOptions { WriteIndented = false };
                string jsonString = JsonSerializer.Serialize(logEntry, options);
                _logQueue.Enqueue(jsonString);
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
            if(!Directory.Exists(_logDirectory))
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
                        await client.ConnectAsync("127.0.0.1", 5000);
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
                    // Wait before trying to reconnect
                    await Task.Delay(2000);
                }
            }
        }

        private void WriteJson(DailyLog logEntry, string dailyFileName)
        {
            string fullPath = Path.Combine(_logDirectory, $"{dailyFileName}.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(logEntry, options);

            File.AppendAllText(fullPath, jsonString + Environment.NewLine);
        }

        private void WriteXml(DailyLog logEntry, string dailyFileName)
        {
            string fullPath = Path.Combine(_logDirectory, $"{dailyFileName}.xml");

            List<DailyLog> logs;
            XmlSerializer serializer = new XmlSerializer(typeof(List<DailyLog>), new XmlRootAttribute("DailyLogs"));

            if (File.Exists(fullPath))
            {
                try
                {
                    using (var stream = new FileStream(fullPath, FileMode.Open))
                    {
                        logs = (List<DailyLog>)serializer.Deserialize(stream);
                    }
                }
                catch
                {
                    logs = new List<DailyLog>();
                }
            }
            else
            {
                logs = new List<DailyLog>();
            }
            logs.Add(logEntry);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                serializer.Serialize(stream, logs);
            }
        }
    }
}
