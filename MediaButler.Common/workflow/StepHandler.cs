using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.workflow
{
    public  abstract class StepHandler
    {
        /// <summary>
        /// This is a config data from Config table if you add a configkey in the chain configuration json string
        /// </summary>
        public string StepConfiguration = null;
        /// <summary>
        /// Next Step to call in the process. Null means last step.
        /// </summary>
        protected StepHandler nextStep;
        /// <summary>
        /// Set the next Step in the process
        /// </summary>
        /// <param name="nextStep">Nest Step in the process</param>
        public void SetSuccessor(StepHandler nextStep)
        {

            this.nextStep = nextStep;

        }
        /// <summary>
        /// Execute the Stpe logic
        /// </summary>
        /// <param name="request">Request sahre by all the steps</param>
        public abstract void HandleExecute(ChainRequest request);
        /// <summary>
        /// Compensatory method if you have an error in Step execution
        /// If you solve the problem and wants continues with the exxecution, you must set  request.BreakChain = false Explicit!
        /// </summary>
        /// <param name="request"></param>
        public abstract void HandleCompensation(ChainRequest request);
       /// <summary>
       /// Start the process.
       /// </summary>
       /// <param name="request">Request sahre by all the steps</param>
        public  void HandleRequest(ChainRequest request)
        {
            string TraceMessge = "";
            try
            {
               // CheckResumeProcess(request);
                //Execute step logic
                TraceMessge = string.Format("[{2}] Start Step {0} at process instance {1}", this.GetType().FullName, request.ProcessInstanceId,request.ProcessTypeId);
                Trace.TraceInformation(TraceMessge);
                this.HandleExecute(request);
                TraceMessge = string.Format("[{2}] Finish Step {0} at process instance {1}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId);
                Trace.TraceInformation(TraceMessge);
            }
            catch (Exception X)
            {
                TraceMessge = string.Format("[{2}] Step Error {0} at process instance {1}: {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId,X.Message);

                Trace.TraceError(TraceMessge);

                request.Exceptions.Add( new Exception(TraceMessge,X));
                //Exception Raise from Step
                request.BreakChain = true;
                try
                {
                    //Compensatory action....
                    this.HandleCompensation(request);
                }
                catch (Exception X2)
                {
                    TraceMessge = string.Format("[{2}] Step Error at compensatory method {0} at process instance {1}: {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, X2.Message);

                    Trace.TraceError(TraceMessge);
                    request.Exceptions.Add(new Exception(TraceMessge, X));
                    throw (X2);
                }
               
            }
            //Update process status
            UpdateProcessStatus(request); 
            if (!request.BreakChain)
            {
                //If exception problem solved
                if (nextStep != null)
                {       
                    nextStep.HandleRequest(request);
                }
            }
            else
            {
                //the exceptionwas not solved, break the chain 
                //and rise the last error
                throw (request.Exceptions.Last());
            }
        }
       
      
        /// <summary>
        /// Update the status in Track table.
        /// If the process ends, delete the data from track table.
        /// </summary>
        /// <param name="request"></param>
        protected void UpdateProcessStatus(ChainRequest request)
        {
            if (this.nextStep != null)
            {
                //persist State of the process
                PersistProcessStatus(request);
            }
            else
            {
                //Delete track, process finish
                FinishProccessStatus(request);
            }
            request.CurrentStepIndex += 1;
        }
        protected void PersistProcessStatus(ChainRequest request)
        {
            
            ProcessSnapShot mysh = new ProcessSnapShot(request.ProcessTypeId, request.ProcessInstanceId);
            try
            {
                Newtonsoft.Json.JsonSerializerSettings x= new Newtonsoft.Json.JsonSerializerSettings();
                x.ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                mysh.jsonContext = Newtonsoft.Json.JsonConvert.SerializeObject(request,Newtonsoft.Json.Formatting.None,x);

                mysh.CurrentStep = request.CurrentStepIndex;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(request.ProcessConfigConn);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
                TableOperation insertOperation = TableOperation.InsertOrReplace(mysh);
                table.CreateIfNotExists();
                table.Execute(insertOperation);
            }
            catch (Exception X)
            {
                string txtMessage = string.Format("[{0}] Persist Process Status Error at process {1} instance {2}: error messagase  {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, X.Message);
                Trace.TraceError(txtMessage);
                throw new Exception(txtMessage);
            }
           
        }
        protected void FinishProccessStatus(ChainRequest request)
        {
            ProcessSnapShot mysh = new ProcessSnapShot(request.ProcessTypeId, request.ProcessInstanceId);
            mysh.ETag = "*";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(request.ProcessConfigConn);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(Configuration.ButlerWorkflowStatus);
            TableOperation insertOperation = TableOperation.Delete(mysh);
            table.Execute(insertOperation);
        }
       
    }
}
