using MediaButler.Common;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
  

    class QueueNotificacionData
    {
        /// <summary>
        /// 0=string ; 1=json
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// Name of the QUEUE for send notification messages
        /// </summary>
        public string QueueName { get; set; }
        public QueueNotificacionData()
        {
            Type = 0;
            QueueName = "butlermediaintegrationqueue";
        }
    }
    /// <summary>
    /// Send Notificacion to QUEUE with Asset & Locators  + LOG info
    /// </summary>
    class QueueNotificacionStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private QueueNotificacionData myConfig;
        private void Setup()
        {
            //_MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServiceContext = myRequest.MediaServiceContext();
            if (!string.IsNullOrEmpty(this.StepConfiguration))
            {
                myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<QueueNotificacionData>(this.StepConfiguration);
            }
            else 
            {
                myConfig = new QueueNotificacionData();
            }
        }
        private string GetStringMessage()
        {

            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            AssetInfo ai = new AssetInfo(x);
            StringBuilder xxx = ai.GetStatsTxt();

            xxx.AppendLine("");
            xxx.AppendLine("Media Butler Process LOG " + DateTime.Now.ToString());
            foreach (string txt in myRequest.Log)
            {
                xxx.AppendLine(txt);

            }
            xxx.AppendLine("-----------------------------");
            return xxx.ToString(); 
 
        }
        private string GetJsonMessage()
        {
            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            AssetInfo ai = new AssetInfo(x);
            return  ai.GetStatJson();
        }
        private void SendMessage()
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(myConfig.QueueName);
            queue.CreateIfNotExists();
            string txtMessageBody="";

            switch (myConfig.Type)
            {
                case 0:
                    txtMessageBody = this.GetStringMessage();
                    break;
                default:

                    txtMessageBody = this.GetJsonMessage();
                    break;
            }
            CloudQueueMessage message = new CloudQueueMessage(txtMessageBody);
            queue.AddMessage(message);
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            SendMessage();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
