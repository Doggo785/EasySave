using System;
using System.IO;
using System.Collections.Generic;
using EasyLog;
using EasyLog.Models;

namespace EasySave.Models
{

    public class SaveJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        // True = Full Save, False = Differential Save
        public bool SaveType { get; set; }

        private LoggerService _logger;
        public SaveJob(int id, string name, string source, string target, bool type)
        {
            Id = id;
            Name = name;
            SourceDirectory = source;
            TargetDirectory = target;
            SaveType = type;
            _logger = new LoggerService();
        }

        public SaveJob() 
        {
            _logger = new LoggerService();
        }

        public void Run()
        {
            if (!Directory.Exists(SourceDirectory))
            {
                // TODO: Log error -> "Source directory missing"
                return;
            }
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
                string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                string targetPath = Path.Combine(TargetDirectory, relativePath);

                stateLog.CurrentSourceFilePath = file.FullName;
                stateLog.CurrentDestinationFilePath = targetPath;
                stateLog.LastActionTimestamp = DateTime.Now;
                stateLog.State = "Active";
                _logger.UpdateStateLog(stateLog);

                bool processFile = false;
                if (SaveType == true)
                {
                    processFile = true;
                }
                else if (SaveType == false)
                {
                    processFile = CheckDifferential(file, targetPath);
                }
                if (processFile)
                {
                    CopyFile(file.FullName, targetPath);
                    filesProcessed++;
                }
                sizeProcessed += file.Length;

                
                stateLog.NbFilesLeftToDo = totalFiles - filesProcessed;
                stateLog.RemainingFilesSize = totalSize - sizeProcessed;

                
                stateLog.Progression = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 100;

                
                _logger.UpdateStateLog(stateLog);
            }

            stateLog.State = "Finished";
            stateLog.CurrentSourceFilePath = "";
            stateLog.CurrentDestinationFilePath = "";
            stateLog.LastActionTimestamp = DateTime.Now;
            stateLog.Progression = 100;
            _logger.UpdateStateLog(stateLog);

        }

        private bool CheckDifferential(FileInfo sourceFile, string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                return true;
            }
            FileInfo targetFile = new FileInfo(targetPath);

            return sourceFile.LastWriteTime > targetFile.LastWriteTime;
        }

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
    }
}