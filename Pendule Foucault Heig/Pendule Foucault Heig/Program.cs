using System;
using System.Collections.Generic;
using System.Text;
using ch.etel.edi.dsa.v40;
using ch.etel.edi.dmd.v40;
using Pendule;
using Microsoft.VisualBasic.FileIO;

namespace pendule
{
    class Program
    {
        static void Main(string[] args)
        {
            const string serverPath = @"\\192.168.125.1\PenduleShare\";

            PFControl pendule;
            try
            {
                pendule = new PFControl(serverPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
                return;
            }
            


            Console.WriteLine("#######################");
            Console.WriteLine("# Pendule de Foucault #");
            Console.WriteLine("#######################");
            Console.WriteLine("1. Run");
            Console.WriteLine("2. Set sinus");
            Console.WriteLine("q. Quit");
            Console.WriteLine("#######################\n");


            for (; ; )
            {
                string ? input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        Console.WriteLine("Run");
                        pendule.Start();
                        Console.WriteLine("Press t to stop");
                        while (Console.ReadKey(true).Key != ConsoleKey.T){}
                        pendule.Stop();
                        break; 
                    case "2":
                        Console.WriteLine("Set sinus in look-up table");
                        pendule.SetSinus();
                        Console.WriteLine("Sinus set");
                        break;
                    case "q":
                        Console.WriteLine("Quit");
                        pendule.Close();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }

        }
    }
}
