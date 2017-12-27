using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;

namespace MediaButler.BaseProcess.miscellaneous
{
    class ListAssetFilesOnRequestLogStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
   
        public override void HandleExecute (ChainRequest request)
        {
           myRequest = (ButlerProcessRequest)request;
            // _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServiceContext = myRequest.MediaServiceContext();
            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            string recordPattern = "[IAssetFile] /{0}/{1}";
            foreach (IAssetFile xFile in x.AssetFiles)
            {
                myRequest.Log.Add(string.Format(recordPattern,x.Uri.Segments[1], xFile.Name));
            }

        }

        public override void HandleCompensation(ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

        }
    }
}
