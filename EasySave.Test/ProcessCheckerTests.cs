using EasySave.Core.Services;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace EasySave.Tests
{

    // ProcessChecker TESTS - with FluentAssertions
    public class ProcessCheckerTests
    {
        // Test that a known process (explorer.exe) is detected as active
        [Fact]
        public void IsProcessRunning_ActiveSystemProcess_ReturnTrue()
        {
            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(new List<string> { "explorer" });

            // Assert
            result.Should().BeTrue(
                because: "'explorer' is a Windows process that is always active; it must be detected");
        }

        // Test that a non-existent process returns false
        [Fact]
        public void IsProcessRunning_ProcessNonexistent_ReturnFalse()
        {
            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(new List<string> { "process_that_doesn't_really_exist_12345" });

            // Assert
            result.Should().BeFalse(
                because: "a process name that does not exist on the machine should never be detected as active");
        }

        // Test with an empty name (must return false without exception)
        [Fact]
        public void IsProcessRunning_EmptyName_ReturnFalse()
        {
            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(new List<string> { "" });

            // Assert
            result.Should().BeFalse(
                because: "an empty string is not a valid process name and must return false without raising an exception");
        }

        // Test with a null name (must return false without exception)
        [Fact]
        public void IsProcessRunning_NameNull_ReturnFalse()
        {
            // Act
            bool result = ProcessChecker.IsAnyProcessRunning(null!);

            // Assert
            result.Should().BeFalse(
                because: "a null value is not a valid process name and must return false without raising an exception");
        }

        // Test that the .exe extension is correctly ignored in the search
        [Fact]
        public void IsProcessRunning_WithExtensionExe_SameResultAsWithoutExtension()
        {
            // Act
            bool withExe = ProcessChecker.IsAnyProcessRunning(new List<string> { "explorer.exe" });
            bool withoutExe = ProcessChecker.IsAnyProcessRunning(new List<string> { "explorer" });

            // Assert
            withExe.Should().Be(withoutExe,
                because: "the .exe extension should be ignored during the search; 'explorer.exe' and 'explorer' should return the same result");
        }
    }
}