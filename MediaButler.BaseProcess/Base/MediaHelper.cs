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
    public class MediaHelper
    {
        public static void SetPrimaryFile(IAsset MyAsset, IAssetFile theAssetFile)
        {

            MyAsset.AssetFiles.ToList().ForEach(af => { af.IsPrimary = false; af.Update(); });
            theAssetFile.IsPrimary = true;
            theAssetFile.Update();
        }
         /// <summary>
        /// Load encoding profile from local disk or Blob Storage (mediabultlerbin container)
        /// </summary>
        /// <param name="profileInfo"></param>
        /// <returns></returns>
        public static string LoadEncodeProfile(string profileInfo, string ProcessConfigConn)
        {
            string auxProfile = null;
            if (File.Exists(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo)))
            {
                //the XML file is in the local file
                //Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from local disk", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
                auxProfile = File.ReadAllText(Path.GetFullPath(@".\encodingdefinitions\" + profileInfo));
            }
            else
            {
                //The profile is not in local file

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ProcessConfigConn);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(MediaButler.Common.Configuration.ButlerExternalInfoContainer);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(profileInfo);
                if (blockBlob.Exists())
                {
                    //Trace.TraceInformation("[{0}] process Type {1} instance {2} Encoder Profile {3} from Blob Storage", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId, profileInfo);
         
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
                    string txtMessage = string.Format("Error Encoder Profile {0} don't exist", profileInfo);
                    throw (new Exception(txtMessage));
                }
                
            }
            return auxProfile;
        }
        public static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName, CloudMediaContext myMediaServicesContext)
        {
            // The possible strings that can be passed into the 
            // method for the mediaProcessor parameter:
            // Azure Media Encoder
            // Windows Azure Media Packager
            // Windows Azure Media Encryptor
            // Azure Media Indexer
            // Storage Decryption

            var processor = myMediaServicesContext.MediaProcessors.Where(p => p.Name == mediaProcessorName).
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor {0}", mediaProcessorName));

            return processor;
        }
        public static IJob GetJob(string jobId, CloudMediaContext myMediaServicesContext)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in myMediaServicesContext.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();
            return job;
        }
        public static void WaitJobFinish(string jobId, CloudMediaContext myMediaServicesContext)
        {
            IJob myJob = GetJob(jobId, myMediaServicesContext);
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
    }
}
