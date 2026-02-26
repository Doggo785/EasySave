using System.Diagnostics;
using System.Collections.Generic;

namespace EasySave.Core.Services
{
    public static class ProcessChecker
    {
        /// <summary>
        /// Vérifie si un processus métier est actif.
        /// </summary>
        /// <param name="processNames">Noms des processus (avec ou sans .exe).</param>
        /// <returns>Vrai si au moins un est en cours.</returns>
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