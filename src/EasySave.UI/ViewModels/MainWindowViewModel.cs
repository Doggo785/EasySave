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
            HomeVM = new HomeViewModel();
            JobsVM = new JobsViewModel();
            SettingsVM = new SettingsViewModel();
        }
    }
}