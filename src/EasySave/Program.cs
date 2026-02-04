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
                    case "1":
                        _view.DisplayJobs(_backupManager.GetJobs());
                        Console.WriteLine($"      {Resources.Msg_Return}");
                        Console.ReadLine();
                        break;
                    case "2":
                        Console.WriteLine("\nNom du travail :");
                        string name = Console.ReadLine() ?? "";
                        Console.WriteLine("Source :");
                        string src = Console.ReadLine() ?? "";
                        Console.WriteLine("Destination :");
                        string dest = Console.ReadLine() ?? "";
                        Console.WriteLine("Type (1=Full, 2=Diff) :");
                        string typeStr = Console.ReadLine() ?? "";
                        BackupType type = typeStr == "2" ? BackupType.Differential : BackupType.Full;

                        _backupManager.CreateJob(name, src, dest, type);
                        _view.DisplayMessage("Travail créé avec succès !");
                        break;
                    case "3":
                        _view.DisplayJobs(_backupManager.GetJobs());
                        Console.WriteLine("\nID du travail à exécuter (ou plage 1-3) :");
                        string input = Console.ReadLine() ?? "";
                        RunCommandLine(input);
                        _view.DisplayMessage("Exécution terminée.");
                        break;
                    case "4":
                        var current = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
                        string newLang = current == "fr-FR" ? "en-US" : "fr-FR";
                        LanguageManager.Instance.ChangeLanguage(newLang);
                        _view.DisplayMessage($"Language changed to {newLang}");
                        break;
                    case "5":
                        exit = true;
                        break;
                }
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