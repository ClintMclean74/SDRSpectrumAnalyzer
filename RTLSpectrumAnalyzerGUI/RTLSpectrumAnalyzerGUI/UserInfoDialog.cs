
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RTLSpectrumAnalyzerGUI
{
    public enum UserInfoDialogs {StartDialog, UserAnalysisDialog, TransitionDialog, ZoomInDialog };

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
