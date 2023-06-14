using ch.etel.edi.dsa.v40;
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
        public Regulateur()
        {
            drv = null;
            drv = new DsaDrive();
            Console.WriteLine("Regulateur: Regulateur created");
        }
        public void OpenBus()
        {
            try
            {
                Console.WriteLine("Open controller");
                drv.open("etb:etn://192.168.125.207:0");
                Console.WriteLine("Power on");
                drv.resetErrorEx(0, 10000);
                drv.powerOn(10000);
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
            drv.resetErrorEx(0, 10000);
            drv.powerOn(10000);

            drv.homingStart(10000);
            drv.waitMovement(60000);
            drv.setProfileVelocity(0, 0.0014);
            drv.setProfileAcceleration(0, 0.0004398);
            
            drv.setJerkTime(0, 2.5);
            drv.setTargetPosition(0, 0);
            
            drv.waitMovement(60000);
            drv.powerOff();
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
        public void test()
        {
                double pos_min, pos_max;

                DsaDrive drv = null;
                try
                {
                    drv = new DsaDrive();
                    Console.WriteLine("Open controller");
                    drv.open("etb:etn://192.168.125.207:0");

                    Console.WriteLine("Power on");
                    drv.resetErrorEx(0, 10000);
                    drv.powerOn(10000);

                    Console.WriteLine("Home");
                    drv.homingStart(10000);

                    pos_min = drv.getMinSoftPositionLimit();
                    pos_max = drv.getMaxSoftPositionLimit();


                    drv.waitMovement(60000);

                    drv.setProfileVelocity(0, 1.0);

                    drv.setProfileAcceleration(0, 1.0);

                    Console.WriteLine("Move to " + (pos_min * 0.95 + pos_max * 0.05));
                    drv.setTargetPosition(0, pos_min * 0.95 + pos_max * 0.05);
                    drv.waitMovement(60000);

                    Console.WriteLine("Move to " + (pos_min * 0.05 + pos_max * 0.95));
                    drv.setTargetPosition(0, pos_min * 0.05 + pos_max * 0.95);
                    drv.waitMovement(60000);

                    Console.WriteLine("Power off");
                    drv.powerOff();

                    drv.close();
                }
                catch (DsaException exc)
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
                    Console.WriteLine("Ended with Error...Press a key");
                    Console.ReadLine();
                    return;
                }

                Console.WriteLine("Ended with Success...Press a key");
                Console.ReadLine();
            }
    }
}
