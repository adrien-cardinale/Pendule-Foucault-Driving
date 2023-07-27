using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Pendule
{
    internal class VisionSystem
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
        private string _ip;
        private int _port;
        private bool _run = true;
        public VisionSystem(string ip, int port)
        {
            _ip = ip;
            _port = port;
            Thread threadReadData = new Thread(new ThreadStart(ReadData));
            threadReadData.Start();

        }
        private void ReadData()
        {
            TcpListener _server = new TcpListener(IPAddress.Parse(_ip), _port);
            try
            {
                _server.Start();
            }
            catch (SocketException e)
            {
                throw new Exception("Error starting vision server");
            }   
            TcpClient _client = _server.AcceptTcpClient();
            NetworkStream _stream = _client.GetStream();
            while (_run)
            {
                try
                {
                    byte[] buffer = new byte[_client.ReceiveBufferSize];

                    int data = _stream.Read(buffer, 0, _client.ReceiveBufferSize);
                    string chaine = Encoding.ASCII.GetString(buffer, 0, data);
                    string[] values = chaine.Split(',');
                    double.TryParse(values[0], out _posX);
                    double.TryParse(values[1], out _posY);
                }
                catch (System.IO.IOException e)
                {
                    _stream.Close();
                    _client.Close();
                    _client = _server.AcceptTcpClient();
                    _stream = _client.GetStream();
                }
            }
        }
    }
}
