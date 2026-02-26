using EasySave.Core.Models;
using EasyLog;
using EasySave.Core.Properties;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace EasySave.Core.Services
{
    public class SettingsManager : INotifyPropertyChanged
    {
        public string Language { get; set; } = "fr";
        public bool LogFormat { get; set; } = true;
        public bool IsDarkMode { get; set; } = true;
        public LogTarget LogTarget { get; set; } = LogTarget.Both;
        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 25549;

        public List<string> BusinessSoftwareNames { get; set; } = new List<string>();
        public List<string> EncryptedExtensions { get; set; } = new List<string>();
        public List<string> PriorityExtensions { get; set; } = new List<string>();

        public int MaxConcurrentJobs { get; set; } = Environment.ProcessorCount;
        public long MaxParallelFileSizeKb { get; set; } = 1000;

        private static SettingsManager? _instance;

        public static SettingsManager Instance => _instance ??= new SettingsManager();

        private readonly string _configFilePath;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Récupère une chaîne de texte traduite.
        /// </summary>
        /// <param name="key">Clé de la ressource ciblée.</param>
        /// <returns>Texte traduit, ou la clé par défaut.</returns>
        public string this[string key]
        {
            get { return Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]"; }
        }

        private SettingsManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProSoft", "EasySave", "UserConfig");
            Directory.CreateDirectory(appDataPath);
            _configFilePath = Path.Combine(appDataPath, "config.json");
        }

        /// <summary>
        /// Charge et applique les paramètres depuis le fichier JSON.
        /// </summary>
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
                        IsDarkMode = settings.IsDarkMode;
                        BusinessSoftwareNames = settings.BusinessSoftwareNames ?? new List<string>();
                        EncryptedExtensions = settings.EncryptedExtensions ?? new List<string>();
                        PriorityExtensions = settings.PriorityExtensions ?? new List<string>();
                        MaxConcurrentJobs = settings.MaxConcurrentJobs > 0 ? settings.MaxConcurrentJobs : Environment.ProcessorCount;
                        MaxParallelFileSizeKb = settings.MaxParallelFileSizeKb > 0 ? settings.MaxParallelFileSizeKb : 1000;
                        LogTarget = (LogTarget)settings.LogTarget;
                        ServerIp = !string.IsNullOrWhiteSpace(settings.ServerIp) ? settings.ServerIp : "127.0.0.1";
                        ServerPort = settings.ServerPort > 0 ? settings.ServerPort : 25549;
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
            ChangeTheme(IsDarkMode);
            LoggerService.CurrentLogTarget = LogTarget;
            LoggerService.ServerIp = ServerIp;
            LoggerService.ServerPort = ServerPort;
        }

        /// <summary>
        /// Sauvegarde la configuration actuelle dans l'AppData.
        /// </summary>
        public void SaveSettings()
        {
            var settings = new SettingsModel
            {
                Language = Language,
                LogFormat = LogFormat,
                IsDarkMode = IsDarkMode,
                EncryptedExtensions = EncryptedExtensions,
                PriorityExtensions = PriorityExtensions,
                MaxConcurrentJobs = MaxConcurrentJobs,
                MaxParallelFileSizeKb = MaxParallelFileSizeKb,
                BusinessSoftwareNames = BusinessSoftwareNames,
                LogTarget = (int)LogTarget,
                ServerIp = ServerIp,
                ServerPort = ServerPort
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_configFilePath, json);
        }

        /// <summary>
        /// Modifie la culture globale des threads de l'application.
        /// </summary>
        /// <param name="languageCode">Code de langue (ex: "fr").</param>
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
        private void ResetSettings()
        {
            Language = "fr";
            LogFormat = true;
            IsDarkMode = true;
            BusinessSoftwareNames = new List<string>();
            EncryptedExtensions = new List<string>();
            PriorityExtensions = new List<string>();
            MaxConcurrentJobs = Environment.ProcessorCount;
            MaxParallelFileSizeKb = 1000;
            LogTarget = LogTarget.Both;
            ServerIp = "127.0.0.1";
            ServerPort = 25549;
        }

        public void ChangeTheme(bool isDark)
        {
            IsDarkMode = isDark;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkMode)));
        }

        private class SettingsModel
        {
            public string Language { get; set; } = "fr";
            public bool LogFormat { get; set; } = true;
            public bool IsDarkMode { get; set; } = true;
            public List<string> BusinessSoftwareNames { get; set; } = new List<string>();
            public List<string> EncryptedExtensions { get; set; } = new List<string>();
            public List<string> PriorityExtensions { get; set; } = new List<string>();
            public int MaxConcurrentJobs { get; set; } = 0;
            public long MaxParallelFileSizeKb { get; set; } = 1000;
            public int LogTarget { get; set; } = 2;
            public string ServerIp { get; set; } = "127.0.0.1";
            public int ServerPort { get; set; } = 25549;
        }
    }
}