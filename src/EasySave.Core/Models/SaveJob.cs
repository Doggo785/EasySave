using EasyLog;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace EasySave.Core.Models
{
    public class SaveJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public bool SaveType { get; set; }

        private LoggerService _logger;

        // Thread control elements
        [JsonIgnore]
        public ManualResetEventSlim PauseEvent { get; } = new ManualResetEventSlim(true);
        public event EventHandler<int> ProgressChanged;

        public SaveJob(int id, string name, string source, string target, bool type)
        {
            Id = id;
            Name = name;
            SourceDirectory = source;
            TargetDirectory = target;
            SaveType = type;
            _logger = new LoggerService(SettingsManager.Instance.LogFormat);
        }

        public SaveJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            _logger = new LoggerService(SettingsManager.Instance.LogFormat);
        }

        // Executes the backup process synchronously
        public void Run(
            List<string> extensionsToEncrypt,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(SourceDirectory)) return;

            var sourceDir = new DirectoryInfo(SourceDirectory);
            var allFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            var allDirs = sourceDir.GetDirectories("*", SearchOption.AllDirectories);

            Directory.CreateDirectory(TargetDirectory);

            foreach (var dir in allDirs)
            {
                string relativePath = Path.GetRelativePath(SourceDirectory, dir.FullName);
                string targetSubDir = Path.Combine(TargetDirectory, relativePath);
                Directory.CreateDirectory(targetSubDir);
            }

            long totalSize = allFiles.Sum(f => f.Length);
            int totalFiles = allFiles.Length;
            int filesProcessed = 0;
            long sizeProcessed = 0;

            displayMessage?.Invoke($"\u25b6 Total: {totalFiles} {Resources.File} ({totalSize / 1024 / 1024} MB)");

            var stateLog = new StateLog
            {
                JobName = Name,
                TotalFilesToCopy = totalFiles,
                TotalFilesSize = totalSize,
                NbFilesLeftToDo = totalFiles,
                RemainingFilesSize = totalSize,
                Progression = 0,
                State = "Active",
                LastActionTimestamp = DateTime.Now
            };
            _logger.UpdateStateLog(stateLog);

            foreach (var file in allFiles)
            {
                // Thread control checkpoints
                cancellationToken.ThrowIfCancellationRequested();
                PauseEvent.Wait(cancellationToken);

                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                string targetPath = Path.Combine(TargetDirectory, relativePath);

                stateLog.CurrentSourceFilePath = file.FullName;
                stateLog.CurrentDestinationFilePath = targetPath;
                stateLog.LastActionTimestamp = DateTime.Now;
                stateLog.State = "Active";
                _logger.UpdateStateLog(stateLog);

                bool processFile = SaveType || CheckDifferential(file, targetPath);
                if (processFile)
                {
                    CopyFile(file.FullName, targetPath);

                    if (ShouldEncrypt(file.Extension, extensionsToEncrypt))
                    {
                        string? password = requestPassword?.Invoke(Resources.PasswordRequest);

                        if (!string.IsNullOrEmpty(password))
                        {
                            int encryptionTime = CryptoService.EncryptFile(targetPath, targetPath, password);
                            if (encryptionTime > 0)
                                displayMessage?.Invoke($"{file.FullName} {Resources.FileEncrypted} ({encryptionTime} ms)");
                            else if (encryptionTime == -1)
                                displayMessage?.Invoke($"{file.FullName} {Resources.EncryptionError}");
                            else if (encryptionTime == -2)
                                displayMessage?.Invoke($"{file.FullName} {Resources.FileNotFound}");
                        }
                    }

                    filesProcessed++;
                }
                sizeProcessed += file.Length;

                stateLog.NbFilesLeftToDo = totalFiles - filesProcessed;
                stateLog.RemainingFilesSize = totalSize - sizeProcessed;
                stateLog.Progression = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 100;
                _logger.UpdateStateLog(stateLog);

                // Notify UI of progression
                ProgressChanged?.Invoke(this, stateLog.Progression);

                displayMessage?.Invoke($"Progression: {stateLog.Progression}% ({filesProcessed}/{totalFiles} {Resources.File})");
            }

            stateLog.State = "Finished";
            stateLog.CurrentSourceFilePath = "";
            stateLog.CurrentDestinationFilePath = "";
            stateLog.LastActionTimestamp = DateTime.Now;
            stateLog.Progression = 100;
            _logger.UpdateStateLog(stateLog);

            // Ensure final completion state is sent to UI
            ProgressChanged?.Invoke(this, 100);

            displayMessage?.Invoke($"\u2705 {Resources.Savejob_sauvegardefinis} {Name}");
        }

        // Executes the backup process asynchronously using a background thread
        public async Task RunAsync(
            List<string> extensionsToEncrypt,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Run(extensionsToEncrypt, requestPassword, displayMessage, cancellationToken), cancellationToken);
        }

        // Checks if source file is newer than target file for differential backup
        private bool CheckDifferential(FileInfo sourceFile, string targetPath)
        {
            if (!File.Exists(targetPath)) return true;

            FileInfo targetFile = new FileInfo(targetPath);
            return sourceFile.LastWriteTime > targetFile.LastWriteTime;
        }

        // Handles file copying and logs the operation
        private void CopyFile(string source, string destination)
        {
            long transferTime = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                File.Copy(source, destination, true);
            }
            catch (Exception)
            {
                transferTime = -1;
            }

            sw.Stop();
            if (transferTime != -1) transferTime = sw.ElapsedMilliseconds;

            var dailyLog = new DailyLog
            {
                TimeStamp = DateTime.Now,
                JobName = Name,
                SourceFile = source,
                TargetFile = destination,
                FileSize = new FileInfo(source).Length,
                TransferTimeMs = transferTime
            };
            _logger.WriteDailyLog(dailyLog);
        }

        // Checks if the file extension matches the encryption list
        private bool ShouldEncrypt(string extension, List<string> extensionsToEncrypt)
        {
            return extensionsToEncrypt != null &&
                   extensionsToEncrypt.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}