using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using static System.Reflection.Metadata.BlobBuilder;

namespace EasySave.Core.Services
{
    public class AppSettings
    {
        public string Language { get; set; } = "fr"; // "fr" ou "en"
        public string LogFormat { get; set; } = "JSON"; //JSON ou XML
        public string BusinessSoftware { get; set; } = ""; 
        public List<string> CryptoExtensions { get; set; } = new List<string>();
    }
    public class SettingsManager
    {   
        private static SettingsManager _instance;
        public static SettingsManager Instance => _instance ??= new SettingsManager();
        private readonly string _configFilePath;

        public AppSettings Config { get; set; }

        private SettingsManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            Directory.CreateDirectory(appDataPath);
            _configFilePath = Path.Combine(appDataPath, "config.json");
        }

        public void LoadSettings()
        {
            if(File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);

                    Config = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    Config = new AppSettings();
                }
            }
            else
            {
                Config = new AppSettings();
                SaveSettings();
            }
            ChangeLanguage(Config.Language);
        }

        public void SaveSettings()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Config, options);
            File.WriteAllText(_configFilePath, json);
            ChangeLanguage(Config.Language);
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

                Config.Language = languageCode;
            }
            catch (CultureNotFoundException)
            {

            }
        }
    }
}
