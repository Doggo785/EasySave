using EasySave.Core.Models;
using FluentAssertions;
using System;
using Xunit;

namespace EasySave.Tests
{

    // SaveManager TESTS - with FluentAssertions   
    public class SaveManagerTests
    {
        // Testing job creation with valid parameters
        [Fact]
        public void CreateJob_ValidParameters_JobAdd()
        {
            // Arrange
            var manager = new SaveManager();
            int countBefore = manager.GetJobs().Count;

            // Act
            manager.CreateJob("MyJob", @"C:\Source", @"C:\Target", true);

            // Assert
            manager.GetJobs().Should().HaveCount(countBefore + 1,
                because: "the list must contain an additional job after a valid CreateJob.");

            // Cleaning
            var jobs = manager.GetJobs();
            manager.DeleteJob(jobs[jobs.Count - 1].Id);
        }

        // Testing job creation with an empty name (should throw an exception)
        [Fact]
        public void CreateJob_EmptyName_LeveException()
        {
            // Arrange
            var manager = new SaveManager();

            // Act
            var act = () => manager.CreateJob("", @"C:\Source", @"C:\Target", true);

            // Assert
            act.Should().Throw<ArgumentException>(
                because: "an empty job title should not be accepted; a name is required.");
        }

        // Testing job creation with a relative path (should throw an exception)
        [Fact]
        public void CreateJob_RelativePath_LeveException()
        {
            // Arrange
            var manager = new SaveManager();

            // Act
            var act = () => manager.CreateJob("MyJob", "source/relative", @"C:\Target", true);

            // Assert
            act.Should().Throw<ArgumentException>(
                because: "a relative path is not accepted; only absolute paths are valid.");
        }

        // Testing the deletion of an existing job
        [Fact]
        public void DeleteJob_JobExisting_JobDelete()
        {
            // Arrange
            var manager = new SaveManager();
            manager.CreateJob("JobToDelete", @"C:\Source", @"C:\Target", false);
            var jobs = manager.GetJobs();
            int idToDelete = jobs[jobs.Count - 1].Id;

            // Act
            manager.DeleteJob(idToDelete);

            // Assert
            manager.GetJobs().Should().NotContain(j => j.Id == idToDelete,
                because: $"the job with the ID {idToDelete} must be removed from the list after DeleteJob");
        }

        // Testing the deletion of a non-existent job (should not crash)
        [Fact]
        public void DeleteJob_IdNonexistent_NoException()
        {
            // Arrange
            var manager = new SaveManager();

            // Act
            var act = () => manager.DeleteJob(999999);

            // Assert
            act.Should().NotThrow(
                because: "deleting a job with a non-existent ID should not raise an exception; it should be ignored silently");
        }

        // Testing modification of an existing job
        [Fact]
        public void EditJob_JobExisting_PropertiesUpdates()
        {
            // Arrange
            var manager = new SaveManager();
            manager.CreateJob("OldName", @"C:\Source", @"C:\Target", true);
            var jobs = manager.GetJobs();
            var jobToEdit = jobs[jobs.Count - 1];

            // Act
            jobToEdit.Name = "NewName";
            jobToEdit.SaveType = false;
            manager.EditJob(jobToEdit);

            // Assert
            var jobModified = manager.GetJobs().Find(j => j.Id == jobToEdit.Id);
            jobModified.Should().NotBeNull(
                because: "The modified job must still exist in the list after EditJob");
            jobModified.Name.Should().Be("NewName",
                because: "The job name must be updated after EditJob");
            jobModified.SaveType.Should().BeFalse(
                because: "The save type must be updated after EditJob");

            // Cleaning
            manager.DeleteJob(jobToEdit.Id);
        }

        // Test that GetJobs returns a copy (not the internal list directly)
        [Fact]
        public void GetJobs_ReturnNonModifiableList()
        {
            // Arrange
            var manager = new SaveManager();
            var jobs1 = manager.GetJobs();
            int countOriginal = jobs1.Count;

            // Act
            jobs1.Clear();

            // Assert
            manager.GetJobs().Should().HaveCount(countOriginal,
                because: "GetJobs must return a copy of the internal list; modifying the copy must not affect the SaveManager.");
        }
    }
}