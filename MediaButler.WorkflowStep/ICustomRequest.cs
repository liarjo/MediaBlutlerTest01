using System;
using System.Collections.Generic;


namespace MediaButler.WorkflowStep
{
    public interface ICustomRequest
    {
        string AssetId { get; set; }
        string MediaAccountName { get; set; }
        string MediaAccountKey { get; set; }
        string MediaStorageConn { get; set; }
        string ConfigData { get; set; }
        DateTime TimeStampProcessingStarted { get; set; }
        List<string> Log { get; set; }
        Dictionary<string, string> MetaData { get; set; }
        string ProcessTypeId { get; set; }
        string ProcessInstanceId { get; set; }
        List<string> Exceptions { get; set; }
        string ProcessConfigConn { get; set; }
        string ButlerRequest_ControlFileUri { get; set; }
        List<string> ButlerRequest_MezzanineFiles { get; set; }
        string StepConfiguration { get; set; }
    }
}
