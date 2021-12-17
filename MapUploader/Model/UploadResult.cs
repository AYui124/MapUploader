using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapUploader.Model
{
    public class UploadResult
    {
        public bool Status { get; set; }
        public string Message { get; set; }

        public MapInfo Info { get; set; }

        public static UploadResult Fail(string msg, MapInfo info = null)
        {
            return new UploadResult
            {
                Status = false,
                Message = msg,
                Info = info
            };
        }

        public static UploadResult Success(string msg, MapInfo info = null)
        {
            return new UploadResult
            {
                Status = true,
                Message = msg,
                Info = info
            };
        }
    }
}
