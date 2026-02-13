using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using EasySave.Views;
using System;

namespace EasySave
{
    class Program
    {
        static SaveManager _SaveManager = new SaveManager();
        static ConsoleView _view = new ConsoleView();

        static void Main(string[] args)
        {

            SettingsManager.Instance.LoadSettings();
            if (args.Length > 0)
            {
                RunCommandLine(args[0]);
                return;
            }

            bool exit = false;
            while (!exit)
            {
                _view.ShowMainMenu();
                string choice = _view.ReadUserChoice();

                switch (choice)
                {
                    case "1": // LISTER
                        _view.DisplayJobs(_SaveManager.GetJobs());
                        Console.WriteLine($"      {Resources.Msg_Return}");
                        Console.ReadLine();
                        break;

                    case "2": // CRÉER
                        CreateJobFlow(_SaveManager, _view);
                        break;

                    case "3": // EXÉCUTER
                        ExecuteJobFlow(_SaveManager, _view);
                        break;

                    case "4": // SUPPRIMER
                        DeleteJobFlow(_SaveManager, _view);
                        break;

                    case "5": // SHOW SETTINGS
                        ShowSettingsFlow(_view);
                        break;

                    case "6": // DÉCHIFFRER
                        DecryptFileFlow(_view);
                        break;

                    case "7": // QUITTER
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
            // get user inputs
            var jobInfo = view.GetNewJobInfo();

            try
            {
                
                manager.CreateJob(jobInfo.name, jobInfo.source, jobInfo.dest, jobInfo.isFull);

                view.DisplayMessage(Resources.Create_Job_Succes);
            }
            catch (Exception ex)
            {
                // show error message
                view.DisplayError($"{Resources.Create_Job_Fail}\n      {ex.Message}");
            }
        }

        static void ExecuteJobFlow(SaveManager manager, ConsoleView view)
        {
            view.DisplayJobs(manager.GetJobs());
            Console.Write(Resources.Get_Job_Arg_ID);
            string input = Console.ReadLine()?.ToLower() ?? "";

            if (input == "all")
            {
                view.DisplayMessage(Resources.Get_Job_All_Try);
                manager.ExecuteAllJobs(ConsoleRequestPassword, ConsoleDisplayMessage);
            }
            else if (int.TryParse(input, out int id))
            {

                view.DisplayMessage(string.Format(Resources.Get_Job_Running, id));
                manager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage);
            }
            view.DisplayMessage(Resources.Get_Job_End);
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
            if (command.Contains("-"))
            {
                var parts = command.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++) _SaveManager.ExecuteJob(i, ConsoleRequestPassword, ConsoleDisplayMessage);
                }
            }
            else if (command.Contains(";"))
            {
                foreach (var idStr in command.Split(';'))
                {
                    if (int.TryParse(idStr, out int id)) _SaveManager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage);
                }
            }
            else if (int.TryParse(command, out int id))
            {
                _SaveManager.ExecuteJob(id, ConsoleRequestPassword, ConsoleDisplayMessage);
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

        static void ConsoleDisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

    }
}