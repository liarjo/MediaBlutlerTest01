using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.sshSteps
{
    public class sshCommandConfig
    {
        public string id { get; set; }
        public bool sync { get; set; }
        public string Commands { get; set; }
        public List<string> patternValues { get; set; }
        public string host { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string BridgePath { get; set; }
        public string BridgeName { get; set; }
    }
}
