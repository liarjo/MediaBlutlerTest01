using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
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

        private IJob currentJob;
        private IEncoderSupport myEncodigSupport;
        private IButlerStorageManager myStorageManager;
        /// <summary>
        /// Load Encoding profile definition for 
        /// </summary>
        /// <returns></returns>
        private string[] getEncodeInformation()
        {
            

            //default Xml Profile
            string xmlEncodeProfile = null;
            string encodeProfileName=null;//

            //First priority Process instance level === .Control as part of the package
            if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
            {
                string jsonData = myStorageManager.ReadTextBlob(myRequest.ButlerRequest.ControlFileUri);
                IjsonKeyValue x = new jsonKeyValue(jsonData);
                try
                {
                    encodeProfileName = x.Read("encodigProfile").ToLower();
                }
                catch (Exception)
                {
                   string txtTrace = string.Format("[{0}] process Type {1} instance {2} Control has not encodigProfile definition ", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

                    Trace.TraceWarning(txtTrace);
                }
                
                if (!string.IsNullOrEmpty(encodeProfileName))
                {
                    try
                    {
                        string xmlURL = myRequest.ButlerRequest.MezzanineFiles.Where(u => u.ToLower().EndsWith(encodeProfileName)).FirstOrDefault();
                        xmlEncodeProfile = myStorageManager.ReadTextBlob(xmlURL);
                    }
                    catch (Exception)
                    {
                        xmlEncodeProfile = null;
                        string txt = string.Format("StandarEncodeStep try to read XMl profile from control but it is  {0} ", DateTime.Now.ToString());
                        Trace.TraceWarning(txt);
                    }

                }
            }
            //Second option is Process Level === Configuration
            if (xmlEncodeProfile==null)
            {
                if (!string.IsNullOrEmpty(this.StepConfiguration))
                {
                    encodeProfileName= this.StepConfiguration;
                }
                else
                {
                    encodeProfileName = "H264 Multiple Bitrate 1080p.json";
                }

                // xmlEncodeProfile = LoadEncodeProfile(encodeProfileName);
                xmlEncodeProfile = myEncodigSupport.LoadEncodeProfile(encodeProfileName, myRequest.ProcessConfigConn);
            }

            return new string[2] { xmlEncodeProfile, encodeProfileName };
        }
        private  void  ConvertMP4toSmooth(IAsset assetToConvert)
        {
            // Declare a new job to contain the tasks
            string jobName = string.Format("Convert to Smooth Streaming job {0} [{1}]",myAssetOriginal.Name,myRequest.ProcessInstanceId);
            currentJob = _MediaServicesContext.Jobs.Create(jobName);
            // Set up the first Task to convert from MP4 to Smooth Streaming. 
            // Read in task configuration XML
            var encodeData = getEncodeInformation();
            string EncodingProfileXmlData = encodeData[0];
            string encodingProfileLabel = encodeData[1];
          
            // Get a media packager reference

            IMediaProcessor processor = myEncodigSupport.GetLatestMediaProcessorByName("Media Encoder Standard");
            
            // Create a task with the conversion details, using the configuration data
            ITask task = currentJob.Tasks.AddNew("Task profile " + encodingProfileLabel,
                   processor,
                   EncodingProfileXmlData,
                   TaskOptions.None);
            // Specify the input asset to be converted.
            task.InputAssets.Add(assetToConvert);
            // Add an output asset to contain the results of the job.
            task.OutputAssets.AddNew(assetToConvert.Name+"_mb", AssetCreationOptions.None);
            // Use the following event handler to check job progress. 
            // The StateChange method is the same as the one in the previous sample
            //currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);
            currentJob.StateChanged += new EventHandler<JobStateChangedEventArgs>(myEncodigSupport.StateChanged);

            //Set advantce on 0%
            string message = "job " + currentJob.Id + " Percent complete: 0%";
            MyEncodigSupport_JobUpdate(message, null);
            
            // Launch the job.
            currentJob.Submit();

            //8. Idenpotence MARK
            myRequest.MetaData.Add(this.GetType() + "_" + myRequest.ProcessInstanceId, currentJob.Id);
            myStorageManager.PersistProcessStatus(myRequest);

            //9. Check Project Status
            myEncodigSupport.OnJobError += MyEncodigSupport_OnJobError;
            myEncodigSupport.JobUpdate += MyEncodigSupport_JobUpdate;
            myEncodigSupport.WaitJobFinish(currentJob.Id);
            
        }
        /// <summary>
        /// Send Jpb and task error message to Trace and Reuqest exception list
        /// </summary>
        /// <param name="sender">JOB</param>
        /// <param name="e"></param>
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
       /// <summary>
        /// Update Transcodig Job advance on Metadata context
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyEncodigSupport_JobUpdate(object sender, EventArgs e)
        {
            string message = (string)sender;
            
            if (!myRequest.MetaData.Any(item=>item.Key==Configuration.TranscodingAdvance))
            {
                myRequest.MetaData.Add(Configuration.TranscodingAdvance, message);
            }
            else
            {
                myRequest.MetaData[Configuration.TranscodingAdvance] = message;
            }
            myStorageManager.PersistProcessStatus(myRequest);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IdenpotenceControl()
        {
            if (myRequest.MetaData.ContainsKey(this.GetType() + "_" + myRequest.ProcessInstanceId))
            {
                string jobidContext = myRequest.MetaData[this.GetType() + "_" + myRequest.ProcessInstanceId];
                currentJob = (from j in _MediaServicesContext.Jobs select j).Where(j => j.Id == jobidContext).FirstOrDefault();

                if (currentJob!=null)
                {
                    if (currentJob.State == JobState.Error)
                    {
                        currentJob = null;
                        myRequest.MetaData.Remove(this.GetType() + "_" + myRequest.ProcessInstanceId);
                    }
                }
            }

            return (currentJob == null);
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            //My request
            myRequest = (ButlerProcessRequest)request;
            //Media Context
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            //0 Encoding Helper
            myEncodigSupport = new EncoderSupport(_MediaServicesContext);
            //1. Storage Manager
            myStorageManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            //2. Load Original Asset (current on context)
            myAssetOriginal = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            if (IdenpotenceControl())
            {
                ConvertMP4toSmooth(myAssetOriginal);
            }
            else
            {
                //Job Just wait for finish the current job
                myEncodigSupport.OnJobError += MyEncodigSupport_OnJobError;
                myEncodigSupport.JobUpdate += MyEncodigSupport_JobUpdate;
                myEncodigSupport.WaitJobFinish(currentJob.Id);
            }
            //Update AssetID
            updateAsset();

        }
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            deleteOutput();
            deleteOtiginalAsset();
        }
        private void deleteOutput()
        {
            if (currentJob != null)
            {
                //Delete Output Asset is exist
                foreach (IAsset item in currentJob.OutputMediaAssets)
                {
                    string txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, item.Id);
                    item.Delete();
                    Trace.TraceWarning(txtTrace);
                }
            }
        }
        private void deleteOtiginalAsset()
        {
            //Delete Original Asset becouse it is wrong
            if (myAssetOriginal != null)
            {
                string txtTrace = string.Format("[{0}] process Type {1} instance {2} deleted Asset id={3}", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, myAssetOriginal.Id);

                myAssetOriginal.Delete();
                Trace.TraceWarning(txtTrace);
            }
        }
        private void updateAsset()
        {
            myRequest.AssetId = currentJob.OutputMediaAssets.FirstOrDefault().Id;

        }
    }
}
