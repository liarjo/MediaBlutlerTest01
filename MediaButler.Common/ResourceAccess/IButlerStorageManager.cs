using MediaButler.Common.workflow;
using System;
namespace MediaButler.Common.ResourceAccess
{
    public interface IButlerStorageManager
    {
        void DeleteBlobFile(string blobUrl);
        string ReadTextBlob(string blobUrl);
        void PersistProcessStatus(ChainRequest request);
        void PersistProcessStatus(ProcessSnapShot processSnapshot);
        ProcessSnapShot readProcessSanpShot(string processName, string processId);
        string GetBlobSasUri(string blobUri,int hours);
        void parkingNewBinaries();
        IjsonKeyValue GetDotControlData(string URL);
    }
}
