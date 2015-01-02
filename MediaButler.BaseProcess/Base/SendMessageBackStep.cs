using MediaButler.Common;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class SendMessageBackStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;

        private void SendMessage()
        {
            string qName = MediaButler.Common.Configuration.ButlerSuccessQueue;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            //TODO: queue info
            CloudQueue queue = queueClient.GetQueueReference(qName);
            MediaButler.Common.ButlerResponse myButlerResponse = new Common.ButlerResponse();

            myButlerResponse.MezzanineFiles = myRequest.ButlerRequest.MezzanineFiles;
            myButlerResponse.TimeStampProcessingCompleted = DateTime.Now.ToString();
            myButlerResponse.TimeStampProcessingStarted = myRequest.TimeStampProcessingStarted.ToString();
            myButlerResponse.WorkflowName = myRequest.ProcessTypeId;
            myButlerResponse.MessageId = myRequest.ButlerRequest.MessageId;
            myButlerResponse.TimeStampRequestSubmitted = myRequest.ButlerRequest.TimeStampUTC;
            myButlerResponse.StorageConnectionString = myRequest.ButlerRequest.StorageConnectionString;

            


            CloudMediaContext _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            AssetInfo ai = new AssetInfo(x);
            StringBuilder AssetInfoResume = ai.GetStatsTxt();

            AssetInfoResume.AppendLine("");
            AssetInfoResume.AppendLine("Media Butler Process LOG " + DateTime.Now.ToString());
            foreach (string txt in myRequest.Log)
            {
                AssetInfoResume.AppendLine(txt);
               
            }
            AssetInfoResume.AppendLine("-----------------------------");
            myButlerResponse.Log = AssetInfoResume.ToString();


            CloudQueueMessage responseMessae = new CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject(myButlerResponse));
            queue.AddMessage(responseMessae);
            Trace.TraceInformation("Return Butler Message sent to queue");
        }

        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            //Send output info back using ButlerResponse Message LOG
            SendMessage();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

            
        }
    }
}
