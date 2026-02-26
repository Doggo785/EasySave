using System.Diagnostics;
using System.Collections.Generic;

namespace EasySave.Core.Services
{
    public static class ProcessChecker
    {
        /// <summary>
        /// Checks if a business process is currently active.
        /// </summary>
        /// <param name="processNames">List of process names (with or without .exe).</param>
        /// <returns>True if at least one process is running; otherwise, false.</returns>
        public static bool IsAnyProcessRunning(List<string> processNames)
        {
            if (processNames == null || processNames.Count == 0)
                return false;

            foreach (var processName in processNames)
            {
                if (string.IsNullOrWhiteSpace(processName)) continue;

                string cleanName = processName.Replace(".exe", "");
                if (Process.GetProcessesByName(cleanName).Length > 0)
                    return true;
            }
            return false;
        }
    }
}