using EasySave.Core.Models;
using ReactiveUI;

namespace EasySave.UI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public HomeViewModel HomeVM { get; }
        public JobsViewModel JobsVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainWindowViewModel()
        {
            var saveManager = new SaveManager();
            HomeVM = new HomeViewModel(saveManager);
            JobsVM = new JobsViewModel(saveManager);
            SettingsVM = new SettingsViewModel();
        }
    }
}