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

namespace MediaButler.BaseProcess.Index
{
    class BasicIndexStep:MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServicesContext;
        private IAsset theAsset;
        private string PreviousJobState;
        private const string _mediaProcessorName = "Azure Media Indexer";

        private void Setup()
        {
            _MediaServicesContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);

        }
       
        private  IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _MediaServicesContext.MediaProcessors
                        .Where(p => p.Name == mediaProcessorName)
                        .ToList()
                        .OrderBy(p => new Version(p.Version))
                        .LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor {0}", mediaProcessorName));

            return processor;
        }
        private void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            IJob job = (IJob)sender;

            if (PreviousJobState != e.CurrentState.ToString())
            {
                PreviousJobState = e.CurrentState.ToString();
                Trace.TraceInformation("Job {0} state Changed from {1} to {2}", job.Id, e.PreviousState, e.CurrentState);

            }
            switch (e.CurrentState)
            {
                case JobState.Finished:
                    break;
                case JobState.Canceled:
                    break;
                case JobState.Error:
                    break;
                default:
                    break;
            }
        }
        private IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in _MediaServicesContext.Jobs
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
        }
        private IJob RunIndexingJob()
        {
            IJob job = _MediaServicesContext.Jobs.Create("My Indexing Job process " + myRequest.ProcessInstanceId);

            theAsset = _MediaServicesContext.Assets.Where(xx => xx.Id == myRequest.AssetId).FirstOrDefault();
            IMediaProcessor indexer = GetLatestMediaProcessorByName(_mediaProcessorName);

            string configuration = "";
            
            ITask task = job.Tasks.AddNew("Indexing task " + theAsset.Name,indexer,configuration,TaskOptions.None);

            // Specify the input asset to be indexed.
            task.InputAssets.Add(theAsset);

            // Add an output asset to contain the results of the job.
            task.OutputAssets.AddNew(theAsset.Name + "_index", AssetCreationOptions.None);
            job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);
            job.Submit();

            this.WaitJobFinish(job.Id);

            return job;

        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            //Run index job
            IJob xJob = RunIndexingJob();
            //Update the asset ID with the Id of output asset.
            myRequest.AssetId = xJob.OutputMediaAssets.FirstOrDefault().Id;
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
