using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using System.Diagnostics;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace MediaButler.BaseProcess.Clipping
{
    class AddAssetFilterXmlStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager myBlobManager;
        private CloudMediaContext _MediaServicesContext;

        public override void HandleCompensation(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
       
        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            // _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);

            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            //Asset 
            CloudStorageAccount assetStorageCount = CloudStorageAccount.Parse(myRequest.MediaStorageConn);
            CloudBlobClient assetClient = assetStorageCount.CreateCloudBlobClient();
            CloudBlobContainer assetContainer = assetClient.GetContainerReference(asset.Uri.Segments[1]);

            //Stage Storage
            CloudStorageAccount MezzamineStorageCount = CloudStorageAccount.Parse(myRequest.ButlerRequest.StorageConnectionString);
            CloudBlobClient MezzamineClient = MezzamineStorageCount.CreateCloudBlobClient();
            CloudBlobContainer MezzamineContainer = MezzamineClient.GetContainerReference(myRequest.ButlerRequest.WorkflowName);
            string filterXmlFileName = "_azuremediaservices.config";
            string filterXmlBlobName = string.Format("Processing/{0}/{1}",myRequest.ProcessInstanceId, filterXmlFileName);
            CloudBlockBlob azureMediaServicesConfig = MezzamineContainer.GetBlockBlobReference(filterXmlBlobName);
            myBlobManager.CopyBlob(azureMediaServicesConfig, assetContainer, filterXmlFileName);
        }
    }
}