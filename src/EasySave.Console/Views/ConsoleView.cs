using System;
using System.Collections.Generic;
using System.Threading;
using EasySave.Core.Properties;
using EasySave.Core.Models;
using EasySave.Core.Services;

namespace EasySave.Views
{
    public class ConsoleView
    {
        public ConsoleView() { }

        public static void DisplayHeader()
        {
            Console.Clear();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
      '||''''|                              .|'''.|                           
       ||  .     ....    ....  .... ...     ||..  '   ....   .... ...   ....  
       ||''|    '' .||  ||. '   '|.  |       ''|||.  '' .||   '|.  |  .|...|| 
       ||       .|' ||  . '|..   '|.|      .     '|| .|' ||    '|.|   ||      
      .||.....| '|..'|' |'..|'    '|       |'....|'  '|..'|'    '|     '|...' 
                               .. |                                           
                               ''                                            
");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("      Safe & Secure Save Solution");
            Console.WriteLine("      " + new string('─', 44));
            Console.ResetColor();
            Console.WriteLine();
        }

        public void ShowMainMenu()
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Menu_Title}");
            Console.WriteLine();

            PrintMenuOption("1", Resources.Menu_Option1);
            PrintMenuOption("2", Resources.Menu_Option2);
            PrintMenuOption("3", Resources.Menu_Option3);
            PrintMenuOption("4", Resources.Menu_Option4);
            PrintMenuOption("5", Resources.Menu_OptionSettings);
            PrintMenuOption("6", Resources.Menu_OptionDecrypt);

            Console.WriteLine();
            PrintMenuOption("7", Resources.Menu_OptionExit, ConsoleColor.Gray);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("       > ");
            Console.ResetColor();
        }

        public static void PrintMenuOption(string key, string label, ConsoleColor textColor = ConsoleColor.White)
        {
            Console.Write("      ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]  ");

            Console.ForegroundColor = textColor;
            Console.WriteLine(label);
            Console.ResetColor();
        }

        public string ReadUserChoice()
        {
            return Console.ReadLine() ?? "";
        }

        public void DisplayJobs(List<SaveJob> jobs)
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Menu_Option1}");

            Console.WriteLine("      " + new string('─', Resources.Menu_Option1.Length));
            Console.WriteLine();


            if (jobs == null || jobs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      (i) {Resources.Msg_NoJobs}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("      {0,-4} {1,-20} {2,-10} {3,-20} {4} {5}",
                    Resources.Header_Id,
                    Resources.Header_Name,
                    Resources.Header_Type,
                    Resources.Header_Source,
                    ">",
                    Resources.Header_Dest);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("      " + new string('─', 85));
                Console.ResetColor();

                for (int i = 0; i < jobs.Count; i++)
                {
                    var job = jobs[i];
                    Console.Write("      ");

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{job.Id,-4} ");
                    Console.ResetColor();

                    Console.Write($"{Truncate(job.Name, 20),-20} ");
                    Console.Write($"{job.SaveType,-10} ");
                    Console.WriteLine($"{Truncate(job.SourceDirectory, 20),-20} > {Truncate(job.TargetDirectory, 20)}");
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.ResetColor();
        }

        public void DisplayMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n      > {message}");
            Console.ResetColor();

            Thread.Sleep(1500);
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
        public (string name, string source, string dest, bool isFull) GetNewJobInfo()
        {
            DisplayHeader(); 

            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Create_Job_Header}");
            
            Console.WriteLine("      " + new string('─', Resources.Create_Job_Header.Length));
            Console.WriteLine();

            // name Input
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Create_Job_Arg_Name} ");
            Console.ResetColor();
            string name = Console.ReadLine() ?? "Job";
            Console.WriteLine();

            // source Input
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Create_Job_Arg_Source} ");
            Console.ResetColor();
            string source = Console.ReadLine() ?? "";
            Console.WriteLine();

            // destination Input
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Create_Job_Arg_Dest} "); 
            Console.ResetColor();
            string dest = Console.ReadLine() ?? "";
            Console.WriteLine();

            // backup Type Input 
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"      {Resources.Create_Job_Arg_TypeSave} ");
            Console.ResetColor();

            string typeChoice = Console.ReadLine() ?? "";
            Console.WriteLine();

            
            bool isFull = (typeChoice != "2");

            return (name, source, dest, isFull);
        }

        // Displays an error message in Red.
        public void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n      [X] {message}");
            Console.ResetColor();
            Console.WriteLine($"      {Resources.Back_To_Menu}"); // press any key
            Console.ReadKey();
        }

    }

}
