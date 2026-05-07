using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace MotorMonitoring.Contracts
{
    [ServiceContract]
    public interface IMotorService
    {
        [OperationContract]
        string StartSession(string meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string PushSample(MotorSample sample);

        [OperationContract]
        string EndSession();
    }
}