using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Index
{
    class LogFileSasAccessStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private IAsset theAsset;
        private void Setup()
        {
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            theAsset = _MediaServicesContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
        }
        private void LogInfo()
        {
            foreach (ILocator locator in theAsset.Locators)
            {
                if (locator.Type == LocatorType.Sas)
                {
                    myRequest.Log.Add("-----------------------------------------------------------");
                    myRequest.Log.Add("INDEX INFO");
                   
                    foreach (var xFile in theAsset.AssetFiles.ToList())
                    {
                        var mp4Uri = new UriBuilder(locator.Path);
                        mp4Uri.Path += "/" + xFile.Name;
                        myRequest.Log.Add(mp4Uri.ToString());
                        myRequest.Log.Add("");
                    }
                    myRequest.Log.Add("-----------------------------------------------------------");
                }
            }

        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            //Add index info to request LOG, sas access to all Files (index output files)
            LogInfo();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
