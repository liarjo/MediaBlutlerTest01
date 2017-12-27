using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{

    class UpdateAlternateIDData
    {
        /// <summary>
        /// where the Alternate Id is:
        /// 0. Control File name [default]
        /// 1. json object inside control "AlternateID"
        /// 2. Original File Name 
        /// 3. GUID container folder
        /// </summary>
        public string OriginType { get; set; }
        public UpdateAlternateIDData()
        {
            OriginType = "0";
        }
    }
    class UpdateAlternateIDStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private UpdateAlternateIDData myConfig;
        private void Setup()
        {
            //_MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServiceContext = myRequest.MediaServiceContext();
            if (string.IsNullOrEmpty(this.StepConfiguration))
            {
                myConfig = new UpdateAlternateIDData();
            }
            else
            {
                myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateAlternateIDData>(this.StepConfiguration);
            }
        }
        private string getAlternativeId()
        {
            string aux = "";
            switch (this.myConfig.OriginType)
            {
                case "1":/// 1. json object inside control "AlternateID"

                    break;
                case "2": /// 2. Original File Name 
                    string fileName= myRequest.ButlerRequest.MezzanineFiles.Where(f => f.EndsWith("mp4")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        aux = fileName;
                    }
                    break;
                case "3": /// 3. GUID container folder
                    if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
                    {
                        Uri controlUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
                        aux = controlUri.Segments[3].Substring(0, controlUri.Segments[3].Length-1);
                    }
                    else
                    {
                        aux = "Undefined GUID folder";
                    }
                    break;
                default:

                    if (!string.IsNullOrEmpty(myRequest.ButlerRequest.ControlFileUri))
                    {
                        Uri controlUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
                        string controlName = controlUri.Segments[controlUri.Segments.Count() - 1];
                        aux = controlName.Substring(0, controlName.ToLower().IndexOf(".control"));
                    }
                    
                    break;
            }
            return aux;
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            IAsset theAsset = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            //Get alternative ID
            theAsset.AlternateId = getAlternativeId();
            //Update theAsset
            theAsset.Update();
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
