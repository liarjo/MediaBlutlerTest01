using System;
using System.Linq;
using MediaButler.Common.workflow;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;
using MediaButler.Common;
using Newtonsoft.Json.Linq;

namespace MediaButler.BaseProcess
{
    class GridEncodeStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager myBlobManager;
        private CloudMediaContext _MediaServicesContext;
        private IEncoderSupport myEncodigSupport;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
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

            if (myJob != null)
            {
                //Delete Output Asset is exist
                foreach (IAsset item in myJob.OutputMediaAssets)
                {
                    string txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, item.Id);
                    item.Delete();
                    Trace.TraceWarning(txtTrace);
                }
            }
            throw new Exception(txt);
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
            myBlobManager.PersistProcessStatus(myRequest);
            Trace.TraceInformation(message);

        }
        
       
        public override void HandleExecute(ChainRequest request)
        {
            //1. Step Setup 
            myRequest = (ButlerProcessRequest)request;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            IjsonKeyValue dotControlData = myBlobManager.GetDotControlData(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue processData =new jsonKeyValue( myBlobManager.GetButlerConfigurationValue(ProcessConfigKeys.DefualtPartitionKey, myRequest.ProcessTypeId + ".config"));
            //2. Get Current Asset
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            
            //3. JOB parameters
            string OutputAssetsName = asset.Name + "_mbGridEncode";
            string JobName = string.Format("GridEncodeStep_{1}_{0}", myRequest.ProcessInstanceId, asset.Name);
            string MediaProcessorName = myEncodigSupport.GetMediaProcessorName(dotControlData,processData);
            string[] encodeConfigurations = myEncodigSupport.GetLoadEncodignProfiles(dotControlData, processData, myRequest.ButlerRequest.MezzanineFiles,myRequest.ProcessConfigConn,this.StepConfiguration);
            
            //4. Execute JOB and Wait
            IJob currentJob = myEncodigSupport.ExecuteGridJob(OutputAssetsName, JobName, MediaProcessorName, encodeConfigurations,"Grid Task",asset.Id, MyEncodigSupport_OnJobError, MyEncodigSupport_JobUpdate);

            //9. Update AssetID
            myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;

           
        }
    }
}
