using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Configuration;
using System.IO;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Service
{
    public class MotorService : IMotorService
    {
        private float Id_threshold;
        private float Iq_threshold;
        private float T_threshold;
        private string storagePath;
        private int totalSamples = 0;

        private FileStream measurementsFileStream;
        private FileStream rejectsFileStream;
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;

        // Delegati
        public delegate void TransferStartedHandler(object sender, TransferStartedEventArgs e);
        public delegate void SampleReceivedHandler(object sender, SampleReceivedEventArgs e);
        public delegate void TransferCompletedHandler(object sender, TransferCompletedEventArgs e);
        public delegate void WarningRaisedHandler(object sender, WarningRaisedEventArgs e);

        // Događaji
        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;

        public MotorService()
        {
            Id_threshold = float.Parse(ConfigurationManager.AppSettings["Id_threshold"]);
            Iq_threshold = float.Parse(ConfigurationManager.AppSettings["Iq_threshold"]);
            T_threshold = float.Parse(ConfigurationManager.AppSettings["T_threshold"]);
            storagePath = ConfigurationManager.AppSettings["storagePath"];

            Console.WriteLine($"Pragovi ucitani: Id={Id_threshold}, Iq={Iq_threshold}, T={T_threshold}");

            // Pretplata na događaje
            OnTransferStarted += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Transfer zapocet: {e.Meta} u {e.Time}");

            OnSampleReceived += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Uzorak primljen: I_q={e.Sample.I_q}, I_d={e.Sample.I_d}");

            OnTransferCompleted += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Transfer zavrsen! Ukupno uzoraka: {e.TotalSamples} u {e.Time}");

            OnWarningRaised += (sender, e) =>
                Console.WriteLine($"[UPOZORENJE] {e.Message}, Polje: {e.FieldName}, Vrijednost: {e.Value} u {e.Time}");
        }

        public string StartSession(string meta)
        {
            Console.WriteLine($"Sesija zapoceta: {meta}");
            totalSamples = 0;

            string measurementsPath = Path.Combine(storagePath, "measurements_session.csv");
            string rejectsPath = Path.Combine(storagePath, "rejects.csv");

            measurementsFileStream = new FileStream(measurementsPath, FileMode.Create, FileAccess.Write);
            measurementsWriter = new StreamWriter(measurementsFileStream);

            rejectsFileStream = new FileStream(rejectsPath, FileMode.Create, FileAccess.Write);
            rejectsWriter = new StreamWriter(rejectsFileStream);

            measurementsWriter.WriteLine("I_q,I_d,Coolant,Profile_Id,Ambient,Torque");
            measurementsWriter.Flush();

            Console.WriteLine("Fajlovi kreirani!");
            Console.WriteLine("Prenos podataka zapocet...");

            // Okini događaj
            OnTransferStarted?.Invoke(this, new TransferStartedEventArgs(meta));

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
            {
                rejectsWriter?.WriteLine($"{sample.I_q},{sample.I_d},{sample.Coolant},{sample.Profile_Id},{sample.Ambient},{sample.Torque}");
                rejectsWriter?.Flush();
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Coolant mora biti pozitivan!",
                        "Coolant", sample.Coolant.ToString()));
            }

            if (sample.I_q < -1000 || sample.I_q > 1000)
            {
                rejectsWriter?.WriteLine($"{sample.I_q},{sample.I_d},{sample.Coolant},{sample.Profile_Id},{sample.Ambient},{sample.Torque}");
                rejectsWriter?.Flush();
                throw new FaultException<ValidationFault>(
                    new ValidationFault("I_q je van dozvoljenog opsega!",
                        "I_q", sample.I_q.ToString()));
            }

            // Provjera pragova - upozorenja
            if (Math.Abs(sample.I_q) > Iq_threshold)
                OnWarningRaised?.Invoke(this, new WarningRaisedEventArgs(
                    "I_q prekoracuje prag!", "I_q", sample.I_q));

            if (Math.Abs(sample.I_d) > Id_threshold)
                OnWarningRaised?.Invoke(this, new WarningRaisedEventArgs(
                    "I_d prekoracuje prag!", "I_d", sample.I_d));

            if (Math.Abs(sample.Coolant) > T_threshold)
                OnWarningRaised?.Invoke(this, new WarningRaisedEventArgs(
                    "Coolant prekoracuje prag!", "Coolant", sample.Coolant));

            measurementsWriter?.WriteLine($"{sample.I_q},{sample.I_d},{sample.Coolant},{sample.Profile_Id},{sample.Ambient},{sample.Torque}");
            measurementsWriter?.Flush();

            totalSamples++;

            // Okini događaj
            OnSampleReceived?.Invoke(this, new SampleReceivedEventArgs(sample));

            Console.WriteLine($"Prenos u toku... I_q={sample.I_q}, I_d={sample.I_d}, Coolant={sample.Coolant}");
            return "ACK - IN_PROGRESS";
        }

        public string EndSession()
        {
            Console.WriteLine("Zavrsen prenos!");

            // Okini događaj
            OnTransferCompleted?.Invoke(this, new TransferCompletedEventArgs(totalSamples));

            measurementsWriter?.Close();
            measurementsFileStream?.Close();
            rejectsWriter?.Close();
            rejectsFileStream?.Close();

            Console.WriteLine("Fajlovi zatvoreni!");
            return "ACK - COMPLETED";
        }
    }
}