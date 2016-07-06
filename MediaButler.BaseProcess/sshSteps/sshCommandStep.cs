using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using System.Diagnostics;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MediaButler.Common.ResourceAccess;
using System.IO;
using System.Reflection;

namespace MediaButler.BaseProcess.sshSteps
{
    public class sshCommandStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager blobManager;
        private IsshBRidgeCommand myBridge;
        static public IsshBRidgeCommand constructBridge(sshCommandConfig BridgeConfiguration)
        {
            Assembly myAssembly1 = Assembly.LoadFrom(BridgeConfiguration.BridgePath);
            Type myType = myAssembly1.GetType(BridgeConfiguration.BridgeName);
            IsshBRidgeCommand theBridge=(IsshBRidgeCommand) Activator.CreateInstance(myType);
            theBridge.sshConfiguration = BridgeConfiguration;
            return theBridge;
        }
        static public string VideoName(string videoUrl)
        {
            Uri x = new Uri(videoUrl);
            return x.Segments[x.Segments.Count() - 1];
        }
        static public string[] internalValues(ButlerProcessRequest xRequest)
        {
            var bm= BlobManagerFactory.CreateBlobManager(xRequest.ProcessConfigConn);
            string[] internalValues = new string[10];
            internalValues[0] = xRequest.ProcessInstanceId;
            internalValues[1] = xRequest.ProcessTypeId;
            internalValues[2] = xRequest.ProcessConfigConn;
            internalValues[3] = bm.GetBlobSasUri(xRequest.ButlerRequest.MezzanineFiles.FirstOrDefault(),5);
            internalValues[4] = VideoName(xRequest.ButlerRequest.MezzanineFiles.FirstOrDefault());
            return internalValues;
        }
        
        public override void HandleCompensation(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            
            sshCommandConfig myConfig= Newtonsoft.Json.JsonConvert.DeserializeObject<sshCommandConfig>(this.StepConfiguration);

            myBridge = sshCommandStep.constructBridge(myConfig);

            string customSshCommand = myBridge.buildCommand(sshCommandStep.internalValues(myRequest),myRequest.MetaData);

            myRequest.Log.Add(string.Format("[0] Start to execute at {1} Command {2}",myRequest.ProcessInstanceId,DateTime.Now.ToString(),customSshCommand));

            myBridge.execCommand(customSshCommand);

            string message = string.Format("[{0}] {3}  Result {1}: {2}", myRequest.ProcessInstanceId, myBridge.ResultCode, myBridge.Result, DateTime.Now.ToString());
            myRequest.Log.Add(message);
        }
    }

   

}
