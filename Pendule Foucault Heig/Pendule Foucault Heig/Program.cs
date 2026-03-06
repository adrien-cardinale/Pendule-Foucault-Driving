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
        static async Task Main(string[] args)
        {

            string _ip = "192.168.125.208";
            int _port = 2091;
            bool run = true;

            const string serverPath = @"\\192.168.125.1\PenduleShare\";

            PFControl pendule;
            try
            {
                pendule = new PFControl(serverPath);
                await pendule.Init();
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

        }
    }
}
