using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Replica
{
   
    class ProcessReplicaStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private ProcessReplicaData myConfig;
        
        private Hashtable triggerConfig;
        private CloudMediaContext triggerMediaContext;
        private IAsset triggerAsset;
        private CloudStorageAccount triggerStorageAccount;

        private CloudMediaContext targetMediaContext;
        private IAsset targetAsset;
        private CloudStorageAccount targetStorageAccount;

        private void Setup()
        {
            //Trigger Info Setup
            triggerConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<Hashtable>(this.LoadTriggerConfig());
            triggerMediaContext = new CloudMediaContext(triggerConfig["MediaAccountName"].ToString(), triggerConfig["MediaAccountKey"].ToString());
            triggerAsset = triggerMediaContext.Assets.Where(xx => xx.Id == triggerConfig["AssetId"].ToString()).FirstOrDefault();
            triggerStorageAccount = CloudStorageAccount.Parse(triggerConfig["MediaStorageConn"].ToString()); 
            
            //Target Info Setup
            if (!string.IsNullOrEmpty(this.StepConfiguration))
            {
                throw new Exception("ProcessReplicaStep has not configutarion!"); 
            }
            else
            {
                myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessReplicaData>(this.StepConfiguration);
            }
            targetMediaContext = new CloudMediaContext(myConfig.TargetAMSName, myConfig.TargetAMSKey);
            targetStorageAccount = CloudStorageAccount.Parse(myConfig.TargetAMSStorageConn);
        }
        /// <summary>
        /// Load Trigger information from .control File
        /// </summary>
        /// <returns>json hashtable</returns>
        private string LoadTriggerConfig()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(myRequest.ProcessTypeId);

            Uri controlUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
            string controlBlobName = "";
            for (int i = 2; i <= controlUri.Segments.Count() - 1; i++)
            {
                controlBlobName += controlUri.Segments[i];
            }
            controlBlobName = Uri.UnescapeDataString(controlBlobName);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(controlBlobName);

            string jsonTriggerConfig;
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                jsonTriggerConfig = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            return jsonTriggerConfig;
        }
      /// <summary>
      /// Copy Blob from Origin to Target Container
      /// </summary>
      /// <param name="OriginContainer"></param>
      /// <param name="OriginBlobURL"></param>
      /// <param name="DestContainer"></param>
      /// <param name="DestBlobURL"></param>
        private void startCopyBlob(CloudBlobContainer OriginContainer, string OriginBlobURL, CloudBlobContainer DestContainer, string DestBlobURL)
        {
            //Destination
            Uri destinUri = new Uri(DestBlobURL);
            string destinBlobName = Uri.UnescapeDataString(destinUri.Segments[destinUri.Segments.Count() - 1]);
            CloudBlockBlob destinBlockBloc = DestContainer.GetBlockBlobReference(destinBlobName);
            DestContainer.SetPermissions(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    }
               );

            //Origin
            Uri originUri = new Uri(OriginBlobURL);
            string originBlobName = Uri.UnescapeDataString(originUri.Segments[originUri.Segments.Count() - 1]);
            CloudBlockBlob originBlockBlob = OriginContainer.GetBlockBlobReference(originBlobName);
            var sas = OriginContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7),
                Permissions = SharedAccessBlobPermissions.Read,
            });

            var srcBlockBlobSasUri = string.Format("{0}{1}", Uri.UnescapeDataString(originBlockBlob.Uri.AbsoluteUri), sas);
            //Start Copy
            destinBlockBloc.StartCopyFromBlob(new Uri(srcBlockBlobSasUri));
               
        }
        private void ProcessReplica()
        {
            //1. Create Target Asset
            targetAsset = targetMediaContext.Assets.Create(triggerAsset.Name, AssetCreationOptions.None);
            IAccessPolicy writePolicy = targetMediaContext.AccessPolicies.Create("writePolicy_" + targetAsset.Name, TimeSpan.FromMinutes(120), AccessPermissions.Write);
            ILocator destinationLocator = targetMediaContext.Locators.CreateLocator(LocatorType.Sas, targetAsset, writePolicy);
  

            //2. Copy each File fron Origin to Target
            //2.1 Target Asset Storage
            
            CloudBlobClient targetAssetClient = targetStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer targetAssetContainer = targetAssetClient.GetContainerReference(targetAsset.Uri.Segments[1]);
           
            //2.2 Trigger (origen) Asset Storage
            CloudBlobClient triggerAssetClient = triggerStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer triggerAssetContainer = triggerAssetClient.GetContainerReference(triggerAsset.Uri.Segments[1]);
            
            //3. Start Copy all Files
            foreach (IAssetFile triggerAssetFile in triggerAsset.AssetFiles)
            {
                //if (triggerAssetFile.Name.ToLower().EndsWith(".mp4"))
                {
                    string triggerAssetFileUrl = triggerAsset.Uri.AbsoluteUri + "/" + triggerAssetFile.Name;
                    string targetAssetFileUrl = targetAsset.Uri.AbsoluteUri + "/" + triggerAssetFile.Name;
                    startCopyBlob(triggerAssetContainer, triggerAssetFileUrl, targetAssetContainer, targetAssetFileUrl);
                }
            }

            //4. Wait all copy process finish
            CloudBlockBlob blobStatusCheck;
            bool waitLighTrafic = true;
            while (waitLighTrafic)
            {
                waitLighTrafic = false;
                foreach (IAssetFile triggerAssetFile in triggerAsset.AssetFiles)
                {
                   // if (triggerAssetFile.Name.ToLower().EndsWith(".mp4"))
                    {
                        blobStatusCheck = (CloudBlockBlob)targetAssetContainer.GetBlobReferenceFromServer(triggerAssetFile.Name);
                        Trace.TraceInformation("{0} copy {1} bytes of {2} Bytes", triggerAssetFile.Name, blobStatusCheck.CopyState.BytesCopied, blobStatusCheck.CopyState.TotalBytes);
                        waitLighTrafic = (blobStatusCheck.CopyState.Status == CopyStatus.Pending);
                    }
                }
                Task.Delay(TimeSpan.FromSeconds(10d)).Wait();
            }

            foreach (IAssetFile triggerAssetFile in triggerAsset.AssetFiles)
            {
                //if (triggerAssetFile.Name.ToLower().EndsWith(".mp4"))
                {
                    //Add the xFile to Asset
                    var assetFile = targetAsset.AssetFiles.Create(triggerAssetFile.Name);
                }
            }

            destinationLocator.Delete();
            writePolicy.Delete();

            targetAsset.Update();
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            ProcessReplica();
            //Update Default Asset ID
            myRequest.AssetId = targetAsset.Id;
            //Change defualt AMS to Target AMS for the rest of the process
            myRequest.ChangeMediaServices(myConfig.TargetAMSName,myConfig.TargetAMSKey,myConfig.TargetAMSStorageConn);
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            if(targetAsset!=null)
            {
                string txtTrace = string.Format("[{0}] process Type {1} instance {2} Compensation deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, targetAsset.Id);
                targetAsset.Delete();
                Trace.TraceWarning(txtTrace);
            }
        }
    }
}
