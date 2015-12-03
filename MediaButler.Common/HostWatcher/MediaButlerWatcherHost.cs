using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaButler.Common.HostWatcher
{
    public class MediaButlerWatcherHost
    {
        private string _storageAccountString;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken Token;
        public MediaButlerWatcherHost(string storageAccountString)
        {
            _storageAccountString = storageAccountString;
            Token = cancellationTokenSource.Token;
        }
        public async Task Run()
        {
            string s = MediaButler.Common.Configuration.GetConfigurationValue("ContainersToScan", "MediaButler.Workflow.WorkerRole");
            var containers = s.Split(',');
           string[] ContainersToScan = containers;

            var taskFailedRequests = Task.Run(() => JobManager.getWorkflowFailedOperations(Token, _storageAccountString));
            var taskSuccessfulRequests = Task.Run(() => JobManager.getWorkflowSuccessOperations(Token, _storageAccountString));
            var taskProcessIncomingJobs = Task.Run(() => BlobWatcher.runInboundJobWatcher(Token, _storageAccountString, ContainersToScan));
            Task.WaitAll(taskFailedRequests, taskSuccessfulRequests, taskProcessIncomingJobs);

        }
    }
}
