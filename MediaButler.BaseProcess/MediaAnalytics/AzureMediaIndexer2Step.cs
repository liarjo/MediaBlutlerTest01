using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using MediaButler.Common.ResourceAccess;
using System.Diagnostics;
using Microsoft.WindowsAzure.MediaServices.Client;
using MediaButler.Common;

namespace MediaButler.BaseProcess.MediaAnalytics
{
    class AzureMediaIndexer2Step: MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager blobManager;
        private CloudMediaContext _MediaServicesContext;
        private IEncoderSupport myEncodigSupport;

        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
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
            blobManager.PersistProcessStatus(myRequest);
            Trace.TraceInformation(message);

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
        public override void HandleExecute(ChainRequest request)
        {
            //Setup
            myRequest = (ButlerProcessRequest)request;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            string jsonControlFile = blobManager.ReadTextBlob(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue dotControlData = new jsonKeyValue(jsonControlFile);

            //1. Create JOB
            IJob currentJob = _MediaServicesContext.Jobs.Create("My Indexing Job");
            //2. Get AMS processor
            string MediaProcessorName = "Azure Media Indexer 2 Preview";
            var processor = myEncodigSupport.GetLatestMediaProcessorByName(MediaProcessorName);

            //3. Create Task
            string configuration = dotControlData.Read("indexer2_profile");
            ITask task = currentJob.Tasks.AddNew("My Indexing Task",processor,configuration,TaskOptions.None);
            
            //4. Input Asset
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            task.InputAssets.Add(asset);
            
            //5. Output Asset
            task.OutputAssets.AddNew(asset.Name + "_mbIndex2", AssetCreationOptions.None);
            //6. Chnage Event
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(myEncodigSupport.StateChanged);
            
            //7.Set advantce on 0%
            string message = "job " + currentJob.Id + " Percent complete: 0%";
            MyEncodigSupport_JobUpdate(message, null);

            //8. Launch the job.
            currentJob.Submit();

            //9. Check Project Status
            myEncodigSupport.OnJobError += MyEncodigSupport_OnJobError;
            myEncodigSupport.JobUpdate += MyEncodigSupport_JobUpdate;
            myEncodigSupport.WaitJobFinish(currentJob.Id);

            //10. Update AssetID
            myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;
        }
        

       
    }
}
