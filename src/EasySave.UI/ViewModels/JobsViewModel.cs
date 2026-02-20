using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.UI.Views;
using System.Linq;

namespace EasySave.UI.ViewModels
{
    // View model wrapper linking a SaveJob to its UI representation and progress
    public class JobItemViewModel : ReactiveObject
    {
        public SaveJob Job { get; }
        public int Id => Job.Id;
        public string Name => Job.Name;
        public string TargetDirectory => Job.TargetDirectory;
        public bool SaveType => Job.SaveType;

        private int _progress;
        public int Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public JobItemViewModel(SaveJob job)
        {
            Job = job;

            // Subscribe to backend progress updates
            Job.ProgressChanged += (sender, value) =>
            {
                // Ensure UI updates are dispatched to the main thread
                Dispatcher.UIThread.Post(() => Progress = value);
            };
        }
    }

    public class JobsViewModel : ReactiveObject
    {
        private readonly SaveManager _saveManager;

        // Observable collection of wrapped jobs for UI binding
        public ObservableCollection<JobItemViewModel> Jobs { get; set; }

        private string _newName = "";
        public string NewName { get => _newName; set => this.RaiseAndSetIfChanged(ref _newName, value); }

        private string _newSource = "";
        public string NewSource { get => _newSource; set => this.RaiseAndSetIfChanged(ref _newSource, value); }

        private string _newDest = "";
        public string NewDest { get => _newDest; set => this.RaiseAndSetIfChanged(ref _newDest, value); }

        private int _selectedTypeIndex = 0;
        public int SelectedTypeIndex { get => _selectedTypeIndex; set => this.RaiseAndSetIfChanged(ref _selectedTypeIndex, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }

        // UI Commands
        public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
        public ReactiveCommand<int, Unit> ExecuteJobCommand { get; }
        public ReactiveCommand<int, Unit> PauseJobCommand { get; }
        public ReactiveCommand<int, Unit> ResumeJobCommand { get; }
        public ReactiveCommand<int, Unit> StopJobCommand { get; }
        public ReactiveCommand<int, Unit> DeleteJobCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteAllCommand { get; }

        public JobsViewModel()
        {
            _saveManager = new SaveManager();
            Jobs = new ObservableCollection<JobItemViewModel>();
            RefreshList();

            CreateJobCommand = ReactiveCommand.Create(CreateJob);
            ExecuteJobCommand = ReactiveCommand.CreateFromTask<int>(ExecuteJobAsync);
            PauseJobCommand = ReactiveCommand.Create<int>(PauseJob);
            ResumeJobCommand = ReactiveCommand.Create<int>(ResumeJob);
            StopJobCommand = ReactiveCommand.Create<int>(StopJob);
            DeleteJobCommand = ReactiveCommand.Create<int>(DeleteJob);
            ExecuteAllCommand = ReactiveCommand.CreateFromTask(ExecuteAllAsync);
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

        private async Task ExecuteJobAsync(int id)
        {
            StatusMessage = "";
            string? password = await RequestPasswordIfNeeded();

            // Run the job asynchronously to keep the UI responsive
            _ = _saveManager.ExecuteJob(id, _ => password, DisplayMessage);
        }

        private async Task ExecuteAllAsync()
        {
            StatusMessage = "";
            string? password = await RequestPasswordIfNeeded();

            // Run all jobs asynchronously
            _ = _saveManager.ExecuteAllJobs(_ => password, DisplayMessage);
        }

        // --- Execution control methods ---
        private void PauseJob(int id) => _saveManager.PauseJob(id);
        private void ResumeJob(int id) => _saveManager.ResumeJob(id);
        private void StopJob(int id) => _saveManager.StopJob(id);

        private void DeleteJob(int id)
        {
            _saveManager.DeleteJob(id);
            RefreshList();
        }

        private async Task<string?> RequestPasswordIfNeeded()
        {
            var extensions = SettingsManager.Instance.EncryptedExtensions;
            if (extensions == null || extensions.Count == 0)
                return null;

            var owner = GetMainWindow();
            if (owner == null) return null;

            var dialog = new PasswordDialog(SettingsManager.Instance["PasswordRequest"]);
            var result = await dialog.ShowDialog<string?>(owner);
            return result;
        }

        private static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }

        private void DisplayMessage(string message)
        {
            // Safely update the status message from any background thread
            Dispatcher.UIThread.Post(() => StatusMessage = message);
        }

        private void RefreshList()
        {
            Jobs.Clear();
            foreach (var job in _saveManager.GetJobs())
            {
                Jobs.Add(new JobItemViewModel(job));
            }
        }
    }
}