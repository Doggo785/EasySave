using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private int _progress;
        public int Progress { get => _progress; set => this.RaiseAndSetIfChanged(ref _progress, value); }

        private bool _showProgress;
        public bool ShowProgress { get => _showProgress; set => this.RaiseAndSetIfChanged(ref _showProgress, value); }

        public ReactiveCommand<Unit, Unit> BrowseSourceFileCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseSourceFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDestCommand { get; }
        public ReactiveCommand<Unit, Unit> DecryptCommand { get; }

        public DecryptViewModel()
        {
            BrowseSourceFileCommand = ReactiveCommand.CreateFromTask(BrowseSourceFileAsync);
            BrowseSourceFolderCommand = ReactiveCommand.CreateFromTask(BrowseSourceFolderAsync);
            BrowseDestCommand = ReactiveCommand.CreateFromTask(BrowseDestAsync);
            DecryptCommand = ReactiveCommand.CreateFromTask(DecryptAsync);
        }

        private async Task BrowseSourceFileAsync()
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

        private async Task BrowseSourceFolderAsync()
        {
            var storageProvider = GetMainWindow()?.StorageProvider;
            if (storageProvider == null) return;

            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Resources.Decrypt_Arg_Source,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                SourcePath = folders[0].Path.LocalPath;
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

        /// <summary>
        /// Triggers decryption based on the source type.
        /// </summary>
        private async Task DecryptAsync()
        {
            StatusMessage = "";
            IsSuccess = false;
            Progress = 0;
            ShowProgress = false;

            if (string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = Resources.Error_Creation_Blank;
                return;
            }

            IsDecrypting = true;

            if (Directory.Exists(SourcePath))
            {
                await DecryptFolderAsync();
            }
            else if (File.Exists(SourcePath))
            {
                await DecryptSingleFileAsync();
            }
            else
            {
                StatusMessage = Resources.Decrypt_FileNotFound;
            }

            IsDecrypting = false;
        }

        /// <summary>
        /// Decrypts a single file via the UI thread.
        /// </summary>
        private async Task DecryptSingleFileAsync()
        {
            string effectiveDest = DestPath;
            if (string.IsNullOrWhiteSpace(effectiveDest))
                effectiveDest = SourcePath;
            else if (Directory.Exists(effectiveDest))
                effectiveDest = Path.Combine(effectiveDest, Path.GetFileName(SourcePath));

            int result = await Task.Run(() => CryptoService.DecryptFile(SourcePath, effectiveDest, Password));

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

        /// <summary>
        /// Decrypts an entire folder and updates the UI.
        /// </summary>
        private async Task DecryptFolderAsync()
        {
            var extensions = SettingsManager.Instance.EncryptedExtensions;
            var sourceDir = new DirectoryInfo(SourcePath);
            var allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            List<FileInfo> filesToDecrypt;
            if (extensions != null && extensions.Count > 0)
            {
                filesToDecrypt = allFiles
                    .Where(f => extensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                filesToDecrypt = allFiles.ToList();
            }

            if (filesToDecrypt.Count == 0)
            {
                StatusMessage = Resources.Decrypt_FileNotFound;
                return;
            }

            ShowProgress = true;
            int total = filesToDecrypt.Count;
            int processed = 0;
            int errors = 0;
            long totalTimeMs = 0;

            await Task.Run(() =>
            {
                foreach (var file in filesToDecrypt)
                {
                    string relativePath = Path.GetRelativePath(SourcePath, file.FullName);

                    string destFilePath;
                    if (!string.IsNullOrWhiteSpace(DestPath))
                    {
                        destFilePath = Path.Combine(DestPath, relativePath);
                        string? destDir = Path.GetDirectoryName(destFilePath);
                        if (!string.IsNullOrWhiteSpace(destDir))
                            Directory.CreateDirectory(destDir);
                    }
                    else
                    {
                        destFilePath = file.FullName;
                    }

                    int result = CryptoService.DecryptFile(file.FullName, destFilePath, Password);

                    if (result >= 0)
                        totalTimeMs += result;
                    else
                        errors++;

                    processed++;
                    int pct = (int)((double)processed / total * 100);
                    Dispatcher.UIThread.Post(() => Progress = pct);
                }
            });

            int success = total - errors;
            IsSuccess = errors == 0;

            if (errors == 0)
            {
                StatusMessage = $"{Resources.Decrypt_Success} {totalTimeMs} ms — {success}/{total} {Resources.File}";
            }
            else
            {
                StatusMessage = $"{success}/{total} {Resources.File} OK — {errors} {Resources.Error_Job}";
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