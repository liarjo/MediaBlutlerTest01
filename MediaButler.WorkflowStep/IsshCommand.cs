using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.WorkflowStep.SSH
{
    public interface IsshCommand
    {
        string User { get; set; }
        string Password { get; set; }
        string Host { get; set; }
        string Result { get; }
        int ResultCode { get; }
        /// <summary>
        /// Execute bash command
        /// </summary>
        /// <param name="executionID">execution id</param>
        /// <returns>VOID</returns>
        void StartExecute(int executionID);
        void WaitProcess(int executionID, string transactionID, string exitcode, int timeout);
        string Command { get; set; }

    }
}
