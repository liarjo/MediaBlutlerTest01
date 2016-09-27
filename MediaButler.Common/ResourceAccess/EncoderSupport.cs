using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
   

    public class EncoderSupport : IEncoderSupport
    {
        private CloudMediaContext _MediaServicesContext;
        private string PreviousJobState="-";
        public EncoderSupport(CloudMediaContext MediaServicesContext)
        {
            _MediaServicesContext = MediaServicesContext;
        }
        public event EventHandler OnJobError;
        public event EventHandler JobUpdate;
        public IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _MediaServicesContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor {0}", mediaProcessorName));

            return processor;
        }
        public void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;

            if (PreviousJobState != e.CurrentState.ToString())
            {
                PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("Job {0} state Changed from {1} to {2}", job.Id, e.PreviousState, e.CurrentState);

            }
            //switch (e.CurrentState)
            //{
            //    case JobState.Finished:
            //        //if (JobUpdate != null)
            //        //{
            //        //    string message = "job " + job.Id + " Percent complete: 100%";
            //        //    JobUpdate(message, null);
            //        //}
            //        break;
            //    //case JobState.Canceling:
            //    //case JobState.Queued:
            //    //case JobState.Scheduled:
            //    //case JobState.Processing:
            //    //    //Trace.TraceInformation("Please wait Job {0} Finish", job.Id);
            //    //    break;
            //    case JobState.Canceled:
            //        //if (OnJobCancel != null)
            //        //{
            //        //    OnJobCancel(this, job);
            //        //}
            //        break;
            //    case JobState.Error:
            //        //if (JobUpdate != null)
            //        //{
            //        //    string message = "job " + job.Id + " Percent complete: -1%";
            //        //    JobUpdate(message, null);
            //        //}
            //        break;
            //    default:
            //        break;
            //}
        }
        public void WaitJobFinish(string jobId)
        {
            IJob myJob = GetJob(jobId);
            double[] myJobProgress = new double[myJob.Tasks.Count];
            string message = "";

            while ((myJob.State != JobState.Finished) && (myJob.State != JobState.Canceled) && (myJob.State != JobState.Error))
            {
                if (myJob.State == JobState.Processing)
                {
                    for (int i = 0; i < myJob.Tasks.Count; i++)
                    {
                       if (myJobProgress[i]!= (myJob.Tasks[i].Progress / 100))
                        {
                            myJobProgress[i]= myJob.Tasks[i].Progress / 100;
                            
                            //Calc Job Advance
                            double jobProgress = 0;

                            for (int id = 0; id < myJobProgress.Count(); id++)
                            {
                                jobProgress += myJobProgress[id];
                                message = string.Format("job {0} Task {1} Progress {2}", myJob.Id, myJob.Tasks[id].Name, myJobProgress[id].ToString("#0.##%"));
                                Trace.TraceInformation(message);
                            }
                           
                            jobProgress = jobProgress /  myJob.Tasks.Count();

                            message = string.Format("job {0} Percent complete {1}", myJob.Id,jobProgress.ToString("#0.##%"));
                            if (JobUpdate != null)
                            {
                                JobUpdate(message, null);
                            }
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(5));
                myJob.Refresh();
            }

            switch (myJob.State)
            {
                case JobState.Queued:
                    break;
                case JobState.Scheduled:
                    break;
                case JobState.Processing:
                    break;
                case JobState.Finished:
                    if (JobUpdate != null)
                    {
                         message = "job " + myJob.Id + " Percent complete: 100%";
                        JobUpdate(message, null);
                    }
                    break;
                case JobState.Error:
                    if (JobUpdate != null)
                    {
                         message = "job " + myJob.Id + " Percent complete: -1%";
                        JobUpdate(message, null);
                    }
                    if (OnJobError != null)
                    {
                        OnJobError(myJob, null);
                    }
                    else
                    {
                        throw new Exception("[Error] Transcoding Job ID " + myJob.Id);
                    }
                    break;
                case JobState.Canceled:
                    break;
                case JobState.Canceling:
                    break;
                default:
                    break;
            }
            

        }
        public IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in _MediaServicesContext.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();
            return job;
        }
        public IJob GetJobByName(string JobName)
        {
            var jobInstance =
                 from j in _MediaServicesContext.Jobs
                 where j.Name.Contains(JobName)
                 select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();
            return job;
        }
        /// <summary>
        /// Create Asset and Add blob
        /// </summary>
        /// <param name="AssetName"></param>
        /// <param name="blobUrl"></param>
        /// <returns></returns>
        public IAsset CreateAsset(string AssetName, string blobUrl, string MediaStorageConn, string StorageConnectionString, string WorkflowName)
        {
            //Create Empty Asset
            Uri MezzamineFileUri = new Uri(blobUrl);
            int segmentscount = MezzamineFileUri.Segments.Count() - 1;
            IAsset currentAsset = _MediaServicesContext.Assets.Create(AssetName, AssetCreationOptions.None);
            
            //Add the File
            IAccessPolicy writePolicy = _MediaServicesContext.AccessPolicies.Create("writePolicy_" + currentAsset.Name, TimeSpan.FromMinutes(120), AccessPermissions.Write);
            ILocator destinationLocator = _MediaServicesContext.Locators.CreateLocator(LocatorType.Sas, currentAsset, writePolicy);
            
            //Asset Storage
            CloudStorageAccount assetStorageCount = CloudStorageAccount.Parse(MediaStorageConn);
            CloudBlobClient assetClient = assetStorageCount.CreateCloudBlobClient();
            CloudBlobContainer assetContainer = assetClient.GetContainerReference(currentAsset.Uri.Segments[1]);

            //Mezzamine Storage
            CloudStorageAccount MezzamineStorageCount = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient MezzamineClient = MezzamineStorageCount.CreateCloudBlobClient();
            CloudBlobContainer MezzamineContainer = MezzamineClient.GetContainerReference(WorkflowName);

            Uri xFile = new Uri(blobUrl);
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
                assetBlob.StartCopyFromBlob(new Uri(srcBlockBlobSasUri));
                
                CloudBlockBlob blobStatusCheck;
                blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(AssetBlobName);
                while (blobStatusCheck.CopyState.Status == CopyStatus.Pending)
                {
                    Task.Delay(TimeSpan.FromSeconds(10d)).Wait();
                    blobStatusCheck = (CloudBlockBlob)assetContainer.GetBlobReferenceFromServer(AssetBlobName);
                }
                
                assetBlob.FetchAttributes();
                //Add the xFile to Asset
                var assetFile = currentAsset.AssetFiles.Create(assetBlob.Name);
                MezzamineBlob.FetchAttributes();
                assetFile.ContentFileSize = MezzamineBlob.Properties.Length;
                assetFile.Update();
            
            destinationLocator.Delete();
            writePolicy.Delete();

            currentAsset.AssetFiles.FirstOrDefault().IsPrimary = true;
            currentAsset.Update();
                        
            return currentAsset;
        }
        public string LoadEncodeProfile(string profileInfo, string ProcessConfigConn)
        {
            string auxProfile = null;
            if (File.Exists(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo)))
            {
                //the XML file is in the local file
                //Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from local disk", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                auxProfile = File.ReadAllText(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo));
            }
            else
            {
                //The profile is not in local file

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ProcessConfigConn);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(MediaButler.Common.Configuration.ButlerExternalInfoContainer);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(profileInfo);
                if (blockBlob.Exists())
                {
                    //Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from Blob Storage", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);

                    using (var memoryStream = new MemoryStream())
                    {
                        blockBlob.DownloadToStream(memoryStream);
                        memoryStream.Position = 0;
                        StreamReader sr = new StreamReader(memoryStream);
                        auxProfile = sr.ReadToEnd();
                    }
                }
                else
                {
                    string txtMessage = string.Format("Error Encoder Profile {0} don't exist", profileInfo);
                    throw (new Exception(txtMessage));
                }

            }
            return auxProfile;
        }
        public  void SetPrimaryFile(IAsset MyAsset, IAssetFile theAssetFile)
        {
            MyAsset.AssetFiles.ToList().ForEach(af => { af.IsPrimary = false; af.Update(); });
            theAssetFile.IsPrimary = true;
            theAssetFile.Update();
        }


    }
}
