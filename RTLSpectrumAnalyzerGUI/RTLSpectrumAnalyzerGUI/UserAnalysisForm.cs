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
    public partial class UserAnalysisForm : Form
    {
        private Form1 mainForm;

        public UserAnalysisForm(Form1 mainForm)
        {
            InitializeComponent();

            this.mainForm = mainForm;
        }

        private void UserAnalysisForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();

            e.Cancel = true;
        }

        private void UserAnalysisForm_Load(object sender, EventArgs e)
        {

        }

        private void button18_Click(object sender, EventArgs e)
        {
            this.mainForm.button5.PerformClick();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.DontShowInfoBox1)
                new UserInfoDialog().ShowDialog();

            ////MessageBox.Show("Move away from the detector's antenna for more than 4 seconds\r\nand return to the computer moving the mouse or pressing a key to indicate that you're near.");
            

            this.mainForm.button3.PerformClick();

            button17.Enabled = false;
            button18.Enabled = false;

            button18.Text = "Recording Far";
            button18.Enabled = false;

            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ////mainForm.AnalyzeLeaderboardFrequency();

            mainForm.button24.PerformClick();

            Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "1";

            mainForm.currentLeaderboardSignalBeingAnalyzedIndex = -1;
            
            mainForm.AnalyzeLeaderboardFrequency();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox15.Text = "";
            textBox16.Text = "";
            textBox18.Text = "";

            chart5.Series[0].Points.Clear();
            chart8.Series[0].Points.Clear();

            ////mainForm.currentLeaderboardSignalBeingAnalyzedIndex++;

            mainForm.AnalyzeLeaderboardFrequency();
        }
    }
}
