using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.workflow
{
    
        public class stepTypeInfo
        {
            public stepTypeInfo()
            { }
            public stepTypeInfo(string AssemblyName, string TypeName, string configKey)
            {
                this.AssemblyName = AssemblyName;
                this.TypeName = TypeName;
                this.ConfigKey = configKey;
            }
            public string AssemblyName { get; set; }
            public string TypeName { get; set; }
            public string ConfigKey { get; set; }


        }
    
}
