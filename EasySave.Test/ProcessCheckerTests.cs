using EasySave.Core.Services;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace EasySave.Tests
{
    public class ProcessCheckerTests
    {
        [Fact]
        public void IsAnyProcessRunning_ActiveSystemProcess_ReturnTrue()
        {
            var processNames = new List<string> { "explorer" };

            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            result.Should().BeTrue(
                because: "'explorer' is a Windows process that is always active; it must be detected");
        }

        [Fact]
        public void IsAnyProcessRunning_ProcessNonexistent_ReturnFalse()
        {
            var processNames = new List<string> { "process_that_doesnt_really_exist_12345" };

            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            result.Should().BeFalse(
                because: "a process name that does not exist on the machine should never be detected as active");
        }

        [Fact]
        public void IsAnyProcessRunning_EmptyList_ReturnFalse()
        {
            var processNames = new List<string>();

            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            result.Should().BeFalse(
                because: "an empty list has no process to check and must return false without raising an exception");
        }

        [Fact]
        public void IsAnyProcessRunning_NullList_ReturnFalse()
        {
            bool result = ProcessChecker.IsAnyProcessRunning(null!);

            result.Should().BeFalse(
                because: "a null list is not valid and must return false without raising an exception");
        }

        [Fact]
        public void IsAnyProcessRunning_ListWithEmptyString_ReturnFalse()
        {
            var processNames = new List<string> { "", "   " };

            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            result.Should().BeFalse(
                because: "empty or whitespace process names should be ignored and not cause a false positive");
        }

        [Fact]
        public void IsAnyProcessRunning_WithExtensionExe_SameResultAsWithoutExtension()
        {
            var withExe = new List<string> { "explorer.exe" };
            var withoutExe = new List<string> { "explorer" };

            bool resultWithExe = ProcessChecker.IsAnyProcessRunning(withExe);
            bool resultWithoutExe = ProcessChecker.IsAnyProcessRunning(withoutExe);

            resultWithExe.Should().Be(resultWithoutExe,
                because: "the .exe extension should be ignored during the search; 'explorer.exe' and 'explorer' should return the same result");
        }

        [Fact]
        public void IsAnyProcessRunning_OneActiveProcessInList_ReturnTrue()
        {
            var processNames = new List<string> { "process_fake_12345", "explorer" };

            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            result.Should().BeTrue(
                because: "if at least one process in the list is active, the method must return true");
        }
    }
}