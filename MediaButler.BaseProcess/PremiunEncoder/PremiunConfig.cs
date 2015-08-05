using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.PremiunEncoder
{
    class PremiunConfig
    {
        private string _AssetWorkflowID;
        private string _EncodingJobName;
        private string _EncodigTaskName;

        public string EncodigTaskName
        {
            get { return _EncodigTaskName; }
            set { _EncodigTaskName = value; }
        }
        
        public string EncodingJobName
        {
            get { return _EncodingJobName; }
            set { _EncodingJobName = value; }
        }
        
        

        public string AssetWorkflowID
        {
            get { return _AssetWorkflowID; }
            set { _AssetWorkflowID = value; }
        }
        
    }
}
