using EasySave.Models;
using EasySave.Properties;
using EasySave.Services;
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
            Views.ConsoleView.DisplayHeader();
            Console.WriteLine($"      {Resources.Chg_Lang}\n");
            Views.ConsoleView.PrintMenuOption("1", "English");
            Views.ConsoleView.PrintMenuOption("2", "Français");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("       > ");
            Console.ResetColor();

            string langChoice = Console.ReadLine() ?? "";

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
                manager.ExecuteAllJobs();
            }
            else if (int.TryParse(input, out int id))
            {
                view.DisplayMessage(string.Format(Resources.Get_Job_Running, id));
                manager.ExecuteJob(id);
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
                    for (int i = start; i <= end; i++) _SaveManager.ExecuteJob(i);
                }
            }
            else if (command.Contains(";"))
            {
                foreach (var idStr in command.Split(';'))
                {
                    if (int.TryParse(idStr, out int id)) _SaveManager.ExecuteJob(id);
                }
            }
            else if (int.TryParse(command, out int id))
            {
                _SaveManager.ExecuteJob(id);
            }
        }
    }
}