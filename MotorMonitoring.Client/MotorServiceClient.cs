using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Client
{
    public class MotorServiceClient : IDisposable
    {
        private ChannelFactory<IMotorService> factory;
        private IMotorService proxy;
        private bool disposed = false;

        public MotorServiceClient()
        {
            factory = new ChannelFactory<IMotorService>("MotorService");
            proxy = factory.CreateChannel();
            Console.WriteLine("Konekcija otvorena!");
        }

        public string StartSession(string meta)
        {
            return proxy.StartSession(meta);
        }

        public string PushSample(MotorSample sample)
        {
            return proxy.PushSample(sample);
        }

        public string EndSession()
        {
            return proxy.EndSession();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Console.WriteLine("Zatvaranje konekcije...");
                    factory.Close();
                    Console.WriteLine("Konekcija zatvorena!");
                }
                disposed = true;
            }
        }

        ~MotorServiceClient()
        {
            Dispose(false);
        }
    }
}