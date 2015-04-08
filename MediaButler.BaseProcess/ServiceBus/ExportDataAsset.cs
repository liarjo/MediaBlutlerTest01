using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.ServiceBus
{
    public class ExportDataAsset
    {
        public string AssetId { get; set; }
        public string AlternateId { get; set; }
        public string Smooth { get; set; }
        public string HLS { get; set; }
        public string DASH { get; set; }
    }
}
