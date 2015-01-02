using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.workflow
{
    public class ProcessSnapShot : TableEntity
    {
        public int CurrentStep { get; set; }
        public string jsonContext { get; set; }
        public ProcessSnapShot(string pk, string rk)
        {
            this.PartitionKey = pk;
            this.RowKey = rk;
        }
        public ProcessSnapShot()
        { }
    }
}
