using Avalonia.Threading;
using EasySave.Core.Models;
using EasySave.Core.Services;
using ReactiveUI;
using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace EasySave.UI.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        private readonly SaveManager _saveManager;
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

        public HomeViewModel()
        {
            _saveManager = new SaveManager();

            // Initial fetch to populate UI immediately
            UpdateDashboard();

            // Setup a timer to refresh data in real-time (every 2 seconds)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += (sender, e) => UpdateDashboard();
            _updateTimer.Start();
        }

        /// <summary>
        /// Updates all dashboard indicators. Triggered by the DispatcherTimer.
        /// </summary>
        private void UpdateDashboard()
        {
            // Indicator 1: Count total jobs configured
            TotalJobsCount = _saveManager.GetJobs().Count;

            // Indicator 2: Fetch last timestamp from state.json
            LastBackupTime = GetLastBackupTime();

            // Indicator 3: Check if the Business Software is currently running
            CheckBusinessProcess();

            // Indicator 4: Get current log format from global settings
            LogFormat = SettingsManager.Instance.LogFormat ? "JSON" : "XML";
        }

        /// <summary>
        /// Reads the state.json file to extract the last action timestamp.
        /// </summary>
        private string GetLastBackupTime()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string stateFilePath = Path.Combine(appDataPath, "ProSoft", "EasySave", "Logs", "state.json");

                // Check if file exists to prevent exceptions
                if (File.Exists(stateFilePath))
                {
                    // Open with FileShare.ReadWrite to prevent locking issues while LoggerService is writing
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
                // Silently ignore parsing/locking errors to avoid UI crashes during real-time fetch
            }
            return "--:--";
        }

        /// <summary>
        /// Checks the running processes on the machine against the BusinessSoftwareName setting.
        /// </summary>
        private void CheckBusinessProcess()
        {
            string businessSoftware = SettingsManager.Instance.BusinessSoftwareName;

            if (string.IsNullOrWhiteSpace(businessSoftware))
            {
                IsBusinessProcessRunning = false;
                return;
            }

            // Clean process name (remove ".exe" if the user typed it in settings)
            string processName = businessSoftware.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? businessSoftware.Substring(0, businessSoftware.Length - 4)
                : businessSoftware;

            // Check system processes
            var processes = Process.GetProcessesByName(processName);
            IsBusinessProcessRunning = processes.Length > 0;
        }
    }
}