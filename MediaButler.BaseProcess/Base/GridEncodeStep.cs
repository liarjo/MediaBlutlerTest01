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
        private IButlerStorageManager blobManager;
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
            blobManager.PersistProcessStatus(myRequest);
            Trace.TraceInformation(message);

        }
        private string[] GetLoadEncodignProfiles(IjsonKeyValue dotControlData)
        {
            string[] aux;
           
            if (!string.IsNullOrEmpty( dotControlData.Read("GridEncodeStep.encodeConfigList") ))
            {
                //Definition encoders on instance level 
                var Xlist = dotControlData.ReadArray("GridEncodeStep.encodeConfigList").ToArray();
                aux = new string[Xlist.Count()];
                int profileId = 0;
                foreach (var profile in Xlist)
                {
                    string url = myRequest.ButlerRequest.MezzanineFiles.Where(f => f.ToLower().EndsWith(profile.ToString().ToLower())).FirstOrDefault();
                    aux[profileId] = blobManager.ReadTextBlob(url) ;
                    profileId += 1;
                }
            }
            else
            {
                //Definition on Process definition
                string encodigProfileFileName;
                if (string.IsNullOrEmpty(this.StepConfiguration))
                {
                    //Use default
                    encodigProfileFileName = "H264 Multiple Bitrate 1080p.json";
                }
                else
                {
                    //Use process level definition
                    encodigProfileFileName = this.StepConfiguration;
                }
                aux = new string[] { myEncodigSupport.LoadEncodeProfile(encodigProfileFileName, myRequest.ProcessConfigConn)  };
            }
            return aux;
        }
        public override void HandleExecute(ChainRequest request)
        {
            //1. Step Setup 
            myRequest = (ButlerProcessRequest)request;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            string jsonControlFile = blobManager.ReadTextBlob(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue dotControlData = new jsonKeyValue(jsonControlFile);

            //2. Setup Names
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            string MediaProcessorName = dotControlData.Read("GridEncodeStep.MediaProcessorName");
            if (string.IsNullOrEmpty(MediaProcessorName))
            {
                MediaProcessorName = "Media Encoder Standard";
            }
            string jobName = string.Format("GridEncodeStep_{1}_{0}", myRequest.ProcessInstanceId,asset.Name);
            string TaskNameBase = "My task";
            
            //2. Create JOB
            IJob currentJob = _MediaServicesContext.Jobs.Create(jobName);
            
            //3. Get AMS processor
            var processor = myEncodigSupport.GetLatestMediaProcessorByName(MediaProcessorName);

            //4. Create multiples Tasks
            // string[] encodeConfigurations = GetLoadEncodignProfiles(dotControlData.Read("GridEncodeStep.encodeConfigList"));
            string[] encodeConfigurations = GetLoadEncodignProfiles(dotControlData);

            ITask firstTask = currentJob.Tasks.AddNew(TaskNameBase, processor, encodeConfigurations[0], TaskOptions.DoNotCancelOnJobFailure | TaskOptions.DoNotDeleteOutputAssetOnFailure);
            firstTask.InputAssets.Add(asset);
            IAsset theOutputAsset= firstTask.OutputAssets.AddNew(asset.Name + "_mbGridEncode", AssetCreationOptions.None);
            
            if (encodeConfigurations.Length>1)
            {
                for (int layerID = 1; layerID < encodeConfigurations.Length; layerID++)
                {
                    string layerConfig = @"";
                    layerConfig = encodeConfigurations[layerID];
                    ITask layerTask = currentJob.Tasks.AddNew(TaskNameBase + "_" + layerID.ToString(), processor, layerConfig, TaskOptions.DoNotCancelOnJobFailure | TaskOptions.DoNotDeleteOutputAssetOnFailure);
                    layerTask.InputAssets.Add(asset);
                    layerTask.OutputAssets.Add(theOutputAsset);
                }
            }

            //5. Change Event
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(myEncodigSupport.StateChanged);

            //6.Set advance on 0%
            string message = "job " + currentJob.Id + " Percent complete: 0%";
            MyEncodigSupport_JobUpdate(message, null);

            //7. Launch the job.
            currentJob.Submit();

            //8. Check Project Status
            myEncodigSupport.OnJobError += MyEncodigSupport_OnJobError;
            myEncodigSupport.JobUpdate  += MyEncodigSupport_JobUpdate;
            myEncodigSupport.WaitJobFinish(currentJob.Id);

            //9. Update AssetID
            myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;
        }
    }
}
