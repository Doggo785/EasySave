using ReactiveUI;

namespace EasySave.UI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        // Il contient les autres ViewModels (Composition)
        public JobsViewModel JobsVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public MainWindowViewModel()
        {
            JobsVM = new JobsViewModel();
            SettingsVM = new SettingsViewModel();
        }
    }
}