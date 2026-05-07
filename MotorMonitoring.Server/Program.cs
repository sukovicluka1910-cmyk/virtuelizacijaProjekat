using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace MotorMonitoring.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(MotorService));
            host.Open();
            Console.WriteLine("Servis je pokrenut...");
            Console.WriteLine("Pritisnite Enter za zaustavljanje.");
            Console.ReadLine();
            host.Close();
        }
    }
}