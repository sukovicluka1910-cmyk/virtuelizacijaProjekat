using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MotorMonitoring.Contracts;

namespace MotorMonitoring.Client
{
    public class CsvReader : IDisposable
    {
        private StreamReader reader;
        private StreamWriter rejectWriter;
        private bool disposed = false;

        public CsvReader(string csvPath, string logPath)
        {
            reader = new StreamReader(csvPath);
            rejectWriter = new StreamWriter(logPath, append: true);
            Console.WriteLine("CSV fajl otvoren!");
        }

        public List<MotorSample> ReadSamples(int count = 100)
        {
            List<MotorSample> samples = new List<MotorSample>();

            // Preskoci header red
            string header = reader.ReadLine();
            Console.WriteLine($"Header: {header}");

            int lineNumber = 0;

            while (!reader.EndOfStream && samples.Count < count)
            {
                lineNumber++;
                string line = reader.ReadLine();

                try
                {
                    MotorSample sample = ParseLine(line);
                    samples.Add(sample);
                }
                catch (Exception ex)
                {
                    // Nevalidan red ide u log
                    rejectWriter.WriteLine($"Red {lineNumber}: {line} -> Greska: {ex.Message}");
                    rejectWriter.Flush();
                    Console.WriteLine($"Nevalidan red {lineNumber} upisan u log!");
                }
            }

            Console.WriteLine($"Ucitano {samples.Count} uzoraka!");
            return samples;
        }

        private MotorSample ParseLine(string line)
        {
            string[] parts = line.Split(',');

            return new MotorSample()
            {
                I_q = float.Parse(parts[0], CultureInfo.InvariantCulture),
                Coolant = float.Parse(parts[1], CultureInfo.InvariantCulture),
                Profile_Id = float.Parse(parts[12], CultureInfo.InvariantCulture),
                I_d = float.Parse(parts[6], CultureInfo.InvariantCulture),
                Ambient = float.Parse(parts[10], CultureInfo.InvariantCulture),
                Torque = float.Parse(parts[11], CultureInfo.InvariantCulture)
            };
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
                    Console.WriteLine("Zatvaranje CSV fajla...");
                    reader?.Close();
                    rejectWriter?.Close();
                    Console.WriteLine("CSV fajl zatvoren!");
                }
                disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}