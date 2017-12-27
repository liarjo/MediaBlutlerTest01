using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    //TODO: Fix to use Service Principal autentication
    //class SetupAMSAccountStep : MediaButler.Common.workflow.StepHandler
    //{
    //    private ButlerProcessRequest myRequest;
    //    private MediaButler.BaseProcess.Replica.ProcessReplicaData myConfig;

    //    public override void HandleExecute(Common.workflow.ChainRequest request)
    //    {
    //        myRequest = (ButlerProcessRequest)request;
    //        //Check and load AMS configuration from step coniguration
    //        if (string.IsNullOrEmpty(this.StepConfiguration))
    //        {
    //            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not AMS configuration Data", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

    //            throw new Exception(errorTxt);
    //        }
    //        myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<MediaButler.BaseProcess.Replica.ProcessReplicaData>(this.StepConfiguration);
    //        //Setup AMS target to work.
    //        myRequest.ChangeMediaServices(myConfig.TargetAMSName, myConfig.TargetAMSKey, myConfig.TargetAMSStorageConn);
    //    }

    //    public override void HandleCompensation(Common.workflow.ChainRequest request)
    //    {
    //        Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
    //    }
    //}
}
