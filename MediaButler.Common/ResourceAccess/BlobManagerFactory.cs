using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess
{
    public class BlobManagerFactory
    {
        public static IButlerStorageManager CreateBlobManager(string strConn)
        {
            return new BlobStorageManager(strConn);
        }
    }
}
