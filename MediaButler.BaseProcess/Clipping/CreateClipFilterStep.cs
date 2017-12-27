using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MediaButler.BaseProcess.Clipping
{
    class CreateClipFilterStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        /// <summary>
        /// Read information from .control file
        /// </summary>
        /// <returns></returns>
        private string readJsonControl()
        {
            string json = null;
            Uri controleFileUri = new Uri(myRequest.ButlerRequest.ControlFileUri);
            string controlFilename = controleFileUri.Segments[2] + controleFileUri.Segments[3] + controleFileUri.Segments[4];
            json = CloudStorageAccount.Parse(myRequest.ProcessConfigConn).CreateCloudBlobClient().GetContainerReference(myRequest.ProcessTypeId).GetBlockBlobReference(controlFilename).DownloadText();
            return json;
        }
        private void  ProcessClipFilters()
        {
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();
            AssetClipFilterData myData = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetClipFilterData>(this.readJsonControl());
            Uri controleFileUri = new Uri(myRequest.ButlerRequest.ControlFileUri);

            //Select the asset
            IAsset theAsset =(from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();
            //Select the ISM file
            IAssetFile ism = (from m in theAsset.AssetFiles select m).Where(m => m.Name.EndsWith(".ism")).FirstOrDefault();
            
            //Asset Storage
            CloudStorageAccount assetStorageCount = CloudStorageAccount.Parse(myRequest.MediaStorageConn);
            CloudBlobClient assetClient = assetStorageCount.CreateCloudBlobClient();
            CloudBlobContainer assetContainer = assetClient.GetContainerReference(theAsset.Uri.Segments[1]);
            CloudBlockBlob blobFilter = assetContainer.GetBlockBlobReference(ism.Name + "f");
            
            string xmlFilter;
            //Load or create the XML filter
            if (blobFilter.Exists())
            {
                xmlFilter = blobFilter.DownloadText(null);
            }
            else
            {
                xmlFilter = string.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?><filters majorVersion=\"0\" minorVersion=\"1\"></filters>");
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlFilter);
            //Update or add the filter by name
            foreach (FilterInfo heFilter in myData.filterList)
            {
                XmlNode xnList = xmlDoc.SelectSingleNode("/filters/filter[@name='" + heFilter.filterName + "']");
                if (xnList != null)
                {
                    //Update the filter
                    xnList.ChildNodes[0].Attributes["ge"].Value = heFilter.ge;
                    xnList.ChildNodes[0].Attributes["le"].Value = heFilter.le;
                }
                else
                {
                    //Create the filter
                    string xmlNewFilter = "<filter name=\"\"><absTimeInHNS ge=\"\" le=\"\"/></filter>";
                    XmlNode newFilter = xmlDoc.CreateNode(XmlNodeType.Element, "node", null);
                    newFilter.InnerXml = xmlNewFilter;
                    newFilter.ChildNodes[0].Attributes["name"].Value = heFilter.filterName;
                    newFilter.ChildNodes[0].ChildNodes[0].Attributes["ge"].Value = heFilter.ge;
                    newFilter.ChildNodes[0].ChildNodes[0].Attributes["le"].Value = heFilter.le;
                    xmlDoc.ChildNodes[1].AppendChild(newFilter.ChildNodes[0]);
                }
            }

            //Update the ISMF file 
            var bytesToUpload = Encoding.UTF8.GetBytes(xmlDoc.InnerXml);
            using (var ms = new MemoryStream(bytesToUpload))
            {
                blobFilter.UploadFromStream(ms);
            }

            
        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;

            ProcessClipFilters();
        }
       
        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
    }
}
