using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Configuration;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Service
{
    public class MotorService : IMotorService
    {
        private float Id_threshold;
        private float Iq_threshold;
        private float T_threshold;

        public MotorService()
        {
            Id_threshold = float.Parse(ConfigurationManager.AppSettings["Id_threshold"]);
            Iq_threshold = float.Parse(ConfigurationManager.AppSettings["Iq_threshold"]);
            T_threshold = float.Parse(ConfigurationManager.AppSettings["T_threshold"]);

            Console.WriteLine($"Pragovi ucitani: Id={Id_threshold}, Iq={Iq_threshold}, T={T_threshold}");
        }

        public string StartSession(string meta)
        {
            Console.WriteLine($"Sesija zapoceta: {meta}");
            return "ACK - IN_PROGRESS";
        }

        public string PushSample(MotorSample sample)
        {
            // Provera formata
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Uzorak je null!", "sample"));

            // Provera obaveznih polja
            if (sample.Profile_Id <= 0)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Profile_Id je obavezno polje!", "Profile_Id"));

            // Provera vrednosti
            if (sample.Coolant <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Coolant mora biti pozitivan!",
                        "Coolant", sample.Coolant.ToString()));

            if (sample.I_q < -1000 || sample.I_q > 1000)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("I_q je van dozvoljenog opsega!",
                        "I_q", sample.I_q.ToString()));

            if (sample.I_d < -1000 || sample.I_d > 1000)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("I_d je van dozvoljenog opsega!",
                        "I_d", sample.I_d.ToString()));

            Console.WriteLine($"Primljen uzorak: I_q={sample.I_q}, I_d={sample.I_d}, Coolant={sample.Coolant}");
            return "ACK - IN_PROGRESS";
        }

        public string EndSession()
        {
            Console.WriteLine("Sesija zavrsena");
            return "ACK - COMPLETED";
        }
    }
}