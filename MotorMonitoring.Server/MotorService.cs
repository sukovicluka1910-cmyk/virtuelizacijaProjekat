using MotorMonitoring.Contracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MotorMonitoring.Service
{
    public class MotorService : IMotorService
    {
        // Pragovi koji se ucitavaju iz appconfiga
        private float Id_threshold;
        private float Iq_threshold;
        private float T_threshold;
        private string storagePath;  //putanja gdje cuvamo fajlove
        private int totalSamples = 0; //brojac primljenih uzoraka

        // Prethodni uzorci za analitiku
        private float? previousIq = null;
        private float? previousId = null;
        private float? previousCoolant = null;

        // Tekući prosek za Coolant
        private float coolantSum = 0;
        private int coolantCount = 0;

        // Tekući prosek za I_q
        private float iqSum = 0;
        private int iqCount = 0;

        // Tekući prosek za I_d
        private float idSum = 0;
        private int idCount = 0;

        private FileStream measurementsFileStream;
        private FileStream rejectsFileStream;
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;

        // Delegati
        public delegate void TransferStartedHandler(object sender, TransferStartedEventArgs e);
        public delegate void SampleReceivedHandler(object sender, SampleReceivedEventArgs e);
        public delegate void TransferCompletedHandler(object sender, TransferCompletedEventArgs e);
        public delegate void WarningRaisedHandler(object sender, WarningRaisedEventArgs e);
        public delegate void ElectricSpikeQHandler(object sender, ElectricSpikeQEventArgs e);
        public delegate void ElectricSpikeDHandler(object sender, ElectricSpikeDEventArgs e);
        public delegate void TemperatureSpikeHandler(object sender, TemperatureSpikeEventArgs e);
        public delegate void OutOfBandWarningHandler(object sender, OutOfBandWarningEventArgs e);

        // Događaji
        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;
        public event ElectricSpikeQHandler OnElectricSpikeQ;
        public event ElectricSpikeDHandler OnElectricSpikeD;
        public event TemperatureSpikeHandler OnTemperatureSpike;
        public event OutOfBandWarningHandler OnOutOfBandWarning;

        public MotorService()
        {
            Id_threshold = float.Parse(ConfigurationManager.AppSettings["Id_threshold"]);
            Iq_threshold = float.Parse(ConfigurationManager.AppSettings["Iq_threshold"]);
            T_threshold = float.Parse(ConfigurationManager.AppSettings["T_threshold"]);
            storagePath = ConfigurationManager.AppSettings["storagePath"];

            Console.WriteLine($"Pragovi ucitani: Id={Id_threshold}, Iq={Iq_threshold}, T={T_threshold}");

            // Pretplata na događaje, Lambda izraz (sender, e) => → anonimna metoda koja reaguje na događaj
            OnTransferStarted += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Transfer zapocet: {e.Meta} u {e.Time}");

            OnSampleReceived += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Uzorak primljen: I_q={e.Sample.I_q}, I_d={e.Sample.I_d}");

            OnTransferCompleted += (sender, e) =>
                Console.WriteLine($"[DOGADJAJ] Transfer zavrsen! Ukupno uzoraka: {e.TotalSamples} u {e.Time}");

            OnWarningRaised += (sender, e) =>
                Console.WriteLine($"[UPOZORENJE] {e.Message}, Polje: {e.FieldName}, Vrijednost: {e.Value} u {e.Time}");

            OnElectricSpikeQ += (sender, e) =>
                Console.WriteLine($"[ANALITIKA 1] Nagli skok I_q! Delta={e.Delta}, Smjer: {e.Direction} u {e.Time}");

            OnElectricSpikeD += (sender, e) =>
                Console.WriteLine($"[ANALITIKA 1] Nagli skok I_d! Delta={e.Delta}, Smjer: {e.Direction} u {e.Time}");

            OnTemperatureSpike += (sender, e) =>
                Console.WriteLine($"[ANALITIKA 2] Nagli skok temperature! Delta={e.Delta}, Smjer: {e.Direction} u {e.Time}");

            OnOutOfBandWarning += (sender, e) =>
                Console.WriteLine($"[VAN OPSEGA] Trenutna={e.CurrentTemp}, Prosek={e.MeanTemp}, Smjer: {e.Direction} u {e.Time}");
        }

        public string StartSession(string meta)
        {
            // Resetujemo sve vrijednosti na početku svake sesije, važno jer servis može imati više sesija jedna za drugom
            Console.WriteLine($"Sesija zapoceta: {meta}");
            totalSamples = 0;
            previousIq = null;
            previousId = null;
            previousCoolant = null;
            coolantSum = 0;
            coolantCount = 0;
            iqSum = 0;
            iqCount = 0;
            idSum = 0;
            idCount = 0;

            // Fajl za validne uzorke i fajl za nevalidne uzorke
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

            //okini dogadjaj
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

            // Analitika 1 - detekcija naglih promena I_q
            if (previousIq.HasValue)
            {
                float deltaIq = sample.I_q - previousIq.Value;
                if (Math.Abs(deltaIq) > Iq_threshold)
                    OnElectricSpikeQ?.Invoke(this, new ElectricSpikeQEventArgs(deltaIq));
            }
            previousIq = sample.I_q;

            // Analitika 1 - detekcija naglih promena I_d
            if (previousId.HasValue)
            {
                float deltaId = sample.I_d - previousId.Value;
                if (Math.Abs(deltaId) > Id_threshold)
                    OnElectricSpikeD?.Invoke(this, new ElectricSpikeDEventArgs(deltaId));
            }
            previousId = sample.I_d;

            // Analitika 2 - detekcija naglih promena Coolant
            if (previousCoolant.HasValue)
            {
                float deltaCoolant = sample.Coolant - previousCoolant.Value;
                if (Math.Abs(deltaCoolant) > T_threshold)
                    OnTemperatureSpike?.Invoke(this, new TemperatureSpikeEventArgs(deltaCoolant));
            }
            previousCoolant = sample.Coolant;

            // Analitika 2 - tekući prosek Coolant i ±25% odstupanje
            coolantSum += sample.Coolant;
            coolantCount++;
            float coolantMean = coolantSum / coolantCount;
            if (sample.Coolant < 0.75f * coolantMean || sample.Coolant > 1.25f * coolantMean)
                OnOutOfBandWarning?.Invoke(this, new OutOfBandWarningEventArgs(sample.Coolant, coolantMean));

            // Zadatak 1 - tekući prosek I_q i ±25% odstupanje 
            iqSum += sample.I_q;
            iqCount++;
            float iqMean = iqSum / iqCount;
            if (iqCount > 1 && (sample.I_q < 0.75f * iqMean || sample.I_q > 1.25f * iqMean))
                Console.WriteLine($"[±25% I_q] Trenutna={sample.I_q}, Prosek={iqMean}, Odstupanje van opsega!");

            // Zadatak 1 - tekući prosek I_d i ±25% odstupanje
            idSum += sample.I_d;
            idCount++;
            float idMean = idSum / idCount;
            if (idCount > 1 && (sample.I_d < 0.75f * idMean || sample.I_d > 1.25f * idMean))
                Console.WriteLine($"[±25% I_d] Trenutna={sample.I_d}, Prosek={idMean}, Odstupanje van opsega!");

            measurementsWriter?.WriteLine($"{sample.I_q},{sample.I_d},{sample.Coolant},{sample.Profile_Id},{sample.Ambient},{sample.Torque}");
            measurementsWriter?.Flush();

            totalSamples++;

            OnSampleReceived?.Invoke(this, new SampleReceivedEventArgs(sample));

            Console.WriteLine($"Prenos u toku... I_q={sample.I_q}, I_d={sample.I_d}, Coolant={sample.Coolant}");
            return "ACK - IN_PROGRESS";
        }

        public string EndSession()
        {
            Console.WriteLine("Zavrsen prenos!");

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