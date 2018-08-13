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
    public partial class LeaderboardGraph : Form
    {
        public LeaderboardGraph()
        {
            InitializeComponent();
        }

        private void LeaderboardGraph_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();

            e.Cancel = true;
        }
    }
}
