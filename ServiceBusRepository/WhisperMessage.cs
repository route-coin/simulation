using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusRepository
{
    [Serializable()]
    public class WhisperMessage
    {
        public enum State
        {
            CreateContract,
            ContractCreated
        }

        public string BaseStationAddress { get; set; }
        public string ContractAddress { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public State Subject { get; set; }

    }
}
