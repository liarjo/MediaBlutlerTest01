using System;
using System.Collections.Generic;
using System.Linq;
using MediaButler.Common.workflow;
using MediaButler.Common.ResourceAccess;
using System.Diagnostics;
using MediaButler.WorkflowStep;
using System.Reflection;

namespace MediaButler.BaseProcess.Control
{
    class MediaButlerCustomStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager blobManager;
        private ICustomStepExecution myCustomStepExecution;
        private ICustomRequest myCustomRequest;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
        private ICustomRequest buildRequest(ButlerProcessRequest xRequest)
        {
            ICustomRequest yRequest = new customeRequest();
            yRequest.AssetId = xRequest.AssetId;
            yRequest.ConfigData = xRequest.ConfigData;
            yRequest.Exceptions = xRequest.Exceptions;
            yRequest.Log = xRequest.Log;
            yRequest.MediaAccountKey = xRequest.MediaAccountKey;
            yRequest.MediaAccountName = xRequest.MediaAccountName;
            yRequest.MediaStorageConn = xRequest.MediaStorageConn;
            yRequest.MetaData = xRequest.MetaData;
            yRequest.ProcessConfigConn = xRequest.ProcessConfigConn;
            yRequest.ProcessInstanceId = xRequest.ProcessInstanceId;
            yRequest.ProcessTypeId = xRequest.ProcessTypeId;
            yRequest.TimeStampProcessingStarted = xRequest.TimeStampProcessingStarted;
            yRequest.ButlerRequest_ControlFileUri=xRequest.ButlerRequest.ControlFileUri;
            yRequest.ButlerRequest_MezzanineFiles=xRequest.ButlerRequest.MezzanineFiles;
            yRequest.StepConfiguration = this.StepConfiguration;
            return yRequest;
        }
        private ICustomStepExecution buildCustomStep(string path, string name)
        {
            Assembly myAssembly1;
            if (path.IndexOf('\\')!=-1)
                 myAssembly1 = Assembly.LoadFrom(path);
            else
                 myAssembly1 = Assembly.Load(blobManager.ReadBytesBlob(path));
            Type myType = myAssembly1.GetType(name);
            return   (ICustomStepExecution)Activator.CreateInstance(myType);
           
        }
        private void sendError(string msg)
        {
            Trace.TraceError(msg);
            throw new Exception(msg);
        }
        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            IjsonKeyValue stepConfig = new jsonKeyValue(StepConfiguration);
            
            
            try
            {
                //Lead External Step from DLL
                myCustomStepExecution = buildCustomStep(stepConfig.Read("AssemblyName"), stepConfig.Read("TypeName"));
            }
            catch (Exception X)
            {
                sendError("MediaButlerCustomStep Loading error : " + X.Message);
            }
           
            //Transform ButlerProcessRequest to ICustomRequest
            myCustomRequest = buildRequest(myRequest);
            //Execute sync
            try
            {
                myCustomStepExecution.execute(myCustomRequest);

                //UPdate myRequest
                myRequest.AssetId = myCustomRequest.AssetId;
            }
            catch (Exception X)
            {
                sendError("MediaButlerCustomStep Execute error " + X.Message);

            }
            //Update status           
            UpdateProcessStatus(myRequest, "Finish Custome Execution");
        }
    }
}
