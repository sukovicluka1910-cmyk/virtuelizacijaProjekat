using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MotorMonitoring.Contracts
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string InvalidValue { get; set; }

        public ValidationFault(string message, string fieldName, string invalidValue)
        {
            Message = message;
            FieldName = fieldName;
            InvalidValue = invalidValue;
        }
    }
}