using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Replica
{
   //TODO: Fix to use Service Principal authentication
    //class TriggerReplicaStep:MediaButler.Common.workflow.StepHandler
    //{
    //    private ButlerProcessRequest myRequest;
    //    private CloudMediaContext _MediaServicesContext;
    //    private TriggerReplicaData myConfig;
    //    private IAsset theAsset;

    //    private void Setup()
    //    {
    //        //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
    //        _MediaServicesContext = myRequest.MediaServiceContext();
    //        theAsset = _MediaServicesContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();

    //        if (string.IsNullOrEmpty(this.StepConfiguration))
    //        {
    //            myConfig = new TriggerReplicaData();
    //            myConfig.StageConnString = myRequest.ProcessConfigConn;
    //        }
    //        else
    //        {
    //            myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<TriggerReplicaData>(this.StepConfiguration);
    //        }
    //    }
    //    /// <summary>
    //    /// Create a .control file, with the Asset Id info for trigger a new process
    //    /// </summary>
    //    private void TriggerReplica()
    //    {
    //        try
    //        {
    //            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myConfig.StageConnString);
    //            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
    //            CloudBlobContainer container = blobClient.GetContainerReference(myConfig.StageContainer);
    //            container.CreateIfNotExists();

    //            //Empty.MP4, Watcher compatibility request
    //            CloudBlockBlob blockBlob = container.GetBlockBlobReference("Incoming/" + myRequest.ProcessInstanceId + "/empty.mp4");
    //            using (MemoryStream data = new MemoryStream())
    //            {
    //                using (var writer = new StreamWriter(data))
    //                {
    //                    writer.Write("");
    //                    writer.Flush();
    //                    data.Position = 0;
    //                    blockBlob.UploadFromStream(data);
    //                }
    //            }

    //            //Control File
    //            blockBlob = container.GetBlockBlobReference("Incoming/" + myRequest.ProcessInstanceId + "/" + theAsset.Name + ".control");
    //            blockBlob.DeleteIfExists();

    //            Hashtable triggerInfo = new Hashtable();
    //            triggerInfo.Add("AssetId", theAsset.Id);
    //            triggerInfo.Add("MediaAccountName", myRequest.MediaAccountName);
    //            triggerInfo.Add("MediaAccountKey", myRequest.MediaAccountKey);
    //            triggerInfo.Add("MediaStorageConn", myRequest.MediaStorageConn);
    //            string jsontriggerInfo = Newtonsoft.Json.JsonConvert.SerializeObject(triggerInfo);

    //            using (MemoryStream data = new MemoryStream())
    //            {
    //                using (var writer = new StreamWriter(data))
    //                {
    //                    writer.Write(jsontriggerInfo);
    //                    writer.Flush();
    //                    data.Position = 0;
    //                    blockBlob.UploadFromStream(data);
    //                }
    //            }
    //        }
    //        catch (Exception X)
    //        {
    //            Trace.TraceError("[{0}] Error process {1} instance {2} error: {3}", this.GetType().FullName,myRequest.ProcessTypeId,myRequest.ProcessInstanceId,X.Message);
    //            throw X;
    //        }
           

    //    }
    //    public override void HandleExecute(Common.workflow.ChainRequest request)
    //    {
    //        //Butler Request
    //        myRequest = (ButlerProcessRequest)request;
    //        //Setup Step
    //        Setup();
    //        //Trigger Replica
    //        TriggerReplica();
    //    }

    //    public override void HandleCompensation(Common.workflow.ChainRequest request)
    //    {
    //        Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
    //    }
    //}
}
