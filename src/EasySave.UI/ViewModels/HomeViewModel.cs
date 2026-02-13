using ReactiveUI;
using EasySave.Core.Models;
using System.Linq;

namespace EasySave.UI.ViewModels
{
    /// <summary>
    /// Home/dashboard view model.
    /// </summary>
    public class HomeViewModel : ReactiveObject
    {
        // Access to save jobs.
        private readonly SaveManager _saveManager;

        private int _activeJobsCount;

        /// <summary>Number of configured save jobs.</summary>
        public int ActiveJobsCount
        {
            get => _activeJobsCount;
            set => this.RaiseAndSetIfChanged(ref _activeJobsCount, value);
        }

        // Recent history will be simulated here in V3.

        /// <summary>Initialize and load dashboard values.</summary>
        public HomeViewModel()
        {
            _saveManager = new SaveManager();
            UpdateDashboard();
        }

        /// <summary>Update dashboard values.</summary>
        public void UpdateDashboard()
        {
            // Update the active jobs count.
            ActiveJobsCount = _saveManager.GetJobs().Count;
        }
    }
}