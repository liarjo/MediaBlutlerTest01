using MediaButler.Common.workflow;
using System;
namespace MediaButler.Common.ResourceAccess
{
    public interface IBlobStorageManager
    {
        void DeleteBlobFile(string blobUrl);
        string ReadTextBlob(string blobUrl);
        void PersistProcessStatus(ChainRequest request);
    }
}
