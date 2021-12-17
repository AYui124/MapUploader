using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapUploader.Model
{
    public class MapInfo
    {
        public Guid Id { get; set; }

        public string FilePath{ get; set; }

        public string MapName { get; set; }

        public string MapCode { get; set; }

        public string Status { get; set; }
    }
}
