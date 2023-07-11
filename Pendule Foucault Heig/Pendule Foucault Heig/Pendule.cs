using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pendule
{
    internal class ConfigPendule
    {
        public Dictionary<string, string> centre { get; set; }
        public double periode { get; set; }
        public double amplitudeNominal { get; set; }
        public double amplitudeExcitation { get; set; }
        public double rayonDetection { get; set; }
    }
    internal class CommandePendule
    {
        private Cognex _cognex;
        private Regulateur _regulateur;

        private double _xCenter;
        private double _yCenter;

        private bool _run = true;
        private bool _runExcitation = true;

        private List<double> _xList ;
        private List<double> _yList ;

        private double _xMin = 0;
        private double _xMax = 0;
        private double _yMin = 0;
        private double _yMax = 0;

        private double _amplitude = 0;

        private double _periodePendule;
        private double _periodeExcitation;
        private int _rayonDetection;
        private int _amplitudeNominal;
        private double _amplitudeExcitation;

        private bool changeAmplitude = true;
        private bool _excitation = true;

        string _serverPath;

        bool first = true;



        Thread ? threadTrigerCenter;
        Thread ? threadComputeData;
        Thread ? threadReadPosition;
        Thread ? threadReadRegulateur;

        public CommandePendule(Cognex cognex, Regulateur regulateur, string serverPath) 
        {
            _cognex = cognex;
            _regulateur = regulateur;
            _xList = new List<double>();
            _yList = new List<double>();
            _serverPath = serverPath;
            using (StreamReader r = new StreamReader(_serverPath + "configPF.json"))
            {
                if (r == null)
                    Console.WriteLine("Error reading config file");
                string json = r.ReadToEnd();
                var config = JsonConvert.DeserializeObject<ConfigPendule>(json);
                _xCenter = double.Parse(config.centre["x"]);
                _yCenter = double.Parse(config.centre["y"]);
                _periodeExcitation = config.periode;
                _amplitudeNominal = (int)config.amplitudeNominal;
                _amplitudeExcitation = config.amplitudeExcitation;
                _rayonDetection = (int)config.rayonDetection;
                Console.WriteLine($" xCenter: {_xCenter}, yCenter: {_yCenter}, periode: {_periodeExcitation}," +
                    $" amplitudeNominal: {_amplitudeNominal}, amplitudeExcitation: {_amplitudeExcitation}," +
                    $" rayonDetection: {_rayonDetection}");
            }
            _periodeExcitation = _periodePendule / 2;
            threadComputeData = new Thread(ComputeData);
            threadReadPosition = new Thread(ListenPosition);
            threadReadRegulateur = new Thread(ReadRegulateur);
            threadComputeData.Start();
            threadReadPosition.Start();
            threadReadRegulateur.Start();
            _regulateur.OpenBus();
        }

        public void Close()
        {
            Stop();
            _run = false;
            threadReadRegulateur.Join();
            threadReadPosition.Join();
            threadComputeData.Join();
            _regulateur.CloseBus();
        }

        public void Start()
        { 
            changeAmplitude = true;
            _regulateur.Init(_periodeExcitation);
            threadTrigerCenter = new Thread(TrigerCenter);
            _runExcitation = true;
            threadTrigerCenter.Start();
        }

        public void Stop()
        {
            Console.WriteLine("Stop excitation");
            _runExcitation = false;
            _regulateur.StopExcitation();
            if(threadTrigerCenter != null)
                threadTrigerCenter.Join();
        }
        public void SaveData(string fileName)
        {
            threadReadPosition = new Thread(ListenPosition);
            _run = true;
            threadReadPosition.Start(fileName);
        }
        public void StopSaveData()
        {
            _run = false;
            threadReadPosition.Join();
        }
        public void ListenPosition()
        {
            while (_run)
            {
                _cognex.ReadData();
                string fileName = "pFposition-" + DateTime.Now.ToString("yyyy-MM-dd");
                using (StreamWriter sw = File.AppendText(_serverPath + fileName))
                {
                    sw.WriteLine("{0}{1:0000}{2:0000}", DateTime.Now.ToString("HHmmssfff"), _cognex.posX, _cognex.posY);
                }
            }
        }

        public void ReadRegulateur()
        {
            while (_run)
            {
                _regulateur.ReadData();
                string fileName = "pFregulateur-" + DateTime.Now.ToString("yyyy-MM-dd");
                using (StreamWriter sw = File.AppendText(_serverPath + fileName))
                {
                    sw.WriteLine("{0},{1},{2}", DateTime.Now.ToString("HHmmssfff"), _regulateur.current, _regulateur.position);
                }
            }
        }

        private void TrigerCenter()
        {
            while(_runExcitation)
            {
                if (changeAmplitude)
                {
                    if (Math.Sqrt(Math.Pow(_cognex.posX - _xCenter, 2) + Math.Pow(_cognex.posY - _yCenter, 2)) < _rayonDetection && _amplitude != 0)
                    {
                        Thread.Sleep((int)(_periodePendule-250));
                        if(_runExcitation)
                            _regulateur.StartExcitation(_amplitudeExcitation);
                        Console.WriteLine($"start Move");
                        changeAmplitude = false;
                    }
                }
            }
            
        }
        
        private void ComputeData()
        {
            while (_run)
            {
                _xList.Add(_cognex.posX);
                _yList.Add(_cognex.posY);
                if(_xList.Count > 400)
                {
                    _xMin = _xList.Min();
                    _xMax = _xList.Max();
                    _yMin = _yList.Min();
                    _yMax = _yList.Max();
                    _xList.Clear();
                    _yList.Clear();
                    _amplitude = Math.Sqrt(Math.Pow(_xMax - _xCenter, 2) + Math.Pow(_yMax - _yCenter, 2));
                    if (_amplitude > _amplitudeNominal)
                    {
                        _regulateur.StopExcitation();
                        Thread.Sleep(10000); //TODO : attendre la fin du mouvement
                        changeAmplitude = true;
                        _excitation = false;
                    }else if (_amplitude < _amplitudeNominal && !_excitation)
                    {
                        _regulateur.StopExcitation();
                        Thread.Sleep(10000);
                        changeAmplitude = true;
                        _excitation = true;
                    }
                    Console.WriteLine($"amplitude = {_amplitude}, amplitude d'excitation = {_amplitudeExcitation}");

                }
                Thread.Sleep(30);
            }
        }
        
    }
}
