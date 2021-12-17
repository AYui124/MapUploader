using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MapUploader.Model;
using MapUploader.Remote;
using Newtonsoft.Json.Linq;

namespace MapUploader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void toolStripButtonCheck_Click(object sender, EventArgs e)
        {
            string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            string server = GlobalContext.FtpConfig.Server;
            string port = GlobalContext.FtpConfig.Port;
            string user = GlobalContext.FtpConfig.User;
            string key = GlobalContext.FtpConfig.Key;
            string path = GlobalContext.FtpConfig.Path;
            SftpHelper sftpHelper = new SftpHelper(server, port, user, key);
            
            string jsonFile = Path.Combine(tempPath, "maplist_third.json");
            sftpHelper.DownloadFile(path + "/sourcemod/data/maplist_third.json", jsonFile);
            
            var ja = JArray.Parse(File.ReadAllText(jsonFile));
            StringBuilder writer = new StringBuilder();
            writer.AppendLine("已存在地图:");
            foreach (var jToken in ja)
            {
                var data = (JObject)jToken;
                if (data.ContainsKey("name") && data.ContainsKey("map"))
                {
                    StringBuilder line = new StringBuilder();
                    
                    line.Append(data.GetValue("name"));
                    line.Append(":  ");
                    line.Append(data.GetValue("map"));

                    writer.AppendLine(line.ToString());
                }
            }

            MsgForm msg = new MsgForm();
            msg.SetMsg(writer.ToString());
            msg.Show();
        }

        private void toolStripButtonAdd_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter= "地图文件|*.vpk"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var files = dialog.FileNames.ToList();
                BindGrid(files);
            }
        }

        private void BindGrid(List<string> files)
        {
            var items = GetGridItems();
            foreach (var file in files)
            {
                items.Add(MapInfoHelper.GetMapInfo(file));
            }

            dgvMain.DataSource = new BindingList<MapInfo>(items);
        }

        private List<MapInfo> GetGridItems()
        {
            if (dgvMain.DataSource == null)
            {
                return new List<MapInfo>();
            }
            var items = JArray.FromObject(dgvMain.DataSource).ToObject<List<MapInfo>>();
            return items ?? new List<MapInfo>();
        }

        private void toolStripButtonUpload_Click(object sender, EventArgs e)
        {
            toolStripButtonAdd.Enabled = false;
            toolStripButtonUpload.Enabled = false;
            toolStripButtonCheck.Enabled = false;

            var list = GetGridItems();
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += UploadDoWork;
            worker.ProgressChanged += UploadProgressChanged;
            worker.RunWorkerCompleted += UploadCompleted;
            worker.RunWorkerAsync(list);
            
        }

        private void UploadDoWork(object sender, DoWorkEventArgs e)
        {
            string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            var worker = sender as BackgroundWorker;
            var data = e.Argument as List<MapInfo>;
            if (data == null || !data.Any())
            {
                worker?.ReportProgress(100, UploadResult.Fail("没有数据"));
                return;
            }

            if (!GlobalContext.FtpConfig.Inited)
            {
                worker?.ReportProgress(100, UploadResult.Fail("服务器参数为空"));
                return;
            }

            string server = GlobalContext.FtpConfig.Server;
            string port = GlobalContext.FtpConfig.Port;
            string user = GlobalContext.FtpConfig.User;
            string key = GlobalContext.FtpConfig.Key;
            string path = GlobalContext.FtpConfig.Path;
            SftpHelper sftpHelper = new SftpHelper(server, port, user, key);
            for (int i = 0; i < data.Count; i++)
            {
                var map = data.ElementAt(i);
                string file = map.FilePath;
                string name = map.MapName;
                string code = map.MapCode;

                long length = new FileInfo(file).Length;
                var oldPercent = 0d;
                var step = i;
                var fileResult = sftpHelper.UploadFile(path + "/workshop/" + name + ".vpk", file,
                    value =>
                    {
                        var percent = (Convert.ToDouble(value) / Convert.ToDouble(length)) * 100;
                        if (Math.Abs(percent - oldPercent) > 0.2
                            || Math.Abs(percent - 100) < 0.01)
                        {
                            oldPercent = percent;
                            map.Status = percent.ToString("F4") + "%";
                            worker?.ReportProgress(step, UploadResult.Success("上传成功", map));
                        }
                    });

                if (!fileResult)
                {
                    map.Status = "失败";
                    worker?.ReportProgress(100, UploadResult.Fail("上传失败", map));
                    return;
                }
                string jsonFile = Path.Combine(tempPath, "maplist_third.json");
                sftpHelper.DownloadFile(path + "/sourcemod/data/maplist_third.json", jsonFile);
                if (string.IsNullOrEmpty(jsonFile) || !File.Exists(jsonFile))
                {
                    map.Status = "有错误";
                    worker?.ReportProgress(99, UploadResult.Success("上传成功但修改配置失败,请联系管理员!", map));
                    return;
                }
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                {
                    map.Status = "成功";
                    worker?.ReportProgress(99, UploadResult.Success("上传成功", map));
                    return;
                }
                var ja = JArray.Parse(File.ReadAllText(jsonFile));
                JObject jo = new JObject { { "map", code }, { "name", name } };
                ja.Add(jo);
                ja[0]["count"] = ja.Children().Count() - 1;

                File.WriteAllText(jsonFile, ja.ToString(Newtonsoft.Json.Formatting.Indented));
                sftpHelper.UploadFile(path + "/sourcemod/data/maplist_third.json", jsonFile);

            }
        }

        private void UploadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!(e.UserState is UploadResult result)) 
                return;
            bool success = result.Status; 
            string msg = result.Message;
            if (success == false && !string.IsNullOrEmpty(msg))
            {
                MessageBox.Show(msg);
                return;
            }
            MapInfo map = result.Info;
            if (map == null)
            {
                return;
            }
            var data = GetGridItems();
            int index= data.FindIndex(o => o.Id == map.Id);
            if (index < 0)
            {
                return;
            }
            data.RemoveAll(o => o.Id == map.Id);
            data.Insert(index, map);

            dgvMain.DataSource = new BindingList<MapInfo>(data);
        }

        private void UploadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripButtonAdd.Enabled = true;
            toolStripButtonUpload.Enabled = true;
            toolStripButtonCheck.Enabled = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetGridHeader();
        }

        private void SetGridHeader()
        {
            dgvMain.DataSource = new BindingList<MapInfo>();
            int totalWidth = (int) (dgvMain.Width * 0.99);
            if (dgvMain.Columns["Id"] != null)
            {
                dgvMain.Columns["Id"].Visible = false;
            }
            if (dgvMain.Columns["FilePath"] != null)
            {
                dgvMain.Columns["FilePath"].HeaderText = "文件路径";
                dgvMain.Columns["FilePath"].Width = (int)(totalWidth * 0.4);
            }
            if (dgvMain.Columns["MapName"] != null)
            {
                dgvMain.Columns["MapName"].HeaderText = "地图名称";
                dgvMain.Columns["MapName"].Width = (int)(totalWidth * 0.2);

            }
            if (dgvMain.Columns["MapCode"] != null)
            {
                dgvMain.Columns["MapCode"].HeaderText = "地图代码";
                dgvMain.Columns["MapCode"].Width = (int)(totalWidth * 0.2);

            }
            if (dgvMain.Columns["Status"] != null)
            {
                dgvMain.Columns["Status"].HeaderText = "状态";
                dgvMain.Columns["Status"].Width = (int)(totalWidth * 0.2);

            }
        }

        
    }
}
