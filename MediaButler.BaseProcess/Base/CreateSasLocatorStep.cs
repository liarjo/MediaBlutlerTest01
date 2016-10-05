using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class CreateSasLocatorStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private IAsset myAsset;
        private IButlerStorageManager StorageManager;
        private void buildlocator()
        {
            myAsset = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            var daysForWhichStreamingUrlIsActive = 365;

            var accessPolicy = _MediaServiceContext.AccessPolicies.Create(
                myAsset.Name
                , TimeSpan.FromDays(daysForWhichStreamingUrlIsActive)
                , AccessPermissions.Read);

            var locator=   _MediaServiceContext.Locators.CreateLocator(LocatorType.Sas, myAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));
            //Add to metadata Locator path
            if (!myRequest.MetaData.ContainsKey("sasPathurl"))
            {
                myRequest.MetaData.Add("sasPathurl", locator.Path);
            }
            //Add all files URL to log?
            string jsonProcessConfiguration = StorageManager.GetButlerConfigurationValue(
                ProcessConfigKeys.DefualtPartitionKey,
                myRequest.ProcessTypeId + ".config");
            var processConfiguration = new Common.ResourceAccess.jsonKeyValue(jsonProcessConfiguration);

            if (processConfiguration.Read(ProcessConfigKeys.CreateSasLocatorStepLogAllAssetFile).ToLower() == "yes")
            {
                List<string> urlList = new List<string>();
                foreach (var file in myAsset.AssetFiles)
                {
                    urlList.Add(locator.BaseUri + "/" + file.Name + locator.ContentAccessComponent);
                }
                string jsonList = Newtonsoft.Json.JsonConvert.SerializeObject(urlList);
                if (myRequest.MetaData.ContainsKey("sasFileUrlList"))
                {
                    myRequest.MetaData["sasFileUrlList"] = jsonList;
                }
                else
                {
                    myRequest.MetaData.Add("sasFileUrlList", jsonList);
                }
            }

        }
       
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            StorageManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            buildlocator();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

               
        }
    }
}
