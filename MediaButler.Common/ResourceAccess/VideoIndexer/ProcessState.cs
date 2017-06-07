using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess.VideoIndexer
{
    public class ProcessState
    {
        public string state { get; set; }
        public string progress { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
    }
}
