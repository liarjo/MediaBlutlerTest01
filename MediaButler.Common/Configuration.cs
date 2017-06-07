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
    public class ProcessConfigKeys
    {
        public static string DefualtPartitionKey = "MediaButler.Common.workflow.ProcessHandler";
        public static string MediaButlerHostHttpCallBackOnError = "HttpCallBackOnError";
        public static string CreateSasLocatorStepLogAllAssetFile = "CreateSasLocatorStep.LogAllAssetFile";
    }
    public class DotControlConfigKeys
    {
        public static string GridEncodeStepEncodeConfigList = "GridEncodeStep.encodeConfigList";
        public static string GridEncodeStepMediaProcessorName = "GridEncodeStep.MediaProcessorName";
        public static string httpNotificationStepGetOnFinishUrl = "httpNotificationStep.GetOnFinishUrl";
        public static string httpNotificationStepPostOnFinishUrl = "httpNotificationStep.PostOnFinishUrl";
        public static string IngestMultiMezzamineFilesPrimaryFile = "myPrimaryFile";
        public static string StandardEncodigProfileName = "encodigProfile";
        public static string Index2EncodeStepEncodeConfigList = "Index2Preview.encodeConfigList";
        public static string Index2EncodeStepMediaProcessorName = "Index2Preview.MediaProcessorName";
        public static string Index2PreviewCopySubTitles = "Index2Preview.CopySubTitles";
        public static string SelectAssetByType = "SelectAssetBy.Type";
        public static string SelectAssetByValue = "SelectAssetBy.Value";
        public static string InjectTTMLSearchServiceName = "InjectTTML.searchServiceName";
        public static string InjectTTMLadminApiKey = "InjectTTML.adminApiKey";
        public static string InjectTTMLindexName = "InjectTTML.indexName";
        public static string SendGridStepConfig = "SendGridStep.Config"; 
        public static string TextAnalitycsStepLanguage="TextAnalitycsStep.Language";
        public static string TextAnalitycsStepApiURL = "TextAnalitycsStep.apiURL";
        public static string TextAnalitycsStepApiKey = "TextAnalitycsStep.apiKey";
        public static string AssetNameSeed = "AssetNameSeed";
        public static string VideoFileExtension = "VideoFileExtension";
        public static string VideoIndexerStepApiKey = "VideoIndexerStep.ApiKey";
        public static string VideoIndexerStepEndPoint = "VideoIndexerStep.EndPoint";
        public static string VideoIndexerStepLanguage = "VideoIndexerStep.Language";
        public static string VideoIndexerStepMetaData = "VideoIndexerStep.MetaData";
        public static string VideoIndexerStepDescription = "VideoIndexerStep.Description";
        public static string VideoIndexerStepPartition = "VideoIndexerStep.Partition";
    }
    public class Configuration
    {
        private const string configurationTableName = "ButlerConfiguration";
        
        private const string butlerStorageConnectionConfigurationKey = "MediaButler.ConfigurationStorageConnectionString";

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

        ///public static string ButlerStorageConnectionConfigurationKey { get { return butlerStorageConnectionConfigurationKey; } }

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
            return GetConfigurationValue(configKey, processKey, System.Configuration.ConfigurationManager.AppSettings[butlerStorageConnectionConfigurationKey]);
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
                if (retrievedResult.HttpStatusCode == 200)
                {
                    ButlerConfigurationEntity resultEntity = (ButlerConfigurationEntity)retrievedResult.Result;
                    configurationValue = resultEntity.ConfigurationValue;
                }
                else
                {
                    Trace.TraceWarning("GetConfigurationValue {0} / {1} don't exist", configKey, processKey);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Get Configuration Value Error , Check connection string and MBF Stage storage Account: " + ex.Message);
                throw new Exception("Get Configuration Value Error , Check connection string and MBF Stage storage Account: " +  ex.Message);
            }
            return configurationValue;
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
