﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;

namespace MediaButler.BaseProcess.Base
{
    class DeleteCurrentAssetStep : Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        public override void HandleCompensation(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            IAsset theAsset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            theAsset.Delete();

        }
    }
}
