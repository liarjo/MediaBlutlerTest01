using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common
{
    public class ButlerResponse
    {
        public ButlerResponse()
        {
            TimeStampProcessingCompleted = String.Format("{0:o}", DateTime.Now.ToUniversalTime());
        }
        public Guid MessageId { get; set; }
        public string StorageConnectionString { get; set; }
        public string WorkflowName { get; set; }
        public List<string> MezzanineFiles { get; set; }
        public string Log { get; set; }

        public string TimeStampRequestSubmitted { get; set; }

        public string TimeStampProcessingStarted { get; set; }
        public string TimeStampProcessingCompleted { get; set; }

    }
}
