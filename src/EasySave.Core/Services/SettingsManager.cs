using EasySave.Core.Models;
using EasySave.Core.Properties; // <-- Ajout pour lire les ressources
using System;
using System.Collections.Generic;
using System.ComponentModel;    // <-- Ajout pour INotifyPropertyChanged
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace EasySave.Core.Services
{
    // On ajoute INotifyPropertyChanged
    public class SettingsManager : INotifyPropertyChanged
    {
        public string Language { get; set; } = "fr";
        public bool LogFormat { get; set; } = true;
        public string BusinessSoftwareName { get; set; } = "";
        public List<string> EncryptedExtensions { get; set; } = new List<string>();

        private static SettingsManager _instance;
        public static SettingsManager Instance => _instance ??= new SettingsManager();
        private readonly string _configFilePath;

        // Événement requis par INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // L'INDEXEUR MAGIQUE : C'est lui qui remplace TranslationSource !
        public string this[string key]
        {
            get { return Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]"; }
        }

        private SettingsManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProSoft", "EasySave", "UserConfig");
            Directory.CreateDirectory(appDataPath);
            _configFilePath = Path.Combine(appDataPath, "config.json");
        }

        public void LoadSettings()
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    var settings = JsonSerializer.Deserialize<SettingsModel>(json);

                    if (settings != null)
                    {
                        Language = settings.Language;
                        LogFormat = settings.LogFormat;
                        BusinessSoftwareName = settings.BusinessSoftwareName;
                        EncryptedExtensions = settings.EncryptedExtensions ?? new List<string>();
                    }
                }
                catch
                {
                    ResetSettings();
                }
            }
            else
            {
                ResetSettings();
                SaveSettings();
            }
            ChangeLanguage(Language);
        }

        public void SaveSettings()
        {
            var settings = new SettingsModel
            {
                Language = Language,
                LogFormat = LogFormat,
                BusinessSoftwareName = BusinessSoftwareName,
                EncryptedExtensions = EncryptedExtensions
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_configFilePath, json);
        }

        public void ChangeLanguage(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                Language = languageCode;

                // On prévient Avalonia que la langue a changé pour qu'il actualise les textes !
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
            catch (CultureNotFoundException)
            {
            }
        }

        private void ResetSettings()
        {
            Language = "fr";
            LogFormat = true;
            BusinessSoftwareName = "";
            EncryptedExtensions = new List<string>();
        }

        private class SettingsModel
        {
            public string Language { get; set; } = "fr";
            public bool LogFormat { get; set; } = true;
            public string BusinessSoftwareName { get; set; } = "";
            public List<string> EncryptedExtensions { get; set; } = new List<string>();
        }
    }
}