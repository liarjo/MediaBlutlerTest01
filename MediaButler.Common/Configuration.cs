using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;

namespace MediaButler.Common
{
    public class Configuration
    {
        private const string configurationTableName = "ButlerConfiguration";
        private const string workflowStatusTableName = "ButlerWorkflowStatus";
        private const string butlerStorageConnectionConfigurationKey = "MediaButler.ConfigurationStorageConnectionString";

        /// <summary>
        /// Name of blob used to implement active/passive for input workers.
        /// </summary>
        private const string blobLeaseName = "butlerwatcherlease";

        public static string BlobLeaseName { get { return blobLeaseName; } }
        public static string WorkflowStatusTableName { get { return WorkflowStatusTableName; } }

        public static int FailedQueuePollingInterval
        {
            get
            {
                int pollingInterval = 5;
                try
                {
                    pollingInterval =  Convert.ToInt32(Configuration.GetConfigurationValue("FailedQueuePollingSeconds", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load Failed queue polling interval using default");
                }
                return pollingInterval;
            }
        }
        public static int SuccessQueuePollingInterval
        {
            get
            {
                int pollingInterval = 5;
                try
                {
                    pollingInterval = Convert.ToInt32(Configuration.GetConfigurationValue("SuccessQueuePollingSeconds", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load Success queue polling interval using default");
                }
                return pollingInterval;
            }
        }
        public static int BlobWatcherPollingInterval
        {
            get
            {
                int pollingInterval = 5;
                try
                {
                    pollingInterval = Convert.ToInt32(Configuration.GetConfigurationValue("BlobWatcherPollingSeconds", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load Blob watcher polling interval using default");
                }
                return pollingInterval;
            }
        }

        public static string ButlerStorageConnectionConfigurationKey { get { return butlerStorageConnectionConfigurationKey; } }

        /// <summary>
        /// Publci enumeration for Workflow status tracking in Workflow
        /// </summary>
        public enum WorkflowStatus
        {
            Pending,
            Started,
            Finished,
            Running,
            Failed
        }

        /// <summary>
        /// Directory naming conventions for blobs in the drop container. 
        /// e.g. /<container>/Incoming/a.mp4 or /Incoming/21B248D8-FCA8-4343-A254-2827AA28E34C/a.mp4
        /// Used in JobCreator and BlobWatcher.
        /// </summary>
        public const string DirectoryInbound = "Incoming";
        public const string DirectoryProcessing = "Processing";
        public const string DirectoryCompleted = "Completed";
        public const string DirectoryFailed = "Failed";

        public const string ControlFileSuffix = ".control";

        public const string ButlerSendQueue = "butlersend";
        public const string ButlerSuccessQueue = "butlersuccess";
        public const string ButlerResponseDeadLetter = "butlerresponsedeadletter";
        public const string ButlerFailedQueue = "butlerfailed";

        public const string ButlerExternalInfoContainer = "mediabutlerbin";
        public const string ButlerWorkflowStatus = "ButlerWorkflowStatus";

        public const string keepStatusProcess = "keepStatusProcess";

        public const string workflowStepListKey = "workflowStepList";
        public const string workflowStepLength = "workflowStepLength";
        public const string TranscodingAdvance = "TranscodingAdvance";

        public const int maxDequeueCount = 3;

        public const int successFinishProcessStep = -100;
        public const int failFinishProcessStep = -200;
        public const int poisonFinishProcessStep = -300;
        public const int workflowFatalError = -400;

        /// <summary>
        /// Get the configuration Value from the configuration Table. Response is a JSON format
        /// </summary>
        /// <param name="configKey">This is a rowkey in Azure Table</param>
        /// <param name="processKey">This is the partition key in Azure Table</param>
        /// <returns>JSON configuration</returns>
        public static string GetConfigurationValue(string configKey, string processKey)
        {
            string configurationValue = "";
            try
            {
                string storageAccountString = CloudConfigurationManager.GetSetting(butlerStorageConnectionConfigurationKey);
                CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                CloudTableClient tableClient = account.CreateCloudTableClient();
                CloudTable configTable = tableClient.GetTableReference(configurationTableName);

                TableOperation retrieveOperation = TableOperation.Retrieve<ButlerConfigurationEntity>(processKey, configKey);

                // Execute the retrieve operation.
                TableResult retrievedResult = configTable.Execute(retrieveOperation);
                //FIX: If configuration don't exist return "" 
                //if (retrievedResult != null)
                if (retrievedResult.HttpStatusCode == 200)
                {
                    ButlerConfigurationEntity resultEntity = (ButlerConfigurationEntity)retrievedResult.Result;
                    configurationValue = resultEntity.ConfigurationValue;
                }
                else
                {
                    Trace.TraceWarning("GetConfigurationValue {0} / {1} don't exist",configKey,processKey);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return configurationValue;
        }
        public static string GetConfigurationValue(string configKey, string processKey, string ConfigurationConn)
        {
            string configurationValue = "";
            try
            {
                string storageAccountString = ConfigurationConn;
                CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                CloudTableClient tableClient = account.CreateCloudTableClient();
                CloudTable configTable = tableClient.GetTableReference(configurationTableName);

                TableOperation retrieveOperation = TableOperation.Retrieve<ButlerConfigurationEntity>(processKey, configKey);

                // Execute the retrieve operation.
                TableResult retrievedResult = configTable.Execute(retrieveOperation);
                if (retrievedResult != null)
                {
                    ButlerConfigurationEntity resultEntity = (ButlerConfigurationEntity)retrievedResult.Result;
                    configurationValue = resultEntity.ConfigurationValue;
                }
            }
            catch (Exception ex)
            {

                throw new Exception("Get Configuration Value Error , Check connection string and MBF Stage storage Account: " +  ex.Message);
            }
            return configurationValue;
        }
        /// <summary>
        /// Sets the configuration Value from the configuration Table. Response is a JSON format
        /// </summary>
        /// <param name="configKey">This is a rowkey in Azure Table</param>
        /// <param name="processKey">This is the partition key in Azure Table</param>
        /// <param name="processKey">This is the value in Azure Table</param>
        /// <returns>Returns true if insert is successful</returns>
        public static bool SetConfigurationValue(string configKey, string processKey, string configurationValue)
        {
            bool result = true;
            try
            {
                string storageAccountString = CloudConfigurationManager.GetSetting(butlerStorageConnectionConfigurationKey);
                CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                CloudTableClient tableClient = account.CreateCloudTableClient();
                CloudTable configTable = tableClient.GetTableReference(configurationTableName);
                ButlerConfigurationEntity configEntity = new ButlerConfigurationEntity(configKey, processKey);
                configEntity.ConfigurationValue = configurationValue;

                // Create the TableOperation that inserts the customer entity.
                TableOperation insertOperation = TableOperation.Insert(configEntity);

                // Execute the insert operation.
                TableResult insertResult = configTable.Execute(insertOperation);
                if (insertResult == null)
                    result = false;

            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }

    }

    public class ButlerConfigurationEntity : TableEntity
    {
        public ButlerConfigurationEntity(string configKey, string processKey)
        {
            this.PartitionKey = processKey;
            this.RowKey = configKey;
        }

        public ButlerConfigurationEntity() { }

        public string ConfigurationValue { get; set; }

    }
}
