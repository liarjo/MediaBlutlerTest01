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
using System.Net.Http;

namespace MediaButler.BaseProcess
{
    class httpNotificationStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager blobManager;
        private IjsonKeyValue dotControlConfig;
        public override void HandleCompensation(ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);

        }
        /// <summary>
        /// Send HTTP Get request with process instance ID
        /// </summary>
        private void HttpGetNotification()
        {
            //CallBack URL with token and all info except Transaction ID
            string url = dotControlConfig.Read(MediaButler.Common.DotControlConfigKeys.httpNotificationStepGetOnFinishUrl);
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
        /// <summary>
        /// Send HTTP Post request with json context on body
        /// </summary>
        private void HttpPostNotification()
        {
            string url = dotControlConfig.Read(MediaButler.Common.DotControlConfigKeys.httpNotificationStepPostOnFinishUrl);
            using (var client = new HttpClient())
            {
                
                var content = new StringContent(
                    blobManager.readProcessSanpShot(myRequest.ProcessTypeId,myRequest.ProcessInstanceId).jsonContext, 
                    Encoding.UTF8, "application/json");

                var result = client.PostAsync(url, content).Result;

                Trace.TraceInformation("Http Post Notification Result: " + result.ToString());
                
            }
        }
        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            blobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);

            string jsonControlFile = blobManager.ReadTextBlob(new Uri(myRequest.ButlerRequest.ControlFileUri));
            dotControlConfig = new jsonKeyValue(jsonControlFile);
            if (dotControlConfig.Read(MediaButler.Common.DotControlConfigKeys.httpNotificationStepGetOnFinishUrl) != "")
            {
                //GET
                HttpGetNotification();
            }
            else
            {
                //POST
                HttpPostNotification();
            }
        }
    }
}
