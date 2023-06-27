using ch.etel.edi.dsa.v40;
using ch.etel.edi.dmd.v40;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pendule
{
    internal class Regulateur
    {
        private DsaDrive drv;
        private DsaAcquisition acq;
        private int nb_points;
        private double[] times;
        private double[] data;

        public Regulateur()
        {
            drv = new DsaDrive();
        }
        public void OpenBus()
        {
            try
            {
                Console.WriteLine("Open controller");
                drv.open("etb:etn://192.168.125.207:0");
                //Console.WriteLine("Power on");
                //drv.resetErrorEx(0, 10000);
                //drv.powerOn(10000);
            }
            catch (DsaException exc)
            {
                error(exc);
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
            }
        }
        public void Init()
        {
            try
            {
                drv.executeSequence(0);
                Thread.Sleep(500);
                while (drv.getStatus().isMoving())
                {
                    Thread.Sleep(500);
                }
                int err = drv.getErrorCode();
                if (err != 0)
                {
                    Console.WriteLine("Error code: " + drv.getErrorText(err));
                }
                if (err == 11)
                {
                    //Console.WriteLine("Seq 3");
                    //drv.executeSequence(3);
                    //drv.waitMovement(60000);
                    //Console.WriteLine("Seq 0");
                    //drv.executeSequence(0);
                }
            }
            catch (DsaException exc)
            {
                error(exc);
            }
        }
        public void StartExcitation(double amplitude)
        {
            try
            {
                int amp = drv.convertInt32FromIso(amplitude, Dsa.MonConv(6, 0));
                drv.setRegister(DmdData.TYP_USER, 0, 0, amp);
                drv.executeSequence(4);
            }
            catch (DsaException exc)
            {
                error(exc);
            }
        }
        public void StopExcitation()
        {
            try
            {
                //drv.executeCommand(cmd: DmdCommand.PROFILED_MOVE, typ1: DmdData.TYP_IMMEDIATE, par1: 0, Dsa.CONV_AUTO, true, false);
                drv.executeSequence(2);
            }
            catch (DsaException exc)
            {
                error(exc);
            }
        }   
        private void error(DsaException exc)
        {
            exc.diag(drv);
            if (drv.isOpen())
            {
                /// We can check if a motor is moving by reading the status of the drive. 
                if (drv.getStatus().isMoving())
                {
                    /// The drive is moving => 
                    /// Stop it immediately (Dsa.QS_BYPASS)
                    /// Stopping its sequence (Dsa.QS_STOP_SEQUENCE)
                    /// With programmed deceleration (Dsa.QS_PROGRAMMED_DEC)
                    drv.quickStop(Dsa.QS_PROGRAMMED_DEC, Dsa.QS_BYPASS | Dsa.QS_STOP_SEQUENCE);
                    ///Wait that motor is stopped
                    drv.waitMovement(60000);
                }

                /// We can check if motor is powered on by reading the status of the drive. 
                if (drv.getStatus().isPowerOn())
                    /// The drive is powered on => 
                    /// Power it off
                    drv.powerOff(60000);

                /// Close the connection.
                drv.close();
            }
            Console.WriteLine("Ended with Error...Press a key");
            Console.ReadLine();
            return;
        }
        public void SetSinus(double T, double phase)
        {
            Console.WriteLine("Set sinus");
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
                float value = (float)(Math.Sin(2 * Math.PI * frequency * t[i] + phase));
                Console.WriteLine($"value {value}");
                drv.setRegisterFloat64(DmdData.TYP_LKT_FLOAT64, i, 0, value);

            }
            Console.WriteLine("Sinus set");
        }
        public void Acquisition()
        {
            Console.WriteLine("Acquisition");
            acq = new DsaAcquisition(drv);
            acq.reserve();

            acq.configTrace(drv, 0, DmdData.TYP_MONITOR, 7, 0);
            acq.configImmediateTrigger(drv);
            acq.configWithNbPointsAndTotalTime(1001, 10, DsaAcquisition.SYNCHRO_MODE_NONE);
            nb_points = acq.getRealNbPoints(drv, 0);
            times = new double[nb_points];
            data = new double[nb_points];
            Console.WriteLine($"Nb points {nb_points}");

            acq.acquire(-1);

            acq.uploadTrace(drv, 0, times, data, Dsa.MonConv(6, 0));
            Console.WriteLine($"{times}");

            acq.unreserve();

            Console.WriteLine("Acquisition done");
            StreamWriter sw = new StreamWriter("excitation.txt");
            for (int i = 0; i < nb_points; i++)
            {
                sw.WriteLine("{0:f3} ;{1:f5};", times[i], data[i]);
            }
            sw.Close();
        }
    }
}
