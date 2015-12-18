using MediaButler.Common;
using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class MessageHiddeControlStep : MediaButler.Common.workflow.StepHandler
    {
        int timeSpanMessage;
        int sleepSeconds;
        public void Setup()
        {
             timeSpanMessage = 30;
             sleepSeconds = 10;
             
        }
        /// <summary>
        /// Of the process ID has context on ButlerWorkflowstatus table, it is load on Metadata
        /// </summary>
        /// <param name="myRequest"></param>
        private void ButlerWorkflowstatus(ButlerProcessRequest myRequest)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
            TableOperation retrieveOperation = TableOperation.Retrieve<ProcessSnapShot>(myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                string txtMessage = string.Format("[{0}] {1} {2} instance {2}", this.GetType().FullName, "On runnung Instance", myRequest.ProcessInstanceId, myRequest.ProcessTypeId);
                Trace.TraceInformation(txtMessage);

                dynamic dynObj = Newtonsoft.Json.JsonConvert.DeserializeObject(((ProcessSnapShot)retrievedResult.Result).jsonContext);
                //Dictionary<string, string> dynMetaData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>((dynObj.MetaData.ToString()));
                myRequest.MetaData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>((dynObj.MetaData.ToString()));


            }
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            ButlerProcessRequest myRequest = (ButlerProcessRequest)request;
            Setup();
            if (myRequest.MessageHiddenTaskStatus==TaskStatus.WaitingToRun)
            {
                //Start to hidden the Process trgigger Message
                myRequest.StartMessageHidden(timeSpanMessage, sleepSeconds);
                Trace.TraceInformation("{0}Process type {1} instance {2} start hidden Message {3}",
                    this.GetType().FullName,
                    myRequest.ProcessTypeId, 
                    myRequest.ProcessInstanceId,
                    myRequest.ButlerRequest.MessageId);
                //Read if We have information on ButlerWorkflowstatus
                ButlerWorkflowstatus(myRequest);
            } 
                else
            {
                //Hidden is running, now Stop it
                myRequest.StopMessageHidden();
                do
                {
                    System.Threading.Thread.Sleep(1 * 1000);

                } while (myRequest.MessageHiddenTaskStatus != TaskStatus.RanToCompletion);
                
                
                Trace.TraceInformation("{0}Process type {1} instance {2} stop hidden Message {3}",
                    this.GetType().FullName,
                    myRequest.ProcessTypeId,
                    myRequest.ProcessInstanceId,
                    myRequest.ButlerRequest.MessageId);

                //DELETE MESSAGE Butler request
                myRequest.DeleteCurrentMessage();

                Trace.TraceInformation("{0}Process type {1} instance {2} delete Message {3}",
                    this.GetType().FullName,
                    myRequest.ProcessTypeId,
                    myRequest.ProcessInstanceId,
                    myRequest.ButlerRequest.MessageId);

            }
            
            
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            ButlerProcessRequest myRequest = (ButlerProcessRequest)request;
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
