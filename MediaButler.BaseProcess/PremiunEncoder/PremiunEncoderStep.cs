using MediaButler.BaseProcess;
using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.PremiunEncoder
{
    class PremiumEncodingStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private IButlerStorageManager myStorageManager;
        private IAsset myAssetOriginal;
        private IAsset myWorkflow;
        private IJob currentJob;
        private PremiunConfig myConfig;
        private IAsset createWorkflowAsset()
        {
            IAsset aux = null;
            IEncoderSupport myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            try
            {
                if (!string.IsNullOrEmpty(myConfig.AssetWorkflowID))
                {
                    //use Exiting Asset
                    aux = _MediaServicesContext.Assets.Where(a => a.Id == myConfig.AssetWorkflowID).FirstOrDefault();
                }
                else
                {
                    string worflowFile = myRequest.ButlerRequest.MezzanineFiles.Where(s => s.IndexOf(".workflow") > 0).FirstOrDefault();
                    aux = myEncodigSupport.CreateAsset
                         (myRequest.ProcessTypeId + "_workflowAssetDefinition_"  + myRequest.ProcessInstanceId,
                         worflowFile,
                         myRequest.MediaStorageConn,
                         myRequest.ButlerRequest.StorageConnectionString,
                         myRequest.ButlerRequest.WorkflowName);
                }
            }
            catch (Exception X)
            {
                
                throw X;
            }
            return aux;
        }
        private IAsset CreateEncodingJob(IAsset workflow, IAsset video)
        {
            IEncoderSupport myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            // Declare a new job.
            currentJob = _MediaServicesContext.Jobs.Create(myConfig.EncodingJobName);
            // Get a media processor reference, and pass to it the name of the 
            // processor to use for the specific task.
            IMediaProcessor processor = myEncodigSupport.GetLatestMediaProcessorByName("Media Encoder Premium Workflow");
            // Create a task with the encoding details, using a string preset.
            ITask task = currentJob.Tasks.AddNew(myConfig.EncodigTaskName, processor, "", TaskOptions.None);
            // Specify the input asset to be encoded.
            task.InputAssets.Add(workflow);
            task.InputAssets.Add(video); // we add one asset
            // Add an output asset to contain the results of the job. 
            // This output is specified as AssetCreationOptions.None, which 
            // means the output asset is not encrypted. 
            task.OutputAssets.AddNew(video.Name + "_mb", AssetCreationOptions.None);
            // Use the following event handler to check job progress. 
            // The StateChange method is the same as the one in the previous sample
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(myEncodigSupport.StateChanged);
            //Set advantce on 0%
            string message = "job " + currentJob.Id + " Percent complete: 0%";
            MyEncodigSupport_JobUpdate(message, null);
            // Launch the job.
            currentJob.Submit();
            //9. Revisa el estado de ejecución del JOB 
            Task progressJobTask = currentJob.GetExecutionProgressTask(CancellationToken.None);
            //10. en vez de utilizar  progressJobTask.Wait(); que solo muestra cuando el JOB termina
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            myEncodigSupport.OnJobError += MyEncodigSupport_OnJobError;
            myEncodigSupport.JobUpdate += MyEncodigSupport_JobUpdate;
            myEncodigSupport.WaitJobFinish(currentJob.Id);
            return currentJob.OutputMediaAssets.FirstOrDefault();
        }
        private void MyEncodigSupport_JobUpdate(object sender, EventArgs e)
        {
            string message = (string)sender;

            if (!myRequest.MetaData.Any(item => item.Key == Configuration.TranscodingAdvance))
            {
                myRequest.MetaData.Add(Configuration.TranscodingAdvance, message);
            }
            else
            {
                myRequest.MetaData[Configuration.TranscodingAdvance] = message;
            }
            myStorageManager.PersistProcessStatus(myRequest);
        }
        private void MyEncodigSupport_OnJobError(object sender, EventArgs e)
        {
            string txt = "JOB ERROR";
            IJob myJob = (IJob)sender;

            foreach (ITask task in myJob.Tasks)
            {
                foreach (ErrorDetail detail in task.ErrorDetails)
                {
                    txt = string.Format("Error Job encoder Code: [{0}] Error Message: {1}", detail.Code, detail.Message);
                    Trace.TraceError(txt);
                    myRequest.Exceptions.Add(txt);
                }
            }

            throw new Exception(txt);
        }
        private void Setup()
        {
            myStorageManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);

            //TODO: load configuration from control // mange control worng properties
            //string jsonControl = myStorageManager.ReadTextBlob(myRequest.ButlerRequest.ControlFileUri);
            //myConfig =null;
            //try
            //{
            //    if (!string.IsNullOrEmpty(jsonControl))
            //    {
            //        //instance process configuration
            //        myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PremiunConfig>(jsonControl);
            //    }
            //    else
            //    {
            //        if (!string.IsNullOrEmpty(this.StepConfiguration))
            //        {
            //            //general process configuration
            //            myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PremiunConfig>(this.StepConfiguration);
            //        }
            //    }
            //}
            //catch (Exception)
            //{
            //    Trace.TraceWarning("PremiumEncodingStep configuration Error, it will use default");
            //}
            
            if(myConfig==null)
            {
                //default configuration
                myConfig = new PremiunConfig()
                    {
                        AssetWorkflowID = null,
                        EncodingJobName = "Media Bulter Premium Workflow encoding job " + myRequest.ProcessInstanceId,
                        EncodigTaskName = "Media Bulter Premium Workflow encoding task" + myRequest.ProcessTypeId
                    };
            }
            
            
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            //Request
            myRequest = (ButlerProcessRequest)request;
            //Media Context
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            // Original Asset
            myAssetOriginal = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            //Setup Step
            Setup();
            //Load the workflow definition
            myWorkflow = createWorkflowAsset();
            //Delete workflow from Original Asset
            string fileName="";
            fileName = myAssetOriginal.AssetFiles.Where(af => af.Name.ToLower().EndsWith(".workflow")).FirstOrDefault().Name;    
            myAssetOriginal.AssetFiles.Where(f => f.Name.ToLower() == fileName.ToLower()).FirstOrDefault().Delete();
            //Encode Uisng Premiun Encoder
            myRequest.AssetId = CreateEncodingJob(myWorkflow, myAssetOriginal).Id;
        }
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string txtTrace;

            if (currentJob != null)
            {
                foreach (IAsset item in currentJob.OutputMediaAssets)
                {
                    txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, item.Id);
                    item.Delete();
                    Trace.TraceWarning(txtTrace);
                }
            }
            if (myAssetOriginal != null)
            {
                txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myAssetOriginal.Id);

                myAssetOriginal.Delete();
                Trace.TraceWarning(txtTrace);
            }

            if ((myWorkflow!=null) && (string.IsNullOrEmpty( myConfig.AssetWorkflowID)))
            {
                txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myWorkflow.Id);
                myWorkflow.Delete();
                Trace.TraceWarning(txtTrace);
            }
        }
    }
}
