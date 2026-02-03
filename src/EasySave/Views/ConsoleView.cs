using System;
using System.Collections.Generic;
using System.Resources;
using System.Text;
using System.Threading;
using EasySave.Properties;
using EasySave.Models;

namespace EasySave.Views
{
    public class ConsoleView
    {
        public ConsoleView() { }

        private void DisplayHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    ╔═══════════════════════════════════════════════════════════════╗
    ║                                                               ║
    ║   ███████╗ █████╗ ███████╗██╗   ██╗███████╗ █████╗ ██╗   ██╗  ║
    ║   ██╔════╝██╔══██╗██╔════╝╚██╗ ██╔╝██╔════╝██╔══██╗██║   ██║  ║
    ║   █████╗  ███████║███████╗ ╚████╔╝ ███████╗███████║██║   ██║  ║
    ║   ██╔══╝  ██╔══██║╚════██║  ╚██╔╝  ╚════██║██╔══██║╚██╗ ██╔╝  ║
    ║   ███████╗██║  ██║███████║   ██║   ███████║██║  ██║ ╚████╔╝   ║
    ║   ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚══════╝╚═╝  ╚═╝  ╚═══╝    ║
    ║                                                               ║
    ╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("                    💾 Backup Made Simple 💾");
            Console.ResetColor();
            Console.WriteLine();
        }

        public void ShowMainMenu()
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    ┌─────────────────────────────────────┐");
            Console.WriteLine($"    │  {Resources.Menu_Title,-35} │");
            Console.WriteLine("    ├─────────────────────────────────────┤");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    │  [1] 📋 {Resources.Menu_Option1,-27} │");
            Console.WriteLine($"    │  [2] ➕ {Resources.Menu_Option2,-27} │");
            Console.WriteLine($"    │  [3] ▶️  {Resources.Menu_Option3,-26} │");
            Console.WriteLine($"    │  [4] 🌐 {Resources.Menu_Option4,-27} │");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    │  [5] 🚪 {Resources.Menu_OptionExit,-27} │");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    └─────────────────────────────────────┘");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"    ➤  ");
            Console.ResetColor();
        }

        public string ReadUserChoice()
        {
            return Console.ReadLine() ?? "";
        }

        public void DisplayJobs(List<BackupJob> jobs)
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"    ═══ {Resources.Menu_Option1} ═══");
            Console.ResetColor();
            Console.WriteLine();

            if (jobs == null || jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"    ⚠️  {Resources.Msg_NoJobs}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("    ┌──────┬──────────────────────┬────────────┬─────────────────────────────────┐");
                Console.WriteLine("    │ {0,-4} │ {1,-20} │ {2,-10} │ {3,-31} │", "ID", "Nom", "Type", "Source → Destination");
                Console.WriteLine("    ├──────┼──────────────────────┼────────────┼─────────────────────────────────┤");
                Console.ResetColor();

                for (int i = 0; i < jobs.Count; i++)
                {
                    var job = jobs[i];
                    Console.WriteLine("    │ {0,-4} │ {1,-20} │ {2,-10} │ {3,-14} → {4,-14} │",
                        i + 1,
                        Truncate(job.Name, 20),
                        job.Type,
                        Truncate(job.SourceDirectory, 14),
                        Truncate(job.TargetDirectory, 14));
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("    └──────┴──────────────────────┴────────────┴─────────────────────────────────┘");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    {Resources.Msg_Return}");
            Console.ResetColor();
            Console.ReadLine();
        }

        public void DisplayMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n    ✨ {message}");
            Console.ResetColor();

            Thread.Sleep(1500);
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }
}
