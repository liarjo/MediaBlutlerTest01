using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.ServiceBus
{
    public class ServiceBusData
    {
        public string connectionString { get; set; }
        public string topicText { get; set; }
        public string SubscriptionName { get; set; }

    }
}
