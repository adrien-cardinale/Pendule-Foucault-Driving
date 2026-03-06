using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Timers;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Pendule
{
    internal class PFConfig
    {
        public PFConfig(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                if (r == null)
                {
                    throw new Exception("Error reading config file");
                }

                string json = r.ReadToEnd();
                var pFConfig = JsonConvert.DeserializeObject<PFConfig>(json);
                if (pFConfig == null)
                {
                    throw new Exception("Error deserializing config file");
                }
                center = pFConfig.center;
                periode = pFConfig.periode;
                nominalAmplitude = pFConfig.nominalAmplitude;
                excitationAmplitude = pFConfig.excitationAmplitude;
                startPosition = pFConfig.startPosition;
                KpAmplitude = pFConfig.KpAmplitude;
                offsetDetection = pFConfig.offsetDetection;
                detectionRadius = pFConfig.detectionRadius;
                offsetAmplitudeFactor = pFConfig.offsetAmplitudeFactor;
                visionIP = pFConfig.visionIP;
                visionPort = pFConfig.visionPort;
                driverIP = pFConfig.driverIP;
            }
        }
        public PFConfig()
        {
            center = new Dictionary<string, int>();
            center.Add("x", 0);
            center.Add("y", 0);
            periode = 0;
            nominalAmplitude = 0;
            excitationAmplitude = 0;
            startPosition = 0;
            KpAmplitude = 0;
            offsetDetection = 0;
            detectionRadius = 0;
            offsetAmplitudeFactor = 0;
        }   

        public Dictionary<string, int> center 
        {
            get; set;
        }
        public double periode 
        {
            get; set;    
        }
        public double nominalAmplitude
        {
            get; set;
        }
        public double excitationAmplitude
        {
            get; set;
        }
        public double startPosition
        {
            get; set;
        }
        public double KpAmplitude
        {
            get; set;
        }
        public double offsetDetection
        {
            get; set;
        }
        public double detectionRadius
        {
            get; set;
        }
        public double offsetAmplitudeFactor
        {
            get; set;
        }
        public string visionIP
        {
            get; set;
        }
        public int visionPort
        {
            get; set;
        }
        public string driverIP
        {
            get; set;
        }
    }

    internal class PFControl
    {
        private VisionSystem _cognex;
        private Driver _driver;
        private PFConfig _config;
        private MqttLogger _mqttLogger;


        private bool _run = true;
        public bool RunExcitation
        {
            get
            {
                return _runExcitation;
            }
        }
        private bool _runExcitation = false;

        private double _amplitude = 0;

        private double _excitationPeriode;

        private bool _excitation = false;

        private string _serverPath;


        private double _phaseError = 0;
        private double _amplitudeError
        {
            get
            {
                return _config.nominalAmplitude - _amplitude;
            }
        }


        private List<long> _periodeMeasured = new List<long>();


        Thread ? threadTrigerCenter;
        Thread ? threadComputeData;
        //Thread ? threadListenPosition;
        Thread ? threadListenDriver;
        Thread ? threadRegulation;

        public PFControl(string serverPath) 
        {
            _serverPath = serverPath;

            DotNetEnv.Env.Load();
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            string host = config["Mqtt:Host"] ?? throw new InvalidOperationException("Variable 'Mqtt:Host' manquante");
            string username = config["Mqtt:Username"] ?? throw new InvalidOperationException("Variable 'Mqtt:Username' manquante");
            string password = config["Mqtt:Password"] ?? throw new InvalidOperationException("Variable 'Mqtt:Password' manquante");
            int port = int.TryParse(config["Mqtt:Port"], out var p) ? p : 1883;

            Console.WriteLine($"Host     : '{config["Mqtt:Host"]}'");
            Console.WriteLine($"Port     : '{config["Mqtt:Port"]}'");
            Console.WriteLine($"Username : '{config["Mqtt:Username"]}'");
            Console.WriteLine($"Password : '{config["Mqtt:Password"]}'");

            _mqttLogger = new MqttLogger(
                broker: config["Mqtt:Host"]!,
                port: int.Parse(config["Mqtt:Port"]!),
                topic: "foucault",
                username: config["Mqtt:Username"]!,
                password: config["Mqtt:Password"]!
            );

            _config = new PFConfig((string)(_serverPath + "configPF.json"));
            _cognex = new VisionSystem(_config.visionIP, _config.visionPort, (VisionSystem.ListenPositionCallBack)ListenPosition);
            _driver = new Driver(_config.driverIP);

            _excitationPeriode = _config.periode / 2;
            threadComputeData = new Thread(ComputeData);
            //threadListenPosition = new Thread(ListenPosition);
            threadListenDriver = new Thread(ListenDriver);
            threadComputeData.Start();
            //threadListenPosition.Start();
            threadListenDriver.Start();
            _driver.OpenBus();
        }

        public async Task Init()
        {
            await _mqttLogger.ConnectAsync();
        }

        public void ReloadConfig()
        {
            _config = new PFConfig((string)(_serverPath + "configPF.json"));
            _excitationPeriode = _config.periode / 2;
            if (_runExcitation)
            {
                Stop();
                Start();
            }
        }

        public void Close()
        {
            Stop();
            _run = false;
            threadListenDriver.Join();
            //threadListenPosition.Join();
            threadComputeData.Join();
            try
            {
                _driver.CloseBus();
            }
            catch
            {
                Console.WriteLine("Erreur lors de la fermeture du driver");
            }
        }

        public void Start()
        { 
            try
            {
                _driver.Init(_excitationPeriode, _config.startPosition);
            }
            catch (DriverErrorException e)
            {
                Console.WriteLine($"Erreur lors de l'initialisation du driver : {e.Message}");
                Stop();
                Start();
                return;
            }
            threadRegulation = new Thread(Regulation);
            _runExcitation = true;
            threadRegulation.Start();
        }

        public void Stop()
        {
            _runExcitation = false;
            if(_excitation)
            {
                Console.WriteLine("Stop excitation");
                try
                {
                    _driver.StopExcitation();
                }
                catch (DriverErrorException e)
                {
                    Console.WriteLine($"Erreur lors de l'arrêt de l'excitation : {e.Message}");
                    return;
                }
                finally
                {
                    _excitation = false;
                }
            }
            if(threadRegulation != null)
                threadRegulation.Join();
        }
        public void SetSinus()
        {
            try
            {
                _driver.SetSinus(_excitationPeriode, 1);
            }
            catch (DriverErrorException e)
            {
                Console.WriteLine($"Erreur lors de la mise en place du sinus : {e.Message}");
                return;
            }
        }
        private void ListenPosition(double x, double y)
        {
            string fileName = "pFposition-" + DateTime.Now.ToString("yyyy-MM-dd");
            try
            {
                using (StreamWriter sw = File.AppendText(_serverPath + fileName))
                {
                    sw.WriteLine("{0}{1:0000}{2:0000}", DateTime.Now.ToString("HHmmssfff"), x, y);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de l'écriture du fichier{fileName} : {e.Message}");
            }
            try
            {
                if (_mqttLogger.IsConnected())
                {
                    _mqttLogger.Publish($"{{\"timestamp\":{DateTime.Now.ToString("HH:mm:ss:ffff")}, \"position_x\":{x}, \"position_y\":{y}}}", "data/position");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la publication MQTT {e.Message}");
            }
            Thread.Sleep(5);
        }
        
        private void ListenDriver()
        {
            while (true)
            {
                try
                {
                    _driver.ReadData();
                }
                catch (DriverErrorException e)
                {
                    Console.WriteLine($"Erreur lors de la lecture du driver : {e.Message}");
                    if (_runExcitation)
                    {
                        Stop();
                        Start();
                    }
                }
                string fileName = "pFregulateur-" + DateTime.Now.ToString("yyyy-MM-dd");
                try
                {
                    using (StreamWriter sw = File.AppendText(_serverPath + fileName))
                    {
                        sw.WriteLine("{0},{1},{2}", DateTime.Now.ToString("HHmmssfff"), _driver.current, _driver.position);
                        Thread.Sleep(50);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erreur lors de l'écriture du fichier{fileName} : {e.Message}");
                }
                try
                {
                    if (_mqttLogger.IsConnected())
                    {
                        _mqttLogger.Publish($"{{\"timestamp\":{DateTime.Now.ToString("HH:mm:ss:ffff")}, \"current\":{_driver.current}, \"position\":{_driver.position}}}", "data/driver");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erreur lors de la publication MQTT {e.Message}");
                }
            }
        }

        private void Regulation()
        {
            double aOld = 0;
            int i = 0;
            List<double> phaseErrorList = new List<double>();
            Console.WriteLine("Start regulation");
            while (_runExcitation)
            {
                i++;
                WaitCenter();
                if (!_excitation & _runExcitation)
                {
                    Thread.Sleep((int)(_config.periode + _config.offsetDetection));
                    if (_runExcitation)
                    {
                        try
                        {
                            _driver.StartExcitation(_config.excitationAmplitude, _excitationPeriode);
                            _excitation = true;
                            Console.WriteLine($"start Move");
                        }
                        catch
                        {
                            Console.WriteLine("Erreur lors du démarrage de l'excitation");
                        }
                    }
                    _excitation = true;
                    continue;
                }



                phaseErrorList.Add(_phaseError);

                //Console.WriteLine($"Amplitude error: {_amplitudeError}");
                if (i >= 5)
                {
                    double a = _config.offsetAmplitudeFactor + _amplitudeError * _config.KpAmplitude;
                    if (a > 1)
                        a = 1;
                    if (a < 0)
                        a = 0;
                    Console.WriteLine($"phase error average: {phaseErrorList.Average()*1000}");
                    if (aOld != a)
                    {
                        Console.WriteLine($"Amplitude régulation: {a}");
                        _driver.SetSinus(_excitationPeriode, a);
                        aOld = a;
                    }
                    if (phaseErrorList.Average() > 0.005 | phaseErrorList.Average() < -0.005)
                    {
                        _driver.StopExcitation();
                        _excitation = false;
                    }
                    phaseErrorList.Clear();
                    i = 0;
                }


                Thread.Sleep(1000);
            }
                
        }

        private void WaitCenter()
        {
            while (Math.Sqrt(Math.Pow(_cognex.posX - _config.center["x"], 2) + Math.Pow(_cognex.posY - _config.center["y"], 2)) > _config.detectionRadius)
            {

            }
            _phaseError = _driver.position - _config.startPosition;
            return;
        }
        
        private void ComputeData()
        {
            double xMin = 0;
            double xMax = 0;
            double yMin = 0;
            double yMax = 0;
            List<double> _xList = new List<double>();
            List<double> _yList = new List<double>();
            int i = 0;
            while (_run)
            {
                _xList.Add(_cognex.posX);
                _yList.Add(_cognex.posY);
                if(_xList.Count > 400)
                {
                    xMin = _xList.Min();
                    xMax = _xList.Max();
                    yMin = _yList.Min();
                    yMax = _yList.Max();
                    _xList.Clear();
                    _yList.Clear();
                    _amplitude = Math.Sqrt(Math.Pow(xMax - _config.center["x"], 2) + Math.Pow(yMax - _config.center["y"], 2));
                    Console.WriteLine($"amplitude = {_amplitude}, amplitude d'excitation = {_config.excitationAmplitude}");
                    Console.WriteLine($"phase error = {_phaseError * 1000}");
                    Console.WriteLine($"amplitude error = {_amplitudeError}");
                    try
                    {
                        if (_mqttLogger.IsConnected())
                        {
                            _mqttLogger.Publish($"{{\"timestamp\":{DateTime.Now.ToString("HH:mm:ss")}, \"amplitude\":{_amplitude}}}", "data/amplitude");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Erreur lors de la publication MQTT {e.Message}");
                    }
                    i++;
                }
                Thread.Sleep(30);
            }
        }
        
    }
}
