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
        private IButlerStorageManager myBlobManager;
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
            myBlobManager.PersistProcessStatus(myRequest);
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
        private void CopyCaptions(IAsset myAssetFrom, IAsset myAssetTo)
        {
            var captionFileList = (from f in myAssetFrom.AssetFiles select f).Where(f => 
                f.Name.EndsWith(".ttml") ||
                f.Name.EndsWith(".vtt") ||
                f.Name.EndsWith(".kw.xml")
            );
            foreach (var assetFile in captionFileList)
            {
                string magicName = assetFile.Name;
                assetFile.Download(magicName);
                IAssetFile newFile = myAssetTo.AssetFiles.Create(assetFile.Name);
                newFile.Upload(magicName);
                System.IO.File.Delete(magicName);
                newFile.Update();
            }
            myAssetTo.Update();
        }
        public override void HandleExecute(ChainRequest request)
        {
            //1. Step Setup 
            myRequest = (ButlerProcessRequest)request;
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            IjsonKeyValue dotControlData = myBlobManager.GetDotControlData(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue processData = new jsonKeyValue(myBlobManager.GetButlerConfigurationValue(ProcessConfigKeys.DefualtPartitionKey, myRequest.ProcessTypeId + ".config"));

            IjsonKeyValue allPorcessData= myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);

            //2. Get Current Asset
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            
            //3. JOB parameters
            string OutputAssetsName = asset.Name + "_mbIndex2";
            string JobName = string.Format("GridEncodeStep_{1}_{0}", myRequest.ProcessInstanceId, asset.Name);
            string MediaProcessorName = myEncodigSupport.GetMediaProcessorName(allPorcessData,DotControlConfigKeys.Index2EncodeStepMediaProcessorName, "Azure Media Indexer 2 Preview");
            string[] encodeConfigurations = myEncodigSupport.GetLoadEncodignProfiles(dotControlData, processData,DotControlConfigKeys.Index2EncodeStepEncodeConfigList, myRequest.ButlerRequest.MezzanineFiles, myRequest.ProcessConfigConn, this.StepConfiguration);

            //4. Execute JOB and Wait
            IJob currentJob = myEncodigSupport.ExecuteGridJob(OutputAssetsName, JobName, MediaProcessorName, encodeConfigurations, "Indexing Task", asset.Id, MyEncodigSupport_OnJobError, MyEncodigSupport_JobUpdate);

            //5. Copy subtitles files to input asste?
            if ("yes" == dotControlData.Read(DotControlConfigKeys.Index2PreviewCopySubTitles))
            {
                CopyCaptions(currentJob.OutputMediaAssets.FirstOrDefault(), asset);
                currentJob.OutputMediaAssets.FirstOrDefault().Delete();
            }
            else
            {
                myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;
            }
        }
        

       
    }
}
