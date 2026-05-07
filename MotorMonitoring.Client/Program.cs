using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IMotorService> factory =
                new ChannelFactory<IMotorService>("MotorService");

            IMotorService proxy = factory.CreateChannel();

            try
            {
                // Pocetak sesije
                string result = proxy.StartSession("TestSesija");
                Console.WriteLine(result);

                // Test sa ispravnim uzorkom
                MotorSample sample = new MotorSample()
                {
                    I_q = 10.5f,
                    I_d = 5.2f,
                    Coolant = 25.0f,
                    Profile_Id = 1,
                    Ambient = 20.0f,
                    Torque = 15.0f
                };

                result = proxy.PushSample(sample);
                Console.WriteLine(result);

                // Kraj sesije
                result = proxy.EndSession();
                Console.WriteLine(result);
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault greska: {ex.Detail.Message}, Polje: {ex.Detail.FieldName}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault greska: {ex.Detail.Message}, Polje: {ex.Detail.FieldName}, Vrednost: {ex.Detail.InvalidValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }
            finally
            {
                factory.Close();
            }

            Console.WriteLine("Pritisnite Enter za izlaz.");
            Console.ReadLine();
        }
    }
}