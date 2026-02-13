using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.UI.Views;
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

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }

        public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
        public ReactiveCommand<int, Unit> ExecuteJobCommand { get; }
        public ReactiveCommand<int, Unit> DeleteJobCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteAllCommand { get; }

        public JobsViewModel()
        {
            _saveManager = new SaveManager();
            Jobs = new ObservableCollection<SaveJob>(_saveManager.GetJobs());

            CreateJobCommand = ReactiveCommand.Create(CreateJob);
            ExecuteJobCommand = ReactiveCommand.CreateFromTask<int>(ExecuteJobAsync);
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
            _saveManager.ExecuteJob(id, _ => password, DisplayMessage);
            RefreshList();
        }

        private async Task ExecuteAllAsync()
        {
            StatusMessage = "";
            string? password = await RequestPasswordIfNeeded();
            _saveManager.ExecuteAllJobs(_ => password, DisplayMessage);
            RefreshList();
        }

        private void DeleteJob(int id) { _saveManager.DeleteJob(id); RefreshList(); }

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
            StatusMessage = message;
        }

        private void RefreshList()
        {
            Jobs.Clear();
            foreach (var job in _saveManager.GetJobs()) Jobs.Add(job);
        }
    }
}