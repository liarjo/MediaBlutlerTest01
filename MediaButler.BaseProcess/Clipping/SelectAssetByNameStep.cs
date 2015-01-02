using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Clipping
{

    class AssetClipFilterData
    {
        public string assetName { get; set; }
        public string assetId { get; set; }
        public List<FilterInfo> filterList { get; set; }
    }

    class FilterInfo
    {
        public string filterName { get; set; }
        public string ge { get; set; }
        public string le { get; set; }
    }
    class SelectAssetByNameStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private string readJsonControl()
        {
            string json = null;
            Uri controleFileUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
            string controlFilename = controleFileUri.Segments[2] + controleFileUri.Segments[3] + controleFileUri.Segments[4];
            json = CloudStorageAccount.Parse(myRequest.ProcessConfigConn).CreateCloudBlobClient().GetContainerReference(myRequest.ProcessTypeId).GetBlockBlobReference(controlFilename).DownloadText();
            return json;
        }
        private void SelectMediaAssetbyName()
        {
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            AssetClipFilterData myData = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetClipFilterData>(this.readJsonControl());
            IAsset theAsset =(from m in _MediaServicesContext.Assets select m).Where(m => m.Name == myData.assetName).FirstOrDefault();

            if (theAsset!=null)
            {
                myRequest.AssetId = theAsset.Id;
            }
            else
            {
                string errorTxt = string.Format("[{0}] process Type {1} instance {2} Error Asset Name {3} don't exist", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myData.assetName);
                Trace.TraceError(errorTxt);
                throw new Exception(errorTxt);
            }
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            //Select the asset by name and updtae the request with this AssetID
            SelectMediaAssetbyName();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
    }
}
