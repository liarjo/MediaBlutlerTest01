using MediaButler.Common.workflow;
using System;
namespace MediaButler.Common.ResourceAccess
{
    public interface IButlerStorageManager
    {
        void DeleteBlobFile(string blobUrl);
        string ReadTextBlob(Uri blobUrl);
        string ReadTextBlob(string containerName,string blobName);
        void PersistProcessStatus(ChainRequest request);
        void PersistProcessStatus(ProcessSnapShot processSnapshot);
        ProcessSnapShot readProcessSanpShot(string processName, string processId);
        string GetBlobSasUri(string blobUri,int hours);
        void parkingNewBinaries();
        IjsonKeyValue GetDotControlData(string URL);
        IjsonKeyValue GetProcessConfig(string dotControlUrl, string ProcessTypeId);
        string GetButlerConfigurationValue(string partition, string row);
    }
}
