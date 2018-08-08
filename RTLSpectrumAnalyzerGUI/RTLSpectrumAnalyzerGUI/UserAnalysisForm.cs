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

        private void StoreAndClearData()
        {            
            mainForm.bufferFramesArray.Flush(mainForm.series1BinData, mainForm.series2BinData, mainForm.series1BinData);

            mainForm.series1BinData.Store();
            mainForm.series2BinData.Store();

            ClearData();
        }

        private void ClearData()
        {
            /*////
            mainForm.currentBufferFramesObject.bufferFrames.Change(BinDataMode.Far, BinDataMode.NotUsed);
            mainForm.currentBufferFramesObject.bufferFrames.Change(BinDataMode.Near, BinDataMode.NotUsed);
            */            
            
            mainForm.ClearSeries1();
            mainForm.ClearSeries2();
        }

        public void AnalyzingCenterFrequency()
        {            
            label1.Visible = false;

            textBox1.Visible = false;

            button3.Visible = false;

            button2.Visible = false;

            StoreAndClearData();
        }

        public void AnalyzingLeaderboardFrequency()
        {
            label1.Visible = true;

            textBox1.Visible = true;

            button3.Visible = true;

            button2.Visible = true;

            StoreAndClearData();
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

        private void button4_Click(object sender, EventArgs e)
        {
            if (!mainForm.recordingSeries1 && !mainForm.recordingSeries2)
            {
                mainForm.ClearSeries1();
                mainForm.ClearSeries2();
            }
            else
            {
                mainForm.series1BinData.clearFrames = true;
                mainForm.series2BinData.clearFrames = true;
            }

            /*////mainForm.ClearSeries1();
            mainForm.ClearSeries2();
            */
            ////mainForm.bufferFramesArray.Flush(mainForm.series1BinData, mainForm.series2BinData, mainForm.series1BinData);

            ////ClearData();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            mainForm.checkBox5.Checked = checkBox5.Checked;

            if (checkBox5.Checked)
            {
                mainForm.checkBox4.Checked = false;
                checkBox6.Checked = false;
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            mainForm.checkBox6.Checked = checkBox6.Checked;

            if (checkBox6.Checked)
            {
                mainForm.checkBox4.Checked = false;
                checkBox5.Checked = false;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (mainForm.form2 == null || mainForm.form2.IsDisposed)
                mainForm.form2 = new Form2();

            mainForm.form2.Show();

            mainForm.form2.Focus();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            mainForm.ShowSaveDataDialogAndSaveData();
        }
    }
}
