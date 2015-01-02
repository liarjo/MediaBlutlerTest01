using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Replica
{
    class TriggerReplicaData
    {
        /// <summary>
        /// Replica Type: 0 Push ; 1 Pull
        /// </summary>
        //public int ReplicaType { get; set; }
        public string StageContainer { get; set; }
        /// <summary>
        /// Stage container Trigger´s message storage 
        /// </summary>
        public string StageConnString { get; set; }
        public TriggerReplicaData()
        {
            StageContainer = "triggerreplica1";
            
        }
    }
}
