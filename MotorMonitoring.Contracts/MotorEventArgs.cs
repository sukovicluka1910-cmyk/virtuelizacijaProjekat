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
}