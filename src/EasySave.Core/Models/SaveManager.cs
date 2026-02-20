using EasySave.Core.Properties;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
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

        // Tracks active jobs and their cancellation tokens
        private readonly Dictionary<int, CancellationTokenSource> _activeJobsTokens = new Dictionary<int, CancellationTokenSource>();

        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _logDirectory = Path.Combine(appDataPath, "ProSoft", "EasySave", "UserConfig");
        private readonly string _saveFilePath = Path.Combine(_logDirectory, "jobs.json");

        public SaveManager()
        {
            _jobs = LoadJobs();

            // Initialize concurrent jobs limit
            int maxConcurrent = SettingsManager.Instance.MaxConcurrentJobs;
            if (maxConcurrent <= 0) maxConcurrent = Environment.ProcessorCount;
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

        // Executes a single job asynchronously
        public async Task ExecuteJob(
            int id,
            Func<string, string?>? requestPassword = null,
            Action<string>? displayMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!CanLaunchJob())
            {
                displayMessage?.Invoke($"{Resources.CanLaunch_ErreurMetier}");
                return;
            }

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
                    displayMessage?.Invoke($"Job {id} {Resources.NotFound}");
                    return;
                }

                // Create and register a cancellation token to allow stopping the job
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _activeJobsTokens[id] = cts;

                displayMessage?.Invoke($"{Resources.Start}: {job.Name}");

                // Execute the job with the cancellation token
                await job.RunAsync(
                    SettingsManager.Instance.EncryptedExtensions,
                    requestPassword,
                    displayMessage,
                    cts.Token);

                displayMessage?.Invoke($"{Resources.Complete}: {job.Name}");
            }
            catch (OperationCanceledException)
            {
                displayMessage?.Invoke($" {Resources.Cancel_job} {id}");
            }
            catch (Exception ex)
            {
                displayMessage?.Invoke($"{Resources.Erreur_job} {id}: {ex.Message}");
            }
            finally
            {
                // cleanup token and release concurrency slot
                _activeJobsTokens.Remove(id);
                _concurrencyLimiter.Release();
            }
        }

        // Executes all jobs concurrently
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

            var tasks = jobsSnapshot.Select(job =>
                ExecuteJob(job.Id, requestPassword, displayMessage, cancellationToken)
            ).ToList();

            try
            {
                await Task.WhenAll(tasks);
                displayMessage?.Invoke(Resources.Alljob_Completed);
            }
            catch (OperationCanceledException)
            {
                displayMessage?.Invoke(Resources.JobViewModel_Cancelexecution);
            }
        }

        // thread Control Methods
        public void PauseJob(int id)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            job?.PauseEvent.Reset();
        }

        public void ResumeJob(int id)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            job?.PauseEvent.Set();
        }

        public void StopJob(int id)
        {
            if (_activeJobsTokens.TryGetValue(id, out var tokenSource))
            {
                tokenSource.Cancel();
                _activeJobsTokens.Remove(id);
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

        // Checks if any configured business software is currently running
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