using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using MediaButler.Common;

namespace MediaButler.Watcher
{
    public static partial class BlobWatcher
    {
        // Note: Directory naming conventions (e.g., using /Incoming for new jobs)
        // is specified by constants in the configuration class.

        /// <summary>
        /// Scan the contents of the specified container for new work
        /// </summary>
        /// <param name="cbc"></param>
        public static void ScanContainerForInbound(CloudBlobContainer cbc)
        {
            var bct = new BlobContinuationToken();
            //TODO: change to async to allow for more outstanding requests
            //var blobList = cbc.ListBlobs(Configuration.DirectoryInbound);

            var blobList = cbc.ListBlobs(Configuration.DirectoryInbound, true);
 
            // Keep track of files per jobID
            var filesByJobID = new Dictionary<Guid, List<IListBlobItem>>(23);

            foreach (var b in blobList)
            {
                // is it a simple file
                if (isSimpleFile(b))
                {
                    // 1. move the file to processing (rename)
                    string newName = AdjustPath(b, Configuration.DirectoryProcessing);
                    RenameBlobWithinContainer(cbc, ExtractBlobPath(b), newName);
                    //TODO: check for copy failure
                    var newBlobUri = cbc.GetBlockBlobReference(newName).Uri;

                    // 1. have jobcreator create the job for this (now renamed) file
                    var j = JobManager.CreateSimpleJob(newBlobUri);

                    Trace.TraceInformation(String.Format("Submitting job: {0} at {1:o} (Simple)", j.JobId, DateTime.UtcNow));

                    // 3. submit the job
                    JobManager.Submit(j);
                }
                else {
                    // it's one of the files for a specific job, so collect them for now
                    
                    // 1. extract GUID
                    var parts = b.Uri.ToString().Split('/');
                    var guidStr = parts[parts.Length - 2];
                    var jobID = new Guid(guidStr);

                    if (!filesByJobID.ContainsKey(jobID)) {
                        filesByJobID.Add(jobID, new List<IListBlobItem>(10));
                    }
                    filesByJobID[jobID].Add(b);
                }  
            }

            // All simple files have been submitted and all complex jobs have had their
            // blobs enumerated and grouped into filesByJobID. 

            if (filesByJobID.Count == 0)
            {
                return;   // no complex jobs found
            }

            foreach (var jobID in filesByJobID.Keys)
            {
                var files = filesByJobID[jobID];
                var jobMediaFiles = new List<Uri>(files.Count);
                Uri jobControl = null;

                // If there was no control file found, then the job is presumed 
                // not yet complete, so leave it for the next pass.

                bool foundJobControlFile = false;
                foreach (var f in files)
                {
                    if (isControlFile(f))
                    {
                        foundJobControlFile = true;
                        break;
                    }
                }

                if (!foundJobControlFile)
                {
                    continue;  // no control file... on to the next potential job
                }

                // We have a complete job, so collect the files, relocating each to the Processing directory

                // remember the path components so that we can kick off a bulk rename

                var sourcePaths = new List<string>(files.Count);
                var destPaths = new List<string>(sourcePaths.Count);

                foreach (var f in files)
                {
                    string newName = AdjustPath(f, Configuration.DirectoryProcessing);
                    sourcePaths.Add(ExtractBlobPath(f));
                    destPaths.Add(newName);
                }

                bool copyWorked = RenameBlobsWithinContainer(cbc, sourcePaths, destPaths);

                // files copied, now build up the job data...
                foreach(var path in destPaths)
                {
                    var bbref = cbc.GetBlockBlobReference(path);
                    var targetUri = bbref.Uri;

                    if (isControlFile(bbref))
                    {
                        jobControl = targetUri;
                    }
                    else
                    {
                        jobMediaFiles.Add(targetUri);
                    }
                }

                // Create & Submit the job for processing
                var job = JobManager.CreateJob(jobMediaFiles, jobControl);

                Trace.TraceInformation(String.Format("Submitting job: {0} at {1:o}",job.JobId, DateTime.UtcNow));
                JobManager.Submit(job);                     
            }
        }

        private const string parentSuffix = "/" + Configuration.DirectoryInbound + "/";

        /// <summary>
        /// Is it a standalone file?  Versus a subdirectory?
        /// </summary>
        /// <param name="b">the item to check</param>
        /// <returns>true if the item resides at /Incoming - so a stand-alone asset</returns>
        public static bool isSimpleFile(IListBlobItem b)
        {
            // true if item is not in a first-level subdirectory
            //TODO: should be more stringent check - e.g. check filetype
            var ret = b.Parent.Uri.ToString().EndsWith(parentSuffix, StringComparison.InvariantCultureIgnoreCase);
            return ret;
        }

        /// <summary>
        /// Is it a standalone file?  Versus a subdirectory?
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool isControlFile(IListBlobItem b)
        {
            return b.Uri.ToString().EndsWith(Configuration.ControlFileSuffix, StringComparison.InvariantCultureIgnoreCase);
            // true if matches the name pattern        
        }

        /// <summary>
        /// Is this the control file for a complex job?  Check file suffix?
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool isControlFile(string s)
        {
            return s.EndsWith(Configuration.ControlFileSuffix, StringComparison.InvariantCultureIgnoreCase);
            // true if matches the name pattern        
        }

        /// <summary>
        /// Extract the path portion for the Storage URI -- e.g. from 
        /// https://htestseq.blob.core.windows.net/hli-0008-vhd-backups/myFolder/myblob.dat
        /// to
        /// /myFlder/myblob.dat
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ExtractBlobPath(IListBlobItem b)
        {
            return BlobUtilities.ExtractBlobPath(b);
            /*
            // remove the storage prefix + the container from the URI
            var parts = b.Uri.LocalPath.Split('/'); 
            // remove the container in [0]
            var pathPortion = String.Join("/", parts, 2, parts.Length - 2);

            return pathPortion;
             */
        }

        /// <summary>
        /// Adjust the path portion (between the container and the basename
        /// parts of the name) to the new path.    
        /// e.g., 
        /// </summary>
        /// <param name="b">The source blob</param>
        /// <param name="newPath">The new path portion</param>
        /// <returns>The adjusted URI string.</returns>
        public static string AdjustPath(IListBlobItem b, string newPath)
        {
            return BlobUtilities.AdjustPath(b, newPath);
            /*
            // remove the storage prefix + the container from the URI
            var parts = b.Uri.LocalPath.Split('/');
            // remove the container in [0]
            var pathPortion = newPath + "/" + String.Join("/", parts, 3, parts.Length - 3);

            return pathPortion;
             */
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
            // Handle the simple version of delete as just a one-element list.

            var srcList = new List<string>(1) { sourcePath };
            var targetList = new List<string>(1) { targetPath };

            return RenameBlobsWithinContainer(container, srcList, targetList);
#if NO
            // CloudBlobContainer container = blobClient.GetContainerReference(blobContainer);
            //TODO: block is okay since WAMS only supports block
            CloudBlockBlob newBlob = container.GetBlockBlobReference(targetPath);
            CloudBlockBlob sourceBlob = container.GetBlockBlobReference(sourcePath);
            string monitorStr = null;
            try
            {
                monitorStr = newBlob.StartCopyFromBlob(sourceBlob);
            }
            catch (Exception ex)
            {
                var e = ex;   // to avoid compiler warning
                throw;
            }
           
            CopyStatus cs = CopyStatus.Pending;
            while (cs == CopyStatus.Pending)
            {
                Thread.Sleep(2000);  // sleep for 2 seconds
                cs = newBlob.CopyState.Status;
            }

            if (cs == CopyStatus.Success)
            {
                try
                {
                    Trace.TraceInformation("Deleting source blob: {0}", sourceBlob.Name);
                    sourceBlob.DeleteIfExists();
                }
                catch (Exception e)
                {
                    Trace.TraceInformation("Delete failed for {1}, msg = '{0}'", e.Message, sourceBlob.Name);
                    // and continue.
                }
                //TODO: actually delete the source
               //   sourceBlob.DeleteIfExists();
            }
            return true;
#endif
        }

        //TODO: Move to utilities
        /// <summary>
        /// Synchronous rename of the specified blobs, each to a new name, within the same container.
        /// </summary>
        /// <remarks>
        /// Note that Azure blobs do not really have a rename operation so the 
        /// implemenation has to be to copy the blob and then delete the original.
        /// </remarks>
        /// <param name="container">The container of the target and destination</param>
        /// <param name="sourcePath">A list containing the portion of the blob name within the container. 
        /// e.g. /Incoming/fred.mp4</param>
        /// <param name="targetPath">A list containing the path portion of the target blob within the container.
        /// e.g., /Processing/fred.mp4</param>
        /// <returns>true if the operation succeeds</returns>
        public static bool RenameBlobsWithinContainer(CloudBlobContainer container, IList<string> sourcePath, IList<string> targetPath)
        {
            // CloudBlobContainer container = blobClient.GetContainerReference(blobContainer);
            //TODO: block is okay since WAMS only supports block

            System.Diagnostics.Debug.Assert(sourcePath.Count == targetPath.Count);
            
            int count = sourcePath.Count;
            var sourceList = new List<CloudBlockBlob>(count);
            var monitorList = new List<CloudBlockBlob>(count);
            var doneList = new CopyStatus[count];
 
            // Start the list of blobs copying
            for (int i = 0; i < sourcePath.Count; i++)
            {
                CloudBlockBlob newBlob = container.GetBlockBlobReference(targetPath[i]);
                CloudBlockBlob sourceBlob = container.GetBlockBlobReference(sourcePath[i]);
                string monitorStr = null;
                try
                {
                    monitorStr = newBlob.StartCopyFromBlob(sourceBlob);
                    monitorList.Add(newBlob);
                    sourceList.Add(sourceBlob);
                    doneList[i] = CopyStatus.Pending;
                }
                catch (Exception ex)
                {
                    var e = ex;   // to avoid compiler warning
                    throw;
                }
            }

            // now monitor for completion

            //JPGG: change becouse is not deleting the original File
            //bool CopiesCompleted = true;
            bool CopiesCompleted = false;

            while (!CopiesCompleted)
            {
                Thread.Sleep(2000);  // sleep for 2 seconds
                CopiesCompleted = true;

                // recheck latest status for any not previously complete
                for (int i = 0; i < doneList.Length; i++)
                {
                    if (doneList[i] == CopyStatus.Pending)
                    {   // check latest status
                        doneList[i] = monitorList[i].CopyState.Status;
                        if (doneList[i] == CopyStatus.Pending)
                        {
                            CopiesCompleted = false;
                        }
                    }
                }
            }

            // so no copying operation remains in Pending state

            // if all were successful, return true, else false
            bool AllSuccessful = true;
            foreach(var cs in doneList) {
                if (cs != CopyStatus.Success) {
                    AllSuccessful = false;
                    break;
                }
            }

            // Delete all of the source blobs if the copies worked.
            if (AllSuccessful) {
                foreach(var blob in sourceList) {
                    try
                    {
                        Trace.TraceInformation("Deleting source blob: {0}", blob.Name);
                        blob.DeleteIfExists();
                    }
                    catch (Exception e)
                    {
                        Trace.TraceInformation("Delete failed for {1}, msg = '{0}'", e.Message, blob.Name);
                        // and continue.
                    }
                }
            }

            return AllSuccessful;
        }
        /// <summary>
        /// Run Mainline for the incoming Blob watcher.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="storageAccountString"></param>
        /// <param name="ContainersToScan"></param>
        /// <returns></returns>
        public static async Task runInboundJobWatcher(CancellationToken ct, string storageAccountString, string[] ContainersToScan)
        {
        //    var pollingInterval = TimeSpan.FromSeconds(60);

            int msBetweenPolls = 1000 * Configuration.BlobWatcherPollingInterval;
            // Run the mainline for BlobWatcher

            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
            var cl = account.CreateCloudBlobClient();

            // Set up the container objects
            var containers = new List<CloudBlobContainer>(ContainersToScan.Length);
            foreach (var containerName in ContainersToScan)
            {
                containers.Add(cl.GetContainerReference(containerName));
            }

            Trace.TraceInformation("Scanning: containers=(" + String.Join(", ", ContainersToScan) + ")");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // for now, just run the scan at serially... later change to background run in parallel.
                    foreach (var cbc in containers)
                    {
                        try
                        {
                            BlobWatcher.ScanContainerForInbound(cbc);
                        }
                        catch (Exception ex)
                        {
                            if (ex as OperationCanceledException != null)
                            {
                                throw;
                            }
                            Trace.TraceInformation("Exception scanning container {0}, Msg='{1}'", cbc.Name, ex.Message);
                            // and continue
                        }
                    }

                    Trace.TraceInformation("Scanning: back to sleep for {0} secs", msBetweenPolls / 1000);
                    await Task.Delay(msBetweenPolls);
                }
            }
            catch (OperationCanceledException ocEx)
            {
                var ex = ocEx;      // to avoid compiler warning
                // Expect this exception to be thrown in normal circumstances or check    
                // the cancellation token. If the role instances are shutting down, a    
                // cancellation request will be signaled.    
                Trace.TraceInformation("Stopping BlobWatcher service, cancellation requested");
                // Re-throw the Operation cancellation exception    
                throw;
            }
        }
    }
}
