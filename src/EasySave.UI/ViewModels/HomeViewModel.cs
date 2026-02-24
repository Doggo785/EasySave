using Avalonia.Threading;
using EasySave.Core.Models;
using EasySave.Core.Services;
using ReactiveUI;
using System;

namespace EasySave.UI.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        private DispatcherTimer _updateTimer;
        private readonly SaveManager _saveManager;

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

        public string WelcomeMessage => $"{SettingsManager.Instance["Welcome"]} {Environment.UserName}";

        public HomeViewModel(SaveManager saveManager)
        {
            _saveManager = saveManager;
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
            TotalJobsCount = _saveManager.GetJobs().Count;

            var lastBackup = _saveManager.LastBackupTime;
            LastBackupTime = lastBackup != DateTime.MinValue
                ? lastBackup.ToString("HH:mm:ss - dd/MM/yyyy")
                : "--:--";

            CheckBusinessProcess();

            LogFormat = SettingsManager.Instance.LogFormat ? "JSON" : "XML";

            CheckLogServerConnection();
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