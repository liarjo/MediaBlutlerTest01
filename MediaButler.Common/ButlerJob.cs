using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common
{
    public class ButlerJob
    {
        public ButlerJob()
        {
            JobId = Guid.NewGuid();
        }
        public Guid JobId { get; set; }
        public IList<Uri> JobMediaFiles { get; set; }
        public Uri JobControlFile { get; set; }

        public Configuration.WorkflowStatus Status { get; set; }

        /// <summary>
        /// contains information to dump in the .log file
        /// </summary>
        public string Information { get; set; }

        /// <summary>
        /// Simple job is indicated by a lack of control file
        /// </summary>
        public bool IsSimpleJob { 
            get { return JobControlFile == null;  } 
        }
    }
}
