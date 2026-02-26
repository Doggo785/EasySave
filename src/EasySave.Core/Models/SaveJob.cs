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

        public bool IsManuallyPaused { get; set; } = false;

        private LoggerService _logger;

        // Thread control elements
        [JsonIgnore]
        public ManualResetEventSlim PauseEvent { get; } = new ManualResetEventSlim(true);
        public event EventHandler<int>? ProgressChanged;

        public SaveJob(int id, string name, string source, string target, bool type)
        {
            Id = id;
            Name = name;
            SourceDirectory = source;
            TargetDirectory = target;
            SaveType = type;
            ApplyLoggerSettings();
            _logger = new LoggerService(SettingsManager.Instance.LogFormat);
        }

        public SaveJob()
        {
            Name = string.Empty;
            SourceDirectory = string.Empty;
            TargetDirectory = string.Empty;
            ApplyLoggerSettings();
            _logger = new LoggerService(SettingsManager.Instance.LogFormat);
        }

        private static void ApplyLoggerSettings()
        {
            LoggerService.CurrentLogTarget = SettingsManager.Instance.LogTarget;
            LoggerService.ServerIp = SettingsManager.Instance.ServerIp;
            LoggerService.ServerPort = SettingsManager.Instance.ServerPort;
        }

        public void Run(
            List<string> extensionsToEncrypt,
            SemaphoreSlim largeFileSemaphore,
            ManualResetEventSlim noPriorityPending,
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

            var priorityExtensions = SettingsManager.Instance.PriorityExtensions;
            long maxFileSizeBytes = SettingsManager.Instance.MaxParallelFileSizeKb * 1024;

            var priorityFiles = new List<FileInfo>();
            var normalFiles = new List<FileInfo>();
            bool hasPriorityExtensions = priorityExtensions is { Count: > 0 };
            foreach (var f in allFiles)
            {
                if (hasPriorityExtensions && priorityExtensions!.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                    priorityFiles.Add(f);
                else
                    normalFiles.Add(f);
            }

            // Announcement to immediately block non-priority files from all jobs.
            SaveManager.RegisterPriorityFiles(priorityFiles.Count);

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

            foreach (var (file, isPriority) in
                priorityFiles.Select(f => (f, true)).Concat(normalFiles.Select(f => (f, false))))
            {
                cancellationToken.ThrowIfCancellationRequested();
                PauseEvent.Wait(cancellationToken);

                bool isLarge = file.Length > maxFileSizeBytes;

                if (!isPriority)
                {
                    noPriorityPending.Wait(cancellationToken);
                }

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
                    DailyLog fileLog;
                    if (isLarge)
                    {
                        largeFileSemaphore.Wait(cancellationToken);
                        try
                        {
                            fileLog = CopyFile(file, targetPath);
                        }
                        finally
                        {
                            largeFileSemaphore.Release();
                        }
                    }
                    else
                    {
                        fileLog = CopyFile(file, targetPath);
                    }

                    int encryptionTimeMs = 0;
                    if (ShouldEncrypt(file.Extension, extensionsToEncrypt))
                    {
                        string? password = requestPassword?.Invoke(Resources.PasswordRequest);

                        if (!string.IsNullOrEmpty(password))
                        {
                            encryptionTimeMs = CryptoService.EncryptFile(targetPath, targetPath, password);
                            if (encryptionTimeMs == -1)
                                displayMessage?.Invoke($"{file.FullName} {Resources.EncryptionError}");
                            else if (encryptionTimeMs == -2)
                                displayMessage?.Invoke($"{file.FullName} {Resources.FileNotFound}");
                        }
                    }

                    fileLog.EncryptionTimeMs = encryptionTimeMs;
                    _logger.WriteDailyLog(fileLog);

                    filesProcessed++;
                }

                if (isPriority)
                {
                    SaveManager.OnPriorityFileDone();
                }

                sizeProcessed += file.Length;
                stateLog.NbFilesLeftToDo = totalFiles - filesProcessed;
                stateLog.RemainingFilesSize = totalSize - sizeProcessed;
                stateLog.Progression = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 100;
                _logger.UpdateStateLog(stateLog);

                ProgressChanged?.Invoke(this, stateLog.Progression);

            }

            stateLog.State = "Finished";
            stateLog.CurrentSourceFilePath = "";
            stateLog.CurrentDestinationFilePath = "";
            stateLog.LastActionTimestamp = DateTime.Now;
            stateLog.Progression = 100;
            _logger.UpdateStateLog(stateLog);

            ProgressChanged?.Invoke(this, 100);

            displayMessage?.Invoke($"\u2705 {Resources.Savejob_sauvegardefinis} {Name}");
        }

        public async Task RunAsync(
            List<string> extensionsToEncrypt,
            SemaphoreSlim largeFileInProgress,
            ManualResetEventSlim noPriorityPending,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Run(extensionsToEncrypt, largeFileInProgress, noPriorityPending, requestPassword, displayMessage, cancellationToken), cancellationToken);
        }

        private bool CheckDifferential(FileInfo sourceFile, string targetPath)
        {
            if (!File.Exists(targetPath)) return true;

            FileInfo targetFile = new FileInfo(targetPath);
            return sourceFile.LastWriteTime > targetFile.LastWriteTime;
        }

        private DailyLog CopyFile(FileInfo sourceFile, string destination)
        {
            long transferTime = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                File.Copy(sourceFile.FullName, destination, true);
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
                SourceFile = sourceFile.FullName,
                TargetFile = destination,
                FileSize = sourceFile.Length,
                TransferTimeMs = transferTime
            };
            return dailyLog;
        }

        private bool ShouldEncrypt(string extension, List<string> extensionsToEncrypt)
        {
            return extensionsToEncrypt != null &&
                   extensionsToEncrypt.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}