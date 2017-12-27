using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class PackageMultiMp4ToSmoothData
    {
        public string videoKey{get;set;}
        public string audioKey { get; set; }
        public string ismFilename { get; set; }
        public string MediaProcessorName { get; set; }
        public string XmlValidationProfile { get; set; }
        public string OutputAssetName { get; set; }
        public PackageMultiMp4ToSmoothData()
        {
            videoKey = "_H264_";
            audioKey = "_AAC_und_";
            ismFilename = "my.ism";
            MediaProcessorName = "Windows Azure Media Packager";
            XmlValidationProfile = "MediaPackager_ValidateTask.xml";
            OutputAssetName = "{0}_onlyPublish";

        }
    }
    class PackageMultiMp4ToSmoothStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private PackageMultiMp4ToSmoothData myConfig;        
        private  void CreateIsmFile(IAsset multibitrateMP4sAsset)
        {
            var AssetFiles = multibitrateMP4sAsset.AssetFiles.ToList();
            var mp4Files = AssetFiles.Where(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();
            using (MemoryStream msISM = new MemoryStream())
            {
                using (var writer = new StreamWriter(msISM))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
                    writer.Write("<smil xmlns=\"http://www.w3.org/2001/SMIL20/Language\">");
                    writer.Write("<head>");
                    writer.Write("<meta name=\"formats\" content=\"mp4\" />");
                    writer.Write("</head>");
                    writer.Write("<body>");
                    writer.Write("<switch>");
                    string xTag;
                    foreach (var mp4 in mp4Files)
                    {
                        if (mp4.Name.Contains(myConfig.videoKey))
                        {
                            //Video
                            xTag = string.Format("<video src=\"{0}\"/>", mp4.Name);
                        }
                        else
                        {
                            //Audio
                            string title = mp4.Name.Substring(mp4.Name.IndexOf(myConfig.audioKey));
                            xTag = string.Format("<audio src=\"{0}\" title=\"{1}\" />", mp4.Name, title);
                        }
                        writer.Write(xTag);
                    }
                    writer.Write("</switch>");
                    writer.Write("</body>");
                    writer.Write("</smil>");

                    writer.Flush();
                    msISM.Position = 0;
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.MediaStorageConn);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference(multibitrateMP4sAsset.Uri.Segments[1]);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(myConfig.ismFilename);
                    blockBlob.UploadFromStream(msISM);
                }

                var ISMassetFile = multibitrateMP4sAsset.AssetFiles.Create(myConfig.ismFilename);
            }
        }
        private void SetISMFileAsPrimary(IAsset asset)
        {
            var ismAssetFiles = asset.AssetFiles.ToList().
                Where(f => f.Name.EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).ToArray();

            // The following code assigns the first .ism file as the primary file in the asset.
            // An asset should have one .ism file.  
            ismAssetFiles.First().IsPrimary = true;
            ismAssetFiles.First().Update();
        }
        private IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            // The possible strings that can be passed into the 
            // method for the mediaProcessor parameter:
            // Azure Media Encoder
            // Windows Azure Media Packager
            // Windows Azure Media Encryptor
            // Azure Media Indexer
            // Storage Decryption

            var processor = _MediaServiceContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor {0}", mediaProcessorName));

            return processor;
        }
        /// <summary>
        /// Load encoding profile from local disk or Blob Storage (mediabultlerbin container)
        /// </summary>
        /// <param name="profileInfo"></param>
        /// <returns></returns>
        private string LoadEncodeProfile(string profileInfo)
        {
            string auxProfile = null;
            if (File.Exists(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo)))
            {
                //the XML file is in the local file
                Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from local disk", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                auxProfile = File.ReadAllText(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo));
            }
            else
            {
                //The profile is not in local file
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(myRequest.ProcessConfigConn);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(MediaButler.Common.Configuration.ButlerExternalInfoContainer);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(profileInfo);
                if (blockBlob.Exists())
                {
                    Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from Blob Storage", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                    using (var memoryStream = new MemoryStream())
                    {
                        blockBlob.DownloadToStream(memoryStream);
                        memoryStream.Position = 0;
                        StreamReader sr = new StreamReader(memoryStream);
                        auxProfile = sr.ReadToEnd();
                    }
                }
                else
                {
                    string txtMessage = string.Format("[{0}] process Type {1} instance {2} Error Encoder Profile {3} don't exist", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                    throw (new Exception(txtMessage));
                }

            }
            return auxProfile;
        }
        private IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in _MediaServiceContext.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();
            return job;
        }
        private void WaitJobFinish(string jobId)
        {
            IJob myJob = GetJob(jobId);
            //se utiliza el siguiente codigo para mostrar avance en porcentaje, como en el portal
            double avance = 0;
            //TODO: imporve wating method
            while ((myJob.State != JobState.Finished) && (myJob.State != JobState.Canceled) && (myJob.State != JobState.Error))
            {
                if (myJob.State == JobState.Processing)
                {
                    if (avance != (myJob.Tasks[0].Progress / 100))
                    {
                        avance = myJob.Tasks[0].Progress / 100;
                        Trace.TraceInformation("job " + myJob.Id + " Percent complete:" + avance.ToString("#0.##%"));
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
                myJob.Refresh();
            }
            //TODO: test this kind of error
            if (myJob.State == JobState.Error)
            {
                throw new Exception(string.Format("Error JOB {0}", myJob.Id));
            }
        }
        private IAsset ValidateMultibitrateMP4s(IAsset multibitrateMP4sAsset)
        {
            // Set .ism as a primary file 
            // in a multibitrate MP4 set.
            SetISMFileAsPrimary(multibitrateMP4sAsset);

            // Create a new job.
            IJob job = _MediaServiceContext.Jobs.Create("MP4 validation and converstion to Smooth Stream job.");

            // Read the task configuration data into a string. 
            string configMp4Validation = LoadEncodeProfile(myConfig.XmlValidationProfile);

            // Get the SDK extension method to  get a reference to the Azure Media Packager.
            IMediaProcessor processor = GetLatestMediaProcessorByName(myConfig.MediaProcessorName);
            
            // Create a task with the conversion details, using the configuration data. 
            ITask task = job.Tasks.AddNew("Mp4 Validation Task", processor, configMp4Validation, TaskOptions.None);

            // Specify the input asset to be validated.
            task.InputAssets.Add(multibitrateMP4sAsset);

            // Add an output asset to contain the results of the job. 
            // This output is specified as AssetCreationOptions.None, which 
            // means the output asset is in the clear (unencrypted). 
            task.OutputAssets.AddNew(string.Format(myConfig.OutputAssetName,multibitrateMP4sAsset.Name), AssetCreationOptions.None);

            // Submit the job and wait until it is completed.
            job.Submit();

            Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);

            this.WaitJobFinish(job.Id);
            return job.OutputMediaAssets[0];
        }
        private void Setup()
        {
            //_MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            _MediaServiceContext = myRequest.MediaServiceContext();

            if (!string.IsNullOrEmpty(this.StepConfiguration))
            {
                myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PackageMultiMp4ToSmoothData>(this.StepConfiguration);
            }
            else
            {
                myConfig = new PackageMultiMp4ToSmoothData();
            }
        }

        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            
            Setup();

            IAsset x = _MediaServiceContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();

            //Create de ISM file base o the MP4 content
            CreateIsmFile(x);

            // Use Azure Media Packager to validate the files.
            IAsset validatedMP4s = ValidateMultibitrateMP4s(x);
            
            //Update the Asset ID in the context
            myRequest.AssetId = validatedMP4s.Id;
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
