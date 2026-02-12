using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using EasySave.Views;
using System;
using System.Linq;

namespace EasySave
{
    public class SettingsFlow
    {
        public static void Show(ConsoleView view)
        {
            bool exit = false;

            while (!exit)
            {
                System.Console.Clear();
                var settings = SettingsManager.Instance;

                string settingsDisplay = $"\n{Resources.SettingsFlow_Parameters}\n" +
                                         $"1. {Resources.SettingsFlow_Language} : {settings.Language}\n" +
                                         $"2. {Resources.SettingsFlow_LogFormat} : {settings.LogFormat}\n" +
                                         $"3. {Resources.SettingsFlow_SoftwareBuis} : {(string.IsNullOrEmpty(settings.BusinessSoftwareName) ? "Aucun" : settings.BusinessSoftwareName)}\n" +
                                         $"4. {Resources.SettingsFlow_Crypto}: {(settings.EncryptedExtensions.Count > 0 ? string.Join(", ", settings.EncryptedExtensions) : "Aucune")}\n" +
                                         $"--------------------------\n" +
                                         $"0. {Resources.SettingsFlow_BackMenu}\n";

                view.DisplayMessage(settingsDisplay);
                System.Console.Write($"\n{Resources.SettingsFlow_Choixmodif}");

                string choice = System.Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ModifyLanguage(settings);
                        break;
                    case "2":
                        ModifyLogFormat(settings);
                        break;
                    case "3":
                        ModifyBusinessSoftware(settings);
                        break;
                    case "4":
                        ModifyCryptoExtensions(settings);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        System.Console.WriteLine($"{Resources.SettingsFlow_ChoixErreur}");
                        System.Console.ReadKey();
                        break;
                }
            }

            System.Console.Clear();
        }

        private static void ModifyLanguage(SettingsManager settings)
        {
            System.Console.Clear();
            System.Console.WriteLine($"\n{Resources.SettingsFlow_Changementlang}");
            System.Console.WriteLine("1. Français (fr)");
            System.Console.WriteLine("2. English (en)");
            System.Console.Write("\nVotre choix : ");

            string choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    settings.Language = "fr";
                    break;
                case "2":
                    settings.Language = "en";
                    break;
                default:
                    System.Console.WriteLine("Choix invalide.");
                    System.Console.ReadKey();
                    return;
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine("\n✓ Langue modifiée avec succès !");
            System.Console.WriteLine("Appuyez sur une touche pour continuer...");
            System.Console.ReadKey();
        }

        private static void ModifyLogFormat(SettingsManager settings)
        {
            System.Console.Clear();
            System.Console.WriteLine("\n--- MODIFICATION DU FORMAT DE LOG ---");
            System.Console.WriteLine("1. JSON");
            System.Console.WriteLine("2. XML");
            System.Console.Write("\nVotre choix : ");

            string choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    settings.LogFormat = true;
                    break;
                case "2":
                    settings.LogFormat = false;
                    break;
                default:
                    System.Console.WriteLine("Choix invalide.");
                    System.Console.ReadKey();
                    return;
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine("\n✓ Format de log modifié avec succès !");
            System.Console.WriteLine("Appuyez sur une touche pour continuer...");
            System.Console.ReadKey();
        }

        private static void ModifyBusinessSoftware(SettingsManager settings)
        {
            System.Console.Clear();
            System.Console.WriteLine("\n--- MODIFICATION DU LOGICIEL MÉTIER ---");
            System.Console.WriteLine($"Valeur actuelle : {(string.IsNullOrEmpty(settings.BusinessSoftwareName) ? "Aucun" : settings.BusinessSoftwareName)}");
            System.Console.Write("\nNom du logiciel (ou vide pour aucun) : ");

            string input = System.Console.ReadLine();
            settings.BusinessSoftwareName = input ?? "";

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine("\n✓ Logiciel métier modifié avec succès !");
            System.Console.WriteLine("Appuyez sur une touche pour continuer...");
            System.Console.ReadKey();
        }

        private static void ModifyCryptoExtensions(SettingsManager settings)
        {
            System.Console.Clear();
            System.Console.WriteLine("\n--- MODIFICATION DES EXTENSIONS CRYPTO ---");
            System.Console.WriteLine($"Extensions actuelles : {(settings.EncryptedExtensions.Count > 0 ? string.Join(", ", settings.EncryptedExtensions) : "Aucune")}");
            System.Console.Write("\nEntrez les extensions séparées par des virgules (ex: .txt,.doc,.pdf) : ");

            string input = System.Console.ReadLine();
            settings.EncryptedExtensions.Clear();

            if (!string.IsNullOrWhiteSpace(input))
            {
                var extensions = input.Split(',');
                foreach (var ext in extensions)
                {
                    var trimmed = ext.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (!trimmed.StartsWith("."))
                            trimmed = "." + trimmed;
                        settings.EncryptedExtensions.Add(trimmed);
                    }
                }
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine("\n✓ Extensions crypto modifiées avec succès !");
            System.Console.WriteLine("Appuyez sur une touche pour continuer...");
            System.Console.ReadKey();
        }
    }
}