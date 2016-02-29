using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public interface IdotControl
    {
        /// <summary>
        /// Read value of key from dotControlFile
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>value or "" if the keyu is not on the file</returns>
        string readControlKey(string key);
    }

    public class dotControl : IdotControl
    {
        string jsonControl=null;
        public dotControl(string dotControlURL)
        {
            if (!string.IsNullOrEmpty(dotControlURL))
            {
                //
                IButlerStorageManager resource = BlobManagerFactory.CreateBlobManager(dotControlURL);
                jsonControl = resource.ReadTextBlob(dotControlURL);
                
            }
        }
        public string readControlKey(string key)
        {
            jsonKeyValue x = new jsonKeyValue(jsonControl);
            return x.Read(key);
        }
    }
}
