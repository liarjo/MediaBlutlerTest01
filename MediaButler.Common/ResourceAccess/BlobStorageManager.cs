using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    internal class BlobStorageManager : MediaButler.Common.ResourceAccess.IBlobStorageManager
    {
        private string storageConn;
        CloudStorageAccount mezzamineStorageAccount;
        CloudBlobClient blobClient;
        public BlobStorageManager(string connStr)
        {
            storageConn = connStr;
            mezzamineStorageAccount = CloudStorageAccount.Parse(storageConn);
            blobClient = mezzamineStorageAccount.CreateCloudBlobClient();

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
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(request.ProcessConfigConn);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
                TableOperation insertOperation = TableOperation.InsertOrReplace(mysh);
                table.CreateIfNotExists();
                table.Execute(insertOperation);
            }
            catch (Exception X)
            {
                string txtMessage = string.Format("[{0}] Persist Process Status Error at process {1} instance {2}: error messagase  {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, X.Message);
                Trace.TraceError(txtMessage);
                throw new Exception(txtMessage);
            }
        }
    }
}
