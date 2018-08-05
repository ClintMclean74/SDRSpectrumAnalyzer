namespace RTLSpectrumAnalyzerGUI
{
    partial class UserAnalysisForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea5 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea6 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea7 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series7 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea8 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series8 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chart8 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chart4 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chart3 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button18 = new System.Windows.Forms.Button();
            this.button17 = new System.Windows.Forms.Button();
            this.label30 = new System.Windows.Forms.Label();
            this.textBox18 = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.textBox17 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label21 = new System.Windows.Forms.Label();
            this.textBox16 = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.textBox15 = new System.Windows.Forms.TextBox();
            this.chart5 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.chart8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart5)).BeginInit();
            this.SuspendLayout();
            // 
            // chart8
            // 
            chartArea5.AxisX.LabelStyle.Enabled = false;
            chartArea5.AxisY.LabelStyle.Enabled = false;
            chartArea5.Name = "ChartArea1";
            this.chart8.ChartAreas.Add(chartArea5);
            this.chart8.Location = new System.Drawing.Point(13, 430);
            this.chart8.Margin = new System.Windows.Forms.Padding(4);
            this.chart8.Name = "chart8";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series5.Color = System.Drawing.Color.Red;
            series5.IsVisibleInLegend = false;
            series5.Name = "Series";
            series5.YValuesPerPoint = 2;
            this.chart8.Series.Add(series5);
            this.chart8.Size = new System.Drawing.Size(807, 345);
            this.chart8.TabIndex = 90;
            this.chart8.Text = "chart8";
            // 
            // chart4
            // 
            chartArea6.AxisX.LabelStyle.Enabled = false;
            chartArea6.AxisY.LabelStyle.Enabled = false;
            chartArea6.Name = "ChartArea1";
            this.chart4.ChartAreas.Add(chartArea6);
            this.chart4.Location = new System.Drawing.Point(849, 62);
            this.chart4.Margin = new System.Windows.Forms.Padding(4);
            this.chart4.Name = "chart4";
            series6.ChartArea = "ChartArea1";
            series6.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series6.Color = System.Drawing.Color.Red;
            series6.IsVisibleInLegend = false;
            series6.Name = "Series";
            series6.YValuesPerPoint = 2;
            this.chart4.Series.Add(series6);
            this.chart4.Size = new System.Drawing.Size(807, 345);
            this.chart4.TabIndex = 89;
            this.chart4.Text = "chart4";
            // 
            // chart3
            // 
            chartArea7.AxisX.LabelStyle.Enabled = false;
            chartArea7.AxisY.LabelStyle.Enabled = false;
            chartArea7.Name = "ChartArea1";
            this.chart3.ChartAreas.Add(chartArea7);
            this.chart3.Location = new System.Drawing.Point(13, 62);
            this.chart3.Margin = new System.Windows.Forms.Padding(4);
            this.chart3.Name = "chart3";
            series7.ChartArea = "ChartArea1";
            series7.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series7.Color = System.Drawing.Color.Red;
            series7.IsVisibleInLegend = false;
            series7.Name = "Series";
            series7.YValuesPerPoint = 2;
            this.chart3.Series.Add(series7);
            this.chart3.Size = new System.Drawing.Size(807, 345);
            this.chart3.TabIndex = 88;
            this.chart3.Text = "chart3";
            // 
            // button18
            // 
            this.button18.Enabled = false;
            this.button18.Location = new System.Drawing.Point(1193, 977);
            this.button18.Margin = new System.Windows.Forms.Padding(4);
            this.button18.Name = "button18";
            this.button18.Size = new System.Drawing.Size(111, 37);
            this.button18.TabIndex = 92;
            this.button18.Text = "Record";
            this.button18.UseVisualStyleBackColor = true;
            this.button18.Click += new System.EventHandler(this.button18_Click);
            // 
            // button17
            // 
            this.button17.Enabled = false;
            this.button17.Location = new System.Drawing.Point(836, 977);
            this.button17.Margin = new System.Windows.Forms.Padding(4);
            this.button17.Name = "button17";
            this.button17.Size = new System.Drawing.Size(349, 37);
            this.button17.TabIndex = 91;
            this.button17.Text = "Check Far to Near Transition Strength Increase";
            this.button17.UseVisualStyleBackColor = true;
            this.button17.Click += new System.EventHandler(this.button17_Click);
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label30.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label30.Location = new System.Drawing.Point(38, 833);
            this.label30.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(517, 40);
            this.label30.TabIndex = 96;
            this.label30.Text = "Near Strength Transition Increase:";
            // 
            // textBox18
            // 
            this.textBox18.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox18.Location = new System.Drawing.Point(624, 833);
            this.textBox18.Margin = new System.Windows.Forms.Padding(4);
            this.textBox18.Name = "textBox18";
            this.textBox18.Size = new System.Drawing.Size(165, 45);
            this.textBox18.TabIndex = 95;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.Location = new System.Drawing.Point(47, 9);
            this.label22.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(183, 40);
            this.label22.TabIndex = 94;
            this.label22.Text = "Frequency:";
            // 
            // textBox17
            // 
            this.textBox17.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox17.Location = new System.Drawing.Point(271, 9);
            this.textBox17.Margin = new System.Windows.Forms.Padding(4);
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new System.Drawing.Size(702, 45);
            this.textBox17.TabIndex = 93;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1605, 977);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(229, 37);
            this.button1.TabIndex = 97;
            this.button1.Text = "Resume Automated Analysis";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.Location = new System.Drawing.Point(47, 915);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(683, 40);
            this.label21.TabIndex = 102;
            this.label21.Text = "Averaged Transitions Near Strength Increase:";
            // 
            // textBox16
            // 
            this.textBox16.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox16.Location = new System.Drawing.Point(812, 915);
            this.textBox16.Margin = new System.Windows.Forms.Padding(4);
            this.textBox16.Name = "textBox16";
            this.textBox16.Size = new System.Drawing.Size(165, 45);
            this.textBox16.TabIndex = 101;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.Location = new System.Drawing.Point(1468, 914);
            this.label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(188, 40);
            this.label20.TabIndex = 99;
            this.label20.Text = "Transitions:";
            // 
            // textBox15
            // 
            this.textBox15.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox15.Location = new System.Drawing.Point(1691, 913);
            this.textBox15.Margin = new System.Windows.Forms.Padding(4);
            this.textBox15.Name = "textBox15";
            this.textBox15.Size = new System.Drawing.Size(97, 45);
            this.textBox15.TabIndex = 98;
            // 
            // chart5
            // 
            chartArea8.AxisX.LabelStyle.Enabled = false;
            chartArea8.AxisY.LabelStyle.Enabled = false;
            chartArea8.Name = "ChartArea1";
            this.chart5.ChartAreas.Add(chartArea8);
            this.chart5.Location = new System.Drawing.Point(849, 430);
            this.chart5.Margin = new System.Windows.Forms.Padding(4);
            this.chart5.Name = "chart5";
            series8.ChartArea = "ChartArea1";
            series8.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series8.Color = System.Drawing.Color.Red;
            series8.IsVisibleInLegend = false;
            series8.Name = "Series";
            series8.YValuesPerPoint = 2;
            this.chart5.Series.Add(series8);
            this.chart5.Size = new System.Drawing.Size(807, 345);
            this.chart5.TabIndex = 100;
            this.chart5.Text = "chart5";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1364, 977);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(233, 37);
            this.button2.TabIndex = 103;
            this.button2.Text = "Next Leaderboard Frequency";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(1220, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(332, 40);
            this.label1.TabIndex = 105;
            this.label1.Text = "Leaderboard Ranking";
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(1605, 9);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(71, 45);
            this.textBox1.TabIndex = 104;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(1691, 9);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(116, 45);
            this.button3.TabIndex = 106;
            this.button3.Text = "Reset to 1\'st";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // UserAnalysisForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1884, 1016);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.textBox16);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.textBox15);
            this.Controls.Add(this.chart5);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label30);
            this.Controls.Add(this.textBox18);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.textBox17);
            this.Controls.Add(this.button18);
            this.Controls.Add(this.button17);
            this.Controls.Add(this.chart8);
            this.Controls.Add(this.chart4);
            this.Controls.Add(this.chart3);
            this.Name = "UserAnalysisForm";
            this.Text = "User Analysis";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UserAnalysisForm_FormClosing);
            this.Load += new System.EventHandler(this.UserAnalysisForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart5)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.DataVisualization.Charting.Chart chart8;
        public System.Windows.Forms.DataVisualization.Charting.Chart chart4;
        public System.Windows.Forms.DataVisualization.Charting.Chart chart3;
        public System.Windows.Forms.Button button18;
        public System.Windows.Forms.Button button17;
        private System.Windows.Forms.Label label30;
        public System.Windows.Forms.TextBox textBox18;
        private System.Windows.Forms.Label label22;
        public System.Windows.Forms.TextBox textBox17;
        public System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label21;
        public System.Windows.Forms.TextBox textBox16;
        private System.Windows.Forms.Label label20;
        public System.Windows.Forms.TextBox textBox15;
        public System.Windows.Forms.DataVisualization.Charting.Chart chart5;
        public System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox textBox1;
        public System.Windows.Forms.Button button3;
    }
}