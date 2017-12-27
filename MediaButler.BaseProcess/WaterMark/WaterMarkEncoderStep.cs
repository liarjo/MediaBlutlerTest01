using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    
    class WaterMarkEncoderStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private WaterMarkData myWaterMarkData;
        private IAsset myAssetOriginal;
       // private IJob currentJob;
        private string PreviousJobState;
        /// <summary>
        /// Read information from .control file
        /// </summary>
        /// <returns></returns>
        private string readJsonControl()
        {
            string json = null;
            Uri controleFileUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
            string controlFilename = controleFileUri.Segments[2] + controleFileUri.Segments[3] + controleFileUri.Segments[4];
            json = CloudStorageAccount.Parse(myRequest.ProcessConfigConn).CreateCloudBlobClient().GetContainerReference(myRequest.ProcessTypeId).GetBlockBlobReference(controlFilename).DownloadText();
            return json;
        }
        private void SetPrimaryAssetFile()
        {
            IEncoderSupport myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            IAssetFile mp4 = myAssetOriginal.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".mp4")).FirstOrDefault();
            myEncodigSupport.SetPrimaryFile(myAssetOriginal, mp4);
        }
        private void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;
            if (PreviousJobState != e.CurrentState.ToString())
            {
                PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("[{3}]Job {0} state Changed from {1} to {2}", job.Id, e.PreviousState, e.CurrentState,myRequest.ProcessInstanceId);
            }
        }
        private IAsset WaterMArkJob()
        {
            IEncoderSupport myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            string xmlEncodeProfile;
            if (!string.IsNullOrEmpty(this.StepConfiguration))
            {
                xmlEncodeProfile = this.StepConfiguration;
            }
            else
            {
                string errorTxt = string.Format("[{0}] process Type {1} instance {2} has xmlEncodeProfile ", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
                throw new Exception(errorTxt);
            }

            // Declare a new job to contain the tasks
            IJob  currentJob = _MediaServicesContext.Jobs.Create("Watermak Video base on " + xmlEncodeProfile + " " + myAssetOriginal.Name);
            // Set up the first Task to convert from MP4 to Smooth Streaming. 
            // Read in task configuration XML
            string myEncoderProfile = myEncodigSupport.LoadEncodeProfile(xmlEncodeProfile, myRequest.ProcessConfigConn);
            // Get a media packager reference
            //IMediaProcessor processor = MediaButler.BaseProcess.MediaHelper.GetLatestMediaProcessorByName("Windows Azure Media Encoder",_MediaServicesContext);
            IMediaProcessor processor = myEncodigSupport.GetLatestMediaProcessorByName("Windows Azure Media Encoder");
            // Create a task with the conversion details, using the configuration data
            ITask task = currentJob.Tasks.AddNew("Task profile " + xmlEncodeProfile,processor,myEncoderProfile, TaskOptions.None);
            // Specify the input asset to be converted.
            task.InputAssets.Add(myAssetOriginal);
            // Add an output asset to contain the results of the job.
            task.OutputAssets.AddNew(myAssetOriginal.Name + "_mb", AssetCreationOptions.None);
            // Use the following event handler to check job progress. 
            // The StateChange method is the same as the one in the previous sample
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);
            // Launch the job.
            currentJob.Submit();
            //9. Revisa el estado de ejecución del JOB 
            Task progressJobTask = currentJob.GetExecutionProgressTask(CancellationToken.None);
            //10. en vez de utilizar  progressJobTask.Wait(); que solo muestra cuando el JOB termina
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            myEncodigSupport.WaitJobFinish(currentJob.Id);
            return currentJob.OutputMediaAssets.FirstOrDefault();
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            myWaterMarkData = Newtonsoft.Json.JsonConvert.DeserializeObject<WaterMarkData>(this.readJsonControl());
            myAssetOriginal = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            SetPrimaryAssetFile();
            myRequest.AssetId = WaterMArkJob().Id;
        }
        
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
    }
}
