﻿using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    internal class BlobStorageManager : MediaButler.Common.ResourceAccess.IButlerStorageManager
    {
        private string storageConn;
        CloudStorageAccount mezzamineStorageAccount;
        CloudBlobClient blobClient;
        CloudTableClient tableClient;
        public BlobStorageManager(string connStr)
        {
            storageConn = connStr;
            mezzamineStorageAccount = CloudStorageAccount.Parse(storageConn);
            blobClient = mezzamineStorageAccount.CreateCloudBlobClient();
            tableClient = mezzamineStorageAccount.CreateCloudTableClient();
        }
        public void DeleteBlobFile(string blobUrl)
        {
            Uri completeUrl = new Uri(blobUrl);
            int segmentIndex = completeUrl.Segments.Count() - 1;
            string containerName = completeUrl.Segments[1];

            string blobName = "";
            for (int i = 2; i <= segmentIndex; i++)
            {
               blobName += Uri.UnescapeDataString(completeUrl.Segments[i]); 
            }
            CloudBlobContainer myContainer = blobClient.GetContainerReference(containerName);

            ICloudBlob blockBlob = myContainer.GetBlockBlobReference(blobName);
          
            blockBlob.Delete();

        }
        public string ReadTextBlob(Uri blobUrl)
        {
            int segmentIndex = blobUrl.Segments.Count() - 1;
            string containerName = blobUrl.Segments[1];

            string blobName = "";
            for (int i = 2; i <= segmentIndex; i++)
            {
               blobName += Uri.UnescapeDataString(blobUrl.Segments[i]); 
            }
            CloudBlobContainer myContainer = blobClient.GetContainerReference(containerName);
            return myContainer.GetBlockBlobReference(blobName).DownloadText();
   

        }
        public string ReadTextBlob(string containerName, string blobName)
        {
            CloudBlobContainer myContainer = blobClient.GetContainerReference(containerName);
            return myContainer.GetBlockBlobReference(blobName).DownloadText();
        }
        public void PersistProcessStatus(ChainRequest request)
        {
            ProcessSnapShot mysh = new ProcessSnapShot(request.ProcessTypeId, request.ProcessInstanceId);
            try
            {
                Newtonsoft.Json.JsonSerializerSettings x = new Newtonsoft.Json.JsonSerializerSettings();
                x.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                mysh.jsonContext = Newtonsoft.Json.JsonConvert.SerializeObject(request, Newtonsoft.Json.Formatting.None, x);

                mysh.CurrentStep = request.CurrentStepIndex;

                PersistProcessStatus(mysh);


            }
            catch (Exception X)
            {
                string txtMessage = string.Format("[{0}] Persist Process Status Error at process {1} instance {2}: error messagase  {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, X.Message);
                Trace.TraceError(txtMessage);
                throw new Exception(txtMessage);
            }
        }
        public void PersistProcessStatus(ProcessSnapShot processSnapshot)
        {  
            try
            {
                CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
                TableOperation insertOperation = TableOperation.InsertOrReplace(processSnapshot);
                table.CreateIfNotExists();
                table.Execute(insertOperation);
            }
            catch (Exception X)
            {
                string txtMessage = string.Format("[{0}] Persist Process Status Error at process {1} instance {2}: error messagase  {3}", this.GetType().FullName, processSnapshot.RowKey,processSnapshot.PartitionKey, X.Message);
                Trace.TraceError(txtMessage);
                throw new Exception(txtMessage);
            }
        }
        public ProcessSnapShot readProcessSanpShot(string processName, string processId)
        {
            ProcessSnapShot aux = null;
            CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
            TableOperation retrieveOperation = TableOperation.Retrieve<ProcessSnapShot>(processName, processId);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                aux = (ProcessSnapShot)retrievedResult.Result;
            }
                return aux;
        }
        public string GetBlobSasUri(string blobUri,int hours)
        {
            var blob = blobClient.GetBlobReferenceFromServer(new Uri(blobUri));
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(hours);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read;
            string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);
            return blob.Uri + sasBlobToken;
        }

        public byte[] ReadBytesBlob(string blobName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference("mediabutlerbin");
            var strm = new MemoryStream();
            var blob = container.GetBlobReference(blobName);
            blob.DownloadToStream(strm);
            byte[] asseblyBytes = strm.ToArray();
            return asseblyBytes;
        }
        public IjsonKeyValue GetDotControlData(string URL)
        {
            string jsonControlFile = null;
            if (!string.IsNullOrEmpty(URL))
            {
                jsonControlFile = ReadTextBlob(new Uri(URL));
            }
            if (string.IsNullOrEmpty(jsonControlFile))
            {
                jsonControlFile = "{}";
                Trace.TraceInformation("Dot Control File has not content");
            }
            return new jsonKeyValue(jsonControlFile);
        }
        public IjsonKeyValue GetProcessConfig(string dotControlUrl,string ProcessTypeId)
        {
            string jsonControlFile = null;
            if (!string.IsNullOrEmpty(dotControlUrl))
            {
                jsonControlFile = ReadTextBlob(new Uri(dotControlUrl));
            }
            if (string.IsNullOrEmpty(jsonControlFile))
            {
                jsonControlFile = "{}";
                Trace.TraceInformation("Dot Control File has not content");
            }
            string jsonProcessFile = GetButlerConfigurationValue(ProcessConfigKeys.DefualtPartitionKey, ProcessTypeId + ".config");

            return new ProcessConfiguration(jsonControlFile, jsonProcessFile);
        }
        public string GetButlerConfigurationValue(string partition, string row)
        {
            string config = "";
            try
            {
                config = MediaButler.Common.Configuration.GetConfigurationValue(row, partition, storageConn);
            }
            catch (Exception)
            {
                string txt = string.Format("Tryto read {0} but it is not in configuration table! at {1} ", row, DateTime.Now.ToString());
                Trace.TraceWarning(txt);
            }
            return config;
        }

        public void CopyBlob(CloudBlockBlob OrigenBlob, CloudBlobContainer TargetContainer, string blobName)
        {
            CloudBlockBlob TargetBlob = TargetContainer.GetBlockBlobReference(blobName);
            string blobToken = OrigenBlob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTime.Now.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTime.Now.AddMinutes(5)
                });

            TargetBlob.StartCopy(new Uri(OrigenBlob.Uri.AbsoluteUri + blobToken));

            CloudBlockBlob blobStatusCheck;

            blobStatusCheck = (CloudBlockBlob)TargetContainer.GetBlobReferenceFromServer(TargetBlob.Name);

            while (blobStatusCheck.CopyState.Status == CopyStatus.Pending)
            {
                Task.Delay(TimeSpan.FromSeconds(10d)).Wait();

                blobStatusCheck = (CloudBlockBlob)TargetContainer.GetBlobReferenceFromServer(TargetBlob.Name);
            }

            if (blobStatusCheck.CopyState.Status != CopyStatus.Success)
            {
                throw new Exception("Error Copying " + TargetBlob.Name + " Copy Status" + blobStatusCheck.CopyState.Status);
            }

        }
       
    }
}
