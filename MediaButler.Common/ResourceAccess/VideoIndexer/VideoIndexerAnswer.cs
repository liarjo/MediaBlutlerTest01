using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess.VideoIndexer
{
    public class VideoIndexerAnswer
    {
        public bool IsError { get; set; }
        public string Error { get; set; }
        public string VideoIndexId { get; set; }
    }
}
