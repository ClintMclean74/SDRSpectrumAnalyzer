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
    public enum UserInfoDialogs {StartDialog, UserAnalysisDialog, TransitionDialog};

    public partial class UserInfoDialog : Form
    {
        int id = -1;        

        public UserInfoDialog(int id)
        {
            InitializeComponent();

            this.id = id;

            this.StartPosition = FormStartPosition.CenterParent;
        }

        public void SetTitle(string text)
        {
            this.Text = text;            
        }

        public void SetText(string text)
        {
            label1.Text = text;

            this.Width = label1.Bounds.X + label1.Bounds.Width + 30;
            this.Height = label1.Bounds.Y + label1.Bounds.Height + 100;
        }

        public void EnableShowMessageAgain(bool value)
        {
            checkBox1.Visible = value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                Properties.Settings.Default.DontShowInfoBoxes[id] = 1;
                ////Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.DontShowInfoBoxes[id] = 0;                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
