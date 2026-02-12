using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;



namespace EasyLog
{
    public class LoggerService
    {
        private string _logDirectory;
        private string _stateFilePath;
        private bool _logFormat; // true = JSON, false = XML

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
            string dailyFileName = $"{DateTime.Now:yyyy-MM-dd}";
            if (_logFormat)
            {
                WriteJson(logEntry, dailyFileName);
            }
            else
            {
                WriteXml(logEntry, dailyFileName);
            }
        }
        public void UpdateStateLog(StateLog stateEntry)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(stateEntry, options);

            File.WriteAllText(_stateFilePath, jsonString);
        }
        public void EnsureDirectoryExist()
        {
            if(!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
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
