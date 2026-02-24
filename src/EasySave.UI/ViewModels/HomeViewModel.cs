using Avalonia.Threading;
using EasySave.Core.Models;
using EasySave.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySave.UI.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        private DispatcherTimer _updateTimer;

        // 1. Total number of jobs indicator
        private int _totalJobsCount;
        public int TotalJobsCount
        {
            get => _totalJobsCount;
            set => this.RaiseAndSetIfChanged(ref _totalJobsCount, value);
        }

        // 2. Last performed backup timestamp
        private string _lastBackupTime = "--:--";
        public string LastBackupTime
        {
            get => _lastBackupTime;
            set => this.RaiseAndSetIfChanged(ref _lastBackupTime, value);
        }

        // 3. Business software alert boolean
        private bool _isBusinessProcessRunning;
        public bool IsBusinessProcessRunning
        {
            get => _isBusinessProcessRunning;
            set => this.RaiseAndSetIfChanged(ref _isBusinessProcessRunning, value);
        }

        // 4. Current log format (JSON or XML)
        private string _logFormat = "JSON";
        public string LogFormat
        {
            get => _logFormat;
            set => this.RaiseAndSetIfChanged(ref _logFormat, value);
        }

        // 5. LogServer connection status
        private bool _isLogServerConnected;
        public bool IsLogServerConnected
        {
            get => _isLogServerConnected;
            set => this.RaiseAndSetIfChanged(ref _isLogServerConnected, value);
        }

        //public string WelcomeMessage => $"Bienvenue {Environment.UserName}";
        public string WelcomeMessage => $"{SettingsManager.Instance["Welcome"]} {Environment.UserName}";

        public HomeViewModel()
        {
            // Initial fetch to populate UI immediately
            UpdateDashboard();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += (sender, e) => UpdateDashboard();
            _updateTimer.Start();
        }

        // Updates all dashboard indicators. Triggered by the DispatcherTimer.
        private void UpdateDashboard()
        {

            TotalJobsCount = GetJobsCount();

            LastBackupTime = GetLastBackupTime();

            CheckBusinessProcess();

            LogFormat = SettingsManager.Instance.LogFormat ? "JSON" : "XML";

            CheckLogServerConnection();
        }

        private int GetJobsCount()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string jobsFilePath = Path.Combine(appDataPath, "ProSoft", "EasySave", "UserConfig", "jobs.json");

                if (File.Exists(jobsFilePath))
                {
                    // Open with FileShare.ReadWrite so we don't block the file if another part of the app is writing
                    using var stream = new FileStream(jobsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();

                    var jobs = JsonSerializer.Deserialize<List<SaveJob>>(json);
                    return jobs?.Count ?? 0;
                }
            }
            catch
            {
                // Silently ignore parsing/locking errors
            }
            return 0;
        }

        private string GetLastBackupTime()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string stateFilePath = Path.Combine(appDataPath, "ProSoft", "EasySave", "Logs", "state.json");

                if (File.Exists(stateFilePath))
                {
                    using var stream = new FileStream(stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();

                    var stateLog = JsonSerializer.Deserialize<StateLog>(json);

                    if (stateLog != null && stateLog.LastActionTimestamp != DateTime.MinValue)
                    {
                        return stateLog.LastActionTimestamp.ToString("HH:mm:ss - dd/MM/yyyy");
                    }
                }
            }
            catch
            {
                // Silently ignore parsing/locking errors
            }
            return "--:--";
        }

        private void CheckBusinessProcess()
        {
            var businessSoftwares = SettingsManager.Instance.BusinessSoftwareNames;
            IsBusinessProcessRunning = ProcessChecker.IsAnyProcessRunning(businessSoftwares);
        }

        private void CheckLogServerConnection()
        {
            IsLogServerConnected = EasyLog.LoggerService.IsServerConnected;
        }
    }
}