using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using EasyLog;
using EasySave.Views;
using System;

namespace EasySave
{
    class Program
    {
        static SaveManager _saveManager = new SaveManager();
        static ConsoleView _view = new ConsoleView();

        static void Main(string[] args)
        {

            SettingsManager.Instance.LoadSettings();
            if (args.Length > 0)
            {
                string fullCommand = string.Join("", args);
                RunCommandLine(fullCommand);
                Console.WriteLine($"{Resources.Button_Cancel}");
                Console.ReadKey();
                return;
            }

            bool exit = false;
            while (!exit)
            {
                _view.ShowMainMenu();
                string choice = _view.ReadUserChoice();

                switch (choice)
                {
                    case "1":
                        _view.DisplayJobs(_saveManager.GetJobs());
                        Console.WriteLine($"      {Resources.Msg_Return}");
                        Console.ReadLine();
                        break;

                    case "2":
                        CreateJobFlow(_saveManager, _view);
                        break;

                    case "3":
                        ExecuteJobFlow(_saveManager, _view);
                        break;

                    case "4":
                        DeleteJobFlow(_saveManager, _view);
                        break;

                    case "5":
                        ShowSettingsFlow(_view);
                        break;

                    case "6":
                        DecryptFileFlow(_view);
                        break;

                    case "7":
                        exit = true;
                        break;

                    default:
                        _view.DisplayMessage(Resources.App_Case_Mauvais);
                        break;
                }
            }
        }

        static void CreateJobFlow(SaveManager manager, ConsoleView view)
        {
            var jobInfo = view.GetNewJobInfo();
            try
            {
                
                manager.CreateJob(jobInfo.name, jobInfo.source, jobInfo.dest, jobInfo.isFull);

                view.DisplayMessage(Resources.Create_Job_Succes);
            }
            catch (Exception ex)
            {
                view.DisplayError($"{Resources.Create_Job_Fail}\n      {ex.Message}");
            }
        }

        static void ExecuteJobFlow(SaveManager manager, ConsoleView view)
        {
            if (!CheckServerBeforeLaunch()) return;

            view.DisplayJobs(manager.GetJobs());
            Console.Write(Resources.Get_Job_Arg_ID);
            string input = Console.ReadLine()?.ToLower() ?? "";

            if (input == "all")
            {
                view.DisplayMessage(Resources.Get_Job_All_Try);
                Task.Run(() => manager.ExecuteAllJobs(ConsoleRequestPassword, ConsoleDisplayMessage));

            }
            else if (int.TryParse(input, out int id))
            {
                view.DisplayMessage(string.Format(Resources.Get_Job_Running, id));
                Task.Run(() => manager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage));

            }
        }

        static void DeleteJobFlow(SaveManager manager, ConsoleView view)
        {
            view.DisplayJobs(manager.GetJobs());
            Console.Write(Resources.Delete_Job_ID);
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                manager.DeleteJob(id);
                view.DisplayMessage(Resources.Delete_Job_Succes);
            }
        }

        static void RunCommandLine(string command)
        {
            if (!CheckServerBeforeLaunch()) return;

            var jobs = _SaveManager.GetJobs();

            if (command.Contains("-"))
            {
                var parts = command.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++) _saveManager.ExecuteJob(i, ConsoleRequestPassword, ConsoleDisplayMessage);
                }
            }
            else if (command.Contains(";"))
            {
                foreach (var idStr in command.Split(';'))
                {
                    if (int.TryParse(idStr, out int id)) _saveManager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage);
                }
            }
            else if (int.TryParse(command, out int id))
            {
                _saveManager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage);
            }
        }

        static void ShowSettingsFlow(ConsoleView view)
        {
            SettingsFlow.Show(view);
        }

        static void DecryptFileFlow(ConsoleView view)
        {
            ConsoleView.DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Decrypt_Header}");
            Console.WriteLine("      " + new string('─', Resources.Decrypt_Header.Length));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Decrypt_Arg_Source}");
            Console.ResetColor();
            string sourcePath = Console.ReadLine() ?? "";
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Decrypt_Arg_Dest}");
            Console.ResetColor();
            string destPath = Console.ReadLine() ?? "";
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(destPath))
                destPath = sourcePath;
            else if (Directory.Exists(destPath))
                destPath = Path.Combine(destPath, Path.GetFileName(sourcePath));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Decrypt_Arg_Password}");
            Console.ResetColor();
            string password = Console.ReadLine() ?? "";
            Console.WriteLine();

            int result = CryptoService.DecryptFile(sourcePath, destPath, password);

            if (result >= 0)
                view.DisplayMessage($"{Resources.Decrypt_Success} {result} ms");
            else if (result == -2)
                view.DisplayError(Resources.Decrypt_FileNotFound);
            else
                view.DisplayError(Resources.Decrypt_Error);
        }

        static string? ConsoleRequestPassword(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? "";
        }

        private static readonly object _consoleLock = new object();
        static void ConsoleDisplayMessage(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(message);
            }
        }

        static bool CheckServerBeforeLaunch()
        {
            var target = SettingsManager.Instance.LogTarget;
            if (target != LogTarget.Centralized && target != LogTarget.Both)
                return true;

            bool reachable = LoggerService.CheckServerConnectionAsync().GetAwaiter().GetResult();
            if (reachable)
                return true;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n      {Resources.JobLaunch_ServerOfflineMessage}");
            Console.ResetColor();
            Console.WriteLine();
            ConsoleView.PrintMenuOption("1", Resources.JobLaunch_SwitchToLocal);
            ConsoleView.PrintMenuOption("2", Resources.JobLaunch_ContinueAnyway);
            ConsoleView.PrintMenuOption("0", Resources.JobLaunch_Cancel);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("       > ");
            Console.ResetColor();

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    SettingsManager.Instance.LogTarget = LogTarget.Local;
                    LoggerService.CurrentLogTarget = LogTarget.Local;
                    SettingsManager.Instance.SaveSettings();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n      {Resources.JobLaunch_SwitchedToLocal}");
                    Console.ResetColor();
                    return true;
                case "2":
                    return true;
                default:
                    return false;
            }
        }

    }
}