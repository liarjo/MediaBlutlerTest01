using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public interface IjsonKeyValue
    {
        /// <summary>
        /// read configuration value of specific key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string Read(string key);
        /// <summary>
        /// Read al Jsonf file
        /// </summary>
        /// <returns></returns>
        string GetJason();
        JToken ReadArray(string key);
    }
}
