using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.Host
{
    public class ConfigurationData
    {
        public int MaxCurrentProcess { get; set; }
        /// <summary>
        /// Seconds 
        /// </summary>
        public int SleepDelay { get; set; }
        public string inWorkQueueName { get; set; }
        public string poisonQueue { get; set; }
        public int MaxDequeueCount { get; set; }
        public string ProcessConfigConn { get; set; }
        public bool IsPaused { get; set; }
    }
}
