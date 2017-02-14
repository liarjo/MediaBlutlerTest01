using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.MediaAnalytics
{
    class JobConfiguration : MediaButler.Common.ResourceAccess.IJobConfiguration
    {
        private string _Processor;
        private List<string[]> _TaskDefinition;
        private List<string> _InputAssetId;
        private List<string> _OutputAssetId;
        private List<string[]> _CopyFilesFilter;
        private string _IdJob;
        private List<string> _TaskId;

        public string Processor { get { return _Processor; } }
        public List<string[]> TaskDefinition { get { return _TaskDefinition; } }
        public List<string> InputAssetId { get { return _InputAssetId; } }
        public List<string> OutputAssetId { get { return _OutputAssetId; } }
        public List<string[]> CopyFilesFilter { get { return _CopyFilesFilter; } }
        public string Id { get { return _IdJob; } }
        public List<string> TaskId { get { return _TaskId; } }
        public JobConfiguration(string processor, string processId)
        {
            _Processor = processor;
            _TaskDefinition = new List<string[]>();
            _InputAssetId = new List<string>();
            _OutputAssetId = new List<string>();
            _CopyFilesFilter = new List<string[]>();
            _IdJob = processId;
            _TaskId = new List<string>();
        }

        public void AddTask(string[] TaskDefinition, string InputAsset, string OutputAsset, string[] fileFilters)
        {
            _TaskDefinition.Add(TaskDefinition);
            _InputAssetId.Add(InputAsset);
            _OutputAssetId.Add(OutputAsset);
            _CopyFilesFilter.Add(fileFilters);
            _TaskId.Add(Guid.NewGuid().ToString());
        }


    }
}
