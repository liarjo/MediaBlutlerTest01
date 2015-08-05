using MediaButler.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class DeleteOriginalBlobStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            IBlobStorageManager resource = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            foreach (string url in myRequest.ButlerRequest.MezzanineFiles)
            {
                resource.DeleteBlobFile(url);
                Trace.TraceInformation("{0} in process {1} processId {2} file {3} deleted", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId,url);
            }

            if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
            {
                resource.DeleteBlobFile(myRequest.ButlerRequest.ControlFileUri);
                Trace.TraceInformation("{0} in process {1} processId {2} file {3} deleted", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myRequest.ButlerRequest.ControlFileUri);
            }
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

        }
    }
}
