using EasySave.Core.Properties;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EasySave.Core.Models
{
    public class SaveManager
    {
        private List<SaveJob> _jobs;

        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "UserConfig");
        private readonly string _saveFilePath = Path.Combine(_logDirectory, "jobs.json");

        public SaveManager()
        {
            _jobs = LoadJobs();
        }

        public List<SaveJob> GetJobs()
        {
            return _jobs;
        }

        public void CreateJob(string name, string src, string dest, bool type)
        {
            
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(src) ||
                string.IsNullOrWhiteSpace(dest))
            {
                throw new ArgumentException(Resources.Erreur_Creation_Blank);
            }

            if (!Path.IsPathRooted(src) || !Path.IsPathRooted(dest))
            {
                throw new ArgumentException(Resources.Erreur_Creation_Chemin);
            }
            // auto id
            int newId = _jobs.Count > 0 ? _jobs.Max(j => j.Id) + 1 : 1;

            var newJob = new SaveJob(newId, name, src, dest, type);
            _jobs.Add(newJob);

            SaveJobs();
        }

        // delete job by id
        public void DeleteJob(int id)
        {
            var jobToDelete = _jobs.FirstOrDefault(j => j.Id == id);

            if (jobToDelete != null)
            {
                _jobs.Remove(jobToDelete);

                SaveJobs();
            }
        }

        // exe unique job 
        public void ExecuteJob(int id, Func<string, string?>? requestPassword = null, Action<string>? displayMessage = null)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);

            if (job != null)
            {
                if (!CanLaunchJob())
                {
                    displayMessage?.Invoke($"{Resources.CanLaunch_ErreurMetier}");
                    return;
                }
                // SaveJob
                job.Run(SettingsManager.Instance.EncryptedExtensions, requestPassword, displayMessage);
            }
        }

        // exe all jobs
        public void ExecuteAllJobs(Func<string, string?>? requestPassword = null, Action<string>? displayMessage = null)
        {
            foreach (var job in _jobs)
            {
                ExecuteJob(job.Id, requestPassword, displayMessage);
            }
        }
        
        // save all jobs in jobs.json
        private void SaveJobs()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_jobs, options);
            EnsureDirectoryExist();
            File.WriteAllText(_saveFilePath, json);
        }

        // Load all jobs
        private List<SaveJob> LoadJobs()
        {
            if (!File.Exists(_saveFilePath)) return new List<SaveJob>();
            
            string json = File.ReadAllText(_saveFilePath);
            return JsonSerializer.Deserialize<List<SaveJob>>(json) ?? new List<SaveJob>();
        }
        public void EnsureDirectoryExist()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private bool CanLaunchJob()
        {
            var businessAppNames = SettingsManager.Instance.BusinessSoftwareNames;
            return !ProcessChecker.IsAnyProcessRunning(businessAppNames);
        }

        public void EditJob(SaveJob job)
        {
            var existingJob = _jobs.FirstOrDefault(j => j.Id == job.Id);
            if (existingJob == null)
                return;

            existingJob.Name = job.Name;
            existingJob.SourceDirectory = job.SourceDirectory;
            existingJob.TargetDirectory = job.TargetDirectory;
            existingJob.SaveType = job.SaveType;

            SaveJobs();
        }
    }
}