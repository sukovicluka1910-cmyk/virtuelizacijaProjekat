using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Configuration;
using System.Threading;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string csvPath = ConfigurationManager.AppSettings["csvPath"];
            string logPath = ConfigurationManager.AppSettings["logPath"];

            try
            {
                using (CsvReader csvReader = new CsvReader(csvPath, logPath))
                using (MotorServiceClient client = new MotorServiceClient())
                {
                    // Ucitaj prvih 100 uzoraka
                    var samples = csvReader.ReadSamples(100);

                    // Pocetak sesije
                    string result = client.StartSession("Sesija_01");
                    Console.WriteLine(result);

                    // Salji uzorke jedan po jedan
                    foreach (var sample in samples)
                    {
                        result = client.PushSample(sample);
                        Console.WriteLine($"Prenos u toku... {result}");

                        // Kašnjenje 500ms između uzoraka - simulacija realnog vremena
                        Thread.Sleep(500);
                    }

                    // Kraj sesije
                    result = client.EndSession();
                    Console.WriteLine($"Zavrsen prenos! {result}");
                }
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault: {ex.Detail.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }

            Console.WriteLine("Pritisnite Enter za izlaz.");
            Console.ReadLine();
        }
    }
}