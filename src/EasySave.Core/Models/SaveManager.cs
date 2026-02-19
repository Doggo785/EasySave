using EasySave.Core.Properties;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Models
{
    public class SaveManager
    {
        private List<SaveJob> _jobs;
        private readonly object _jobsLock = new object(); 
        private SemaphoreSlim _concurrencyLimiter;

        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "UserConfig");
        private readonly string _saveFilePath = Path.Combine(_logDirectory, "jobs.json");

        public SaveManager()
        {
            _jobs = LoadJobs();
            int maxConcurrent = SettingsManager.Instance.MaxConcurrentJobs;
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        }

        public List<SaveJob> GetJobs()
        {
            lock (_jobsLock)
            {
                return _jobs.ToList();
            }
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

            lock (_jobsLock)
            {
                int newId = _jobs.Count > 0 ? _jobs.Max(j => j.Id) + 1 : 1;
                var newJob = new SaveJob(newId, name, src, dest, type);
                _jobs.Add(newJob);
            }

            SaveJobs();
        }

        public void DeleteJob(int id)
        {
            lock (_jobsLock)
            {
                var jobToDelete = _jobs.FirstOrDefault(j => j.Id == id);
                if (jobToDelete != null)
                {
                    _jobs.Remove(jobToDelete);


                }
            }
            SaveJobs();
        }

        public async Task ExecuteJob(
            int id,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);


            try
            {
                SaveJob job;
                lock (_jobsLock)
                {
                    job = _jobs.FirstOrDefault(j => j.Id == id);
                }

                if (job == null)
                {
                    displayMessage?.Invoke($"Job {id} non trouvé");
                    return;
                }

                displayMessage?.Invoke($"Démarrage: {job.Name}");

                // Exécuter le job (la vraie copie/chiffrement des fichiers)
                await job.RunAsync(
                    SettingsManager.Instance.EncryptedExtensions,
                    requestPassword,
                    displayMessage,
                    cancellationToken);

                displayMessage?.Invoke($"Terminé: {job.Name}");
            }
            catch (OperationCanceledException)
            {
                displayMessage?.Invoke($" Annulé: Job {id}");
            }
            catch (Exception ex)
            {
                displayMessage?.Invoke($" Erreur Job {id}: {ex.Message}");
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        public async Task ExecuteAllJobs(
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            List<SaveJob> jobsSnapshot;
            lock (_jobsLock)
            {
                jobsSnapshot = _jobs.ToList();
            }

            displayMessage?.Invoke($"Lancement de {jobsSnapshot.Count} job(s) en parallèle...");

            var tasks = jobsSnapshot.Select(job =>
                ExecuteJob(job.Id, requestPassword, displayMessage, cancellationToken)
            ).ToList();

            try
            {
                await Task.WhenAll(tasks);
                displayMessage?.Invoke(" Tous les jobs sont terminés");
            }
            catch (OperationCanceledException)
            {
                displayMessage?.Invoke(" Exécution annulée");
            }
        }
        
        private void SaveJobs()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_jobs, options);
            EnsureDirectoryExist();
            File.WriteAllText(_saveFilePath, json);
        }

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
            string businessAppName = SettingsManager.Instance.BusinessSoftwareName;
            return !ProcessChecker.IsProcessRunning(businessAppName);
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