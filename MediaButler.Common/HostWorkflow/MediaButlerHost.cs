using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MediaButler.Common.Host
{
    public class MediaButlerHost
    {
        private ConfigurationData _Configuration;
        private MediaButler.Common.workflow.ProcessHandler myProcessHandler;
        private CloudQueue InWorkQueue;
        private string ButlerWorkFlowManagerHostConfigKey ="MediaButler.Workflow.ButlerWorkFlowManagerWorkerRole";
        public MediaButlerHost(ConfigurationData Configuration)
        {
            _Configuration = Configuration;
        }
        /// <summary>
        /// Setup Role
        /// </summary>
        private void Setup(string ConfigurationStorageConnectionString)
        {
            string json = MediaButler.Common.Configuration.GetConfigurationValue("roleconfig", ButlerWorkFlowManagerHostConfigKey, ConfigurationStorageConnectionString);
            _Configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigurationData>(json);
            _Configuration.poisonQueue = MediaButler.Common.Configuration.ButlerFailedQueue;
            _Configuration.inWorkQueueName = MediaButler.Common.Configuration.ButlerSendQueue;
            _Configuration.ProcessConfigConn = ConfigurationStorageConnectionString;
            _Configuration.MaxCurrentProcess = _Configuration.MaxCurrentProcess;
            _Configuration.SleepDelay = _Configuration.SleepDelay;
            _Configuration.MaxDequeueCount = _Configuration.MaxDequeueCount;

        }
        /// <summary>
        /// Get the message from IN Queue
        /// </summary>
        /// <param name="myqueueClient">QueClient</param>
        /// <returns>New Message, it can be null if it has not message</returns>
        private CloudQueueMessage GetNewMessage(CloudQueueClient myqueueClient)
        {
            CloudQueueMessage currentMessage = null;
            try
            {

                InWorkQueue = myqueueClient.GetQueueReference(_Configuration.inWorkQueueName);
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
            return (_Configuration.MaxDequeueCount < theMessage.DequeueCount);
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
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_Configuration.ProcessConfigConn);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue poisonQueue = queueClient.GetQueueReference(_Configuration.poisonQueue);
                poisonQueue.CreateIfNotExists();
                Common.ResourceAccess.IButlerStorageManager storageManager = Common.ResourceAccess.BlobManagerFactory.CreateBlobManager(_Configuration.ProcessConfigConn);


                //1. Create Butler Response
                ButlerRequest myButlerRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<ButlerRequest>(poisonMessage.AsString);
                ButlerResponse myButlerResponse = new Common.ButlerResponse();

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

                //2. Lookin for Errors in Table Status   
                string processId = myProcessHandler.getProcessId(myButlerRequest.ControlFileUri, myButlerRequest.MessageId.ToString());
                var processSnap = storageManager.readProcessSanpShot(myButlerRequest.WorkflowName, processId);

                if (processSnap != null)
                {
                    //2.1 Load Erros
                    dynamic dynObj = Newtonsoft.Json.JsonConvert.DeserializeObject((processSnap).jsonContext);
                    var errorList= Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>((dynObj.Exceptions.ToString()));
                    foreach (var errorTxt in errorList)
                    {
                        myButlerResponse.Log += "\r\n" + errorTxt;
                    }
                    //2.2 Update status
                    processSnap.CurrentStep = Configuration.poisonFinishProcessStep;
                    storageManager.PersistProcessStatus(processSnap);
                }
                else
                {
                    //Update satus no process Status
                    workflow.ProcessSnapShot x = new workflow.ProcessSnapShot(myButlerResponse.WorkflowName, processId);
                }

                //3. Send Poison Mesagge
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
        
        private void ExecuteWatcherProcess()
        {
            CloudStorageAccount storageAccount;
            CloudQueueClient queueClient;
            CloudQueueMessage currentMessage;

           
            //QUEUE infra
            storageAccount = CloudStorageAccount.Parse(_Configuration.ProcessConfigConn);
            queueClient = storageAccount.CreateCloudQueueClient();
            
            //TraceInfo auxiliar
            string txt;
            //1. Setup()
            Setup(_Configuration.ProcessConfigConn);
            //Check if is ON/Pausa
            if (!_Configuration.IsPaused)
            {
                //2. how many process is running in this instance?
                if (myProcessHandler.CurrentProcessRunning < _Configuration.MaxCurrentProcess)
                {
                    //2.1 Execute new
                    //3. Peek Message
                    currentMessage = GetNewMessage(queueClient);
                    if (currentMessage != null)
                    {
                        //We have a new message
                        txt = string.Format("[{0}] has a new message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                        Trace.TraceInformation(txt);
                        //3.1 Check if is a posion Message
                        if (!CheckPoison(currentMessage))
                        {
                            //4. Good Message
                            //4.1 Start process, fire and Forgot
                            txt = string.Format("[{0}] Starting New Process, MessageID {1}", this.GetType().FullName, currentMessage.Id);
                            Trace.TraceInformation(txt);
                            myProcessHandler.Execute(currentMessage);
                        }
                        else
                        {
                            //Send dedletter message
                            txt = string.Format("[{0}] has a new  Poison message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                            Trace.TraceWarning(txt);
                            if (SendPoisonMessage(currentMessage))
                            {
                                InWorkQueue.DeleteMessage(currentMessage);
                                txt = string.Format("[{0}] Deleted Poison message, messageId {1}", this.GetType().FullName, currentMessage.Id);
                                Trace.TraceWarning(txt);
                            }
                        }
                    }
                    else
                    {
                        txt = string.Format("[{0}] has not a new message. # current process {1}", this.GetType().FullName, myProcessHandler.CurrentProcessRunning);
                        Trace.TraceInformation(txt);
                    }
                }
                else
                {
                    txt = string.Format("[{0}] Max number of process in parallel ({1} current {2})", this.GetType().FullName, _Configuration.MaxCurrentProcess, myProcessHandler.CurrentProcessRunning);
                    Trace.TraceInformation(txt);
                }
            }
            else
            {
                Trace.TraceInformation("[{0}] Workflow Manager is paused.", this.GetType().FullName);
            }
        }
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //Create de Handler process
            myProcessHandler = new Common.workflow.ProcessHandler(_Configuration.ProcessConfigConn);
            //Infinite Loop
            while (true)
            {   
                //2. Execute
                ExecuteWatcherProcess();

                System.Threading.Thread.Sleep(1000 * _Configuration.SleepDelay);
                //await Task.Delay(1000 * _Configuration.SleepDelay);
            }
        }
    }
}