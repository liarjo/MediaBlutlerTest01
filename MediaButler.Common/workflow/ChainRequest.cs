using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.workflow
{
    public class ChainRequest
    {
        internal int CurrentStepIndex { get; set; }
        
        /// <summary>
        /// Process Type ID
        /// </summary>
        public string ProcessTypeId { get; set; }
        /// <summary>
        /// Exceptions list in the process steps
        /// </summary>
        public string ProcessInstanceId { get; set; }
        // public List<Exception> Exceptions;
        public List<string> Exceptions;
        public ChainRequest()
        {
            //config = new ChainConfig();
            //context = new ChainContext();
            Exceptions=new List<string>();
            BreakChain = false;
            IsResumeable = false;
        }
       /// <summary>
       /// Flag for Stop the process
       /// </summary>
        public bool BreakChain
        {
            get;
            set;
        }
        public bool IsResumeable { get; set; }
        public string ProcessConfigConn { get; set; }
        /// <summary>
        /// Dispose here all the Request resource, for example TASK running in parallel.
        /// </summary>
        public virtual void DisposeRequest()
        { }
    }

    public  class ProcessRequest: ChainRequest
    {
        public CloudQueueMessage CurrentMessage { get; set; }
        public string ConfigData { get; set; }
        public DateTime TimeStampProcessingStarted { get; set; }
        public List<string> Log { get; set; }
        public ProcessRequest()
        {
            this.Log = new List<string>();
            this.TimeStampProcessingStarted = DateTime.Now;
            MetaData = new Dictionary<string, string>();
        }
        public Dictionary<string, string>  MetaData { get; set; }
       
    }
}
