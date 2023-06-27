using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pendule
{
    internal class CommandePendule
    {
        private Cognex _cognex;
        private Regulateur _regulateur;

        private double _xCenter = -7;
        private double _yCenter = 6;

        private bool _run = true;

        private List<double> _xList ;
        private List<double> _yList ;

        private double _xMin = 0;
        private double _xMax = 0;
        private double _yMin = 0;
        private double _yMax = 0;

        private double _amplitude = 0;

        private int _periodePendule = 8950;
        private int _periodeExcitation;
        private int _rayonDetection = 100;
        private double _vMoy;
        private int _amplitudeNominal = 1100;
        private double _amplitudeExcitation = 0;
        private double _amplitudeExcitationNominal = 0.01;
        private double _kp = 2e-6;

        private bool changeAmplitude = true;


        bool first = true;



        Thread threadTrigerCenter;
        Thread threadComputeData;

        public CommandePendule(Cognex c, Regulateur r) 
        {
            //Console.WriteLine("CommandePendule");
            _cognex = c;
            _regulateur = r;
            _xList = new List<double>();
            _yList = new List<double>();

            _periodeExcitation = (int)_periodePendule / 2;
            //Console.WriteLine("periodePendule = {0}", _periodePendule);
            //Console.WriteLine("amplitude nominal {0}", _amplitudeNominal);
            _vMoy = (double)_amplitudeNominal / _periodePendule;
        }

        public void Start()
        { 
            _regulateur.OpenBus();
            _regulateur.Init();
            threadTrigerCenter = new Thread(TrigerCenter);
            threadComputeData = new Thread(ComputeData);
            _run = true;
            _cognex.Start("data.csv");
            threadTrigerCenter.Start();
            threadComputeData.Start();
        }

        public void Stop()
        {
            _run = false;
            threadTrigerCenter.Join();
            threadComputeData.Join();
            _cognex.Stop();
            _regulateur.CloseBus();
        }   

        private void TrigerCenter()
        {
            int i = 0;
            int err = 0;
            while(_run)
            {
                if (Math.Sqrt(Math.Pow(_cognex.posX - _xCenter, 2) + Math.Pow(_cognex.posY - _yCenter, 2)) < _rayonDetection && _amplitude != 0)
                {
                    if (changeAmplitude)
                    {
                        err = (int)(_amplitudeNominal - _amplitude);
                        _amplitudeExcitation = _amplitudeExcitationNominal + err * _kp;
                        Thread.Sleep((int)(_periodeExcitation - _rayonDetection / _vMoy - 250));
                        _regulateur.StartExcitation(_amplitudeExcitation);
                        Console.WriteLine($"amplitude = {_amplitude}, erreur = {err}, amplitudeExcitation = {_amplitudeExcitation}");
                        changeAmplitude = false;
                    }
                        

                    Thread.Sleep(1000);
                }
            }
            
        }
        
        private void ComputeData()
        {
            while (_run)
            {
                _cognex.ReadData();
                _xList.Add(_cognex.posX);
                _yList.Add(_cognex.posY);
                if(_xList.Count > 200)
                {
                    _xMin = _xList.Min();
                    _xMax = _xList.Max();
                    _yMin = _yList.Min();
                    _yMax = _yList.Max();
                    _xList.Clear();
                    _yList.Clear();
                    _amplitude = Math.Sqrt(Math.Pow(_xMax, 2) + Math.Pow(_yMax, 2));
                    Console.WriteLine($"xMin = {_xMin}, xMax = {_xMax}, yMin = {_yMin}, yMax = {_yMax}, amplitude = {_amplitude}");
                    _regulateur.StopExcitation();
                    changeAmplitude = true;

                }
                Thread.Sleep(30);
            }
        }
        
    }
}
