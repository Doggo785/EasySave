using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using EasySave.Core.Models;
using System.Linq;

namespace EasySave.UI.ViewModels
{
    public class JobsViewModel : ReactiveObject
    {
        private readonly SaveManager _saveManager;
        public ObservableCollection<SaveJob> Jobs { get; set; }

        private string _newName = "";
        public string NewName { get => _newName; set => this.RaiseAndSetIfChanged(ref _newName, value); }

        private string _newSource = "";
        public string NewSource { get => _newSource; set => this.RaiseAndSetIfChanged(ref _newSource, value); }

        private string _newDest = "";
        public string NewDest { get => _newDest; set => this.RaiseAndSetIfChanged(ref _newDest, value); }

        private int _selectedTypeIndex = 0;
        public int SelectedTypeIndex { get => _selectedTypeIndex; set => this.RaiseAndSetIfChanged(ref _selectedTypeIndex, value); }

        public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
        public ReactiveCommand<int, Unit> ExecuteJobCommand { get; }
        public ReactiveCommand<int, Unit> DeleteJobCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteAllCommand { get; }

        public JobsViewModel()
        {
            _saveManager = new SaveManager();
            Jobs = new ObservableCollection<SaveJob>(_saveManager.GetJobs());

            CreateJobCommand = ReactiveCommand.Create(CreateJob);
            ExecuteJobCommand = ReactiveCommand.Create<int>(ExecuteJob);
            DeleteJobCommand = ReactiveCommand.Create<int>(DeleteJob);
            ExecuteAllCommand = ReactiveCommand.Create(ExecuteAll);
        }

        private void CreateJob()
        {
            bool isFull = (SelectedTypeIndex == 0);
            try
            {
                _saveManager.CreateJob(NewName, NewSource, NewDest, isFull);
                RefreshList();
                NewName = ""; NewSource = ""; NewDest = "";
            }
            catch { }
        }

        private void ExecuteJob(int id) => _saveManager.ExecuteJob(id);
        private void ExecuteAll() => _saveManager.ExecuteAllJobs();
        private void DeleteJob(int id) { _saveManager.DeleteJob(id); RefreshList(); }

        private void RefreshList()
        {
            Jobs.Clear();
            foreach (var job in _saveManager.GetJobs()) Jobs.Add(job);
        }
    }
}