using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.AzureSearch
{
    public class videotext
    {
        public string id { get; set; }
        public string assetid { get; set; }
        public string title { get; set; }
        public double recognizability { get; set; }
        public string begin { get; set; }
        public string end { get; set; }
        public string text { get; set; }
        public string url { get; set; }
    }
}
