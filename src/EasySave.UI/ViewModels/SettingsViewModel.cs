using ReactiveUI;
using EasySave.Core.Services;

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

        public SettingsViewModel()
        {
            string currentLang = SettingsManager.Instance.Language;
            _selectedLanguageIndex = currentLang == "en" ? 1 : 0;
        }

        private void ChangeLanguage(int index)
        {
            string code = index == 1 ? "en" : "fr";
            // SettingsManager handles saving and UI update
            SettingsManager.Instance.ChangeLanguage(code);
            SettingsManager.Instance.SaveSettings();
        }
    }
}