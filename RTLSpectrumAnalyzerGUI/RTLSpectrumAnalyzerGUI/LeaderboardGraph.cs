using System;
using System.Data;
using System.Linq;
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

        private void chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            Utilities.AutoAdjustChartZoom(chart1, e, "Series");
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
