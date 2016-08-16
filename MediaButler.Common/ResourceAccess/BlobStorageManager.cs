using MediaButler.Common.workflow;
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


        public string ReadTextBlob(string blobUrl)
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
            //json = CloudStorageAccount.Parse(myRequest.ProcessConfigConn).CreateCloudBlobClient().GetContainerReference(myRequest.ProcessTypeId).GetBlockBlobReference(controlFilename).DownloadText();
            string data = myContainer.GetBlockBlobReference(blobName).DownloadText();
            return data;

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

        public void parkingNewBinaries()
        {
            CloudBlobContainer container = blobClient.GetContainerReference("mediabutlerbin");
            foreach (IListBlobItem dll in container.ListBlobs(null, false))
            {
                Uri myUri = dll.Uri;
                int seg = myUri.Segments.Length - 1;
                string name = myUri.Segments[seg];
                if (!File.Exists(@".\" + name))
                {
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);
                    using (var fileStream = System.IO.File.OpenWrite(@".\" + name))
                    {
                        blockBlob.DownloadToStream(fileStream);
                    }
                }
            }
        }
    }
}
