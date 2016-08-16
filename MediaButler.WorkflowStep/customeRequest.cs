using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.WorkflowStep
{
    public class customeRequest : MediaButler.WorkflowStep.ICustomRequest
    {
        private string _AssetId;
        private string _ConfigData;
        private List<string> _Exceptions;
        private List<string> _Log;
        private string _MediaAccountKey;
        private string _MediaAccountName;
        private string _MediaStorageConn;
        private Dictionary<string, string> _MetaData;
        private string _ProcessConfigConn;
        private string _ProcessInstanceId;
        private string _ProcessTypeId;
        private DateTime _TimeStampProcessingStarted;
        private string _ButlerRequest_ControlFileUri;
        private List<string> _ButlerRequest_MezzanineFiles;
        private string _ConfigStep;
        public string AssetId
        {
            get
            {
                return _AssetId;
            }

            set
            {
                _AssetId = value;
            }
        }

        public string ButlerRequest_ControlFileUri
        {
            get
            {
                return _ButlerRequest_ControlFileUri;
            }

            set
            {
                _ButlerRequest_ControlFileUri = value;
            }
        }

        public List<string> ButlerRequest_MezzanineFiles
        {
            get
            {
                return _ButlerRequest_MezzanineFiles;
            }

            set
            {
                _ButlerRequest_MezzanineFiles = value;
            }
        }

        public string ConfigData
        {
            get
            {
                return _ConfigData;
            }

            set
            {
                _ConfigData = value;
            }
        }

        public List<string> Exceptions
        {
            get
            {
                return _Exceptions;
            }

            set
            {
                _Exceptions = value;
            }
        }

        public List<string> Log
        {
            get
            {
                return _Log;
            }

            set
            {
                _Log = value;
            }
        }

        public string MediaAccountKey
        {
            get
            {
                return _MediaAccountKey;
            }

            set
            {
                _MediaAccountKey = value;
            }
        }

        public string MediaAccountName
        {

            get
            {
                return _MediaAccountName;
            }

            set
            {
                _MediaAccountName = value;
            }
        }

        public string MediaStorageConn
        {
            get
            {
                return _MediaStorageConn;
            }

            set
            {
                _MediaStorageConn = value;

            }
        }

        public Dictionary<string, string> MetaData
        {
            get
            {
                return _MetaData;
            }

            set
            {
                _MetaData = value;
            }
        }

        public string ProcessConfigConn
        {
            get
            {
                return _ProcessConfigConn;
            }

            set
            {
                _ProcessConfigConn = value;
            }
        }

        public string ProcessInstanceId
        {
            get
            {
                return _ProcessInstanceId;
            }

            set
            {
                _ProcessInstanceId = value;
            }
        }

        public string ProcessTypeId
        {
            get
            {
                return _ProcessTypeId;
            }

            set
            {
                _ProcessTypeId = value;
            }
        }

        public DateTime TimeStampProcessingStarted
        {
            get
            {
                return _TimeStampProcessingStarted;
            }

            set
            {
                _TimeStampProcessingStarted = value;
            }
        }

        public string StepConfiguration
        {
            get
            {
                return _ConfigStep;
            }

            set
            {
                _ConfigStep = value;
            }
        }
    }
}
