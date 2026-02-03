using System;
using System.IO;
using System.Collections.Generic;

namespace EasySave.Models
{

    public class BackupJob
    {
        // Unique Id
        public int ID { get; set; }
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }

        public BackupJob(int id, string name, string source, string target, BackupType type)
        {
            ID = id;
            Name = name;
            SourceDirectory = source;
            TargetDirectory = target;
            Type = type;
        }

        public BackupJob() { }

        public void Run()
        {
            // Implementation of backup logic goes here

            try
            {
                var dir = new DirectoryInfo(SourceDirectory);
                if (!dir.Exists)
                {
                    Console.Writeline($"ERROR : source {SourceDirectory} doesn't exist.")
                    return;
                }

                FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);

                foreach (FileInfo file in files)
                {
                    // build the target path
                    string relativePath = Path.GetRelativePath(SourceDirectory, file.FullName);
                    string targetPath = Path.Combine(TargetDirectory, relativePath);

                    new FileInfo(targetPath).Directory?.Create(); // Ensure target directory exists

                    if (Type == BackupType.Full)
                    {
                        CopyFile(file.FullName, targetPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : An error occurred during backup {Name}: {ex.Message}");
            }
        }

        private bool CheckDifferential(FileInfo sourceFile, string targetPath)
        {
            if (!sourceFile.Exists(targetPath))
            {
                return true;
            }
            FileInfo targetFile = new FileInfo(targetPath);

            return sourceFile.LastWriteTime > targetFile.LastWriteTime;
        }

        private void CopyFile(string source, string destination)
        {
            try
            {
                // TODO here : call logger
                File.Copy(sourcePath, targetPath, true);
                Console.WriteLine($"INFO : Copied {source} to {destination}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : Failed to copy {source} to {destination}: {ex.Message}.");
            }
        }
    }
}