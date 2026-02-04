using EasySave.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EasySave.Models
{
    public class SaveManager
    {
        private List<SaveJob> _jobs;

        private readonly string _filePath = "jobs.json";

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

            // job limit
            if (_jobs.Count >= 5) { Console.WriteLine("Max jobs !"); throw new Exception(Resources.Erreur_Creation_Trop_Nombreux); ; }

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
        public void ExecuteJob(int id)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);

            if (job != null)
            {
                // SaveJob
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
        private List<SaveJob> LoadJobs()
        {
            if (!File.Exists(_filePath)) return new List<SaveJob>();
            
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SaveJob>>(json) ?? new List<SaveJob>();
        }
    }
}