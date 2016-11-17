using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaButler.Common.workflow;
using MediaButler.Common.ResourceAccess;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Net;
using System.Xml;
using System.IO;
using MediaButler.Common;

namespace MediaButler.BaseProcess.AzureSearch
{
    class InjectTTML : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager myBlobManager;
        private CloudMediaContext _MediaServicesContext;
       
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }
        private void CreateVideoTextIndex(SearchServiceClient serviceClient,string indexName)
        {
            var definition = new Microsoft.Azure.Search.Models.Index()
            {
                Name = indexName,
                Fields = new[]
                {
                    new Field("id",DataType.String)                 { IsKey=true,IsRetrievable=true,IsFacetable=true},
                    new Field("assetid",DataType.String)            { IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("title", DataType.String)             { IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("recognizability", DataType.Double)   { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("begin", DataType.String)             { IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("end", DataType.String)               { IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("text", DataType.String)              { IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new Field("url", DataType.String)              { IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = true }
                }
            };

            serviceClient.Indexes.Create(definition);
        }
        private ILocator CreateStreamingLocator(string outputAssetid)
        {
            IAssetFile assetFile = null;
            ILocator locator = null;

            var daysForWhichStreamingUrlIsActive = 365;
            var outputAsset = _MediaServicesContext.Assets.Where(a => a.Id == outputAssetid).FirstOrDefault();

            var accessPolicy = _MediaServicesContext.AccessPolicies.Create(
                outputAsset.Name
                , TimeSpan.FromDays(daysForWhichStreamingUrlIsActive)
                , AccessPermissions.Read);
            var assetFiles = outputAsset.AssetFiles.ToList();

            assetFile = assetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();
            locator = _MediaServicesContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, outputAsset, accessPolicy, DateTime.UtcNow.AddMinutes(-5));
            //Add Smooth URL to Metadata
            if (!myRequest.MetaData.ContainsKey("smoothurl"))
            {
                myRequest.MetaData.Add("smoothurl", locator.Path + assetFile.Name + "/manifest");
            }
            return locator;
        }
        private void UploadTTML(SearchServiceClient serviceClient, string indexName, IAsset myAsset)
        {
            List<IndexAction<videotext>> data = new List<IndexAction<videotext>>();
           
            SearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            IAssetFile myTTML = (from f in myAsset.AssetFiles select f).Where(f=>f.Name.EndsWith("ttml")).FirstOrDefault();
            ILocator locator = (from l in myAsset.Locators select l).Where(l => l.Type == LocatorType.OnDemandOrigin).FirstOrDefault();
            if (locator==null)
            {
                locator = CreateStreamingLocator(myAsset.Id);
            }
            IAssetFile ismfile = (from f in myAsset.AssetFiles select f).Where(f => f.Name.EndsWith("ism")).FirstOrDefault();

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                myTTML.Download(myRequest.ProcessInstanceId);
                xmlDoc.Load(myRequest.ProcessInstanceId);
                File.Delete(myRequest.ProcessInstanceId);
            }
            catch (Exception X)
            {
                if (File.Exists(myRequest.ProcessInstanceId))
                {
                    File.Delete(myRequest.ProcessInstanceId);
                }
                Trace.TraceError("Error on UploadTTML TTML file don't exist or XML error");
                throw X;
            }

            int sentenceID = 0;
            foreach (XmlNode item in xmlDoc.SelectNodes("/").Item(0).ChildNodes.Item(1).ChildNodes.Item(1).ChildNodes.Item(0).ChildNodes)
            {
                string begin = item.Attributes["begin"].Value;
                string end = item.Attributes["end"].Value;
                string txt = item.InnerText;

                data.Add(IndexAction.Upload(
                    new videotext {
                        assetid = myAsset.Id,
                        begin = begin,
                        end = end,
                        id = string.Format("{0}-{1}",myRequest.ProcessInstanceId, sentenceID),
                        text = txt,
                        title = myAsset.Name,
                        recognizability = 0.0,
                        url= locator.Path + ismfile.Name + "/manifest"
                    }));
                sentenceID += 1;
            }

            var batch = IndexBatch.New(data);
            try
            {
                indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException e)
            {
                Trace.TraceError(
                    "Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
                throw e;
            }

        }
        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            //IjsonKeyValue dotControlData = myBlobManager.GetDotControlData(myRequest.ButlerRequest.ControlFileUri);
            IjsonKeyValue dotControlData = myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);

            string searchServiceName = dotControlData.Read(DotControlConfigKeys.InjectTTMLSearchServiceName);
            string adminApiKey = dotControlData.Read(DotControlConfigKeys.InjectTTMLadminApiKey);
            string indexName = dotControlData.Read(DotControlConfigKeys.InjectTTMLindexName);

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));

            if (!serviceClient.Indexes.Exists(indexName))
            {
               
                CreateVideoTextIndex(serviceClient,indexName);
            }
          
            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            UploadTTML(serviceClient, indexName, asset);
        }
    }
}
