using EasySave.Core.Services;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace EasySave.Tests
{
    // ProcessChecker TESTS with FluentAssertions en suivant les règles de xUnit (Arrange, Act, Assert)
    public class ProcessCheckerTests
    {
        // Test that a known process (explorer.exe) is detected as active
        [Fact]
        public void IsAnyProcessRunning_ActiveSystemProcess_ReturnTrue()
        {
            // Arrange
            var processNames = new List<string> { "explorer" };

            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            // Assert
            result.Should().BeTrue(
                because: "'explorer' is a Windows process that is always active; it must be detected");
        }

        // Test that a non-existent process returns false
        [Fact]
        public void IsAnyProcessRunning_ProcessNonexistent_ReturnFalse()
        {
            // Arrange
            var processNames = new List<string> { "process_that_doesnt_really_exist_12345" };

            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            // Assert
            result.Should().BeFalse(
                because: "a process name that does not exist on the machine should never be detected as active");
        }

        // Test with an empty list (must return false without exception)
        [Fact]
        public void IsAnyProcessRunning_EmptyList_ReturnFalse()
        {
            // Arrange
            var processNames = new List<string>();

            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            // Assert
            result.Should().BeFalse(
                because: "an empty list has no process to check and must return false without raising an exception");
        }

        // Test with a null list (must return false without exception)
        [Fact]
        public void IsAnyProcessRunning_NullList_ReturnFalse()
        {
            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(null!);

            // Assert
            result.Should().BeFalse(
                because: "a null list is not valid and must return false without raising an exception");
        }

        // Test with a list containing an empty string (must be ignored)
        [Fact]
        public void IsAnyProcessRunning_ListWithEmptyString_ReturnFalse()
        {
            // Arrange
            var processNames = new List<string> { "", "   " };

            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            // Assert
            result.Should().BeFalse(
                because: "empty or whitespace process names should be ignored and not cause a false positive");
        }

        // Test that the .exe extension is correctly ignored in the search
        [Fact]
        public void IsAnyProcessRunning_WithExtensionExe_SameResultAsWithoutExtension()
        {
            // Arrange
            var withExe = new List<string> { "explorer.exe" };
            var withoutExe = new List<string> { "explorer" };

            // Act
            bool resultWithExe = ProcessChecker.IsAnyProcessRunning(withExe);
            bool resultWithoutExe = ProcessChecker.IsAnyProcessRunning(withoutExe);

            // Assert
            resultWithExe.Should().Be(resultWithoutExe,
                because: "the .exe extension should be ignored during the search; 'explorer.exe' and 'explorer' should return the same result");
        }

        // Test that at least one active process in the list returns true
        [Fact]
        public void IsAnyProcessRunning_OneActiveProcessInList_ReturnTrue()
        {
            // Arrange 
            var processNames = new List<string> { "process_fake_12345", "explorer" };

            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(processNames);

            // Assert
            result.Should().BeTrue(
                because: "if at least one process in the list is active, the method must return true");
        }
    }
}