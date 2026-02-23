using EasySave.Core.Models;
using EasySave.Core.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace EasySave.Core.Services
{
    // Manages application settings using a Singleton pattern
    public class SettingsManager : INotifyPropertyChanged
    {
        // General settings
        public string Language { get; set; } = "fr";
        public bool LogFormat { get; set; } = true;

        // Lists for restrictions and cryptography
        public List<string> BusinessSoftwareNames { get; set; } = new List<string>();
        public List<string> EncryptedExtensions { get; set; } = new List<string>();

        // Job execution settings
        public int MaxConcurrentJobs { get; set; } = Environment.ProcessorCount;

        // Settings for parallel file transfer (Part 3)
        public long MaxParallelFileSizeKb { get; set; } = 1000;

        private static SettingsManager _instance;

        // Singleton instance of SettingsManager
        public static SettingsManager Instance => _instance ??= new SettingsManager();

        private readonly string _configFilePath;

        public event PropertyChangedEventHandler PropertyChanged;

        // Retrieves localized resource strings
        public string this[string key]
        {
            get { return Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]"; }
        }

        // Initializes the settings directory and file path
        private SettingsManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProSoft", "EasySave", "UserConfig");
            Directory.CreateDirectory(appDataPath);
            _configFilePath = Path.Combine(appDataPath, "config.json");
        }

        // Loads settings from the JSON configuration file
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
                        BusinessSoftwareNames = settings.BusinessSoftwareNames ?? new List<string>();
                        EncryptedExtensions = settings.EncryptedExtensions ?? new List<string>();
                        MaxConcurrentJobs = settings.MaxConcurrentJobs > 0 ? settings.MaxConcurrentJobs : Environment.ProcessorCount;
                        MaxParallelFileSizeKb = settings.MaxParallelFileSizeKb > 0 ? settings.MaxParallelFileSizeKb : 1000;
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

        // Saves current settings to the JSON configuration file
        public void SaveSettings()
        {
            var settings = new SettingsModel
            {
                Language = Language,
                LogFormat = LogFormat,
                BusinessSoftwareNames = BusinessSoftwareNames,
                EncryptedExtensions = EncryptedExtensions,
                MaxConcurrentJobs = MaxConcurrentJobs,
                MaxParallelFileSizeKb = MaxParallelFileSizeKb
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_configFilePath, json);
        }

        // Updates the application's culture and language
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

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
            catch (CultureNotFoundException)
            {
            }
        }

        // Resets settings to their default values
        private void ResetSettings()
        {
            Language = "fr";
            LogFormat = true;
            BusinessSoftwareNames = new List<string>();
            EncryptedExtensions = new List<string>();
            MaxConcurrentJobs = Environment.ProcessorCount;
            MaxParallelFileSizeKb = 1000;
        }

        // Internal model for JSON serialization
        private class SettingsModel
        {
            public string Language { get; set; } = "fr";
            public bool LogFormat { get; set; } = true;
            public List<string> BusinessSoftwareNames { get; set; } = new List<string>();
            public List<string> EncryptedExtensions { get; set; } = new List<string>();
            public int MaxConcurrentJobs { get; set; } = 0;
            public long MaxParallelFileSizeKb { get; set; } = 1000;
        }
    }
}