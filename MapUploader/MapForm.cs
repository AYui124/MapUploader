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
        private SftpHelper _sftpHelper;
        private string _remotePath;
        public MapForm()
        {
            InitializeComponent();
            string server = GlobalContext.FtpConfig.Server;
            string port = GlobalContext.FtpConfig.Port;
            string user = GlobalContext.FtpConfig.User;
            string key = GlobalContext.FtpConfig.Key;

            _sftpHelper = new SftpHelper(server, port, user, key);
            _remotePath = GlobalContext.FtpConfig.Path;
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

            string jsonFile = Path.Combine(tempPath, "maplist_third.json");
            _sftpHelper.DownloadFile(_remotePath + "/sourcemod/data/maplist_third.json", jsonFile);

            return JArray.Parse(File.ReadAllText(jsonFile));
        }

        private void dgvMain_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string columnName = dgvMain.Columns[e.ColumnIndex].Name;
            if (columnName != "操作")
                return;
            var mapCode = dgvMain.Rows[e.RowIndex].Cells[4].Value?.ToString();

            var ja = Download();
            JObject find = ja.Cast<JObject>()
                .FirstOrDefault(jo => jo.ContainsKey("map")
                                      && jo.GetValue("map")?.ToString() == mapCode);

            if (find == null)
            {
                MessageBox.Show("删除失败");
                return;
            }

            if (!DeleteFile(find))
            {
                MessageBox.Show("删除失败");
                return;
            }

            ja.Remove(find);
            ja[0]["count"] = ja.Children().Count() - 1;

            string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            string jsonFile = Path.Combine(tempPath, "maplist_third.json");

            File.WriteAllText(jsonFile, ja.ToString(Newtonsoft.Json.Formatting.Indented));
            _sftpHelper.UploadFile(_remotePath + "/sourcemod/data/maplist_third.json", jsonFile);

            BindData();
        }

        private bool DeleteFile(JObject map)
        {
            if (!map.ContainsKey("name"))
            {
                return false;
            }

            var mapName = map.GetValue("name")?.ToString();
            if (string.IsNullOrEmpty(mapName))
            {
                return false;
            }
            try
            {
                return _sftpHelper.DeleteFile(_remotePath + "/workshop/" + mapName + ".vpk");
            }
            catch
            {
                return false;
            }
        }
    }
}
