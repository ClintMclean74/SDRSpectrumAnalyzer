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
            mainForm.checkBox8.Checked = mainForm.showGraphs = checkBox1.Checked;

            if (checkBox10.Checked)
                mainForm.LoadData("session.rtl");                
            else
            {
                mainForm.textBox1.Text = textBox1.Text;
                mainForm.textBox2.Text = textBox2.Text;
                mainForm.textBox3.Text = textBox3.Text;                

                mainForm.ActivateSettings();
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
    }
}
