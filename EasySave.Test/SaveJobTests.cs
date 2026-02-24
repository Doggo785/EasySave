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
    public class SaveJobTests
    {
        [Fact]
        public void CreationSaveJob_PropertiesAreCorrect()
        {
            var job = new SaveJob(1, "TestJob", @"C:\Source", @"C:\Target", true);

            job.Id.Should().Be(1,
                because: "the ID must match the one passed to the constructor");
            job.Name.Should().Be("TestJob",
                because: "the name must match the one given to the constructor");
            job.SourceDirectory.Should().Be(@"C:\Source",
                because: "the source directory must match the one passed to the constructor");
            job.TargetDirectory.Should().Be(@"C:\Target",
                because: "the target directory must match the one passed to the constructor");
            job.SaveType.Should().BeTrue(
                because: "SaveType=true indicates a full backup (Full Save)");
        }

        [Fact]
        public void CreationSaveJob_Empty_DefaultProperties()
        {
            var job = new SaveJob();

            job.Name.Should().BeEmpty(
                because: "the name must be an empty string by default, not null");
            job.SourceDirectory.Should().BeEmpty(
                because: "the source directory must be an empty string by default, not null");
            job.TargetDirectory.Should().BeEmpty(
                because: "the target directory must be an empty string by default, not null");
        }

        [Fact]
        public void ModificationSaveJob_ModifiedProperties()
        {
            var job = new SaveJob(1, "OldName", @"C:\OldSource", @"C:\OldTarget", true);

            job.Name = "NewName";
            job.SourceDirectory = @"C:\NewSource";
            job.TargetDirectory = @"C:\NewTarget";
            job.SaveType = false;

            job.Name.Should().Be("NewName",
                because: "the name must be updated after modification via the setter");
            job.SourceDirectory.Should().Be(@"C:\NewSource",
                because: "the source directory must be updated after modification via the setter");
            job.TargetDirectory.Should().Be(@"C:\NewTarget",
                because: "the target directory must be updated after modification via the setter");
            job.SaveType.Should().BeFalse(
                because: "SaveType=false indicates a differential backup");
        }

        [Fact]
        public void Run_SourceDirectoryNonexistent_ReturnWithoutError()
        {
            var job = new SaveJob(1, "TestJob", @"C:\CheminQuiNExistePas_XYZ", @"C:\Target", true);
            var semaphore = new SemaphoreSlim(1, 1);
            var noPriorityPending = new ManualResetEventSlim(true); // signaled = no priority pending
            var messages = new List<string>();

            var act = () => job.Run(new List<string>(), semaphore, noPriorityPending, null, msg => messages.Add(msg));

            act.Should().NotThrow(
                because: "a non-existent source directory should be silently ignored without raising an exception");
        }

        [Fact]
        public void Run_saveComplete_FilesCopied()
        {
            string sourceDir = Path.Combine(Path.GetTempPath(), $"EasySave_Src_{Guid.NewGuid():N}");
            string targetDir = Path.Combine(Path.GetTempPath(), $"EasySave_Dst_{Guid.NewGuid():N}");
            Directory.CreateDirectory(sourceDir);

            string testFile = Path.Combine(sourceDir, "test.txt");
            File.WriteAllText(testFile, "EasySave test contents");

            var job = new SaveJob(1, "FullTest", sourceDir, targetDir, true);
            var semaphore = new SemaphoreSlim(1, 1);
            var noPriorityPending = new ManualResetEventSlim(true); // signaled = no priority pending

            try
            {
                job.Run(new List<string>(), semaphore, noPriorityPending);

                string copiedFile = Path.Combine(targetDir, "test.txt");
                File.Exists(copiedFile).Should().BeTrue(
                    because: "a full backup should copy all files from the source directory to the target directory");
                File.ReadAllText(copiedFile).Should().Be("EasySave test contents",
                    because: "the content of the copied file must be identical to the original source file");
            }
            finally
            {
                Directory.Delete(sourceDir, true);
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            }
        }
    }
}