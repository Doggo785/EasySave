using System.Diagnostics;

namespace EasySave.Core.Services
{
    public static class ProcessChecker
    {
        public static bool IsProcessRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;
    
            string cleanName = processName.Replace(".exe", "");
            return Process.GetProcessesByName(cleanName).Length > 0;
        }
    }
}
