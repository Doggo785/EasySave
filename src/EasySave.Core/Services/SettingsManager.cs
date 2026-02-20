using EasySave.Core.Models;
using EasySave.Core.Properties; // for resource access
using System;
using System.Collections.Generic;
using System.ComponentModel;    // for INotifyPropertyChanged
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace EasySave.Core.Services
{
    public class SettingsManager : INotifyPropertyChanged
    {
        public string Language { get; set; } = "fr";
        public bool LogFormat { get; set; } = true;
        public string BusinessSoftwareName { get; set; } = "";
        public List<string> EncryptedExtensions { get; set; } = new List<string>();
        public int MaxConcurrentJobs { get; set; } = Environment.ProcessorCount;
        public long MaxParallelFileSizeKb { get; set; } = 1000; // Ajout partie 3

        private static SettingsManager _instance;
        public static SettingsManager Instance => _instance ??= new SettingsManager();
        private readonly string _configFilePath;

        public event PropertyChangedEventHandler PropertyChanged;

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
                        MaxConcurrentJobs = settings.MaxConcurrentJobs > 0 ? settings.MaxConcurrentJobs : Environment.ProcessorCount;
                        MaxParallelFileSizeKb = settings.MaxParallelFileSizeKb > 0 ? settings.MaxParallelFileSizeKb : 1000; // Ajout partie 3
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
                EncryptedExtensions = EncryptedExtensions,
                MaxConcurrentJobs = MaxConcurrentJobs,
                MaxParallelFileSizeKb = MaxParallelFileSizeKb 
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

                // Notify UI that the language changed
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
            MaxConcurrentJobs = Environment.ProcessorCount;
            MaxParallelFileSizeKb = 1000; // Ajout partie 3
        }

        private class SettingsModel
        {
            public string Language { get; set; } = "fr";
            public bool LogFormat { get; set; } = true;
            public string BusinessSoftwareName { get; set; } = "";
            public List<string> EncryptedExtensions { get; set; } = new List<string>();
            public int MaxConcurrentJobs { get; set; } = 0;
            public long MaxParallelFileSizeKb { get; set; } = 1000; // Ajout partie 3
        }
    }
}