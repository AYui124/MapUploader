using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MapUploader.Model
{
    public class FtpConfig
    {
        private const string CfgName = "jsconfig.json";

        public bool Inited { get; set; }

        public string Server{ get; set; }

        public string Port { get; set; }

        public string User{ get; set; }

        public string Key{ get; set; }

        public string Path { get; set; }

        public string Cmd { get; set; }

        public static FtpConfig Load()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = System.IO.Path.Combine(basePath, CfgName);
                var cfg = JsonConvert.DeserializeObject<FtpConfig>(File.ReadAllText(configPath));
                cfg.Inited = true;
                return cfg;
            }
            catch
            {
                return new FtpConfig {Inited = false};
            }
        }
    }
}
