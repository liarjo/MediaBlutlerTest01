using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using MediaButler.Common;

namespace MediaButler.Watcher
{
    /// <summary>
    /// The portion of the Watcher that manipulates the 
    /// </summary>
    public partial class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// Directory naming conventions for blobs in a container. 
        /// e.g. /<container>/Incoming/a.mp4
        /// </summary>
        const string DirectoryInbound = "/Incoming";
        const string DirectoryProcessing = "/Processing";
        const string DirectoryCompleted = "/Completed";
        const string DirectoryFailed = "/Failed";
        /// <summary>
        /// Scan the contents of the specified container for 
        /// </summary>
        /// <param name="cbc"></param>
        public void ScanContainerForInbound(Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer cbc)
        {

        }
    }
}
