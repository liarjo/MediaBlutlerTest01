using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public interface IJobConfiguration
    {
        List<string[]> CopyFilesFilter { get; }
        string Id { get; }
        List<string> InputAssetId { get; }
        List<string> OutputAssetId { get; }
        string Processor { get; }
        List<string[]> TaskDefinition { get; }
        List<string> TaskId { get; }

        void AddTask(string[] TaskDefinition, string InputAsset, string OutputAsset, string[] fileFilters);
    }
}
