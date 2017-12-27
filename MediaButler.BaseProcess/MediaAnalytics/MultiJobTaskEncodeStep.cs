using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.MediaAnalytics
{
    class MultiJobTaskEncodeStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private IEncoderSupport myEncodigSupport;
        private IButlerStorageManager myBlobManager;
        IjsonKeyValue allPorcessData;

        private int consolidateId = -1;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, request.ProcessTypeId, request.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
        private List<IAssetFile> GetFiles(IAsset myAsset, string[] extensions)
        {
            List<IAssetFile> xLists = new List<IAssetFile>();

            foreach (var file in myAsset.AssetFiles)
            {
                string ext = Path.GetExtension(file.Name);
                if (extensions.Contains(ext))
                {
                    xLists.Add(file);
                }
            }
            return xLists;
        }
        private List<IJobConfiguration> GetJobConfig(string ProcessId, string InputAssetId, string OutPutAssetId)
        {
            List<IJobConfiguration> myJobs = new List<IJobConfiguration>();
            JArray MultiJobTaskEncode = (JArray)allPorcessData.ReadArray("MultiJobTaskEncode");

            consolidateId = int.Parse(allPorcessData.Read("ConsolidateOutput"));

            for (int i = 0; i < MultiJobTaskEncode.Count(); i++)
            {

                string ProcessorName = (string)MultiJobTaskEncode[i]["ProcessorName"];
                string[] ProfileFileList = ((JArray)MultiJobTaskEncode[i]["ProfileFileList"]).ToObject<string[]>();
                string[] FilesToCopy = ((JArray)MultiJobTaskEncode[i]["FilesToCopy"]).ToObject<string[]>();
                for (int profileId = 0; profileId < ProfileFileList.Count(); profileId++)
                {
                    ProfileFileList[profileId] = myBlobManager.ReadTextBlob("mediabutlerbin", "encoderdefinitions/" + ProfileFileList[profileId]);
                }
                JobConfiguration J = new JobConfiguration(ProcessorName, ProcessId);
                J.AddTask(ProfileFileList, InputAssetId, OutPutAssetId, FilesToCopy);
                myJobs.Add(J);
            }

            return myJobs;
        }
        private string getJobName(IJobConfiguration myJobDef)
        {

            return string.Format("[{0}]{1}", myJobDef.Id, myJobDef.Processor);
        }
        private string getTaskName(IJobConfiguration myJobDef, int id, int layer)
        {
            IAsset myInputAsset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myJobDef.InputAssetId[id]).FirstOrDefault();
            return string.Format("{1}.Task[{0}]{2}", layer, myInputAsset.Name, myJobDef.TaskId[id]);
        }
        private void CopyAssetFiles(IAsset myAssetTo, List<IAssetFile> files)
        {
           
            foreach (var assetFile in files)
            {
                string magicName = string.Format(@".\{0}\{1}", myRequest.ProcessInstanceId, assetFile.Name);
                assetFile.Download(magicName);
                try
                {
                    Trace.TraceInformation("Copying {0}", magicName);
                    IAssetFile newFile = myAssetTo.AssetFiles.Create(assetFile.Name);
                    newFile.Upload(magicName);
                    newFile.Update();
                }
                catch (Exception X)
                {
                    Trace.TraceError("Error CopyAssetFiles " + X.Message);
                    if (File.Exists(magicName))
                    {
                        System.IO.File.Delete(magicName);
                    }
                    
                    throw X;
                }
                System.IO.File.Delete(magicName);

            }
            myAssetTo.Update();


        }
        private ITask GetTask(IJobConfiguration jobDef, int jobId, int layerId)
        {
            IJob xJob = (from j in _MediaServicesContext.Jobs select j).Where(j => j.Name == getJobName(jobDef)).FirstOrDefault();

            string taskKey = getTaskName(jobDef, jobId, layerId);

            return (from t in xJob.Tasks select t).Where(t => t.Name == taskKey).FirstOrDefault();

        }
        private string UpdateOutPut(List<IJobConfiguration> myJobs, int XConsolidateId)
        {
            string outPutID = null;
            if (XConsolidateId > -1)
            {
                ITask xTask = GetTask(myJobs[consolidateId], 0, 0);
                outPutID = xTask.OutputAssets[0].Id;
                foreach (var jobDef in myJobs)
                {
                    for (int i = 0; i < jobDef.TaskDefinition.Count(); i++)
                    {
                        jobDef.OutputAssetId[i] = outPutID;
                    }
                }
            }
            return outPutID ?? GetTask(myJobs[0], 0, 0).OutputAssets[0].Id;
        }
        private void ConsolidateOutputs(List<IJobConfiguration> myJobs, int XConsolidateId)
        {
            IAsset ConsolidateOutPut;
            Directory.CreateDirectory(myRequest.ProcessInstanceId);
            foreach (var jobDef in myJobs)
            {
                for (int i = 0; i < jobDef.TaskDefinition.Count(); i++)
                {
                    if (jobDef.OutputAssetId[i] != "")
                    {
                        ConsolidateOutPut = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == jobDef.OutputAssetId[i]).FirstOrDefault();
                        ITask xTask = GetTask(jobDef, 0, 0);
                        IAsset taskOutputAsset = xTask.OutputAssets.FirstOrDefault();
                        List<IAssetFile> filesToCopy;

                        if ((jobDef.CopyFilesFilter[i] != null) && (taskOutputAsset.Id != ConsolidateOutPut.Id))
                        {

                            filesToCopy = GetFiles(taskOutputAsset, jobDef.CopyFilesFilter[i]);
                            CopyAssetFiles(ConsolidateOutPut, filesToCopy);
                            taskOutputAsset.Delete();
                        }
                    }
                }
            }
            Directory.Delete(myRequest.ProcessInstanceId);
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
        public override void HandleExecute(ChainRequest request)
        {
            //1. Setup Step
            myRequest = (ButlerProcessRequest)request;
            //_MediaServicesContext = new CloudMediaContext(new MediaServicesCredentials(myRequest.MediaAccountName, myRequest.MediaAccountKey));
            _MediaServicesContext = myRequest.MediaServiceContext();
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            allPorcessData = myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);

            //2. Create Jobs definition
            List<IJobConfiguration> myJobsDefinition = GetJobConfig(myRequest.ProcessInstanceId, myRequest.AssetId, "");

            //3. Execute
            myEncodigSupport.ExecuteMultiJobTaskEncode(myJobsDefinition, myRequest.ProcessInstanceId, MyEncodigSupport_OnJobError, MyEncodigSupport_JobUpdate);
      
            //4. Update context with output asset ID
            myRequest.AssetId = UpdateOutPut(myJobsDefinition, this.consolidateId);

            //5. Copy all asset files to Output asset and delete all temp Asset
            ConsolidateOutputs(myJobsDefinition, consolidateId);

        }
    }
}
