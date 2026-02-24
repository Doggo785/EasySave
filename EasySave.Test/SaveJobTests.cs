using EasySave.Core.Models;
using EasySave.Core.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace EasySave.Tests
{

    // SaveJob TESTS - with FluentAssertions
    public class SaveJobTests
    {
        // Testing the creation of a SaveJob with its basic properties
        [Fact]
        public void CreationSaveJob_PropertiesAreCorrect()
        {
            // Arrange & Act
            var job = new SaveJob(1, "TestJob", @"C:\Source", @"C:\Target", true);

            // Assert
            job.Id.Should().Be(1,
                because: "the ID must match the one passed to the manufacturer");
            job.Name.Should().Be("TestJob",
                because: "the name must match the one given to the manufacturer");
            job.SourceDirectory.Should().Be(@"C:\Source",
                because: "the source directory must match the one passed to the constructor.");
            job.TargetDirectory.Should().Be(@"C:\Target",
                because: "the target directory must match the one passed to the constructor");
            job.SaveType.Should().BeTrue(
                because: "SaveType=true indicates a full backup (Full Save)");
        }

        // Testing the creation of an empty SaveJob (constructor without parameters)
        [Fact]
        public void CreationSaveJob_Empty_DefaultProperties()
        {
            // Arrange & Act
            var job = new SaveJob();

            // Assert
            job.Name.Should().BeEmpty(
                because: "the name must be an empty string by default, not null");
            job.SourceDirectory.Should().BeEmpty(
                because: "the source directory must be an empty string by default, not null");
            job.TargetDirectory.Should().BeEmpty(
                because: "The target directory must be an empty string by default, not null");
        }

        // Testing the modification of SaveJob properties
        [Fact]
        public void ModificationSaveJob_ModifiedProperties()
        {
            // Arrange
            var job = new SaveJob(1, "OldName", @"C:\OldSource", @"C:\OldTarget", true);

            // Act
            job.Name = "NewName";
            job.SourceDirectory = @"C:\NewSource";
            job.TargetDirectory = @"C:\NewTarget";
            job.SaveType = false;

            // Assert
            job.Name.Should().Be("NewName",
                because: "the name must be updated after modification via the setter");
            job.SourceDirectory.Should().Be(@"C:\NewSource",
                because: "the source directory must be updated after modification via the setter.");
            job.TargetDirectory.Should().Be(@"C:\NewTarget",
                because: "the target directory must be updated after modification via the setter.");
            job.SaveType.Should().BeFalse(
                because: "SaveType=false indicates a differential backup");
        }

        // Testing the execution of a job on a non-existent source directory (must not crash)
        [Fact]
        public void Run_SourceDirectoryNonexistent_ReturnWithoutError()
        {
            // Arrange
            var job = new SaveJob(1, "TestJob", @"C:\CheminQuiNExistePas_XYZ", @"C:\Target", true);
            var semaphore = new SemaphoreSlim(1, 1);
            var messages = new List<string>();

            // Act
            var noPriority = new ManualResetEventSlim(true);
            var act = () => job.Run(new List<string>(), semaphore, noPriority, null, msg => messages.Add(msg));

            // Assert
            act.Should().NotThrow(
                because: "a non-existent source directory should be silently ignored without raising an exception.");
        }

        // Testing the execution of a complete job (Full Save) on real files
        [Fact]
        public void Run_SaveComplete_FilesCopies()
        {
            // Arrange
            string sourceDir = Path.Combine(Path.GetTempPath(), $"EasySave_Src_{Guid.NewGuid():N}");
            string targetDir = Path.Combine(Path.GetTempPath(), $"EasySave_Dst_{Guid.NewGuid():N}");
            Directory.CreateDirectory(sourceDir);

            string testFile = Path.Combine(sourceDir, "test.txt");
            File.WriteAllText(testFile, "EasySave test contents");

            var job = new SaveJob(1, "FullTest", sourceDir, targetDir, true);
            var semaphore = new SemaphoreSlim(1, 1);

            try
            {
                // Act
                job.Run(new List<string>(), semaphore, new ManualResetEventSlim(true));

                // Assert
                string copiedFile = Path.Combine(targetDir, "test.txt");
                File.Exists(copiedFile).Should().BeTrue(
                    because: "a full backup should copy all files from the source directory to the target directory.");
                File.ReadAllText(copiedFile).Should().Be("EasySave test contents",
                    because: "the content of the copied file must be identical to the original source file.");
            }
            finally
            {
                Directory.Delete(sourceDir, true);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            }
        }
    }
}