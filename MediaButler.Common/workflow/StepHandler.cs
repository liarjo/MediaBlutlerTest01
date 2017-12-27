﻿using MediaButler.Common.ResourceAccess;
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
        /// Process Step exception
        /// 1. Log Execption
        /// 2. Execute Compensation
        /// 3. If you have a exception on compensation. log it too
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stepException"></param>
        private void manageException(ChainRequest request,Exception stepException)
        {
            string TraceMessge = string.Format("[{2}] Step Error {0} at process instance {1}: {3}", GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, stepException.Message);
            Trace.TraceError(TraceMessge);
            //request.Exceptions.Add(new Exception(TraceMessge, stepException));
            request.Exceptions.Add(TraceMessge);

            //Exception Raise from Step
            request.BreakChain = true;
            try
            {
                //Compensatory action....
                this.HandleCompensation(request);
            }
            catch (Exception compensationException)
            {
                TraceMessge = string.Format("[{2}] Step Error at compensatory method {0} at process instance {1}: {3}", GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, compensationException.Message);

                Trace.TraceError(TraceMessge);
                //request.Exceptions.Add(new Exception(TraceMessge, compensationException));
                request.Exceptions.Add(TraceMessge);

                throw (compensationException);
            }
        }
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
            Exception stepException=null;
            string TraceMessge = "";
            try
            {
                //Execute step logic
                request.CurrentStepIndex += 1;
                //Update process status
                TraceMessge = string.Format("[{2}] Start Step {0} at process instance {1} step # {3}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, request.CurrentStepIndex.ToString());
                UpdateProcessStatus(request, TraceMessge);
                //Execute Workflow's Step
                HandleExecute(request);
                //Update Process Status
                TraceMessge = string.Format("[{2}] Finish Step {0} at process instance {1}", this.GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId);
                Trace.TraceInformation(TraceMessge);
            }
            catch (Exception currentStepException)
            {
                Trace.TraceError($"HandleRequest Error: {currentStepException.Message}");
                //Break chain error
                if (currentStepException.Message.Contains("[*breakall*]"))
                {
                    throw currentStepException;
                }
                stepException = currentStepException;
                //Manage Exception and Trigger Compensation
                //manageException(request, currentStepException);
            }

            //Update table status after finish Step
            UpdateProcessStatus(request, "");

            if (!request.BreakChain)
            {
                //If exception problem solved Or not Execption
                if (nextStep != null)
                {       
                    nextStep.HandleRequest(request);
                }
                else
                {
                    // This is the Workflow last step end.
                    FinishProccessStatus(request);
                }
            }
            else
            {
                //the exceptionwas not solved, break the chain 
                //and rise the last error
                if (stepException == null)
                {
                    TraceMessge = string.Format("[{2}] Step Error  {0} at process instance {1}: {3}", GetType().FullName, request.ProcessInstanceId, request.ProcessTypeId, "Not Exception Information");

                    stepException = new Exception(TraceMessge);
                }
                throw (stepException);
            }
        }
        /// <summary>
        /// Update the status in Track table.
        /// </summary>
        /// <param name="request"></param>
        protected void UpdateProcessStatus(ChainRequest request, string txtInformation)
        {
            Trace.TraceInformation(txtInformation);
            IButlerStorageManager storageManager = BlobManagerFactory.CreateBlobManager(request.ProcessConfigConn);
            storageManager.PersistProcessStatus(request);
        }
       /// <summary>
       /// Check if keep or delete Status on Table
       /// Base on Configuration
       /// </summary>
       /// <param name="request"></param>
        protected void FinishProccessStatus(ChainRequest request)
        {
            string jsonData = MediaButler.Common.Configuration.GetConfigurationValue("roleconfig", "MediaButler.Workflow.ButlerWorkFlowManagerWorkerRole", request.ProcessConfigConn);
            IjsonKeyValue x = new jsonKeyValue(jsonData);
            if ((string.IsNullOrEmpty(x.Read(Configuration.keepStatusProcess))) || (x.Read(Configuration.keepStatusProcess) == "0"))
            {
                //Delete track, process finish
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
}
