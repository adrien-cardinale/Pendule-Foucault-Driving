using System;
using System.Collections.Generic;
using System.Text;
using ch.etel.edi.dsa.v40;
using ch.etel.edi.dmd.v40;
using Pendule;
using Microsoft.VisualBasic.FileIO;
using System.Net.Sockets;
using System.Net;

namespace pendule
{
    class Program
    {
        static void Main(string[] args)
        {

            string _ip = "192.168.125.208";
            int _port = 2091;
            bool run = true;

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

            pendule.Start();

            TcpListener server = new TcpListener(IPAddress.Parse(_ip), _port);
            try
            {
                server.Start();
            }
            catch (SocketException e)
            {
                throw new Exception("Error starting Command server");
            }
            while (run)
            {
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int data = stream.Read(buffer, 0, client.ReceiveBufferSize);
                string chaine = Encoding.ASCII.GetString(buffer, 0, data);
                switch (chaine)
                {
                    case "RUN":
                        pendule.Start();
                        break;
                    case "STOP":
                        pendule.Stop();
                        break;
                    case "RELOAD":
                        pendule.ReloadConfig();
                        break;
                    case "GET":
                        bool runExcitation = pendule.RunExcitation;
                        byte[] message = Encoding.ASCII.GetBytes(runExcitation.ToString());
                        stream.Write(message, 0, message.Length);
                        break;
                }
            }



            Console.WriteLine("#######################");
            Console.WriteLine("# Pendule de Foucault #");
            Console.WriteLine("#######################");
            Console.WriteLine("1. Run");
            Console.WriteLine("2. Set sinus");
            Console.WriteLine("3. Test Controle");
            Console.WriteLine("4. ReloadConfig");
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
                    case "3":
                        //pendule.StartTest();
                        Console.WriteLine("Press t to stop");
                        while (Console.ReadKey(true).Key != ConsoleKey.T) { }
                        //pendule.StopTest();
                        break;
                    case "4":
                        Console.WriteLine("ReloadConfig");
                        pendule.ReloadConfig();
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
