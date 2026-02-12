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
                var settings = SettingsManager.Instance.Config;

                string settingsDisplay = $"\n{Resources.SettingsFlow_Parameters}\n" +
                                         $"1. {Resources.SettingsFlow_Language} : {settings.Language}\n" +
                                         $"2. {Resources.SettingsFlow_LogFormat} : {settings.LogFormat}\n" +
                                         $"3. {Resources.SettingsFlow_SoftwareBuis} : {(string.IsNullOrEmpty(settings.BusinessSoftware) ? $"{Resources.None2}" : settings.BusinessSoftware)}\n" +
                                         $"4. {Resources.SettingsFlow_Crypto}: {(settings.CryptoExtensions.Count > 0 ? string.Join(", ", settings.CryptoExtensions) : $"{Resources.None}")}\n" +
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

        private static void ModifyLanguage(AppSettings settings)
        {
            System.Console.Clear();
            System.Console.WriteLine($"\n{Resources.SettingsFlow_Changementlang}");
            System.Console.WriteLine("1. Français (fr)");
            System.Console.WriteLine("2. English (en)");
            System.Console.Write($"\n{Resources.SettingsFlow_Choice} ");

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
                    System.Console.WriteLine($"{Resources.App_Case_Mauvais}");
                    System.Console.ReadKey();
                    return;
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine($"\n✓ {Resources.SettingsFlow_LanguageSucessfull} ");
            System.Console.WriteLine($"{Resources.SettingsFlow_Continue}");
            System.Console.ReadKey();
        }

        private static void ModifyLogFormat(AppSettings settings)
        {
            System.Console.Clear();
            System.Console.WriteLine($"\n{Resources.SettingsFlow_ModifLogFormat}");
            System.Console.WriteLine("1. JSON");
            System.Console.WriteLine("2. XML");
            System.Console.Write($"\n{Resources.SettingsFlow_Choice}");

            string choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    settings.LogFormat = "JSON";
                    break;
                case "2":
                    settings.LogFormat = "XML";
                    break;
                default:
                    System.Console.WriteLine($"{Resources.SettingsFlow_ChoixErreur}");
                    System.Console.ReadKey();
                    return;
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine($"\n✓ {Resources.SettingsFlow_LogFormatSuccess}");
            System.Console.WriteLine($"{Resources.SettingsFlow_Continue}");
            System.Console.ReadKey();
        }

        private static void ModifyBusinessSoftware(AppSettings settings)
        {
            System.Console.Clear();
            System.Console.WriteLine($"\n{Resources.SettingsFlow_ModifBusinesSoftWare}");
            System.Console.WriteLine($"{Resources.Current_Value} {(string.IsNullOrEmpty(settings.BusinessSoftware) ? $"{Resources.None2}" : settings.BusinessSoftware)}");
            System.Console.Write($"\n{Resources.SettingsFlow_BSoftWareName}");

            string input = System.Console.ReadLine();
            settings.BusinessSoftware = input ?? "";

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine($"\n✓ {Resources.SettingsFlow_BSoftWareSuccess}");
            System.Console.WriteLine($"{Resources.SettingsFlow_Continue}");
            System.Console.ReadKey();
        }

        private static void ModifyCryptoExtensions(AppSettings settings)
        {
            System.Console.Clear();
            System.Console.WriteLine($"\n {Resources.SettingsFlow_ModifEncrypt}");
            System.Console.WriteLine($"{Resources.SettingsFlow_Encryp_Exten} {(settings.CryptoExtensions.Count > 0 ? string.Join(", ", settings.CryptoExtensions) : $"{Resources.None}")}");
            System.Console.Write($"\n {Resources.SettingsFlow_EncryptExtenChoice}");

            string input = System.Console.ReadLine();
            settings.CryptoExtensions.Clear();

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
                        settings.CryptoExtensions.Add(trimmed);
                    }
                }
            }

            SettingsManager.Instance.SaveSettings();
            System.Console.WriteLine($"\n✓ {Resources.SettingsFlow_EncryptSuccess}");
            System.Console.WriteLine($"{Resources.SettingsFlow_Continue}");
            System.Console.ReadKey();
        }
    }
}