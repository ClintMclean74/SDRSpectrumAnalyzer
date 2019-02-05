
/*
* Author: Clint Mclean
*
* RTL SDR SpectrumAnalyzer turns a RTL2832 based DVB dongle into a spectrum analyzer
* 
* This spectrum analyzer, though, is specifically designed for detecting reradiated
* signals from humans. These are frequencies that are transmitted and could have intentional
* and unintentional biological effects.
* 
* The signals generate electrical currents in humans, and could have biological effects
* because of our electrochemical systems that use biologically generated electrical currents.
* 
* These radio/microwaves are reradiated, the electrical currents that are produced generate
* another reradiated electromagnetic wave. So they are detectable.
* 
* This rtl sdr spectrum analyzer then is designed for automatically detecting these reradiated signals.
* 
* Uses RTLSDRDevice.DLL for doing the frequency scans
* which makes use of the librtlsdr library: https://github.com/steve-m/librtlsdr
* and based on that library's included rtl_power.c code to get frequency strength readings
* 
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Windows.Forms;

namespace RTLSpectrumAnalyzerGUI
{
    public partial class quickStartForm : Form
    {
        Form1 mainForm;

        public quickStartForm(Form1 mainForm)
        {
            this.mainForm = mainForm;

            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
			mainForm.SetStartup(checkBox4.Checked);

            mainForm.transitionAnalysesMode = checkBox2.Checked;

            if (!checkBox2.Checked && !checkBox3.Checked)
                mainForm.checkBox9.Checked = false;

            if (checkBox1.Checked)
            {
                mainForm.checkBox8.Checked = mainForm.checkBox13.Checked = mainForm.showGraphs = checkBox1.Checked;

                mainForm.checkBox13.Checked = true;
                mainForm.checkBox15.Checked = false;

                mainForm.checkBox13.Enabled = mainForm.checkBox15.Enabled = false;

                ////mainForm.neverShowGraphs = false;
            }            
            else
            {
                mainForm.checkBox8.Checked = mainForm.showGraphs = checkBox1.Checked;
                mainForm.checkBox13.Checked = false;
                mainForm.checkBox15.Checked = true;

                mainForm.checkBox13.Enabled = mainForm.checkBox15.Enabled = true;

                ////mainForm.neverShowGraphs = true;
            }

            if (checkBox10.Checked)
                mainForm.LoadData(Form1.SESSION_PATH + Form1.SESSION_FILE_NAME);                
            else
            {
                mainForm.textBox1.Text = textBox1.Text;
                mainForm.textBox2.Text = textBox2.Text;
                mainForm.textBox3.Text = textBox3.Text;                

                mainForm.ActivateSettings();
            }

            if (mainForm.transitionAnalysesMode)
            {
                /////////mainForm.InitializeTransitionSignalsToBeAnalysed(10, mainForm.dataLowerFrequency, mainForm.dataUpperFrequency);                
                mainForm.InitializeTransitionSignalsToBeAnalysed(Form1.MAX_TRANSITION_SCANS, mainForm.dataLowerFrequency, mainForm.dataUpperFrequency);
            }

            mainForm.RecordSeries2();

            this.Hide();
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox10.Checked)
            {
                textBox1.Enabled = textBox2.Enabled  = textBox3.Enabled = false;
            }
            else
                textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                checkBox2.Checked = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                checkBox3.Checked = false;
        }

		private void checkBox4_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void quickStartForm_Load(object sender, EventArgs e)
		{
			try
			{
				if (Properties.Settings.Default.FirstRun == true)
				{
					checkBox4.Checked = true;

					Properties.Settings.Default.FirstRun = false;
					Properties.Settings.Default.Save();
				}
				else
				{
					Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

					if (rk != null)
					{
						object rValue = rk.GetValue("RTLSpectrumAnalyzer");

						if (rValue != null)
						{
							checkBox4.Checked = true;
						}
						else
							checkBox4.Checked = false;
					}
				}
			}
			catch(Exception ex)
			{

			}			
		}
	}
}
