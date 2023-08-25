using ch.etel.edi.dsa.v40;
using ch.etel.edi.dmd.v40;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Runtime;

namespace Pendule
{
    public class DriverErrorException : Exception
    {
        public DriverErrorException(string message) : base(message)
        {
        }
    }
    internal class Driver
    {
        private DsaDrive drv;
        private DsaAcquisition acq;
        private int nb_points;
        private double[] times;
        private double[] data;

        private bool _run = false;

        public double current
        {
            get
            {
                return _current;
            }
        }
        private double _current;

        public double position
        {
            get
            {
                return _position;
            }
        }
        private double _position;

        public double amplitude { get; set; }
        public double phase { get; set; }

        public Driver()
        {
            drv = new DsaDrive();
        }
        public void OpenBus()
        {
            try
            {
                Console.WriteLine("Open controller");
                drv.open("etb:etn://192.168.125.207:0");
            }
            catch (DsaException exc)
            {
                error(exc);
                throw new DriverErrorException("Error during open bus");
            }

        }
        public void CloseBus() 
        {
            try
            {
                Console.WriteLine("Power off");
                drv.powerOff();
                Console.WriteLine("Close controller");
                drv.close();
            }
            catch (DsaException exc)
            {
                error(exc);
                throw new DriverErrorException("Error during close bus");
            }
        }
        public void Init(double periodeExcitation, double startPosition)
        {
            try
            {
                int periode = drv.convertInt32FromIso(periodeExcitation / 1000, Dsa.PpkConv(204, 0));
                drv.setRegister(DmdData.TYP_PPK, 204, 0, periode);
                int position = drv.convertInt32FromIso(startPosition, Dsa.MonConv(6, 0));
                drv.setRegister(DmdData.TYP_USER, 1, 0, position);
                Thread.Sleep(500);
                if (drv.getStatusFromDrive().isHomingDone())
                {
                    drv.executeSequence(2); // go to start position
                }
                else
                {
                    drv.executeSequence(0); // init sequence
                    Thread.Sleep(500);
                    drv.waitMovement(60000);
                    int err = drv.getErrorCode();
                    if (err != 0)
                        throw new Exception($"Error during homing error code = {drv.getErrorText}");
                }
            }
            catch (DsaException exc)
            {
                error(exc);
                throw new DriverErrorException("Error during init");
            }
        }
        public void StartExcitation(double amplitude, double periodeExcitation)
        {
            try
            {
                int amp = drv.convertInt32FromIso(amplitude, Dsa.MonConv(6, 0));
                drv.setRegister(DmdData.TYP_USER, 0, 0, amp);
                drv.executeSequence(1); // excitation sequence
            }
            catch (DsaException exc)
            {
                error(exc);
                throw new DriverErrorException("Error during start excitation");
            }
        }
        public void StopExcitation()
        {
            try
            {
                int error = drv.getErrorCode();
                if (error == 0)
                {
                    drv.executeSequence(3);
                    drv.waitMovement(60000);
                }
            }
            catch (DsaException exc)
            {
                error(exc);
                throw new DriverErrorException("Error during stop excitation");
            }
        }
        public void StartTestExcitation(double frequency)
        {
            Console.WriteLine("Start test excitation");
            _run = true;
            Task.Run(() => TestExcitation(frequency));
        }
        private void TestExcitation(double frequency)
        {
            Console.WriteLine($"Test excitation {frequency}");
            long delay = 50;
            long x = 0;
            Stopwatch stopwatch = new Stopwatch();
            drv.setRegister(DmdData.TYP_USER, DmdParameters.CONCATENATED_MVT, 0, 1);
            drv.setRegister(DmdData.TYP_PPK, 202, 0, 1);
            long speedInc = drv.convertInt64FromIso(0.05, Dsa.KLConv(211, 0));
            drv.setRegisterInt64(DmdData.TYP_PPK_INT64, 211, 0, speedInc);
            long accInc = drv.convertInt64FromIso(0.05, Dsa.KLConv(212, 0));
            drv.setRegisterInt64(DmdData.TYP_PPK_INT64, 212, 0, accInc);
            while (_run)
            {
                stopwatch.Restart();
                double y = amplitude * Math.Sin(2 * Math.PI * frequency * x/1000 + phase * Math.PI / 180);
                x += delay;
                Console.WriteLine($"x {x} y {y}");
                drv.executeCommand(DmdCommand.PROFILED_MOVE, DmdData.TYP_IMMEDIATE_INT64, y, Dsa.KLConv(210, 0), true, true);
                Thread.Sleep((int)(delay - stopwatch.ElapsedMilliseconds));
            }
        }
        public void StopTestExcitation()
        {
            _run = false;
        }
        public void SetSinus(double T, double amplitude)
        {
            Console.WriteLine($"Set sinus T {T} amplitude {amplitude}");
            int N = 8191;
            double frequency = 1 / T;
            double[] t = new double[N+1];
            double step = T / N;
            for (int i = 0; i <= N; i++)
            {
                t[i] = i * step;
            }
            for(int i = 0; i <= N; i++)
            {
                float value = (float)(amplitude*Math.Sin(2 * Math.PI * frequency * t[i]));
                //Console.WriteLine($"value {value}");
                try
                {
                    drv.setRegisterFloat64(DmdData.TYP_LKT_FLOAT64, i, 0, value);
                }
                catch (DsaException exc)
                {
                    //error(exc);
                    //throw new DriverErrorException("Error during set sinus");
                    Console.WriteLine($"Error during set sinus {exc.Message}");
                }
                
            }
            Console.WriteLine("Set sinus");
        }
        public void Acquisition()
        {
            Console.WriteLine("Acquisition");
            acq = new DsaAcquisition(drv);
            acq.reserve();

            acq.configTrace(drv, 0, DmdData.TYP_MONITOR, 7, 0);
            acq.configImmediateTrigger(drv);
            acq.configWithSamplingTimeAndTotalTime(0.03, 1800, DsaAcquisition.SYNCHRO_MODE_NONE);
            nb_points = acq.getRealNbPoints(drv, 0);
            times = new double[nb_points];
            data = new double[nb_points];
            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            acq.acquire(-1);

            acq.uploadTrace(drv, 0, times, data, Dsa.MonConv(6, 0));

            acq.unreserve();

            StreamWriter sw = new StreamWriter("excitation.txt");
            sw.WriteLine(timeNow);
            for (int i = 0; i < nb_points; i++)
            {
                sw.WriteLine("{0:f3} ,{1:f5};", times[i], data[i]);
            }
            sw.Close();
            Console.WriteLine("Acquisition done");
        }
        public void ReadData()
        {
            try
            {
                int positionIncr = drv.getRegister(DmdData.TYP_MONITOR, 7, 0, Dsa.GET_CURRENT);
                _position = drv.convertInt32ToIso(positionIncr, Dsa.MonConv(7, 0));
                float currentIncr = drv.getRegisterFloat32(DmdData.TYP_MONITOR_FLOAT32, 31, 0, Dsa.GET_CURRENT);
                _current = drv.convertFloat32ToIso(currentIncr, Dsa.RegConv(DmdData.TYP_MONITOR_FLOAT32, 31, 0));
                int error = drv.getErrorCode();
                if (error != 0)
                    throw new DriverErrorException($"Driver error {error}, {drv.getErrorText(error)}");
                Thread.Sleep(30);
            }
            catch(DsaException exc)
            {

            }
        }
        private void error(DsaException exc)
        {
            exc.diag(drv);
            if (drv.isOpen())
            {
                if (drv.getStatus().isMoving())
                {
                    drv.quickStop(Dsa.QS_PROGRAMMED_DEC, Dsa.QS_BYPASS | Dsa.QS_STOP_SEQUENCE);
                    drv.waitMovement(60000);
                }
                if (drv.getStatus().isPowerOn())
                    drv.powerOff(60000);
                drv.close();
            }
            OpenBus();
            return;
        }
    }
}
