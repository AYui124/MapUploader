using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapUploader
{
    public partial class MsgForm : Form
    {
        public MsgForm()
        {
            InitializeComponent();
        }

        public void SetMsg(string msg)
        {
            tbMsg.Text = msg;
            tbMsg.Select(0, 0);
        }
    }
}
