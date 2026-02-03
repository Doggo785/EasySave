using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Models
{
    public class BackupManager
    {
        private List<BackupJob> _jobs;

        public BackupManager()
        {
            _jobs = new List<BackupJob>();
        }

        public List<BackupJob> GetJobs()
        {
            return _jobs;
        }

        public void CreateJob(string name, string src, string dest, BackupType type)
        {
            // auto id
            int newId = _jobs.Count > 0 ? _jobs.Max(j => j.Id) + 1 : 1;

            // job limit
            if (_jobs.Count >= 5) { Console.WriteLine("Max jobs !"); return; }

            var newJob = new BackupJob(newId, name, src, dest, type);
            _jobs.Add(newJob);
        }

        // delete job by id
        public void DeleteJob(int id)
        {
            var jobToDelete = _jobs.FirstOrDefault(j => j.Id == id);

            if (jobToDelete != null)
            {
                _jobs.Remove(jobToDelete);
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
    }
}