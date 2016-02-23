using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

using Microsoft.WindowsAzure.Storage.Queue;
using MediaButler.Common;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;

namespace MediaButler.Workflow
{
    public class ButlerWorkFlowManagerWorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        public override void Run()
        {
            Trace.TraceInformation("MediaButler.Workflow is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("MediaButler.Workflow has been started");

            return result;
        }
        public override void OnStop()
        {
            Trace.TraceInformation("MediaButler.Workflow is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("MediaButler.Workflow has stopped");
        }
    
        private MediaButler.Common.Host.ConfigurationData myConfigData;
        private CloudQueue InWorkQueue;
        
        private MediaButler.Common.workflow.ProcessHandler myProcessHandler;
        /// <summary>
        /// Setup Role
        /// </summary>
        private void Setup()
        {
            string json = MediaButler.Common.Configuration.GetConfigurationValue("roleconfig", this.GetType().FullName, CloudConfigurationManager.GetSetting("MediaButler.ConfigurationStorageConnectionString"));
            myConfigData = Newtonsoft.Json.JsonConvert.DeserializeObject<MediaButler.Common.Host.ConfigurationData>(json);
            myConfigData.poisonQueue = MediaButler.Common.Configuration.ButlerFailedQueue;
            myConfigData.inWorkQueueName = MediaButler.Common.Configuration.ButlerSendQueue;
            myConfigData.ProcessConfigConn = RoleEnvironment.GetConfigurationSettingValue("MediaButler.ConfigurationStorageConnectionString");
            myConfigData.MaxCurrentProcess = myConfigData.MaxCurrentProcess;
            myConfigData.SleepDelay = myConfigData.SleepDelay;
            myConfigData.MaxDequeueCount = myConfigData.MaxDequeueCount;

        }
        /// <summary>
        /// Get the message from IN Queue
        /// </summary>
        /// <param name="myqueueClient">QueClient</param>
        /// <returns>New Message, it can be null if it has not message</returns>
        private CloudQueueMessage GetNewMessage(CloudQueueClient myqueueClient)
        {
            CloudQueueMessage currentMessage=null;
            try
            {

                InWorkQueue = myqueueClient.GetQueueReference(myConfigData.inWorkQueueName);
                currentMessage = InWorkQueue.GetMessage();
            }
            catch (Exception X)
            {
                string txt = string.Format("[{0}] at {1} has an error: {2}", this.GetType().FullName, "GetNewMessage", X.Message);
                Trace.TraceError(txt);
            }
            return currentMessage;
        }
        /// <summary>
        /// Check if the incoming message was dequeue more than N time. N is defines in configurtion
        /// </summary>
        /// <param name="theMessage">incoming message</param>
        /// <returns>Is or not poisson message</returns>
        private bool CheckPoison(CloudQueueMessage theMessage)
        {
            return (myConfigData.MaxDequeueCount < theMessage.DequeueCount);
        }
        /// <summary>
        /// Send back to watcher a "Posion messsage" and delete from in queue
        /// </summary>
        /// <param name="poisonMessage">the poison message</param>
        /// <returns>Sueccess or not</returns>
        private bool SendPoisonMessage(CloudQueueMessage poisonMessage)
        {
            bool sw = false;
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myConfigData.ProcessConfigConn);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue poisonQueue = queueClient.GetQueueReference(myConfigData.poisonQueue);
                poisonQueue.CreateIfNotExists();

                MediaButler.Common.ButlerRequest myButlerRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ButlerRequest>(poisonMessage.AsString);
                MediaButler.Common.ButlerResponse myButlerResponse = new Common.ButlerResponse();

                myButlerResponse.MezzanineFiles = myButlerRequest.MezzanineFiles;
                ////Add to Mezzamine Files the control File URL if it exist
                //Becouse it is needed to move/delete the control file from processing to succes or fail
                if (!string.IsNullOrEmpty(myButlerRequest.ControlFileUri))
                {
                    myButlerResponse.MezzanineFiles.Add(myButlerRequest.ControlFileUri);
                }
                myButlerResponse.TimeStampProcessingCompleted = DateTime.Now.ToString();
                myButlerResponse.TimeStampProcessingStarted = DateTime.Now.ToString();
                myButlerResponse.WorkflowName = myButlerRequest.WorkflowName;
                myButlerResponse.MessageId = myButlerRequest.MessageId;
                myButlerResponse.TimeStampRequestSubmitted = myButlerRequest.TimeStampUTC;
                myButlerResponse.StorageConnectionString = myButlerRequest.StorageConnectionString;
                myButlerResponse.Log = "Poison Message";

                //Lookin for Errors in Table Status               
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(MediaButler.Common.Configuration.ButlerWorkflowStatus);
                TableOperation retrieveOperation = 
                    TableOperation.Retrieve<MediaButler.Common.workflow.ProcessSnapShot>(myButlerRequest.WorkflowName,myButlerRequest.MessageId.ToString());
                TableResult retrievedResult = table.Execute(retrieveOperation);
                if (retrievedResult.Result!=null)
                {
                    //we have process info
                    var status=(MediaButler.Common.workflow.ProcessSnapShot)retrievedResult.Result;
                    try
                    {
                        var request = Newtonsoft.Json.JsonConvert.DeserializeObject<MediaButler.Common.workflow.ChainRequest>(status.jsonContext);
                        foreach (var error in request.Exceptions)
                        {
                            myButlerResponse.Log += "\r\n" + error.Message;
                        }
                    }
                    catch (Exception X)
                    {
                        Trace.TraceWarning("Unable to load Error LOG in response.log on poison message");
                        myButlerResponse.Log += "\r\n" + X.Message;
                        myButlerResponse.Log += "\r\n" + status.jsonContext;
                        
                    }
                    
                    
                    //Delete register from Status table
                    TableOperation insertOperation = TableOperation.Delete(status);
                    table.Execute(insertOperation);
                }
                //Send Poison Mesagge
                CloudQueueMessage poison = new CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject(myButlerResponse));
                poisonQueue.AddMessage(poison);
                sw = true;
            }
            catch (Exception X)
            {

                string txt = string.Format("[{0}] at {1} has an error: {2}", this.GetType().FullName, "GetNewMessage", X.Message);
                Trace.TraceError(txt);
            }
            return sw;
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            Setup();
            MediaButler.Common.Host.MediaButlerHost xHost = new Common.Host.MediaButlerHost(myConfigData);

           await  xHost.ExecuteAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000 * myConfigData.SleepDelay);
            }

                //    
                //    CloudStorageAccount storageAccount;
                //    CloudQueueClient queueClient;
                //    CloudQueueMessage currentMessage;
                //    //TraceInfo auxiliar
                //    string txt;
                //    Setup(); 
                //    //QUEUE infra
                //    storageAccount = CloudStorageAccount.Parse(myConfigData.ProcessConfigConn);
                //    queueClient = storageAccount.CreateCloudQueueClient();
                //    //Create de Handler process
                //    myProcessHandler = new Common.workflow.ProcessHandler(myConfigData.ProcessConfigConn);
                //    //Infinite Loop
                //    while (!cancellationToken.IsCancellationRequested)
                //    {
                //        //1. Setup()
                //        Setup();
                //        //Check if is ON/Pausa
                //        if (!myConfigData.IsPaused)
                //        {
                //            //2. how many process is running in this instance?
                //            if (myProcessHandler.CurrentProcessRunning < myConfigData.MaxCurrentProcess)
                //            {
                //                //2.1 Execute new
                //                //3. Peek Message
                //                currentMessage = GetNewMessage(queueClient);
                //                if (currentMessage != null)
                //                {
                //                    //We have a new message
                //                    txt = string.Format("[{0}] has a new message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                //                    Trace.TraceInformation(txt);
                //                    //3.1 Check if is a posion Message
                //                    if (!CheckPoison(currentMessage))
                //                    {
                //                        //4. Good Message
                //                        //4.1 Start process, fire and Forgot
                //                        txt = string.Format("[{0}] Starting New Process, MessageID {1}", this.GetType().FullName, currentMessage.Id);
                //                        Trace.TraceInformation(txt);
                //                        myProcessHandler.Execute(currentMessage);
                //                    }
                //                    else
                //                    {
                //                        //Send dedletter message
                //                        txt = string.Format("[{0}] has a new  Poison message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                //                        Trace.TraceWarning(txt);
                //                        if (SendPoisonMessage(currentMessage))
                //                        {
                //                            InWorkQueue.DeleteMessage(currentMessage);
                //                            txt = string.Format("[{0}] Deleted Poison message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                //                            Trace.TraceWarning(txt);
                //                        }
                //                    }
                //                }
                //                else
                //                {
                //                    txt = string.Format("[{0}] has not a new message. # current process {1}", this.GetType().FullName, myProcessHandler.CurrentProcessRunning);
                //                    Trace.TraceInformation(txt);
                //                }
                //            }
                //            else
                //            {
                //                txt = string.Format("[{0}] Max number of process in parallel ({1} current {2})", this.GetType().FullName, myConfigData.MaxCurrentProcess, myProcessHandler.CurrentProcessRunning);
                //                Trace.TraceInformation(txt);
                //            }
                //        }
                //        else
                //        {
                //            Trace.TraceInformation("[{0}] Workflow Manager is paused.", this.GetType().FullName);
                //        }

                //        await Task.Delay(1000* myConfigData.SleepDelay);
                //    }
        }
    }
}
