using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorMonitoring.Contracts
{
    // Događaj kada sesija počne
    public class TransferStartedEventArgs : EventArgs
    {
        public string Meta { get; set; }
        public DateTime Time { get; set; }

        public TransferStartedEventArgs(string meta)
        {
            Meta = meta;
            Time = DateTime.Now;
        }
    }

    // Događaj kada uzorak primljen
    public class SampleReceivedEventArgs : EventArgs
    {
        public MotorSample Sample { get; set; }
        public DateTime Time { get; set; }

        public SampleReceivedEventArgs(MotorSample sample)
        {
            Sample = sample;
            Time = DateTime.Now;
        }
    }

    // Događaj kada sesija završi
    public class TransferCompletedEventArgs : EventArgs
    {
        public int TotalSamples { get; set; }
        public DateTime Time { get; set; }

        public TransferCompletedEventArgs(int totalSamples)
        {
            TotalSamples = totalSamples;
            Time = DateTime.Now;
        }
    }

    // Događaj kada upozorenje
    public class WarningRaisedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string FieldName { get; set; }
        public float Value { get; set; }
        public DateTime Time { get; set; }

        public WarningRaisedEventArgs(string message, string fieldName, float value)
        {
            Message = message;
            FieldName = fieldName;
            Value = value;
            Time = DateTime.Now;
        }
    }

    // Događaj za nagli skok struje Q komponente
    public class ElectricSpikeQEventArgs : EventArgs
    {
        public float Delta { get; set; }
        public string Direction { get; set; }
        public DateTime Time { get; set; }

        public ElectricSpikeQEventArgs(float delta)
        {
            Delta = delta;
            Direction = delta > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
            Time = DateTime.Now;
        }
    }

    // Događaj za nagli skok struje D komponente
    public class ElectricSpikeDEventArgs : EventArgs
    {
        public float Delta { get; set; }
        public string Direction { get; set; }
        public DateTime Time { get; set; }

        public ElectricSpikeDEventArgs(float delta)
        {
            Delta = delta;
            Direction = delta > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
            Time = DateTime.Now;
        }
    }

    // Događaj za nagli skok temperature rashladne tečnosti
    public class TemperatureSpikeEventArgs : EventArgs
    {
        public float Delta { get; set; }
        public string Direction { get; set; }
        public DateTime Time { get; set; }

        public TemperatureSpikeEventArgs(float delta)
        {
            Delta = delta;
            Direction = delta > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
            Time = DateTime.Now;
        }
    }

    // Događaj kada temperatura odstupa od tekućeg proseka ±25%
    public class OutOfBandWarningEventArgs : EventArgs
    {
        public float CurrentTemp { get; set; }
        public float MeanTemp { get; set; }
        public string Direction { get; set; }
        public DateTime Time { get; set; }

        public OutOfBandWarningEventArgs(float currentTemp, float meanTemp)
        {
            CurrentTemp = currentTemp;
            MeanTemp = meanTemp;
            Direction = currentTemp > meanTemp ? "iznad ocekivane vrijednosti" : "ispod ocekivane vrijednosti";
            Time = DateTime.Now;
        }
    }
}