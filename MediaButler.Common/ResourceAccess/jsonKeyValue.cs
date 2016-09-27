using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public class jsonKeyValue : IjsonKeyValue
    {
        private dynamic data;
        public jsonKeyValue(string jsonTxt)
        {
            try
            {
                //Check if json is well formed
                if (jsonTxt[0]!= '{')
                {
                    jsonTxt = jsonTxt.Substring(1, jsonTxt.Length - 1);
                }
                  data = JObject.Parse(jsonTxt);
            }
            catch (Exception X)
            {
                Trace.TraceError("jsonKeyValue error: " + jsonTxt + " is not valid json file. Error  " + X.Message);
                //throw X;
            }
            
        }
        

        public string GetJason()
        {
            throw new NotImplementedException();
        }

        public string Read(string key)
        {
            string aux="";
            try
            {
                //Explicit TOStirng
                aux = data[key].ToString();

            }
            catch (Exception X)
            {
                Trace.TraceWarning("Configuration key " + key + "was not availale. " +  X.Message);
            }
            return aux;
        }

        public JToken ReadArray(string key)
        {
            JToken aux = null;
            try
            {
                aux = (JArray)data[key];

            }
            catch (Exception X)
            {
                Trace.TraceWarning("Configuration key " + key + "was not availale. " + X.Message);
            }
            return aux;
        }
    
    }
}
