using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace MapUploader.Remote
{
    public class SftpHelper
    {
        private readonly SftpClient _sftpClient;

        public SftpHelper(string server, string port, string user ,string key)
        {
            if (!int.TryParse(port, out int intPort))
            {
                intPort = 22;
            }
            _sftpClient = new SftpClient(server, intPort, user, key);
        }

        public bool Connected => _sftpClient.IsConnected;
        
        public bool Connect()
        {
            try
            {
                if (!Connected)
                {
                    _sftpClient.Connect();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_sftpClient != null && Connected)
                {
                    _sftpClient.Disconnect();
                }
            }
            catch
            {
               //
            }
        }

        public bool UploadFile(string remotePath, string localPath, Action<ulong> callBack = null)
        {
            try
            {
                int iIndex = remotePath.LastIndexOf("/", StringComparison.Ordinal);
                string strDir = remotePath.Substring(0, iIndex);
                if (!CreateDir(strDir))
                    return false;
                using (var file = File.OpenRead(localPath))
                {
                    if (!Connect())
                        return false;
                    _sftpClient.UploadFile(file, remotePath, callBack);
                    Disconnect();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DownloadFile(string remotePath, string localPath)
        {
            try
            {
                Connect();
                var byt = _sftpClient.ReadAllBytes(remotePath);
                Disconnect();
                File.WriteAllBytes(localPath, byt);
                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool CreateDir(string remotePath)
        {
            try
            {
                if(!Connect())
                    return false;
                string[] items = remotePath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                string strPath = "";
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(strPath))
                        strPath = item;
                    else
                        strPath = strPath + "/" + item;

                    if (!_sftpClient.Exists(strPath))
                    {
                        _sftpClient.CreateDirectory(strPath);
                    }
                }

                Disconnect();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
