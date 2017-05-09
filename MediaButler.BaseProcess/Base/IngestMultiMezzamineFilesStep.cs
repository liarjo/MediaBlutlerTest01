﻿using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MediaButler.BaseProcess
{
    class IngestMultiMezzamineFilesStep : MediaButler.Common.workflow.StepHandler
    {
        private string extensionFilter = ".mp4";
        ButlerProcessRequest myRequest;
        CloudMediaContext MediaContext;
        IAsset currentAsset = null;
        IjsonKeyValue dotControlData = null;

        /// <summary>
        /// Create empty Asset
        /// </summary>
        /// <returns></returns>
        private IAsset CreateAsset()
        {
            string AssetNameSeed = null;
            //If control file exist, this will be namre  of asset
            // second option first MP4
            //third option first file in Mezzamine list
            Uri MezzamineFileUri;
            if(!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
            {
                //Control file 
                MezzamineFileUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
                //Is asset Name on control?
                AssetNameSeed=dotControlData.Read(DotControlConfigKeys.AssetNameSeed);
            } 
            else
            {
                string videoFileUrl = myRequest.ButlerRequest.MezzanineFiles.Where(af => af.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (!string.IsNullOrEmpty(videoFileUrl))
                {
                    //MP4
                    MezzamineFileUri = new Uri(videoFileUrl);
                }
                else
                {
                    //first file
                    MezzamineFileUri = new Uri(myRequest.ButlerRequest.MezzanineFiles.FirstOrDefault());
                }
            }

            int segmentscount = MezzamineFileUri.Segments.Count()-1;
            
            if (string.IsNullOrEmpty(AssetNameSeed))
            {
                AssetNameSeed = Uri.UnescapeDataString(MezzamineFileUri.Segments[segmentscount]);
            }

            string assetName =
                string.Format("{0}_{1}_Butler_{2}",
                    myRequest.ButlerRequest.WorkflowName,
                    AssetNameSeed,
                    myRequest.ProcessInstanceId);
           
            IAsset  myAsset = MediaContext.Assets.Create(assetName, AssetCreationOptions.None);
              
            return myAsset;
        }
        /// <summary>
        /// Ingest all Mezzamine files to Asset
        /// </summary>
        private void IngestAssets()
        {
            IAccessPolicy writePolicy = MediaContext.AccessPolicies.Create("writePolicy_" + currentAsset.Name, TimeSpan.FromMinutes(120), AccessPermissions.Write);
            ILocator destinationLocator = MediaContext.Locators.CreateLocator(LocatorType.Sas, currentAsset, writePolicy);
  
            //Asset Storage
            CloudStorageAccount assetStorageCount = CloudStorageAccount.Parse(myRequest.MediaStorageConn);
            CloudBlobClient assetClient = assetStorageCount.CreateCloudBlobClient();
            CloudBlobContainer assetContainer = assetClient.GetContainerReference(currentAsset.Uri.Segments[1]);

            //Mezzamine Storage
            CloudStorageAccount MezzamineStorageCount = CloudStorageAccount.Parse(myRequest.ButlerRequest.StorageConnectionString);
            CloudBlobClient MezzamineClient = MezzamineStorageCount.CreateCloudBlobClient();
            CloudBlobContainer MezzamineContainer = MezzamineClient.GetContainerReference(myRequest.ButlerRequest.WorkflowName);

            //Filter Ingest extensionFilter 
            foreach (string  urlMezzamineFile in myRequest.ButlerRequest.MezzanineFiles.Where(mf=>mf.ToLower().EndsWith(extensionFilter)))
            {
                Uri xFile = new Uri(urlMezzamineFile);
                int segmentIndex = xFile.Segments.Count() - 1;
                //Asset BLOB Xfile
                string AssetBlobName = Uri.UnescapeDataString(xFile.Segments[segmentIndex]);
                CloudBlockBlob assetBlob = assetContainer.GetBlockBlobReference(AssetBlobName);

                assetContainer.SetPermissions(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    }
                );

                //Mezzamine BLOB Xfile
                string MezzamineBlobName = "";
                for (int i = 2; i <= segmentIndex; i++)
                {
                    MezzamineBlobName += xFile.Segments[i];
                }
               
                MezzamineBlobName = Uri.UnescapeDataString(MezzamineBlobName);
                CloudBlockBlob MezzamineBlob = MezzamineContainer.GetBlockBlobReference(MezzamineBlobName);
               
                var sas = MezzamineContainer.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7),
                    Permissions = SharedAccessBlobPermissions.Read,
                });

                //USE decode URL for spetial characters
                var srcBlockBlobSasUri = string.Format("{0}{1}",Uri.UnescapeDataString(MezzamineBlob.Uri.AbsoluteUri), sas);
                //TODO: upgrade
                assetBlob.StartCopyFromBlob(new Uri(srcBlockBlobSasUri));
                
                
                Trace.TraceInformation("{0} in process {1} processId {2} Start copy  MezzamineFile {3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, MezzamineBlobName);

                CloudBlockBlob blobStatusCheck;
                blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(AssetBlobName);
                while (blobStatusCheck.CopyState.Status == CopyStatus.Pending)
                {
                    Task.Delay(TimeSpan.FromSeconds(10d)).Wait();

                    Trace.TraceInformation("{0} in process {1} processId {2} copying MezzamineFile {3} status {4}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, MezzamineBlobName, blobStatusCheck.CopyState.Status);

                    blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(AssetBlobName);
                }
                
                assetBlob.FetchAttributes();
                //Add the xFile to Asset
                var assetFile = currentAsset.AssetFiles.Create(AssetBlobName);
                MezzamineBlob.FetchAttributes();
                assetFile.ContentFileSize = MezzamineBlob.Properties.Length;
                assetFile.Update();

                Trace.TraceInformation("{0} in process {1} processId {2} finish MezzamineFile {3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, MezzamineBlobName);
                
            }
            destinationLocator.Delete();
            writePolicy.Delete();

            currentAsset.Update();

        }
        private bool IdenpotenceControl()
        {
           bool aux = true;
           if (myRequest.MetaData.ContainsKey(this.GetType() + "_" + myRequest.ProcessInstanceId))
            {
                string assetId=myRequest.MetaData[this.GetType() + "_" + myRequest.ProcessInstanceId];
                aux = (null== (from m in MediaContext.Assets select m).Where(m => m.Id == assetId).FirstOrDefault());
                //Delete mark if asset don't exist
                if (aux)
                {
                    myRequest.MetaData.Remove(this.GetType() + "_" + myRequest.ProcessInstanceId);
                }
            }
            return aux;
        }
        private void setPrimaryFile()
        {
            string myPrimaryFile = null;
            IAssetFile videoFile=null;
            
            if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
            {
                //
                IButlerStorageManager resource = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
                string jsonControl = resource.ReadTextBlob(new Uri(myRequest.ButlerRequest.ControlFileUri));
                if (!string.IsNullOrEmpty(jsonControl))
                {
                    IjsonKeyValue myControl = new jsonKeyValue(jsonControl);
                    myPrimaryFile = myControl.Read(DotControlConfigKeys.IngestMultiMezzamineFilesPrimaryFile);
                }
            }
            
            IEncoderSupport myEncodigSupport = new EncoderSupport(MediaContext);
            if (!string.IsNullOrEmpty(myPrimaryFile))
            {
                videoFile = currentAsset.AssetFiles.Where(f => f.Name.ToLower()==myPrimaryFile.ToLower()).FirstOrDefault();
                
            }
            if (videoFile==null)
           {
                videoFile = currentAsset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(extensionFilter)).FirstOrDefault();
            }
            if (videoFile != null)
            {
                myEncodigSupport.SetPrimaryFile(currentAsset, videoFile);
            }
            else
            {
                Trace.TraceWarning("{0} setPrimaryFile {2} processId {1}, has not {3} file", this.GetType().FullName, myRequest.ProcessInstanceId, myRequest.ProcessTypeId, extensionFilter);
            }
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            //Custome Request
            myRequest = (ButlerProcessRequest)request;
            //IjsonKeyValue control configuration
            dotControlData = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn).GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);
            
            //Video Filter
            if (dotControlData.Read(DotControlConfigKeys.VideoFileExtension)!="")
                extensionFilter = dotControlData.Read(DotControlConfigKeys.VideoFileExtension);

            //Media context 
            MediaContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            if (IdenpotenceControl())
            {
                //Create empty asset
                currentAsset = CreateAsset();
                //Update Asset Id in the process context
                myRequest.AssetId = currentAsset.Id;
                //Ingest all Mezzamine File to asset
                IngestAssets();
                //Set MP4 as primary
                setPrimaryFile();
                //mark for idenpotence
                myRequest.MetaData.Add(this.GetType() + "_" + myRequest.ProcessInstanceId, myRequest.AssetId);
            }
            else
            {
                string txtMessage = string.Format("{0} IdenpotenceControl {1} instanceID {2} was trigger, not ingest Files",this.GetType(),myRequest.ProcessTypeId,myRequest.ProcessInstanceId);
                Trace.TraceInformation(txtMessage);
                myRequest.AssetId = myRequest.MetaData[this.GetType() + "_" + myRequest.ProcessInstanceId];
            }
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
           
            if (currentAsset != null)
            {
                string id = currentAsset.Id;
                currentAsset.Delete();
                Trace.TraceWarning("{0} HandleCompensation in process {2} processId {1}, with asset deletion assetid {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId,id);
            }
            else
            {
                Trace.TraceWarning("{0} HandleCompensation in process {2} processId {1}, without asset deletion", this.GetType().FullName,request.ProcessInstanceId,request.ProcessTypeId);
            }
        }
    }
}
