using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outbox.Application
{
    public abstract class ServiceBusSendable
    {
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid RequestSessionId { get; set; }
        public string Jwt { get; set; }
        public string MoulaApiKey { get; set; }
    }

    public class OutboxTestServiceBusEvent : ServiceBusSendable
    {

    }
}
