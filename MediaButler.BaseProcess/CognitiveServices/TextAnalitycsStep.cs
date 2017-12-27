using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using System.Diagnostics;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using MediaButler.Common;
using System.IO;

namespace MediaButler.BaseProcess.CognitiveServices
{
    class TextAnalitycsStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager myBlobManager;
        private CloudMediaContext _MediaServicesContext;
        private IjsonKeyValue allPorcessData;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            allPorcessData = myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);
            //1. Select VTT
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            var vttFile = (from f in asset.AssetFiles select f).Where(f => f.Name.EndsWith(".vtt")).FirstOrDefault();
            vttFile.Download(myRequest.ProcessInstanceId);
            string readText = File.ReadAllText(myRequest.ProcessInstanceId);
            System.IO.File.Delete(myRequest.ProcessInstanceId);
            
            //2. Cognitive Service
            IAzureMLTextAnalyticsClient myClient = new AzureMLTextAnalyticsClient();
            Language myLanguage = (Language)Enum.Parse(typeof(Language), allPorcessData.Read(DotControlConfigKeys.TextAnalitycsStepLanguage));
            string apiURL = allPorcessData.Read(DotControlConfigKeys.TextAnalitycsStepApiURL);
            string apiKey = allPorcessData.Read(DotControlConfigKeys.TextAnalitycsStepApiKey);
            FileType ft = FileType.VTT;
            string jsonResponse=myClient.keyPhrasesTxt(readText, myLanguage, ft, apiURL, apiKey);

            //3. Add File
            string assetFileName = "keyPhrases." + myRequest.ProcessInstanceId + ".txt";
            var xf=asset.AssetFiles.Create(assetFileName);
            File.WriteAllText(assetFileName, jsonResponse);
            xf.Upload(assetFileName);
            File.Delete(myRequest.ProcessInstanceId);
            asset.Update();
        }
    }
}
