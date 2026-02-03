using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;

namespace EasyLog
{
    public class LoggerService
    {
        private string _logDirectory;
        private string _stateFilePath;

        public LoggerService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "Logs");
            _stateFilePath = Path.Combine(_logDirectory, "state.json");

            EnsureDirectoryExist();
        }
        public void WriteDailyLog(DailyLog logEntry)
        {
            string dailyFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
            string fullPath = Path.Combine(_logDirectory, dailyFileName);

            var options = new JsonSerializerOptions{ WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(logEntry, options);

            File.AppendAllText(fullPath, jsonString + Environment.NewLine);
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
    }
}
