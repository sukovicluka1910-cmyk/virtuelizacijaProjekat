using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MotorMonitoring.Contracts
{
    [DataContract]
    public class MotorSample
    {
        [DataMember]
        public float I_q { get; set; }

        [DataMember]
        public float I_d { get; set; }

        [DataMember]
        public float Coolant { get; set; }

        [DataMember]
        public float Profile_Id { get; set; }

        [DataMember]
        public float Ambient { get; set; }

        [DataMember]
        public float Torque { get; set; }
    }
}