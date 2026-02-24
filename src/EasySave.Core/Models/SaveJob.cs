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

        // Executes the backup process synchronously
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

            // Priority and non-priority separation
            var priorityExtensions = SettingsManager.Instance.PriorityExtensions;
            long maxFileSizeBytes = SettingsManager.Instance.MaxParallelFileSizeKb * 1024;

            var priorityFiles = allFiles
                .Where(f => priorityExtensions != null &&
                            priorityExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                .ToList();
            var normalFiles = allFiles
                .Where(f => priorityExtensions == null ||
                            !priorityExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Announcement, before the start of the job, of the number of priority files in order to immediately block non-priority files from all jobs.
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

            // Priority files first, then regular ones
            foreach (var file in priorityFiles.Concat(normalFiles))
            {
                // Thread control checkpoints
                cancellationToken.ThrowIfCancellationRequested();
                PauseEvent.Wait(cancellationToken);

                bool isPriority = priorityExtensions != null &&
                                  priorityExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
                bool isLarge = file.Length > maxFileSizeBytes;

                // Non-priority files are blocked as long as there are priority files waiting on a job in progress.
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
                        // Large file: we wait for the semaphore to become free (only one at a time)
                        largeFileSemaphore.Wait(cancellationToken);
                        try
                        {
                            fileLog = CopyFile(file.FullName, targetPath);
                        }
                        finally
                        {
                            largeFileSemaphore.Release();
                        }
                    }
                    else
                    {
                        // Small file: free parallel transfer
                        fileLog = CopyFile(file.FullName, targetPath);
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

                // Notify UI of progression
                ProgressChanged?.Invoke(this, stateLog.Progression);

            }

            stateLog.State = "Finished";
            stateLog.CurrentSourceFilePath = "";
            stateLog.CurrentDestinationFilePath = "";
            stateLog.LastActionTimestamp = DateTime.Now;
            stateLog.Progression = 100;
            _logger.UpdateStateLog(stateLog);

            // final completion state is sent to UI
            ProgressChanged?.Invoke(this, 100);

            displayMessage?.Invoke($"\u2705 {Resources.Savejob_sauvegardefinis} {Name}");
        }

        // Executes the backup process asynchronously using a background thread
        public async Task RunAsync(
            List<string> extensionsToEncrypt,
            SemaphoreSlim grosFichierEnCours,
            ManualResetEventSlim noPriorityPending,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Run(extensionsToEncrypt, grosFichierEnCours, noPriorityPending, requestPassword, displayMessage, cancellationToken), cancellationToken);
        }

        private bool CheckDifferential(FileInfo sourceFile, string targetPath)
        {
            if (!File.Exists(targetPath)) return true;

            FileInfo targetFile = new FileInfo(targetPath);
            return sourceFile.LastWriteTime > targetFile.LastWriteTime;
        }

        private DailyLog CopyFile(string source, string destination)
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
            return dailyLog;
        }

        private bool ShouldEncrypt(string extension, List<string> extensionsToEncrypt)
        {
            return extensionsToEncrypt != null &&
                   extensionsToEncrypt.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}