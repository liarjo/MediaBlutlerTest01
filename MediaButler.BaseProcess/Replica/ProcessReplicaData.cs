using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.Replica
{
    class ProcessReplicaData
    {
        public string TargetAMSName { get; set; }
        public string TargetAMSKey { get; set; }
        public string TargetAMSStorageConn { get; set; }
        public ProcessReplicaData()
        {

        }
    }
}
