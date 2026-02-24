using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace EasySave.Core.Services
{
    public static class ProcessChecker
    {
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
