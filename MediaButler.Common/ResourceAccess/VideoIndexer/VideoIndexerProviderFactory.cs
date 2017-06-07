using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess.VideoIndexer
{
    public class VideoIndexerProviderFactory
    {
        public static IVideoIndexerProvider CreateVideoIndexer(string ApiKey, string EndPoint)
        {
            return new VideoIndexerProvider(ApiKey, EndPoint);
        }
    }
}
