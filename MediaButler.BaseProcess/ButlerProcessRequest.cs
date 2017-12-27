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
    public class ButlerProcessRequest:MediaButler.Common.workflow.ProcessRequest
    {
        private object lockMessageHiddeControl = new object();
        private bool MessageHiddenControl;
        private Task myMessageHiddenTask;

        //deprecate
        //private string _MediaServiceAccountName;
        //private string _PrimaryMediaServiceAccessKey;
        private string _tenant;
        private string _clientId;
        private string _clientSecret;
        private string _amsApiEndpoint;


        private string _MediaStorageConn;

        private CloudMediaContext _MediaServiceContext=null;

        public TaskStatus MessageHiddenTaskStatus
        { get
            {
                TaskStatus aux = TaskStatus.WaitingToRun;
                if (myMessageHiddenTask != null)
                {
                    aux = myMessageHiddenTask.Status;
                }
                return aux;
            }
        }
        /// <summary>
        /// Long Running task for keep the message hidden in the queue
        /// </summary>
        /// <param name="timeSpanMessage"> time for hidde the message</param>
        /// <param name="sleepSeconds">Next time in seonds to renew the hidden status</param>
        private void KeepMessageHidden(int timeSpanMessage,int sleepSeconds)
        {
            string qName = MediaButler.Common.Configuration.ButlerSendQueue;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.ProcessConfigConn);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(qName);
            do
                {
                    try
                    {
                        //precheck
                        if (MessageHiddenControl)
                        {
                            queue.UpdateMessage(this.CurrentMessage, TimeSpan.FromSeconds(timeSpanMessage), MessageUpdateFields.Visibility);
                            System.Threading.Thread.Sleep(sleepSeconds * 1000);
                        }
                    }
                    catch (Exception X)
                    {
                        if (!MessageHiddenControl)
                        {
                            //REal Error
                            string txt =
                                string.Format("[{0}] in prosess type {1} instnace {2} at {4} has an erro: {3}",
                                this.GetType().FullName, this.ProcessTypeId, this.ProcessInstanceId, X.Message, "KeepMessageHidden");
                            Trace.TraceError(txt);
                        }
                    }
                   
                } while (MessageHiddenControl);
                    
        }
        /// <summary>
        /// Delete de current message, the idea is doing at the end of the process
        /// </summary>
        public void DeleteCurrentMessage()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.ProcessConfigConn);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            string qName = MediaButler.Common.Configuration.ButlerSendQueue;
            CloudQueue queue = queueClient.GetQueueReference(qName);
            try
            {
                string id = this.CurrentMessage.Id;
                queue.DeleteMessage(this.CurrentMessage);
                Trace.TraceInformation("[{0}] in prosess type {1} instnace {2} at {4} delete message {3}",
                    this.GetType().FullName, this.ProcessTypeId, this.ProcessInstanceId, id, "KeepMessageHidden");
                
            }
            catch (Exception X)
            {
                Trace.TraceError("[{0}] in prosess type {1} instnace {2} at {4} has an erro: {3}",
                    this.GetType().FullName, this.ProcessTypeId, this.ProcessInstanceId, X.Message, "KeepMessageHidden");
                throw;
            }
            
           
        }
        /// <summary>
        /// Start idenpotent to hidden message
        /// </summary>
        /// <param name="timeSpanMessage"></param>
        /// <param name="sleepSeconds"></param>
        public void StartMessageHidden(int timeSpanMessage,int sleepSeconds)
        {
            if (!MessageHiddenControl)
            {
                lock (lockMessageHiddeControl)
                {
                    MessageHiddenControl = true;
                }
                    myMessageHiddenTask =Task.Factory.StartNew(() => { this.KeepMessageHidden(timeSpanMessage, sleepSeconds); });
            }
        }
        /// <summary>
        /// strat Stop hidden message singnal.
        /// </summary>
        public  void StopMessageHidden()
        {
            lock (lockMessageHiddeControl)
            {
                MessageHiddenControl = false;
            }
        }
        public string AssetId { get; set; }
        public MediaButler.Common.ButlerRequest ButlerRequest
        { get
            {
               return Newtonsoft.Json.JsonConvert.DeserializeObject<MediaButler.Common.ButlerRequest>(CurrentMessage.AsString);
               
            }
        }
        #region Media Service Context
       
        //Deprecated
        //public string MediaAccountName
        //{
        //    get
        //    {
        //        string aux;
        //        if (string.IsNullOrEmpty(_MediaServiceAccountName))
        //        {
        //            aux = MediaButler.Common.Configuration.GetConfigurationValue("MediaServiceAccountName", "general");
        //        }
        //        else
        //        {
        //            aux = _MediaServiceAccountName;

        //        }
        //        return aux;
        //    }
        //}
        //public string MediaAccountKey
        //{
        //    get
        //    {
        //        string aux;
        //        if (string.IsNullOrEmpty(_PrimaryMediaServiceAccessKey))
        //        {
        //            aux = MediaButler.Common.Configuration.GetConfigurationValue("PrimaryMediaServiceAccessKey", "general");
        //        }
        //        else
        //        {
        //            aux = _PrimaryMediaServiceAccessKey;
        //        }
        //        return aux;
        //    }
        //}

        /// <summary>
        /// service principal authentication: Client ID
        /// </summary>
        public string ClientId
        {
            get
            {
                if (string.IsNullOrEmpty(_clientId))
                {
                    _clientId = MediaButler.Common.Configuration.GetConfigurationValue("clientId", "general");
                }
                return _clientId;
            }
        }
        /// <summary>
        /// service principal authentication: Client Secret
        /// </summary>
        public string ClientSecret
        {
            get
            {
                if (string.IsNullOrEmpty(_clientSecret))
                {
                    _clientSecret = MediaButler.Common.Configuration.GetConfigurationValue("clientSecret", "general");
                }
              
                return _clientSecret;
            }
        }
        /// <summary>
        /// service principal authentication: AMS endpoint
        /// </summary>
        public string AmsEndPoint
        {
            get
            {
                if (string.IsNullOrEmpty(_amsApiEndpoint))
                {
                    _amsApiEndpoint = MediaButler.Common.Configuration.GetConfigurationValue("amsApiEndpoint", "general");
                }
                
              
                return _amsApiEndpoint;
            }
        }/// <summary>
         /// service principal authentication: Tenant
         /// </summary>
        public string Tenant
        {
            get
            {
                if (string.IsNullOrEmpty(_tenant))
                {
                    _tenant = MediaButler.Common.Configuration.GetConfigurationValue("tenant", "general");
                }
               
                return _tenant;
            }
        }
        /// <summary>
        /// Get Azure Media Services Context.
        /// </summary>
        /// <returns></returns>
        public CloudMediaContext MediaServiceContext()
        {
            if (_MediaServiceContext == null)
            {
                //Explicit service principal authentication configuration validations
                if (string.IsNullOrEmpty(this.Tenant) || (string.IsNullOrEmpty(this.ClientId)) || (string.IsNullOrEmpty(this.ClientSecret)) || (string.IsNullOrEmpty(this.AmsEndPoint)))
                {
                    string error = $"MediaServiceContext service principal authentication prameters error: Tenant={_tenant}/ClientId={_clientId}/ClientSecret={_clientSecret}/AMPEndpoint={_amsApiEndpoint} [*breakall*]";
                    Trace.TraceError(error);
                    throw new Exception(error);
                }
                try
                {
                    var tokenCredentials = new AzureAdTokenCredentials(
                    this.Tenant,
                    new AzureAdClientSymmetricKey(this.ClientId, this.ClientSecret),
                    AzureEnvironments.AzureCloudEnvironment);

                    var tokenProvider = new AzureAdTokenProvider(tokenCredentials);

                    _MediaServiceContext = new CloudMediaContext(new Uri(this.AmsEndPoint), tokenProvider);
                }
                catch (Exception X )
                {
                   
                    throw new Exception($"MediaServiceContext service principal authentication error: {X.Message} Tenant={_tenant}/ClientId={_clientId}/ClientSecret={_clientSecret}/AMPEndpoint={_amsApiEndpoint} [*breakall*]");
                }
                
            }
            return _MediaServiceContext ;
        }
#endregion

        public string MediaStorageConn
        {
            get
            {
                string aux;
                if (string.IsNullOrEmpty(_MediaStorageConn))
                {
                    aux = MediaButler.Common.Configuration.GetConfigurationValue("MediaStorageConn", "general");
                }
                else
                {
                    aux = _MediaStorageConn;
                }
                return aux;
            }
               
        }
        public ButlerProcessRequest()
        {
            
        }
        /// <summary>
        /// Dispose the TASK for keepMessage Hidden!
        /// </summary>
        public override void DisposeRequest()
        {
            base.DisposeRequest();
            //Check if we are using keep message hidden
            if (this.myMessageHiddenTask != null)
            {
                StopMessageHidden();
            }
        }
       
        //TODO: Fix to use service Principal
        ///// <summary>
        ///// Change default AMS
        ///// </summary>
        ///// <param name="MediaAccountName"></param>
        ///// <param name="MediaAccountKey"></param>
        //public void ChangeMediaServices(string MediaAccountName, string MediaAccountKey, string MediaStorageConn)
        //{
        //    //TODO: fix
        //    _MediaServiceAccountName = MediaAccountName;
        //    _PrimaryMediaServiceAccessKey = MediaAccountKey;
        //    _MediaStorageConn = MediaStorageConn;
        //    Trace.TraceInformation("{0} Changed default AMS to ", this.GetType().FullName, _MediaServiceAccountName);
        //}
        
    }

    
}
