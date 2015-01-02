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
    public static partial class JobManager
    {
        /// <summary>
        /// Create a job object for a multipart job -- one that consists of 
        /// multiple mezannine files.
        /// </summary>
        /// <param name="mediaAssets">The list of mezannine Assets</param>
        /// <param name="controlFile">Butler control file.</param>
        /// <returns>the new Butler job object</returns>
        public static ButlerJob CreateJob(IList<Uri> mediaAssets, Uri controlFile)
        {
            var b = new ButlerJob()
            {
                JobControlFile = controlFile,
                JobMediaFiles = mediaAssets,
            };
            return b;
        }

        /// <summary>
        /// Create a Butler job object for the case of a single mezannine
        /// file.  No control file provided.
        /// </summary>
        /// <param name="mediaAsset">The Uri of the single mezannine file (e.g.,
        /// for .../Incoming/myVid.mp4).</param>
        /// <returns></returns>
        public static ButlerJob CreateSimpleJob(Uri mediaAsset)
        {
            var b = new ButlerJob()
            {
                JobControlFile = null,
                JobMediaFiles = new List<Uri>() { mediaAsset }
            };
            return b;
        }

        /// <summary>
        /// This needs to be called from an Azure Worker as it gets the connection string from the context
        /// </summary>
        /// <param name="j">Job to submit</param>
        /// <returns>the Guid of the JOB if submission was successful</returns>
        public static Guid Submit(ButlerJob j)
        {
            string storageAccountString = CloudConfigurationManager.GetSetting(Configuration.ButlerStorageConnectionConfigurationKey);
            return Submit(j, storageAccountString);
        }
        /// <summary>
        /// Submit this job to the Butler Request queue
        /// </summary>
        /// <param name="j">Job to submit</param>
        /// <returns>the Guid of the JOB if submission was successful</returns>
        public static Guid Submit(ButlerJob j, string storageAccountString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
            CloudQueueClient sendQueueClient = account.CreateCloudQueueClient();
            try
            {
                CloudQueue sendQueue = sendQueueClient.GetQueueReference(Configuration.ButlerSendQueue);
                sendQueue.CreateIfNotExists();

                var message = new ButlerRequest
                {
                    MessageId = j.JobId,
                    MezzanineFiles = new List<string> { },
                    StorageConnectionString = storageAccountString,
                    WorkflowName = "",
                    TimeStampUTC = String.Format("{0:o}", DateTime.Now.ToUniversalTime()),
                    ControlFileUri = ""
                    };
                if (j.JobMediaFiles.Count > 0)
                {
                    var blob = new CloudBlockBlob(j.JobMediaFiles[0]);
                    message.WorkflowName = blob.Container.Name;
                }
                foreach (Uri blobUri in j.JobMediaFiles)
                {
                    message.MezzanineFiles.Add(blobUri.ToString());
                }

                if (j.JobControlFile != null)
                {
                    message.ControlFileUri = j.JobControlFile.ToString();
                }

                CloudQueueMessage butlerRequestMessage = new CloudQueueMessage(JsonConvert.SerializeObject(message));
                sendQueue.AddMessageAsync(butlerRequestMessage);
            }
            catch (Exception)
            {

                throw;
            }

            return j.JobId;
        }

        public static async Task getWorkflowSuccessOperations(CancellationToken ct)
        {
            string storageAccountString = CloudConfigurationManager.GetSetting(Configuration.ButlerStorageConnectionConfigurationKey);
            await getWorkflowSuccessOperations(ct, storageAccountString);
        }

        public static async Task getWorkflowSuccessOperations(CancellationToken ct, string storageAccountString)
        {
            var pollingInterval = TimeSpan.FromSeconds(Configuration.SuccessQueuePollingInterval);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Wake up and do some background processing if not canceled.      

                    Trace.TraceInformation("reading from workflow success results queue");
                    CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                    CloudQueueClient successQueueClient = account.CreateCloudQueueClient();
                    CloudQueue successQueue = successQueueClient.GetQueueReference(Configuration.ButlerSuccessQueue);
                    successQueue.CreateIfNotExists();
                    CloudQueueMessage retrievedMessage = successQueue.GetMessage();
                    if (retrievedMessage != null)
                    {
                        try
                        {
                            ButlerResponse messageResponse = JsonConvert.DeserializeObject<ButlerResponse>(retrievedMessage.AsString);
                            Configuration.WorkflowStatus messageStatus = Configuration.WorkflowStatus.Finished;
                            processFilesFromResponse(messageResponse, messageStatus, account);
                        }
                        catch (Exception X)
                        {
                            Trace.TraceError(string.Format("[{0}] Error: ", "getWorkflowSuccessOperations",X.Message));
                            throw;
                        }
                        // TODO: updateQueuemessage if it is taking longer
                        //Process the message in less than 30 seconds, and then delete the message
                        successQueue.DeleteMessage(retrievedMessage);

                    }

                    // Go back to sleep for a period of time unless asked to cancel.      
                    // Task.Delay will throw an OperationCanceledException when canceled.      
                    await Task.Delay(pollingInterval, ct);
                }
            }
            catch (OperationCanceledException ocEx)
            {
                // Expect this exception to be thrown in normal circumstances or check    
                // the cancellation token. If the role instances are shutting down, a    
                // cancellation request will be signaled.    
                Trace.TraceInformation("Stopping service, cancellation requested");

                // Re-throw the Operation cancellation exception    
                throw ocEx;
            }
        }

        public static async Task getWorkflowFailedOperations(CancellationToken ct) 
        { 
            string storageAccountString = CloudConfigurationManager.GetSetting(Configuration.ButlerStorageConnectionConfigurationKey);
            await getWorkflowFailedOperations(ct, storageAccountString);
        }
        public static async Task getWorkflowFailedOperations(CancellationToken ct, string storageAccountString)
        {
            ButlerJob job = new ButlerJob();
            var pollingInterval = TimeSpan.FromSeconds(Configuration.FailedQueuePollingInterval);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Wake up and do some background processing if not canceled.      
                    // TODO: Add reading from failed queue
                    Trace.TraceInformation("reading from workflow failed results queue");
                    CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                    CloudQueueClient failedQueueClient = account.CreateCloudQueueClient();
                    CloudQueue failedQueue = failedQueueClient.GetQueueReference(Configuration.ButlerFailedQueue);
                    failedQueue.CreateIfNotExists();
                    CloudQueueMessage retrievedMessage = failedQueue.GetMessage();
                    if (retrievedMessage != null)
                    {
                        try
                        {
                            ButlerResponse messageResponse = JsonConvert.DeserializeObject<ButlerResponse>(retrievedMessage.AsString);
                            Configuration.WorkflowStatus messageStatus = Configuration.WorkflowStatus.Failed;
                            processFilesFromResponse(messageResponse, messageStatus, account);
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                        finally 
                        {
                            // TODO: updateQueuemessage if it is taking longer
                            //Process the message in less than 30 seconds, and then delete the message
                            failedQueue.DeleteMessage(retrievedMessage);
                        }

                    }

                    // Go back to sleep for a period of time unless asked to cancel.      
                    // Task.Delay will throw an OperationCanceledException when canceled.      
                    await Task.Delay(pollingInterval, ct);
                }
            }
            catch (OperationCanceledException ocEx)
            {
                // Expect this exception to be thrown in normal circumstances or check    
                // the cancellation token. If the role instances are shutting down, a    
                // cancellation request will be signaled.    
                Trace.TraceInformation("Stopping service, cancellation requested");

                // Re-throw the Operation cancellation exception    
                throw ocEx;
            }
        }

        private static bool processFilesFromResponse(ButlerResponse jobResponse, Configuration.WorkflowStatus jobStatus, CloudStorageAccount account)
        {
            bool returnValue = true;
            CloudBlockBlob baseBlob = new CloudBlockBlob(new Uri(jobResponse.MezzanineFiles[0]), account.Credentials);
            CloudBlobContainer container = baseBlob.Container;
            string directoryTo = (jobStatus == Configuration.WorkflowStatus.Failed) ? Configuration.DirectoryFailed:Configuration.DirectoryCompleted;
            string timestampFileAppend = (string.IsNullOrEmpty(jobResponse.TimeStampProcessingStarted)) ? "":jobResponse.TimeStampProcessingStarted.Replace(':','-');
            // fix: substitute / with - in date to avoid file being treated as series of dirs
            timestampFileAppend = timestampFileAppend.Replace('/', '-');
            try
            {
                foreach (string videoUri in jobResponse.MezzanineFiles)
                {
                    CloudBlockBlob fileToMove = new CloudBlockBlob(new Uri(videoUri), account.Credentials);
                    var blobContinuationToken = new BlobContinuationToken();

                    string blobTarget = BlobUtilities.AdjustPath(fileToMove, directoryTo);
                    int trimEnd = blobTarget.LastIndexOf('.');
                    string blobTargetFileExt = blobTarget.Substring(trimEnd, blobTarget.Length - trimEnd);
                    blobTarget = string.Concat(blobTarget, ".", timestampFileAppend, ".", blobTargetFileExt);
                    BlobUtilities.RenameBlobWithinContainer(container, BlobUtilities.ExtractBlobPath(fileToMove), blobTarget);
                }


                // write log file
                string blobUriString = BlobUtilities.AdjustPath(baseBlob, directoryTo);
                // remove file ext
                // append .log
                blobUriString = string.Concat(blobUriString, ".", timestampFileAppend, ".log");
                CloudBlockBlob logBlob = container.GetBlockBlobReference(blobUriString);
                logBlob.Properties.ContentType = "text/plain";
                logBlob.UploadText(jobResponse.Log);

            }
            catch (Exception)
            {
                
                throw;
            }
            

            return returnValue;
        }
    }
}
