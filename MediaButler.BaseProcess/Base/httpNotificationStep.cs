using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using System.Diagnostics;
using System.Net;
using System.IO;
using MediaButler.Common.ResourceAccess;

namespace MediaButler.BaseProcess
{
    class httpNotificationStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager blobManager;
        public override void HandleCompensation(ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);

            string jsonControlFile = blobManager.ReadTextBlob(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue stepConfig = new jsonKeyValue(jsonControlFile);
            //CallBack URL with token and all info except Transaction ID
            string url = stepConfig.Read("baseUrl");
            //Add process Instance ID as value parameter on the URL
            var httpRequest = WebRequest.Create(url + myRequest.ProcessInstanceId);

            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

            using (var sr = new StreamReader(httpResponse.GetResponseStream()))
            {
                string xResponse = sr.ReadToEnd();
                Trace.TraceInformation(xResponse);
                myRequest.Log.Add(xResponse);

            }
        }
    }
}
