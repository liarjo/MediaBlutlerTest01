using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                data = JObject.Parse(jsonTxt);
            }
            catch (Exception X)
            {
                Trace.TraceError("jsonKeyValue error: " + jsonTxt + " is not valid json file");
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
                aux = data.SelectToken(key);
            }
            catch (Exception)
            {

              
            }
            return aux;
        }
    }
}
