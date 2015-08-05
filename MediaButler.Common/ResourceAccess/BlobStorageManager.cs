using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
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
            return myContainer.GetBlockBlobReference(blobName).DownloadText();

        }
    }
}
