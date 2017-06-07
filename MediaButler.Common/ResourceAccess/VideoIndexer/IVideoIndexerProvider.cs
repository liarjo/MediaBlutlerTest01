using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess.VideoIndexer
{
    public interface IVideoIndexerProvider
    {
        Task<VideoIndexerAnswer> UploadVideo(Dictionary<string, string> queryString);
        Task<ProcessState> GetProcessSatet(string VideoIndexerId);
    }


}
