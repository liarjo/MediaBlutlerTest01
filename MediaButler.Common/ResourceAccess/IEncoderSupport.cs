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
        IJob GetJob(string jobId);
        IJob GetJobByName(string JobName);
        IAsset CreateAsset(string AssetName, string blobUrl, string MediaStorageConn, string StorageConnectionString, string WorkflowName);
        string LoadEncodeProfile(string profileInfo, string ProcessConfigConn);
        void SetPrimaryFile(IAsset MyAsset, IAssetFile theAssetFile);
    }
}
