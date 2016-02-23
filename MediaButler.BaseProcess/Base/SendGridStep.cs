using MediaButler.Common;
using Microsoft.WindowsAzure.MediaServices.Client;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class SenGridData
    {
        public string UserName;
        public string Pswd;
        public string To;
        public string FromName;
        public string FromMail;
    }
    class SendGridStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private SenGridData mySenGridData;
        private CloudMediaContext _MediaServiceContext;

        private void Setup()
        {
            //this.mySenGridData = new SenGridData();
            mySenGridData = Newtonsoft.Json.JsonConvert.DeserializeObject<SenGridData>(this.StepConfiguration);
        }
        private void SendMail()
        {
            Setup();
            var credentials = new NetworkCredential(mySenGridData.UserName, mySenGridData.Pswd);
            // Create the email object first, then add the properties.
            SendGridMessage myMessage = new SendGridMessage();
            myMessage.AddTo(mySenGridData.To);
            myMessage.From = new MailAddress(mySenGridData.FromMail, mySenGridData.FromName);
            myMessage.Subject = string.Format("Butler Media Process {0} inctance {1}",myRequest.ProcessTypeId,myRequest.ProcessInstanceId);

            

            _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            AssetInfo ai = new AssetInfo(x);

            StringBuilder AssetInfoResume = ai.GetStatsTxt();
            AssetInfoResume.AppendLine("");
            AssetInfoResume.AppendLine("Media Butler Process LOG " + DateTime.Now.ToString());
            foreach (string txt in myRequest.Log)
            {
                AssetInfoResume.AppendLine(txt);

            }
            AssetInfoResume.AppendLine("-----------------------------");

            myMessage.Html = AssetInfoResume.Replace(" ", "&nbsp;").Replace(Environment.NewLine, "<br />").ToString();

            var transportWeb = new Web(credentials);
            transportWeb.DeliverAsync(myMessage).Wait();
            //transportWeb.Deliver(myMessage);
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            SendMail();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
