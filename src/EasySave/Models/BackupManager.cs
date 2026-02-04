using EasySave.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EasySave.Models
{
    public class BackupManager
    {
        private List<BackupJob> _jobs;

        private readonly string _filePath = "jobs.json";

        public BackupManager()
        {
            _jobs = LoadJobs();
        }

        public List<BackupJob> GetJobs()
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

            // job limit
            if (_jobs.Count >= 5) { Console.WriteLine("Max jobs !"); throw new Exception(Resources.Erreur_Creation_Trop_Nombreux); ; }

            var newJob = new BackupJob(newId, name, src, dest, type);
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
        public void ExecuteJob(int id)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);

            if (job != null)
            {
                // BackupJob
                job.Run();
            }
        }

        // exe all jobs
        public void ExecuteAllJobs()
        {
            foreach (var job in _jobs)
            {
                ExecuteJob(job.Id);
            }
        }
        
        // save all jobs in jobs.json
        private void SaveJobs()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_jobs, options);
            File.WriteAllText(_filePath, json);
        }

        // Load all jobs
        private List<BackupJob> LoadJobs()
        {
            if (!File.Exists(_filePath)) return new List<BackupJob>();
            
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
        }
    }
}