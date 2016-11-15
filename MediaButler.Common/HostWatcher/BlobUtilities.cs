using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.Common.HostWatcher
{
    public static partial class BlobUtilities
    {
        /// <summary>
        /// Extract the path portion for the Storage URI -- e.g. from 
        /// https://myaccount.blob.core.windows.net/myContainer/myFolder/myblob.dat
        /// to
        /// /myFolder/myblob.dat
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ExtractBlobPath(IListBlobItem b)
        {
            // remove the storage prefix + the container from the URI
            var parts = b.Uri.LocalPath.Split('/'); // parts[1] container parts[2] folder parts[>2] blobfile
            var pathPortion = String.Join("/", parts, 2, parts.Length - 2);

            return pathPortion;
        }

        /// <summary>
        /// Adjust the path portion (between the container and the basename
        /// parts of the name) to the new path. E.g. from    
        /// https://myaccount.blob.core.windows.net/myContainer/myFolder/myblob.dat
        /// to
        /// /myFolderNew/myblob.dat
        /// </summary>
        /// <param name="b">The source blob</param>
        /// <param name="newPath">The new path portion</param>
        /// <returns>The adjusted URI string.</returns>
        public static string AdjustPath(IListBlobItem b, string newPath)
        {
            // TODO: add timestamp adding management
            // remove the storage prefix + the container from the URI
            var parts = b.Uri.LocalPath.Split('/'); // parts[1] container parts[2] folder parts[>2] blobfile
            var pathPortion = newPath + "/" + String.Join("/", parts, 3, parts.Length - 3);

            return pathPortion;
        }

        //TODO: Move to utilities
        /// <summary>
        /// Synchronous rename of the specified blob to a new name, within the same container.
        /// </summary>
        /// <remarks>
        /// Note that Azure blobs do not really have a rename operation so the 
        /// implemenation has to be to copy the blob and then delete the original.
        /// </remarks>
        /// <param name="container">The container of the target and destination</param>
        /// <param name="sourcePath">The portion of the blob name within the container. 
        /// e.g. /Incoming/fred.mp4</param>
        /// <param name="targetPath">The path portion of the target blob within the container.
        /// e.g., /Processing/fred.mp4</param>
        /// <returns>true if the operation succeeds.</returns>
        public static bool RenameBlobWithinContainer(CloudBlobContainer container, string sourcePath, string targetPath)
        {
            // CloudBlobContainer container = blobClient.GetContainerReference(blobContainer);
            //TODO: block is okay since WAMS only supports block
            CloudBlockBlob newBlob = container.GetBlockBlobReference(targetPath);
            CloudBlockBlob sourceBlob = container.GetBlockBlobReference(sourcePath);
            //var monitorStr = newBlob.StartCopyFromBlob(sourceBlob);
            var monitorStr = newBlob.StartCopy(sourceBlob);
            CopyStatus cs = CopyStatus.Pending;
            while (cs == CopyStatus.Pending)
            {
                Thread.Sleep(2000);  // sleep for 2 seconds
                cs = newBlob.CopyState.Status;
            }

            if (cs == CopyStatus.Success)
            {
                sourceBlob.DeleteIfExists();
            }
            return true;
        }

    }
}
