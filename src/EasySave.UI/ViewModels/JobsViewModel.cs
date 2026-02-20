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
    // Represents the current execution state of a job
    public enum JobState { Stopped, Running, Paused }

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

        // Job execution state driving the UI button
        private JobState _state = JobState.Stopped;
        public JobState State
        {
            get => _state;
            set
            {
                this.RaiseAndSetIfChanged(ref _state, value);
                this.RaisePropertyChanged(nameof(ActionIcon));
                this.RaisePropertyChanged(nameof(ActionColor));
            }
        }

        // Dynamic icon based on current state
        public string ActionIcon => State switch
        {
            JobState.Stopped => "▶",
            JobState.Running => "⏸",
            JobState.Paused => "▶",
            _ => "▶"
        };

        // Dynamic color based on current state
        public string ActionColor => State switch
        {
            JobState.Stopped => "#2E8B57", // Green (Ready to play)
            JobState.Running => "#DAA520", // Yellow (Ready to pause)
            JobState.Paused => "#4682B4",  // Blue (Ready to resume)
            _ => "#2E8B57"
        };

        public JobItemViewModel(SaveJob job)
        {
            Job = job;
            Job.ProgressChanged += (sender, value) =>
            {
                Dispatcher.UIThread.Post(() => Progress = value);
            };
        }
    }

    public class JobsViewModel : ReactiveObject
    {
        private readonly SaveManager _saveManager;

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

        private bool _isExecutingAll = false;
        public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }

        // UI Commands
        public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
        public ReactiveCommand<int, Unit> TogglePlayPauseCommand { get; } // Single unified command
        public ReactiveCommand<int, Unit> StopJobCommand { get; }
        public ReactiveCommand<int, Unit> DeleteJobCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteAllCommand { get; }

        public JobsViewModel()
        {
            _saveManager = new SaveManager();
            Jobs = new ObservableCollection<JobItemViewModel>();
            RefreshList();

            CreateJobCommand = ReactiveCommand.Create(CreateJob);
            TogglePlayPauseCommand = ReactiveCommand.CreateFromTask<int>(TogglePlayPauseAsync);
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

        // Handles Play, Pause, and Resume logic based on current state
        private async Task TogglePlayPauseAsync(int id)
        {
            var jobVm = Jobs.FirstOrDefault(j => j.Id == id);
            if (jobVm == null) return;

            if (jobVm.State == JobState.Stopped)
            {
                jobVm.State = JobState.Running;
                jobVm.Progress = 0;
                StatusMessage = "";

                string? password = await RequestPasswordIfNeeded();

                // Run in background and reset state when finished
                _ = Task.Run(async () =>
                {
                    await _saveManager.ExecuteJob(id, _ => password, DisplayMessage);
                    Dispatcher.UIThread.Post(() => jobVm.State = JobState.Stopped);
                });
            }
            else if (jobVm.State == JobState.Running)
            {
                jobVm.State = JobState.Paused;
                _saveManager.PauseJob(id);
            }
            else if (jobVm.State == JobState.Paused)
            {
                jobVm.State = JobState.Running;
                _saveManager.ResumeJob(id);
            }
        }

        private void StopJob(int id)
        {
            _saveManager.StopJob(id);
            var jobVm = Jobs.FirstOrDefault(j => j.Id == id);
            if (jobVm != null)
            {
                jobVm.State = JobState.Stopped;
                jobVm.Progress = 0;
            }
        }

        private async Task ExecuteAllAsync()
        {
            
            if (_isExecutingAll) return;

            try
            {
                _isExecutingAll = true;
                StatusMessage = "";

                string? password = await RequestPasswordIfNeeded();


                foreach (var job in Jobs)
                {
                    job.State = JobState.Running;
                    job.Progress = 0;
                }


                await _saveManager.ExecuteAllJobs(_ => password, DisplayMessage);
            }
            finally
            {
                _isExecutingAll = false;

                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var job in Jobs) job.State = JobState.Stopped;
                });
            }
        }

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