using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using ReactiveUI;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace EasySave.UI.ViewModels
{
    public class DecryptViewModel : ReactiveObject
    {
        private string _sourcePath = "";
        public string SourcePath { get => _sourcePath; set => this.RaiseAndSetIfChanged(ref _sourcePath, value); }

        private string _destPath = "";
        public string DestPath { get => _destPath; set => this.RaiseAndSetIfChanged(ref _destPath, value); }

        private string _password = "";
        public string Password { get => _password; set => this.RaiseAndSetIfChanged(ref _password, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                this.RaiseAndSetIfChanged(ref _isSuccess, value);
                this.RaisePropertyChanged(nameof(StatusColor));
            }
        }

        public string StatusColor => IsSuccess ? "#4CAF50" : "#ff5252";

        private bool _isDecrypting;
        public bool IsDecrypting { get => _isDecrypting; set => this.RaiseAndSetIfChanged(ref _isDecrypting, value); }

        public ReactiveCommand<Unit, Unit> BrowseSourceCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDestCommand { get; }
        public ReactiveCommand<Unit, Unit> DecryptCommand { get; }

        public DecryptViewModel()
        {
            BrowseSourceCommand = ReactiveCommand.CreateFromTask(BrowseSourceAsync);
            BrowseDestCommand = ReactiveCommand.CreateFromTask(BrowseDestAsync);
            DecryptCommand = ReactiveCommand.CreateFromTask(DecryptAsync);
        }

        private async Task BrowseSourceAsync()
        {
            var storageProvider = GetMainWindow()?.StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Resources.Decrypt_Arg_Source,
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                SourcePath = files[0].Path.LocalPath;
            }
        }

        private async Task BrowseDestAsync()
        {
            var storageProvider = GetMainWindow()?.StorageProvider;
            if (storageProvider == null) return;

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Resources.Decrypt_Arg_Dest,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                DestPath = folders[0].Path.LocalPath;
            }
        }

        private async Task DecryptAsync()
        {
            StatusMessage = "";
            IsSuccess = false;

            if (string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = Resources.Erreur_Creation_Blank;
                return;
            }

            string effectiveDest = DestPath;
            if (string.IsNullOrWhiteSpace(effectiveDest))
                effectiveDest = SourcePath;
            else if (Directory.Exists(effectiveDest))
                effectiveDest = Path.Combine(effectiveDest, Path.GetFileName(SourcePath));

            IsDecrypting = true;

            int result = await Task.Run(() => CryptoService.DecryptFile(SourcePath, effectiveDest, Password));

            IsDecrypting = false;

            if (result >= 0)
            {
                IsSuccess = true;
                StatusMessage = $"{Resources.Decrypt_Success} {result} ms";
            }
            else if (result == -2)
            {
                StatusMessage = Resources.Decrypt_FileNotFound;
            }
            else
            {
                StatusMessage = Resources.Decrypt_Error;
            }
        }

        private static Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }
    }
}
