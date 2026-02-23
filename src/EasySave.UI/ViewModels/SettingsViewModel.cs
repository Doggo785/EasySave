using ReactiveUI;
using EasySave.Core.Services;
using EasyLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace EasySave.UI.ViewModels
{
    public class SettingsViewModel : ReactiveObject
    {
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
                CheckServerIfNeeded((LogTarget)value);
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

        private bool _serverIpConfirmationVisible;
        public bool ServerIpConfirmationVisible
        {
            get => _serverIpConfirmationVisible;
            set => this.RaiseAndSetIfChanged(ref _serverIpConfirmationVisible, value);
        }

        public ReactiveCommand<Unit, Unit> SaveServerIpCommand { get; }

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

            // Initialize observable collections
            BusinessSoftwareNames = new ObservableCollection<string>(settings.BusinessSoftwareNames);
            _maxParallelFileSizeKbText = settings.MaxParallelFileSizeKb.ToString();
            EncryptedExtensions = new ObservableCollection<string>(settings.EncryptedExtensions);

            // Initialize commands
            AddBusinessSoftwareCommand = ReactiveCommand.Create(AddBusinessSoftware);
            RemoveBusinessSoftwareCommand = ReactiveCommand.Create<string>(RemoveBusinessSoftware);

            AddExtensionCommand = ReactiveCommand.Create(AddExtension);
            RemoveExtensionCommand = ReactiveCommand.Create<string>(RemoveExtension);
            SaveMaxSizeCommand = ReactiveCommand.Create(SaveMaxSize);
            SaveServerIpCommand = ReactiveCommand.Create(SaveServerIp);

            CheckServerIfNeeded(settings.LogTarget);
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
            if (!ext.StartsWith("."))
                ext = "." + ext;

            if (!EncryptedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                EncryptedExtensions.Add(ext);
                SyncExtensionsToSettings();
            }
            NewExtension = "";
        }

        // Removes an encrypted extension from the list
        private void RemoveExtension(string ext)
        {
            EncryptedExtensions.Remove(ext);
            SyncExtensionsToSettings();
        }

        private void SaveServerIp()
        {
            if (!string.IsNullOrWhiteSpace(ServerIp))
            {
                SettingsManager.Instance.ServerIp = ServerIp.Trim();
                LoggerService.ServerIp = ServerIp.Trim();
                SettingsManager.Instance.SaveSettings();

                ServerIpConfirmationVisible = true;
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        ServerIpConfirmationVisible = false);
                });

                CheckServerIfNeeded(SettingsManager.Instance.LogTarget);
            }
        }

        private void CheckServerIfNeeded(LogTarget target)
        {
            if (target == LogTarget.Centralized || target == LogTarget.Both)
            {
                // Instant feedback from the background loop state
                ServerOfflineWarningVisible = !LoggerService.IsServerConnected;

                // Then verify async with a short timeout to confirm
                System.Threading.Tasks.Task.Run(async () =>
                {
                    bool reachable = await LoggerService.CheckServerConnectionAsync();
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        ServerOfflineWarningVisible = !reachable);
                });
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