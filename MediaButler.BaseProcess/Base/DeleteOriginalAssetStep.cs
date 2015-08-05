using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class DeleteOriginalAssetStep:MediaButler.Common.workflow.StepHandler
    {
        ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;

        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
             myRequest = (ButlerProcessRequest)request;
            _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);

            ////Get transcode asset
            //Lista all media parent from current Asset
            IAsset myAsset = (from m in _MediaServiceContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            foreach (IAsset xParent in myAsset.ParentAssets)
            {
                if (xParent.Name.Contains(myRequest.ProcessInstanceId))
                {
                    //Delete parent asset becouse it is part of the Media blutler process instance
                    xParent.Delete();
                }
            }

          }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
