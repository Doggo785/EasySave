using Avalonia;
using Avalonia.ReactiveUI;
using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.Core.Services; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EasySave.UI
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                HandleCommandLine(args).GetAwaiter().GetResult();
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
        private static async Task HandleCommandLine(string[] args)
        {
            Console.WriteLine($"\n{Resources.UI_Program_CLIMOD}");

            SettingsManager.Instance.LoadSettings();
            SaveManager manager = new SaveManager();
            var jobs = manager.GetJobs();

            string command = string.Join("", args).Replace(" ", "");

            if (command.Contains("-"))
            {
                var parts = command.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        await ExecuteJobIfExist(manager, jobs, i);
                    }
                }
            }
            else if (int.TryParse(command, out int id))
            {
                await ExecuteJobIfExist(manager, jobs, id);
            }
            else
            {
                Console.WriteLine(Resources.UI_Program_Error);
            }

            Console.WriteLine(Resources.UI_Completed);
        }

        private static async Task ExecuteJobIfExist(SaveManager manager, List<SaveJob> jobs, int id)
        {
            var job = jobs.FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                Console.WriteLine($"\n>>> Execution : {job.Name} (ID: {id})");
                await manager.ExecuteJob(id, (p) => {
                    Console.Write($"> {Resources.UI_Password} ");
                    return Console.ReadLine() ?? "";
                }, (msg) => Console.WriteLine($"  {msg}"));
            }
            else
            {
                Console.WriteLine($"[!] ID {id} {Resources.UI_NotFound}");
            }
        }
    }
}