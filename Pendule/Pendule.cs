using System;
using System.Collections.Generic;
using System.Text;
using ch.etel.edi.dsa.v40;
using Pendule;

namespace pendule
{
    class Pendule
    {
        static void Main(string[] args)
        {
            Regulateur regulateur = new Regulateur();
            regulateur.OpenBus();
            regulateur.Init();
            regulateur.CloseBus();
            //regulateur.test();
        }
    }
}
