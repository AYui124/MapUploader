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
    public partial class MapForm : Form
    {
        public MapForm()
        {
            InitializeComponent();
        }

        private void MapForm_Load(object sender, EventArgs e)
        {
            SetGridHeader();
            BindData();
        }

        private void SetGridHeader()
        {
            dgvMain.DataSource = new BindingList<MapInfo>();
            int totalWidth = (int)(dgvMain.Width * 0.99);
            if (dgvMain.Columns["Id"] != null)
            {
                dgvMain.Columns["Id"].Visible = false;
            }
            if (dgvMain.Columns["FilePath"] != null)
            {
                dgvMain.Columns["FilePath"].Visible = false;
            }
            if (dgvMain.Columns["MapName"] != null)
            {
                dgvMain.Columns["MapName"].HeaderText = "地图名称";
                dgvMain.Columns["MapName"].Width = (int)(totalWidth * 0.4);

            }
            if (dgvMain.Columns["MapCode"] != null)
            {
                dgvMain.Columns["MapCode"].HeaderText = "地图代码";
                dgvMain.Columns["MapCode"].Width = (int)(totalWidth * 0.4);

            }
            if (dgvMain.Columns["Status"] != null)
            {
                dgvMain.Columns["Status"].Visible = false;
            }
            DataGridViewLinkColumn column = new DataGridViewLinkColumn();
            column.Name = "操作";
            column.UseColumnTextForLinkValue = true;
            column.Text = "删除";
            column.LinkBehavior = LinkBehavior.NeverUnderline;
            column.LinkColor = Color.Black;
            column.VisitedLinkColor = Color.Black;
            column.ActiveLinkColor = Color.Green;
            dgvMain.Columns.Add(column);
        }

        private void BindData()
        {
            var ja = Download();
            List<MapInfo> items = MapInfoHelper.GetMapInfoes(ja);
            dgvMain.DataSource = new BindingList<MapInfo>(items);
        }

        private JArray Download()
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

            return JArray.Parse(File.ReadAllText(jsonFile));
        }

        private void dgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string columnName = dgvMain.Columns[e.ColumnIndex].Name;
            if (columnName != "操作")
                return;
            var mapCode = dgvMain.Rows[e.RowIndex].Cells[4].Value?.ToString();

            var ja = Download();
            JObject find = null;
            foreach (var jToken in ja)
            {
                var jo = (JObject) jToken;
                if (jo.ContainsKey("map") && jo.GetValue("map")?.ToString() == mapCode)
                {
                    find = jo;
                    break;
                }
            }

            if (find != null)
            {
                ja.Remove(find);
                ja[0]["count"] = ja.Children().Count() - 1;
            }

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

            File.WriteAllText(jsonFile, ja.ToString(Newtonsoft.Json.Formatting.Indented));
            sftpHelper.UploadFile(path + "/sourcemod/data/maplist_third.json", jsonFile);

            BindData();
        }
    }
}
