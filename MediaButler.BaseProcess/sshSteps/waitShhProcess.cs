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

namespace MediaButler.BaseProcess.sshSteps
{
    class waitShhProcess : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IsshBRidgeCommand myBridge;

        
        public static string ExitCodePatter="fin-{0}-end";
        public override void HandleCompensation(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;

            sshCommandConfig myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<sshCommandConfig>(this.StepConfiguration);

            myBridge = sshCommandStep.constructBridge(myConfig);

           string exitCode = string.Format(ExitCodePatter, myRequest.ProcessInstanceId);
           string customSshCommand = myBridge.buildCommand( sshCommandStep.internalValues(myRequest),myRequest.MetaData);

            myBridge.WaitForProcessEnd(customSshCommand, exitCode, myRequest.ProcessInstanceId);
            myRequest.Log.Add(string.Format("[0] finish to execute at {1} ------------------------------", myRequest.ProcessInstanceId, DateTime.Now.ToString()));
        }
    }

    
}
