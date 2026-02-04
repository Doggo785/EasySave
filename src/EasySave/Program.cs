using EasySave.Models;
using EasySave.Properties;
using EasySave.Services;
using EasySave.Views;
using System;

namespace EasySave
{
    class Program
    {
        static BackupManager _backupManager = new BackupManager();
        static ConsoleView _view = new ConsoleView();

        static void Main(string[] args)
        {

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
                        _view.DisplayJobs(_backupManager.GetJobs());
                        Console.WriteLine($"      {Resources.Msg_Return}");
                        Console.ReadLine();
                        break;

                    case "2": // CRÉER
                        CreateJobFlow(_backupManager, _view);
                        break;

                    case "3": // EXÉCUTER
                        ExecuteJobFlow(_backupManager, _view);
                        break;

                    case "4": // SUPPRIMER
                        DeleteJobFlow(_backupManager, _view);
                        break;

                    case "5": // CHANGER DE LANGUE
                        ChangeLangueFlow(_view);
                        break;

                    case "6": // QUITTER
                        exit = true;
                        break;

                    default:
                        _view.DisplayMessage(Resources.App_Case_Mauvais);
                        break;
                }
            }
        }

        static void ChangeLangueFlow(ConsoleView view)
        {
            Console.Clear();
            Console.WriteLine("=== Change Language / Changer la Langue ===");
            Console.WriteLine("[1] English");
            Console.WriteLine("[2] Français");
            Console.Write("\n➤ ");

            string langChoice = Console.ReadLine();

            // Appel du Singleton LanguageManager
            if (langChoice == "1")
            {
                LanguageManager.Instance.ChangeLanguage("en-US");
                view.DisplayMessage("Language set to English!");
            }
            else if (langChoice == "2")
            {
                LanguageManager.Instance.ChangeLanguage("fr-FR");
                view.DisplayMessage("Langue changée en Français !");
            }
            else
            {
                view.DisplayMessage("Invalid choice / Choix invalide.");
            }
        }
        static void CreateJobFlow(BackupManager manager, ConsoleView view)
        {
            Console.Clear();
            Console.WriteLine(Resources.Create_Job_Header);

            Console.Write(Resources.Create_Job_Arg_Name);
            string name = Console.ReadLine() ?? "Job";

            Console.Write(Resources.Create_Job_Arg_Source);
            string src = Console.ReadLine() ?? "";

            Console.Write(Resources.Create_Job_Arg_Dest);
            string dest = Console.ReadLine() ?? "";

            Console.Write(Resources.Create_Job_Arg_TypeSave);
            string typeChoice = Console.ReadLine();
            BackupType type = (typeChoice == "2") ? BackupType.Differential : BackupType.Full;

            bool success;
            try
            {
                manager.CreateJob(name, src, dest, type);
                success = true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{ex.Message}");
                success = false;
            }

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                view.DisplayMessage(Resources.Create_Job_Succes);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Resources.Create_Job_Fail);
                Console.ResetColor();
                Console.WriteLine(Resources.Back_To_Menu);
                Console.ReadKey();
            }
        }

        static void ExecuteJobFlow(BackupManager manager, ConsoleView view)
        {
            view.DisplayJobs(manager.GetJobs());
            Console.Write(Resources.Get_Job_Arg_ID);
            string input = Console.ReadLine()?.ToLower();

            if (input == "all")
            {
                view.DisplayMessage(Resources.Get_Job_All_Try);
                manager.ExecuteAllJobs();
            }
            else if (int.TryParse(input, out int id))
            {
                view.DisplayMessage(string.Format(Resources.Get_Job_Running, id));
                manager.ExecuteJob(id);
            }
            view.DisplayMessage(Resources.Get_Job_End);
        }

        static void DeleteJobFlow(BackupManager manager, ConsoleView view)
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
                    for (int i = start; i <= end; i++) _backupManager.ExecuteJob(i);
                }
            }
            else if (command.Contains(";"))
            {
                foreach (var idStr in command.Split(';'))
                {
                    if (int.TryParse(idStr, out int id)) _backupManager.ExecuteJob(id);
                }
            }
            else if (int.TryParse(command, out int id))
            {
                _backupManager.ExecuteJob(id);
            }
        }
    }
}