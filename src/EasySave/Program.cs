using EasySave.Models;
using EasySave.Views;
using EasySave.Properties;
using EasySave.Services;

var manager = new BackupManager();
var view = new ConsoleView();


if (args.Length > 0)
{
    ExecuteFromArgs(args[0], manager);
    return;
}


bool keepRunning = true;
while (keepRunning)
{
    view.ShowMainMenu();
    string choice = view.ReadUserChoice();

    switch (choice)
    {
        case "1": // LISTER
            view.DisplayJobs(manager.GetJobs());
            break;

        case "2": // CRÉER
            CreateJobFlow(manager, view);
            break;

        case "3": // EXÉCUTER
            ExecuteJobFlow(manager, view);
            break;

        case "4": // SUPPRIMER
            DeleteJobFlow(manager, view);
            break;

        case "5": // CHANGER DE LANGUE
            ChangeLangueFlow(view);
            break;

        case "6": // QUITTER
            keepRunning = false;
            break;

        default:
            view.DisplayMessage("Choix invalide.");
            break;
    }
}

void ChangeLangueFlow(ConsoleView view)
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

void CreateJobFlow(BackupManager manager, ConsoleView view)
{
    Console.Clear();
    Console.WriteLine("=== Création d'une sauvegarde ===");

    Console.Write("Nom : ");
    string name = Console.ReadLine() ?? "Job";

    Console.Write("Source (Chemin complet) : ");
    string src = Console.ReadLine() ?? "";

    Console.Write("Destination (Chemin complet) : ");
    string dest = Console.ReadLine() ?? "";

    Console.Write("Type (1 = Complet, 2 = Différentiel) : ");
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
        Console.WriteLine($"Action annulée:{ex.Message}");
        success = false;
    }

    if (success)
    {
        view.DisplayMessage("Sauvegarde créée avec succès !");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nImpossible de créer la sauvegarde. Vérifiez les erreurs ci-dessus.");
        Console.ResetColor();
        Console.WriteLine("\nAppuyez sur une touche pour revenir au menu...");
        Console.ReadKey();
    }
}

void ExecuteJobFlow(BackupManager manager, ConsoleView view)
{
    view.DisplayJobs(manager.GetJobs());
    Console.Write("\nEntrez l'ID à exécuter (ou 'all' pour tout) : ");
    string input = Console.ReadLine()?.ToLower();

    if (input == "all")
    {
        view.DisplayMessage("Exécution globale lancée...");
        manager.ExecuteAllJobs();
    }
    else if (int.TryParse(input, out int id))
    {
        view.DisplayMessage($"Exécution du job {id}...");
        manager.ExecuteJob(id);
    }
    view.DisplayMessage("Opération terminée.");
}

void DeleteJobFlow(BackupManager manager, ConsoleView view)
{
    view.DisplayJobs(manager.GetJobs());
    Console.Write("\nID du job à supprimer : ");
    if (int.TryParse(Console.ReadLine(), out int id))
    {
        manager.DeleteJob(id);
        view.DisplayMessage("Job supprimé.");
    }
}

// Pensez à aller dans cd src/EasySave/bin/Debug/net10.0/ pour execute la commande
void ExecuteFromArgs(string arg, BackupManager manager)
{
    var ids = new List<int>();
    string input = arg.ToLower().Trim();

    // all sauvegardes
    if (input == "all")
    {
        Console.WriteLine("--- Exécution de TOUTES les sauvegardes ---");
        manager.ExecuteAllJobs();
        return;
    }

    try
    {
        // Interval
        if (input.Contains("-"))
        {
            var parts = input.Split('-');
            if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            {
                for (int i = start; i <= end; i++) ids.Add(i);
            }
        }
        // Liste
        else if (input.Contains(";"))
        {
            var parts = input.Split(';');
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int id)) ids.Add(id);
            }
        }
        // Unique
        else if (int.TryParse(input, out int singleId))
        {
            ids.Add(singleId);
        }

       
        if (ids.Count > 0)
        {
            Console.WriteLine($"--- Mode Automatique : Exécution de {ids.Count} job(s) ---");
            foreach (int id in ids)
            {
                
                var jobExists = manager.GetJobs().Any(j => j.Id == id);
                if (jobExists)
                {
                    Console.WriteLine($"\n> Lancement du Job ID: {id}");
                    manager.ExecuteJob(id);
                }
                else
                {
                    Console.WriteLine($"\n[!] Erreur : Le job ID {id} n'existe pas dans le JSON.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur lors de l'analyse des arguments : {ex.Message}");
    }

    Console.WriteLine("\nTravaux terminés. Appuyez sur une touche pour quitter...");
    Console.ReadKey();
}