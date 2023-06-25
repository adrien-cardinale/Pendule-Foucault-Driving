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
        public Cognex(string ip, int port)
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();
        }

        public void Start(string ? filename)
        {
            thread = new Thread(Listen);
            _run = true;
            thread.Start(filename);
        }

        public void Stop()
        {
            _run = false;
            thread.Join();
        }

        public void Listen(object fileName)
        {
            while (_run)
            {
                ReadData();
                if (fileName != null)
                {
                    using (StreamWriter sw = File.AppendText(fileName.ToString()))
                    {
                        sw.WriteLine("{0},{1}", _posX, _posY);
                    }
                }
            }
        }

        public void ReadData()
        {
            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int data = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string chaine = Encoding.ASCII.GetString(buffer, 0, data);
            string[] values = chaine.Split(',');

            double.TryParse(values[0], out _posX);
            double.TryParse(values[1], out _posY);

            stream.Close();
            client.Close();
        }
    }
}
