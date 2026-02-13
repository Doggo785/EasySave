using ReactiveUI;
using EasySave.Core.Models;
using System.Linq;

namespace EasySave.UI.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        private readonly SaveManager _saveManager;

        private int _activeJobsCount;
        public int ActiveJobsCount
        {
            get => _activeJobsCount;
            set => this.RaiseAndSetIfChanged(ref _activeJobsCount, value);
        }

        // Pour la V3, on simulera l'historique récent ici, 
        // tu pourras plus tard le lier à la lecture de ton fichier state.json (EasyLog)

        public HomeViewModel()
        {
            _saveManager = new SaveManager();
            UpdateDashboard();
        }

        public void UpdateDashboard()
        {
            // Récupère le vrai nombre de jobs configurés
            ActiveJobsCount = _saveManager.GetJobs().Count;
        }
    }
}