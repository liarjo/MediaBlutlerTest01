
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.sshSteps
{
    public interface IsshBRidgeCommand
    {
        /// <summary>
        /// SSH standar output  command result 
        /// </summary>
        string Result { get; }
        /// <summary>
        /// SSH command result code
        /// </summary>
        int ResultCode { get; }
        /// <summary>
        /// Generate the command using configuration
        /// </summary>
        /// <param name="xcommand">patter command </param>
        /// <param name="internalValues">arrays of MBF valuesto use on command</param>
        /// <param name="Metadata">arrau of metadata values to use on command</param>
        /// <returns></returns>
        string buildCommand( string[] internalValues, Dictionary<string, string> Metadata);
        /// <summary>
        /// Execute command async
        /// </summary>
        /// <param name="command"></param>
        void  execCommand(string command);
        /// <summary>
        /// Wait for las commands ends
        /// </summary>
        /// <param name="customSshCommand">patter command </param>
        /// <param name="exitCode">Exite code string </param>
        /// <param name="transactionId">MBF transaction ID</param>
        void WaitForProcessEnd(string customSshCommand, string exitCode, string transactionId);
        sshCommandConfig sshConfiguration { get; set; }
    }
    
}
