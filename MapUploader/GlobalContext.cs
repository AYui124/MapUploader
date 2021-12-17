using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapUploader.Model;

namespace MapUploader
{
    public class GlobalContext
    {
        private static FtpConfig _ftpConfig;
        public static FtpConfig FtpConfig => _ftpConfig ?? (_ftpConfig = FtpConfig.Load());
    }
}
