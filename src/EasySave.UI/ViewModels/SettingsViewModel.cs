using ReactiveUI;
using EasySave.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace EasySave.UI.ViewModels
{
    public class SettingsViewModel : ReactiveObject
    {
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

        private int _selectedLogFormatIndex;
        public int SelectedLogFormatIndex
        {
            get => _selectedLogFormatIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogFormatIndex, value);
                SettingsManager.Instance.LogFormat = (value == 0);
                SettingsManager.Instance.SaveSettings();
            }
        }

        private string _businessSoftwareName = "";
        public string BusinessSoftwareName
        {
            get => _businessSoftwareName;
            set => this.RaiseAndSetIfChanged(ref _businessSoftwareName, value);
        }

        private string _newExtension = "";
        public string NewExtension
        {
            get => _newExtension;
            set => this.RaiseAndSetIfChanged(ref _newExtension, value);
        }

        public ObservableCollection<string> EncryptedExtensions { get; }

        public ReactiveCommand<Unit, Unit> SaveBusinessSoftwareCommand { get; }
        public ReactiveCommand<Unit, Unit> AddExtensionCommand { get; }
        public ReactiveCommand<string, Unit> RemoveExtensionCommand { get; }

        public SettingsViewModel()
        {
            var settings = SettingsManager.Instance;

            _selectedLanguageIndex = settings.Language == "en" ? 1 : 0;
            _selectedLogFormatIndex = settings.LogFormat ? 0 : 1;
            _businessSoftwareName = settings.BusinessSoftwareName;
            EncryptedExtensions = new ObservableCollection<string>(settings.EncryptedExtensions);

            SaveBusinessSoftwareCommand = ReactiveCommand.Create(SaveBusinessSoftware);
            AddExtensionCommand = ReactiveCommand.Create(AddExtension);
            RemoveExtensionCommand = ReactiveCommand.Create<string>(RemoveExtension);
        }

        private void ChangeLanguage(int index)
        {
            string code = index == 1 ? "en" : "fr";
            SettingsManager.Instance.ChangeLanguage(code);
            SettingsManager.Instance.SaveSettings();
        }

        private void SaveBusinessSoftware()
        {
            SettingsManager.Instance.BusinessSoftwareName = BusinessSoftwareName;
            SettingsManager.Instance.SaveSettings();
        }

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

        private void RemoveExtension(string ext)
        {
            EncryptedExtensions.Remove(ext);
            SyncExtensionsToSettings();
        }

        private void SyncExtensionsToSettings()
        {
            SettingsManager.Instance.EncryptedExtensions = EncryptedExtensions.ToList();
            SettingsManager.Instance.SaveSettings();
        }
    }
}