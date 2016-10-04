using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public interface IEncoderSupport
    {
        IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName);
        void StateChanged(object sender, JobStateChangedEventArgs e);
        void WaitJobFinish(string jobId);
        event EventHandler JobUpdate;
        event EventHandler OnJobError;
        IJob GetJob(string jobId);
        IJob GetJobByName(string JobName);
        IAsset CreateAsset(string AssetName, string blobUrl, string MediaStorageConn, string StorageConnectionString, string WorkflowName);
        string LoadEncodeProfile(string profileInfo, string ProcessConfigConn);
        void SetPrimaryFile(IAsset MyAsset, IAssetFile theAssetFile);
        IJob ExecuteGridJob(string OutputAssetsName, string JobName, string MediaProcessorName, string[] EncodingConfiguration, string TaskNameBase, string AssetId, EventHandler OnJob_Error, EventHandler OnJob_Update);
        string[] GetLoadEncodignProfiles(IjsonKeyValue dotControlData, string EncodeStepEncodeConfigList, List<string> MezzanineFiles, string ProcessConfigConn, string StepConfiguration);
    }
}
