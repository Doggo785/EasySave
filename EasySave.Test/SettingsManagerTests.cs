using EasySave.Core.Models;
using FluentAssertions;
using System;
using Xunit;

namespace EasySave.Tests
{

    public class SaveManagerTests
    {
        [Fact]
        public void CreateJob_ValidParameters_JobAdd()
        {
            var manager = new SaveManager();
            int countBefore = manager.GetJobs().Count;

            manager.CreateJob("MyJob", @"C:\Source", @"C:\Target", true);

            manager.GetJobs().Should().HaveCount(countBefore + 1,
                because: "the list must contain an additional job after a valid CreateJob.");

            var jobs = manager.GetJobs();
            manager.DeleteJob(jobs[jobs.Count - 1].Id);
        }

        [Fact]
        public void CreateJob_EmptyName_LeveException()
        {
            var manager = new SaveManager();

            var act = () => manager.CreateJob("", @"C:\Source", @"C:\Target", true);

            act.Should().Throw<ArgumentException>(
                because: "an empty job title should not be accepted; a name is required.");
        }

        [Fact]
        public void CreateJob_RelativePath_LeveException()
        {
            var manager = new SaveManager();

            var act = () => manager.CreateJob("MyJob", "source/relative", @"C:\Target", true);

            act.Should().Throw<ArgumentException>(
                because: "a relative path is not accepted; only absolute paths are valid.");
        }

        [Fact]
        public void DeleteJob_JobExisting_JobDelete()
        {
            var manager = new SaveManager();
            manager.CreateJob("JobToDelete", @"C:\Source", @"C:\Target", false);
            var jobs = manager.GetJobs();
            int idToDelete = jobs[jobs.Count - 1].Id;

            manager.DeleteJob(idToDelete);

            manager.GetJobs().Should().NotContain(j => j.Id == idToDelete,
                because: $"the job with the ID {idToDelete} must be removed from the list after DeleteJob");
        }

        [Fact]
        public void DeleteJob_IdNonexistent_NoException()
        {
            var manager = new SaveManager();

            var act = () => manager.DeleteJob(999999);

            act.Should().NotThrow(
                because: "deleting a job with a non-existent ID should not raise an exception; it should be ignored silently");
        }

        [Fact]
        public void EditJob_JobExisting_PropertiesUpdates()
        {
            var manager = new SaveManager();
            manager.CreateJob("OldName", @"C:\Source", @"C:\Target", true);
            var jobs = manager.GetJobs();
            var jobToEdit = jobs[jobs.Count - 1];

            jobToEdit.Name = "NewName";
            jobToEdit.SaveType = false;
            manager.EditJob(jobToEdit);

            var jobModified = manager.GetJobs().Find(j => j.Id == jobToEdit.Id);
            jobModified.Should().NotBeNull(
                because: "The modified job must still exist in the list after EditJob");
            jobModified.Name.Should().Be("NewName",
                because: "The job name must be updated after EditJob");
            jobModified.SaveType.Should().BeFalse(
                because: "The save type must be updated after EditJob");

            manager.DeleteJob(jobToEdit.Id);
        }

        [Fact]
        public void GetJobs_ReturnNonModifiableList()
        {
            var manager = new SaveManager();
            var jobs1 = manager.GetJobs();
            int countOriginal = jobs1.Count;

            jobs1.Clear();

            manager.GetJobs().Should().HaveCount(countOriginal,
                because: "GetJobs must return a copy of the internal list; modifying the copy must not affect the SaveManager.");
        }
    }
}