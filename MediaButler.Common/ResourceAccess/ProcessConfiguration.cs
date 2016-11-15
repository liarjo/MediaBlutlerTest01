using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace MediaButler.Common.ResourceAccess
{
    class ProcessConfiguration : IjsonKeyValue
    {
        private dynamic _jsonDotControl=null;
        private dynamic _jsonProcess=null;
        private  string readData(dynamic data,string key)
        {
            string aux = "";
            try
            {
                aux = data[key].ToString();
            }
            catch (Exception X)
            {
                Trace.TraceWarning("Configuration key " + key + "was not availale. " + X.Message);
            }
            return aux;
        }
        private JToken readDataArray(dynamic data, string key)
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
        public string Read(string key)
        {
            string value = "";
            if (_jsonDotControl!=null)
            {
                value = readData(_jsonDotControl, key);
            }
            if ((value == "") && (_jsonProcess!=null))
            {
                value = readData(_jsonProcess, key);
            }
            return value;
        }
        public JToken ReadArray(string key)
        {
            JToken value = "";
            if (_jsonDotControl != null)
            {
                value = readDataArray(_jsonDotControl, key);
            }
            if ((value == null) && (_jsonProcess != null))
            {
                value = readDataArray(_jsonProcess, key);
            }
            return value;
        }
        private dynamic lodaData(string jsonTxt)
        {
            dynamic data = null;
            try
            {
                //Check if json is well formed
                if (jsonTxt[0] != '{')
                {
                    jsonTxt = jsonTxt.Substring(1, jsonTxt.Length - 1);
                }
                data = JObject.Parse(jsonTxt);
            }
            catch (Exception X)
            {
                string strError= "jsonKeyValue error: " + jsonTxt + " is not valid json file. Error  " + X.Message;
                Trace.TraceError(strError);
                throw new Exception(strError);
            }
            return data;

        }
        public ProcessConfiguration(string jsonDotControl,string jsonProcess)
        {
            if (!string.IsNullOrEmpty(jsonDotControl))
            {
                _jsonDotControl = lodaData(jsonDotControl);
            }
            if (!string.IsNullOrEmpty(jsonProcess))
            {
                _jsonProcess = lodaData(jsonProcess);
            }
        }
    }
}
