using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RTLSpectrumAnalyzerGUI
{
    public partial class UserInfoDialog : Form
    {
        public UserInfoDialog()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DontShowInfoBox1 = !checkBox1.Checked;
        }
    }
}
