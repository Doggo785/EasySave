using EasyLog;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using EasySave.Core.Services;

namespace EasySave.UI.ViewModels
{
    public class SettingsViewModel : ReactiveObject
    {
        private DispatcherTimer _serverStatusTimer;
        // Language settings
        private int _selectedLanguageIndex;
        public int SelectedLanguageIndex
        {
            get => _selectedLanguageIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLanguageIndex, value);
                ChangeLanguage(value);
            }
        }

        // Log format settings
        private int _selectedLogFormatIndex;
        public int SelectedLogFormatIndex
        {
            get => _selectedLogFormatIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogFormatIndex, value);

                bool isJson = (value == 0);
                LoggerService._logFormat = isJson;
                SettingsManager.Instance.LogFormat = (value == 0);
                SettingsManager.Instance.SaveSettings();
            }
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                this.RaiseAndSetIfChanged(ref _isDarkMode, value);

                SettingsManager.Instance.ChangeTheme(value);
                SettingsManager.Instance.SaveSettings();

                if (Avalonia.Application.Current != null)
                {
                    Avalonia.Application.Current.RequestedThemeVariant = value ?
                        Avalonia.Styling.ThemeVariant.Dark :
                        Avalonia.Styling.ThemeVariant.Light;
                }
            }
        }

        // Business software properties
        private string _newBusinessSoftware = "";
        public string NewBusinessSoftware
        {
            get => _newBusinessSoftware;
            set => this.RaiseAndSetIfChanged(ref _newBusinessSoftware, value);
        }

        public ObservableCollection<string> BusinessSoftwareNames { get; }
        public ReactiveCommand<Unit, Unit> AddBusinessSoftwareCommand { get; }
        public ReactiveCommand<string, Unit> RemoveBusinessSoftwareCommand { get; }

        // Encrypted extensions properties
        private string _newExtension = "";
        public string NewExtension
        {
            get => _newExtension;
            set => this.RaiseAndSetIfChanged(ref _newExtension, value);
        }

        // Priority extensions properties
        private string _newPriorityExtension = "";
        public string NewPriorityExtension
        {
            get => _newPriorityExtension;
            set => this.RaiseAndSetIfChanged(ref _newPriorityExtension, value);
        }

        public ObservableCollection<string> PriorityExtensions { get; }
        public ReactiveCommand<Unit, Unit> AddPriorityExtensionCommand { get; }
        public ReactiveCommand<string, Unit> RemovePriorityExtensionCommand { get; }

        private string _maxParallelFileSizeKbText = "";
        public string MaxParallelFileSizeKbText
        {
            get => _maxParallelFileSizeKbText;
            set => this.RaiseAndSetIfChanged(ref _maxParallelFileSizeKbText, value);
        }

        private bool _maxSizeConfirmationVisible;
        public bool MaxSizeConfirmationVisible
        {
            get => _maxSizeConfirmationVisible;
            set => this.RaiseAndSetIfChanged(ref _maxSizeConfirmationVisible, value);
        }

        // Log target settings
        private int _selectedLogTargetIndex;
        public int SelectedLogTargetIndex
        {
            get => _selectedLogTargetIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogTargetIndex, value);
                SettingsManager.Instance.LogTarget = (LogTarget)value;
                LoggerService.CurrentLogTarget = (LogTarget)value;
                SettingsManager.Instance.SaveSettings();
                UpdateServerWarning();
            }
        }

        // Server offline warning
        private bool _serverOfflineWarningVisible;
        public bool ServerOfflineWarningVisible
        {
            get => _serverOfflineWarningVisible;
            set => this.RaiseAndSetIfChanged(ref _serverOfflineWarningVisible, value);
        }

        // Server IP settings
        private string _serverIp = "";
        public string ServerIp
        {
            get => _serverIp;
            set => this.RaiseAndSetIfChanged(ref _serverIp, value);
        }

        // Server Port settings
        private string _serverPort = "";
        public string ServerPort
        {
            get => _serverPort;
            set => this.RaiseAndSetIfChanged(ref _serverPort, value);
        }

        private bool _serverConnectionConfirmationVisible;
        public bool ServerConnectionConfirmationVisible
        {
            get => _serverConnectionConfirmationVisible;
            set => this.RaiseAndSetIfChanged(ref _serverConnectionConfirmationVisible, value);
        }

        public ReactiveCommand<Unit, Unit> SaveServerConnectionCommand { get; }

        public ReactiveCommand<Unit, Unit> SaveMaxSizeCommand { get; }

        public ObservableCollection<string> EncryptedExtensions { get; }
        public ReactiveCommand<Unit, Unit> AddExtensionCommand { get; }
        public ReactiveCommand<string, Unit> RemoveExtensionCommand { get; }

        public SettingsViewModel()
        {
            SettingsManager.Instance.LoadSettings();

            var settings = SettingsManager.Instance;

            // Initialize base settings
            _selectedLanguageIndex = settings.Language == "en" ? 1 : 0;
            _selectedLogFormatIndex = settings.LogFormat ? 0 : 1;
            _selectedLogTargetIndex = (int)settings.LogTarget;
            _serverIp = settings.ServerIp;
            _serverPort = settings.ServerPort.ToString();
            _isDarkMode = settings.IsDarkMode;
            IsDarkMode = settings.IsDarkMode;

            // Initialize observable collections
            BusinessSoftwareNames = new ObservableCollection<string>(settings.BusinessSoftwareNames);
            _maxParallelFileSizeKbText = settings.MaxParallelFileSizeKb.ToString();
            EncryptedExtensions = new ObservableCollection<string>(settings.EncryptedExtensions);
            PriorityExtensions = new ObservableCollection<string>(settings.PriorityExtensions);

            // Initialize commands
            AddBusinessSoftwareCommand = ReactiveCommand.Create(AddBusinessSoftware);
            RemoveBusinessSoftwareCommand = ReactiveCommand.Create<string>(RemoveBusinessSoftware);

            AddExtensionCommand = ReactiveCommand.Create(AddExtension);
            RemoveExtensionCommand = ReactiveCommand.Create<string>(RemoveExtension);

            AddPriorityExtensionCommand = ReactiveCommand.Create(AddPriorityExtension);
            RemovePriorityExtensionCommand = ReactiveCommand.Create<string>(RemovePriorityExtension);

            SaveMaxSizeCommand = ReactiveCommand.Create(SaveMaxSize);
            SaveServerConnectionCommand = ReactiveCommand.Create(SaveServerConnection);

            UpdateServerWarning();

            _serverStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _serverStatusTimer.Tick += (_, _) => UpdateServerWarning();
            _serverStatusTimer.Start();
        }

        // Changes the application language
        private void ChangeLanguage(int index)
        {
            string code = index == 1 ? "en" : "fr";
            SettingsManager.Instance.ChangeLanguage(code);
            SettingsManager.Instance.SaveSettings();
        }

        // Adds a new business software to the list
        private void AddBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(NewBusinessSoftware)) return;

            string name = NewBusinessSoftware.Trim();
            if (!BusinessSoftwareNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                BusinessSoftwareNames.Add(name);
                SettingsManager.Instance.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
                SettingsManager.Instance.SaveSettings();
            }
            NewBusinessSoftware = "";
        }

        // Removes a business software from the list
        private void RemoveBusinessSoftware(string name)
        {
            BusinessSoftwareNames.Remove(name);
            SettingsManager.Instance.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
            SettingsManager.Instance.SaveSettings();
        }

        private void SaveMaxSize()
        {
            if (long.TryParse(MaxParallelFileSizeKbText, out long value) && value > 0)
            {
                SettingsManager.Instance.MaxParallelFileSizeKb = value;
                SettingsManager.Instance.SaveSettings();

                // Displays confirmation then cache after 2 seconds
                MaxSizeConfirmationVisible = true;
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        MaxSizeConfirmationVisible = false);
                });
            }
        }

        // Adds a new encrypted extension to the list
        private void AddExtension()
        {
            if (string.IsNullOrWhiteSpace(NewExtension)) return;

            string ext = NewExtension.Trim();
            if (!ext.StartsWith(".")) ext = "." + ext;

            if (!EncryptedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                EncryptedExtensions.Add(ext);
                SyncExtensionsToSettings();
            }
            NewExtension = "";
        }

        private void RemoveExtension(string ext)
        {
            EncryptedExtensions.Remove(ext);
            SyncExtensionsToSettings();
        }

        // Méthodes Priority files
        private void AddPriorityExtension()
        {
            if (string.IsNullOrWhiteSpace(NewPriorityExtension)) return;

            string ext = NewPriorityExtension.Trim();
            if (!ext.StartsWith(".")) ext = "." + ext;

            if (!PriorityExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                PriorityExtensions.Add(ext);
                SyncPriorityExtensionsToSettings();
            }
            NewPriorityExtension = "";
        }

        private void RemovePriorityExtension(string ext)
        {
            PriorityExtensions.Remove(ext);
            SyncPriorityExtensionsToSettings();
        }

        private void SyncPriorityExtensionsToSettings()
        {
            SettingsManager.Instance.PriorityExtensions = PriorityExtensions.ToList();
            SettingsManager.Instance.SaveSettings();
        }

        private void SaveServerConnection()
        {
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(ServerIp))
            {
                SettingsManager.Instance.ServerIp = ServerIp.Trim();
                LoggerService.ServerIp = ServerIp.Trim();
                changed = true;
            }

            if (int.TryParse(ServerPort, out int port) && port > 0 && port <= 65535)
            {
                SettingsManager.Instance.ServerPort = port;
                LoggerService.ServerPort = port;
                changed = true;
            }

            if (changed)
            {
                SettingsManager.Instance.SaveSettings();
                LoggerService.ForceReconnect();

                ServerConnectionConfirmationVisible = true;
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        ServerConnectionConfirmationVisible = false);
                });
            }
        }

        /// <summary>
        /// Reads the ground truth from the background loop.
        /// Called periodically by the timer so the warning stays in sync with the dashboard.
        /// </summary>
        private void UpdateServerWarning()
        {
            var target = SettingsManager.Instance.LogTarget;
            if (target == LogTarget.Centralized || target == LogTarget.Both)
            {
                ServerOfflineWarningVisible = !LoggerService.IsServerConnected;
            }
            else
            {
                ServerOfflineWarningVisible = false;
            }
        }

        // Synchronizes the extensions collection with the settings manager
        private void SyncExtensionsToSettings()
        {
            SettingsManager.Instance.EncryptedExtensions = EncryptedExtensions.ToList();
            SettingsManager.Instance.SaveSettings();
        }


    }
}