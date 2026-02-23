using EasySave.Core.Models;
using EasySave.Core.Properties;
using EasySave.Core.Services;
using EasySave.Views;
using System;
using System.Linq;
using System.Threading;

namespace EasySave
{
    public class SettingsFlow
    {
        public static void Show(ConsoleView view)
        {
            bool exit = false;

            while (!exit)
            {
                var settings = SettingsManager.Instance;

                ConsoleView.DisplayHeader();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"      {Resources.SettingsFlow_Parameters}");
                Console.WriteLine("      " + new string('─', Resources.SettingsFlow_Parameters.Length));
                Console.WriteLine();

                ConsoleView.PrintMenuOption("1", $"{Resources.SettingsFlow_Language} : {settings.Language}");
                ConsoleView.PrintMenuOption("2", $"{Resources.SettingsFlow_LogFormat} : {(settings.LogFormat ? "JSON" : "XML")}");
                ConsoleView.PrintMenuOption("3", $"{Resources.SettingsFlow_SoftwareBuis} : {(settings.BusinessSoftwareNames.Count > 0 ? string.Join(", ", settings.BusinessSoftwareNames) : Resources.None2)}");
                ConsoleView.PrintMenuOption("4", $"{Resources.SettingsFlow_Crypto} : {(settings.EncryptedExtensions.Count > 0 ? string.Join(", ", settings.EncryptedExtensions) : Resources.None)}");

                Console.WriteLine();
                ConsoleView.PrintMenuOption("0", Resources.SettingsFlow_BackMenu, ConsoleColor.Gray);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("       > ");
                Console.ResetColor();

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ChangeLangue(view);
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
                        view.DisplayError(Resources.SettingsFlow_ChoixErreur);
                        break;
                }
            }
        }

        private static void ChangeLangue(ConsoleView view)
        {
            ConsoleView.DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Chg_Lang}");
            Console.WriteLine("      " + new string('─', Resources.Chg_Lang.Length));
            Console.WriteLine();

            ConsoleView.PrintMenuOption("1", "English");
            ConsoleView.PrintMenuOption("2", "Français");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("       > ");
            Console.ResetColor();

            string langChoice = Console.ReadLine() ?? "";

            if (langChoice == "1")
            {
                SettingsManager.Instance.ChangeLanguage("en");
            }
            else if (langChoice == "2")
            {
                SettingsManager.Instance.ChangeLanguage("fr");
            }
            else
            {
                view.DisplayError(Resources.SettingsFlow_ChoixErreur);
                return;
            }

            SettingsManager.Instance.SaveSettings();
            view.DisplayMessage($" {Resources.SettingsFlow_LanguageSucessfull}");
        }

        private static void ModifyLogFormat(SettingsManager settings)
        {
            ConsoleView.DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.SettingsFlow_ModifLogFormat}");
            Console.WriteLine("      " + new string('─', Resources.SettingsFlow_ModifLogFormat.Length));
            Console.WriteLine();

            ConsoleView.PrintMenuOption("1", "JSON");
            ConsoleView.PrintMenuOption("2", "XML");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("       > ");
            Console.ResetColor();

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    settings.LogFormat = true;
                    break;
                case "2":
                    settings.LogFormat = false;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n      [X] {Resources.SettingsFlow_ChoixErreur}");
                    Console.ResetColor();
                    Console.WriteLine($"      {Resources.SettingsFlow_Continue}");
                    Console.ReadKey();
                    return;
            }

            SettingsManager.Instance.SaveSettings();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n      > {Resources.SettingsFlow_LogFormatSuccess}");
            Console.ResetColor();
            Thread.Sleep(1500);
        }

        private static void ModifyBusinessSoftware(SettingsManager settings)
        {
            ConsoleView.DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.SettingsFlow_ModifBusinesSoftWare}");
            Console.WriteLine("      " + new string('─', Resources.SettingsFlow_ModifBusinesSoftWare.Length));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"      {Resources.Current_Value} {(settings.BusinessSoftwareNames.Count > 0 ? string.Join(", ", settings.BusinessSoftwareNames) : Resources.None2)}");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.SettingsFlow_BSoftWareName} (séparés par des virgules) ");
            Console.ResetColor();

            string input = Console.ReadLine();
            settings.BusinessSoftwareNames.Clear();

            if (!string.IsNullOrWhiteSpace(input))
            {
                var softwares = input.Split(',');
                foreach (var soft in softwares)
                {
                    var trimmed = soft.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        settings.BusinessSoftwareNames.Add(trimmed);
                    }
                }
            }

            SettingsManager.Instance.SaveSettings();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n      > {Resources.SettingsFlow_BSoftWareSuccess}");
            Console.ResetColor();
            Thread.Sleep(1500);
        }

        private static void ModifyCryptoExtensions(SettingsManager settings)
        {
            ConsoleView.DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.SettingsFlow_ModifEncrypt}");
            Console.WriteLine("      " + new string('─', Resources.SettingsFlow_ModifEncrypt.Length));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"      {Resources.SettingsFlow_Encryp_Exten} {(settings.EncryptedExtensions.Count > 0 ? string.Join(", ", settings.EncryptedExtensions) : Resources.None)}");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.SettingsFlow_EncryptExtenChoice} ");
            Console.ResetColor();

            string input = Console.ReadLine();
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

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n      > {Resources.SettingsFlow_EncryptSuccess}");
            Console.ResetColor();
            Thread.Sleep(1500);
        }
    }
}