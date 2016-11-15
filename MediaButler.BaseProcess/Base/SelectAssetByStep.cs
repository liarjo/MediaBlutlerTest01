using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using System.Diagnostics;
using MediaButler.Common.ResourceAccess;
using MediaButler.Common;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace MediaButler.BaseProcess
{
    class SelectAssetByStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            var myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            IjsonKeyValue dotControlData = myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);

            switch (dotControlData.Read(DotControlConfigKeys.SelectAssetByType))
            {
                case "assetid":
                    myRequest.AssetId = dotControlData.Read(DotControlConfigKeys.SelectAssetByValue);
                    break;
                default:
                    var _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
                    string AssetName = dotControlData.Read(DotControlConfigKeys.SelectAssetByValue);
                    IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Name == AssetName).FirstOrDefault();
                    myRequest.AssetId = asset.Id;
                    break;
            }
        }
    }
}
