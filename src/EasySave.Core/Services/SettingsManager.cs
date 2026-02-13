using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace EasySave.Core.Services
{
    public class SettingsManager
    {
        public string Language { get; set; } = "fr"; // "fr" ou "en"
        public bool LogFormat { get; set; } = true; // true = JSON, false = XML
        public string BusinessSoftwareName { get; set; } = "";
        public List<string> EncryptedExtensions { get; set; } = new List<string>();

        private static SettingsManager _instance;
        public static SettingsManager Instance => _instance ??= new SettingsManager();
        private readonly string _configFilePath;

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
            ChangeLanguage(Language);
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
