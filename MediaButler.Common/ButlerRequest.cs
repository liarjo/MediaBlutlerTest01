using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common
{
    public class ButlerRequest
    {
        public Guid MessageId { get; set; }
        public string StorageConnectionString { get; set; }
        public string WorkflowName { get; set; }
        public List<string> MezzanineFiles { get; set; }
        public string ControlFileUri { get; set; }
        public string TimeStampUTC { get; set; }
    }
}
