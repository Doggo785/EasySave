using System;
using System.Collections.Generic;
using System.Resources;
using System.Text;
using System.Threading;
using EasySave.Properties;
using EasySave.Models;

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
            Console.WriteLine("      Safe & Secure Backup Solution");
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
            PrintMenuOption("5", Resources.Menu_Option5);
            
            Console.WriteLine(); 
            PrintMenuOption("6", Resources.Menu_OptionExit, ConsoleColor.Gray);

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

        public void DisplayJobs(List<BackupJob> jobs)
        {
            DisplayHeader();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"      {Resources.Menu_Option1}");
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
                    Console.Write($"{i + 1,-4} ");
                    Console.ResetColor();

                    Console.Write($"{Truncate(job.Name, 20),-20} ");
                    Console.Write($"{job.Type,-10} ");
                    Console.WriteLine($"{Truncate(job.SourceDirectory, 20),-20} → {Truncate(job.TargetDirectory, 20)}");
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
    }
}
