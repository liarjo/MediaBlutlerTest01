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

namespace MediaButler.BaseProcess
{
   
    class StandarEncodeStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private IAsset myAssetOriginal;
        private string PreviousJobState;
        private IJob currentJob;
        private  IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            // The possible strings that can be passed into the 
            // method for the mediaProcessor parameter:
            // Azure Media Encoder
            // Windows Azure Media Packager
            // Windows Azure Media Encryptor
            // Azure Media Indexer
            // Storage Decryption

            var processor = _MediaServicesContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor {0}", mediaProcessorName));

            return processor;
        }
        private void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;

            if (PreviousJobState != e.CurrentState.ToString())
            {
                PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("Job {0} state Changed from {1} to {2}", job.Id, e.PreviousState, e.CurrentState);

            }
            switch (e.CurrentState)
            {
                case JobState.Finished:
                    //if (OnJobFinish != null)
                    //{
                    //    OnJobFinish(this, job);
                    //}
                    break;
                //case JobState.Canceling:
                //case JobState.Queued:
                //case JobState.Scheduled:
                //case JobState.Processing:
                //    //Trace.TraceInformation("Please wait Job {0} Finish", job.Id);
                //    break;
                case JobState.Canceled:
                    //if (OnJobCancel != null)
                    //{
                    //    OnJobCancel(this, job);
                    //}
                    break;
                case JobState.Error:
                    //if (OnJobError != null)
                    //{
                    //    OnJobError(this, job);
                    //}
                    break;
                default:
                    break;
            }
        }
        private IJob GetJob(string jobId)
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
        private void WaitJobFinish(string jobId)
        {
            IJob myJob = GetJob(jobId);
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            double avance = 0;
            //TODO: imporve wating method
            while ((myJob.State != JobState.Finished) && (myJob.State != JobState.Canceled) && (myJob.State != JobState.Error))
            {
                if (myJob.State == JobState.Processing)
                {
                    if (avance != (myJob.Tasks[0].Progress / 100))
                    {
                        avance = myJob.Tasks[0].Progress / 100;
                        Trace.TraceInformation("job " + myJob.Id + " Percent complete:" + avance.ToString("#0.##%"));
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
                myJob.Refresh();
            }
            //TODO: test this kind of error
            if (myJob.State == JobState.Error)
            {
                throw new Exception(string.Format("Error JOB {0}", myJob.Id));
            }
        }
        /// <summary>
        /// Load encoding profile from local disk or Blob Storage (mediabultlerbin container)
        /// </summary>
        /// <param name="profileInfo"></param>
        /// <returns></returns>
        private string LoadEncodeProfile(string profileInfo)
        {
            string auxProfile = null;
            if (File.Exists(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo)))
            {
                //the XML file is in the local file
                Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from local disk", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                auxProfile = File.ReadAllText(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo));
            }
            else
            {
                //The profile is not in local file

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(MediaButler.Common.Configuration.ButlerExternalInfoContainer);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(profileInfo);
                if (blockBlob.Exists())
                {
                    Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from Blob Storage", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
         
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
                    string txtMessage = string.Format("[{0}] process Type {1} instance {2} Error Encoder Profile {3} don't exist", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                    throw (new Exception(txtMessage));
                }
                
            }
            return auxProfile;
        }
        private  void  ConvertMP4toSmooth(IAsset assetToConvert)
        {

            string xmlEncodeProfile = "H264 Adaptive Bitrate MP4 Set 1080p.xml";
            //if (this.StepConfiguration != null)
            if (!string.IsNullOrEmpty(this.StepConfiguration))
            {
                //TODO:support storage and LABEL
                xmlEncodeProfile = this.StepConfiguration;
            }
            else
            {
                string txt = string.Format("StandarEncodeStep try to read StepConfiguration but it is not in configuration table! at {0} ",  DateTime.Now.ToString());
                Trace.TraceWarning(txt);
            }
            // Declare a new job to contain the tasks
            currentJob = _MediaServicesContext.Jobs.Create("Convert to Smooth Streaming job " +  myAssetOriginal.Name);

            // Set up the first Task to convert from MP4 to Smooth Streaming. 
            // Read in task configuration XML
            string configMp4ToSmooth = LoadEncodeProfile(xmlEncodeProfile);
            
            // Get a media packager reference
            IMediaProcessor processor = GetLatestMediaProcessorByName("Windows Azure Media Encoder");
            
            // Create a task with the conversion details, using the configuration data
            ITask task = currentJob.Tasks.AddNew("Task profile " + xmlEncodeProfile,
                   processor,
                   configMp4ToSmooth,
                   TaskOptions.None);
            // Specify the input asset to be converted.
            task.InputAssets.Add(assetToConvert);
            // Add an output asset to contain the results of the job.
            task.OutputAssets.AddNew(assetToConvert.Name+"_mb", AssetCreationOptions.None);
            // Use the following event handler to check job progress. 
            // The StateChange method is the same as the one in the previous sample
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);
            // Launch the job.
            currentJob.Submit();
            //9. Revisa el estado de ejecución del JOB 
            Task progressJobTask = currentJob.GetExecutionProgressTask(CancellationToken.None);

            //10. en vez de utilizar  progressJobTask.Wait(); que solo muestra cuando el JOB termina
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            this.WaitJobFinish(currentJob.Id);
            
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;

            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);

            myAssetOriginal = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            
            ConvertMP4toSmooth(myAssetOriginal);

            //Update AssetID
            myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;
        }
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            string txtTrace;

            if (currentJob!=null )
            {
                foreach (IAsset item in currentJob.OutputMediaAssets)
                {
                    txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, item.Id);
                    item.Delete();
                    Trace.TraceWarning(txtTrace);
                }
            }

            if (myAssetOriginal!=null)
            {
                txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myAssetOriginal.Id);
                   
                myAssetOriginal.Delete();
                Trace.TraceWarning(txtTrace);
            }
        }
    }
}
