using System;
using System.Collections.Generic;
using System.Text;
using ch.etel.edi.dsa.v40;
using ch.etel.edi.dmd.v40;
using Pendule;

namespace pendule
{
    class Program
    {
        static void Main(string[] args)
        {
            Regulateur regulateur = new Regulateur();
            Cognex cognex = new Cognex("192.168.125.208", 2090);
            CommandePendule pendule = new CommandePendule(cognex, regulateur);

            //regulateur.OpenBus();
            //regulateur.Init();
            //regulateur.SetSinus(4.5,Math.PI);
            //regulateur.CloseBus();
            //regulateur.test();

            //cognex.Listen();

            //Console.WriteLine("Press any key to stop");
            //cognex.Start();
            //Console.ReadKey();
            //cognex.Stop();

            //menu
            Console.WriteLine("#######################");
            Console.WriteLine("# Pendule de Foucault #");
            Console.WriteLine("#######################");
            Console.WriteLine("1. Run");
            Console.WriteLine("2. Set sinus");
            Console.WriteLine("3. Save mesure");
            Console.WriteLine("q. Quit");
            Console.WriteLine("#######################\n");

            for (; ; )
            {
                Console.Write(">");
                string ? input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        Console.WriteLine("Run");
                        pendule.Start();
                        Console.WriteLine("Press any key to stop");
                        Console.ReadKey();
                        pendule.Stop();
                        break; 
                    case "2":
                        Console.WriteLine("Set sinus");
                        regulateur.OpenBus();
                        regulateur.SetSinus(4.5475, Math.PI);
                        regulateur.CloseBus();
                        Console.WriteLine("Sinus set");
                        break;
                    case "3":
                        string ? filename = "data.csv";
                        Console.WriteLine("Chose a filename (default: data.csv)");
                        Console.Write(">");
                        string? inputFilename = Console.ReadLine();
                        if (inputFilename != "")
                        {
                            filename = inputFilename;
                        }
                        Console.WriteLine("Save position");
                        Console.WriteLine("Press any key to stop");
                        cognex.Start(filename);
                        Console.ReadKey();
                        cognex.Stop();
                        Console.WriteLine("Position saved");
                        break;
                    case "q":
                        Console.WriteLine("Quit");
                        return;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }

        }
    }
}
