using MediaButler.Common;
using MediaButler.Common.ResourceAccess;
using MediaButler.Common.ResourceAccess.VideoIndexer;
using MediaButler.Common.workflow;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace MediaButler.BaseProcess.CognitiveServices
{
    class VideoIndexerStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private IButlerStorageManager myBlobManager;
        private CloudMediaContext _MediaServicesContext;
        public override void HandleCompensation(ChainRequest request)
        {
            string errorTxt = string.Format("[{0}] process Type {1} instance {2} has not compensation method", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
            Trace.TraceWarning(errorTxt);
        }

        private string GetEncodedUrlVideoLow(IAsset myAsset)
        {

            long value = long.MaxValue;
            string fileName = "";
            foreach (var item in myAsset.AssetFiles.Where(f => f.Name.ToLower().EndsWith("mp4")))
            {
                if (item.ContentFileSize < value)
                {
                    value = item.ContentFileSize;
                    fileName = item.Name;
                }


                Console.WriteLine($"{item.Name} {item.ContentFileSize}");
            }
            var x = myAsset.AssetFiles.Where(f => (f.Name == fileName)).FirstOrDefault();
            string fileUrl = $"{myAsset.Uri.AbsoluteUri}/{x.Name}";
            return HttpUtility.UrlEncode(myBlobManager.GetBlobSasUri(fileUrl, 6));
            //return fileUrl;


        }
        private void AddOrUpdateMetadata(string key, string value)
        {
            if (!myRequest.MetaData.Any(item => item.Key == key))
            {
                myRequest.MetaData.Add(key, value);
            }
            else
            {
                myRequest.MetaData[key] = value;
            }
        }
        public override void HandleExecute(ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            myBlobManager = BlobManagerFactory.CreateBlobManager(myRequest.ProcessConfigConn);
            var allPorcessData = myBlobManager.GetProcessConfig(myRequest.ButlerRequest.ControlFileUri, myRequest.ProcessTypeId);
            //_MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServicesContext = myRequest.MediaServiceContext();

            var ApiKey = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepApiKey);
            var EndPoint = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepEndPoint);
            IVideoIndexerProvider myVideoIndexer = VideoIndexerProviderFactory.CreateVideoIndexer(ApiKey, EndPoint);

            IAsset asset = (from m in _MediaServicesContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            string videoUrlEncode = GetEncodedUrlVideoLow(asset);

            Dictionary<string, string> videoInfo = new Dictionary<string, string>();
            videoInfo["name"] = asset.Name;
            videoInfo["privacy"] = "Private";
            videoInfo["videoUrl"] = videoUrlEncode;
            videoInfo["language"] = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepLanguage);
            videoInfo["externalId"] = asset.Id;
            videoInfo["metadata"] = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepMetaData);
            videoInfo["description"] = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepDescription);
            videoInfo["partition"] = allPorcessData.Read(DotControlConfigKeys.VideoIndexerStepPartition);

            DateTime startTime = DateTime.Now;
            VideoIndexerAnswer myData = myVideoIndexer.UploadVideo(videoInfo).Result;
            if (myData.IsError)
            {
                Trace.TraceError($"Error Id: {myData.Error}");
                throw new Exception($"Error Id: {myData.Error}");
            }
            else
            {
                Trace.TraceInformation($"[{myRequest.ProcessTypeId}] Instance {myRequest.ProcessInstanceId} Video Id: {myData.VideoIndexId}");
                AddOrUpdateMetadata("VideoIndexId", myData.VideoIndexId);
                string Id = myData.VideoIndexId.TrimEnd('"').TrimStart('"');
                string jobProgressPorcentage = "0%";
                var info = new
                {
                    jobid = Id,
                    StartTme = startTime,
                    FinishTime = DateTime.Now,
                    Progress = jobProgressPorcentage,
                    State = "Submited",
                    currentTime = DateTime.Now,
                    OutPut = asset.Name
                };
                AddOrUpdateMetadata("JobInfo", Newtonsoft.Json.JsonConvert.SerializeObject(info));
                myBlobManager.PersistProcessStatus(myRequest);

                int retrayControl = 0;
                bool sw = true;
                while (sw)
                {
                    System.Threading.Thread.Sleep(30 * 1000);
                   // string Id = myData.VideoIndexId.TrimEnd('"').TrimStart('"');
                    var xState = myVideoIndexer.GetProcessSatet(Id).Result;
                    if (!string.IsNullOrEmpty(xState.ErrorType))
                    {
                        //error
                        //sw = false;
                        retrayControl += 1;
                        Trace.TraceWarning($"ErrorType {xState.ErrorType}");
                        Trace.TraceWarning($"Message {xState.Message}");

                        if (retrayControl>3)
                        {
                            Trace.TraceError($"ErrorType {xState.ErrorType}");
                            Trace.TraceError($"Message {xState.Message}");
                            throw new Exception($"Error Id: {Id} ErrorType {xState.ErrorType} ErrorMessage {xState.ErrorType}");
                        }
                        
                    }
                    else
                    {
                        //running?
                        Trace.TraceInformation($"[{myRequest.ProcessTypeId}] Instance {myRequest.ProcessInstanceId} State {xState.state}");
                        Trace.TraceInformation($"[{myRequest.ProcessTypeId}] Instance {myRequest.ProcessInstanceId} progress {xState.progress}");
                        TimeSpan span = DateTime.Now.Subtract(startTime);
                        Trace.TraceInformation($"[{myRequest.ProcessTypeId}] Instance {myRequest.ProcessInstanceId} Process Time {span.Minutes} Minutes");

                        //double jobProgress = 0;
                       // string jobProgressPorcentage ="0%";
                        if (xState.state == "Processed")
                        {
                            //Finish
                            sw = false;
                            jobProgressPorcentage = "100%";
                            //Player URL
                            string playerurl=myVideoIndexer.GetPlayerWidgetUrl(Id).Result;
                            AddOrUpdateMetadata("PlayerWidgetUrl", playerurl.TrimEnd('"').TrimStart('"'));
                        }
                        else
                        {
                            jobProgressPorcentage = xState.progress;
                        }
                        

                        string message = string.Format("job {0} Percent complete {1}", Id, jobProgressPorcentage);
                        AddOrUpdateMetadata(Configuration.TranscodingAdvance, message);
                        
                        var currentInfo = new
                            {
                                jobid = Id,
                                StartTme = startTime,
                                FinishTime=DateTime.Now,
                                Progress = jobProgressPorcentage,
                                State=xState.state,
                                currentTime =DateTime.Now,
                                OutPut = asset.Name
                        };
                        
                        AddOrUpdateMetadata("JobInfo", Newtonsoft.Json.JsonConvert.SerializeObject(currentInfo));

                        myBlobManager.PersistProcessStatus(myRequest);
                        Trace.TraceInformation(message);

                    }
                }
            }

        }
    }
}
