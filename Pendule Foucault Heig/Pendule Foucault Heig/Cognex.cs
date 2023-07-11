using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Pendule
{
    internal class Cognex
    {
        public double posX 
        { 
            get
            {
                return _posX;
            }
        }
        private double _posX;
        public double posY
        {
            get
            {
                return _posY;
            }
        }
        private double _posY;
        private TcpListener server;
        private Thread thread;
        private bool _run = true;
        TcpClient client;
        NetworkStream stream;
        public Cognex(string ip, int port)
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start(); 
            client = server.AcceptTcpClient();
            stream = client.GetStream();

        }
        public void ReadData()
        {
            
            try
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];

                int data = stream.Read(buffer, 0, client.ReceiveBufferSize);
                string chaine = Encoding.ASCII.GetString(buffer, 0, data);
                string[] values = chaine.Split(',');
                double.TryParse(values[0], out _posX);
                double.TryParse(values[1], out _posY);
            }
            catch (System.IO.IOException e)
            {
                stream.Close();
                client.Close();
                client = server.AcceptTcpClient();
                stream = client.GetStream();
            }
        }
    }
}
