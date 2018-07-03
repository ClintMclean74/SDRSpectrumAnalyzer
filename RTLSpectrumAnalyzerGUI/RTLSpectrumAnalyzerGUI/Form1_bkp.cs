/*
* Author: Clint Mclean
*
* RTLSpectrumAnalyzerGUI turns a RTL2832 based DVB dongle into a spectrum analyzer
* 
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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace RTLSpectrumAnalyzerGUI
{
    public partial class Form1 : Form
    {
        public const bool PROXIMITRY_DETECTOR = false;
        public static long MAXIMUM_GRAPH_BIN_COUNT = 100000;

        public const long SAMPLE_RATE = 1000000;
        const double nearSignalMin = 10;

        Form1 mainForm;

        ProximitryFrequency proximitryFrequency = new ProximitryFrequency();

        double graph1BinFreqInc;
        double graph2BinFreqInc;
        double graph1LowerFrequency;
        double graph1UpperFrequency;

        public double graph2LowerFrequency;
        public double graph2UpperFrequency;

        List<long> nearSignal = new List<long>();

        public uint dataLowerFrequency = 0, dataUpperFrequency = 0, stepSize;

        double rangeSamplingPercentage = 100;

        double difThreshold = 0;
        uint totalBinCount = 0;

        public bool recordingSeries1 = false;
        public bool recordingSeries2 = false;

        public bool startRecordingSeries1 = false;
        public bool startRecordingSeries2 = false;


        float[] difBinArray = null;

        bool resetGraph = true;

        bool newData = false;

        double minAvgStrength = 99999999;
        double maxAvgStrength = -99999999;

        Waterfall waterFall, waterFallAvg;

        double prevWaterFallMinimum;
        double prevWaterFallMaximum;
        double prevNearStrengthDeltaRange;

        double totalADCMagnitudeFar = 0;
        double totalADCMagnitudeNear = 0;

        int magnitudeBufferCount = 0;

        double[] magnitudeBuffer = new double[100];

        double series1MinYChart1 = 99999999, series2MinYChart1 = 99999999;
        double series1MaxYChart1 = -99999999, series2MaxYChart1 = -99999999;

        static public double series1MinYChart2 = 99999999, series2MinYChart2 = 99999999;
        static public double series1MaxYChart2 = -99999999, series2MaxYChart2 = -99999999;

        static public double series2Max = -99999999;

        double averageSeries1CurrentFrameStrength = 0;
        double averageSeries2CurrentFrameStrength = 0;

        double averageSeries1TotalFramesStrength = 0;
        double averageSeries2TotalFramesStrength = 0;

        int deviceCount = 1;

        List<InterestingSignal> leaderBoardSignals = new List<InterestingSignal>();
        List<InterestingSignal> interestingSignals = new List<InterestingSignal>();

        short MAX_LEADER_BOARD_LIST_COUNT = 8;
        short MAX_INTERESTING_SIGNAL_LIST_COUNT = 100;

        string evaluatedFrequencyString = "";


        System.Windows.Forms.Timer eventTimer;

        int currentLeaderBoardSignalIndex = -1;

        bool analyzingLeaderBoardSignals = false;

        string originalStartFrequency;

        string originalEndFrequency;


        private enum TimeBasedGraphData { CurrentGraph, AverageGraph };

        TimeBasedGraphData dataUsedForTimeBasedGraph = TimeBasedGraphData.AverageGraph;

        public const long MAXIMUM_TIME_BASED_GRAPH_POINTS = 100;

        private NotifyIcon notifyIcon1 = new NotifyIcon();
        private NotifyIcon notifyIcon2 = new NotifyIcon();

        private long[] notificationTime = { 20000, 21000, 22000, 23000, 24000, 25000, 26000, 27000, 28000, 29000, 30000 };

        ////private long[] notificationTime = { 1000, 10000 };

        private uint currentNotificationTimeIndex = 0;


        public BinDataMode currentMode = BinDataMode.Indeterminate;

        private bool analyzingNearFarTransitions = false;

        private BufferFramesArray bufferFramesArray = new BufferFramesArray();

        private CommandBuffer commandBuffer = new CommandBuffer();
        private CommandBuffer commandQueue = new CommandBuffer();

        public BufferFramesObject currentBufferFramesObject = null;

        private int nearFarBufferIndex;

        public float[] totalBinBufferArray;
        public float[] avgBinBufferArray;

        public bool recordingIntoBuffer = true;

        private string saveFarSeriesBufferFileFileName = "saveBufferFiles/farBufferFile.rtl";

        private string saveNearSeriesBufferFileFileName = "saveBufferFiles/nearBufferFile.rtl";


        MouseInput mouseInput;

        KeyboardInput keyboardInput;



        Form2 form2 = new Form2();



        BinData series1BinDataFullRange;

        BinData series2BinDataFullRange;

        BinData series1BinData;
        BinData series2BinData;

        public double binSize;

        Stack<Utilities.FrequencyRange> graph1FrequencyRanges = new Stack<Utilities.FrequencyRange>();
        Stack<Utilities.FrequencyRange> graph2FrequencyRanges = new Stack<Utilities.FrequencyRange>();

        double avgFramesForRegion, prevAvgFramesForRegion;
        uint nearFrames, prevNearFrames;
        uint indeterminateFrames, prevIndeterminateFrames;
        uint framesDif;

        bool automatedZooming = true;        


        private void ZoomOutOfFrequency()
        {
            if (graph1FrequencyRanges.Count > 0)
            ////if (false)
            {
                commandBuffer.AddCommand("ZoomOutOfFrequency");

                analyzingNearFarTransitions = false;

                automatedZooming = true;

                button24.Enabled = false;

                ////TransitionGradientArray transitionGradientArray = bufferFramesArray.GetStrongestTransitionsFrequencyGradientArray();

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                textBox1.Text = zoomedOutBufferObject.lowerFrequency.ToString();
                textBox2.Text = zoomedOutBufferObject.upperFrequency.ToString();


                if (button3.Text == "Stop Recording")
                    button3.PerformClick();
                else
                    if (button5.Text == "Stop Recording")
                    button5.PerformClick();

                ////if (button5.Text == "Record Near Series Data")
                ////button5.PerformClick();

                startRecordingSeries2 = true;

                eventTimer = new System.Windows.Forms.Timer();

                eventTimer.Tick += new EventHandler(NewSettingsThread);
                eventTimer.Interval = 1000;

                eventTimer.Start();
            }
        }


        private void InitializeZoomToFrequencyThread()
        {
            this.Invoke(new Action(() =>
            {
                Command command = commandBuffer.GetMostRecentCommand();

                if (command == null || command.name != "InitializeZoomToFrequencyThread" || Environment.TickCount - command.time > 20000)
                {
                    commandBuffer.AddCommand("InitializeZoomToFrequencyThread");

                    eventTimer = new System.Windows.Forms.Timer();

                    eventTimer.Tick += new EventHandler(DetermineInterestingSignalAndZoomToFrequencyThread);
                    eventTimer.Interval = 10000;

                    eventTimer.Start();
                }
            }));
        }

        private void DetermineInterestingSignalAndZoomToFrequencyThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                eventTimer.Stop();

                eventTimer.Interval = 10000000;

                eventTimer.Dispose();

                eventTimer = null;

                DetermineInterestingSignalAndZoomToFrequency();
            }
        }

        private void DetermineInterestingSignalAndZoomToFrequency()
        {
            if (leaderBoardSignals.Count > 0)
            {
                int secondOptionLeaderBoardSignalIndex = -1;

                if (currentLeaderBoardSignalIndex < 0)
                    currentLeaderBoardSignalIndex++;

                if (currentLeaderBoardSignalIndex >= MAX_LEADER_BOARD_LIST_COUNT)
                    currentLeaderBoardSignalIndex = 0;

                Utilities.FrequencyRange frequencyRange;
                BufferFramesObject bufferFramesObject = null;

                while (currentLeaderBoardSignalIndex < MAX_LEADER_BOARD_LIST_COUNT && currentLeaderBoardSignalIndex < leaderBoardSignals.Count)
                {
                    frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)leaderBoardSignals[currentLeaderBoardSignalIndex].frequency);

                    bufferFramesObject = bufferFramesArray.GetBufferFramesObject((long)frequencyRange.lower, (long)frequencyRange.upper);

                    if (bufferFramesObject != null && (bufferFramesObject.transitionBufferFrames.transitions >= BufferFrames.MIN_ZOOMED_IN_TRANSISTION_FRAMES) && currentLeaderBoardSignalIndex < MAX_LEADER_BOARD_LIST_COUNT)
                    {
                        currentLeaderBoardSignalIndex++;
                    }
                    else
                    {
                        if (bufferFramesObject != null && bufferFramesObject.transitionBufferFrames.bufferFramesArray.Count > 0 && bufferFramesObject.transitionBufferFrames.GetStrongestTransitionsGradientFrequency().array[0].strength < 0)
                        {
                            secondOptionLeaderBoardSignalIndex = currentLeaderBoardSignalIndex;
                            currentLeaderBoardSignalIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (currentLeaderBoardSignalIndex < MAX_LEADER_BOARD_LIST_COUNT)
                    ZoomToFrequency((long)leaderBoardSignals[currentLeaderBoardSignalIndex].frequency);
                else
                    if (secondOptionLeaderBoardSignalIndex > -1)
                    ZoomToFrequency((long)leaderBoardSignals[secondOptionLeaderBoardSignalIndex].frequency);
                else
                {
                    ////MessageBox.Show("Leaderboard signals analayzed for " + BufferFrames.MIN_ZOOMED_IN_TRANSISTION_FRAMES + " transitions");

                    notifyIcon2.Icon = SystemIcons.Exclamation;

                    notifyIcon2.BalloonTipTitle = "Transitions Analysis";
                    notifyIcon2.BalloonTipText = "Leaderboard signals analayzed for " + BufferFrames.MIN_ZOOMED_IN_TRANSISTION_FRAMES + " or more transitions";

                    notifyIcon2.Visible = true;

                    notifyIcon2.ShowBalloonTip(10000);

                    Random rnd = new Random();
                    int index = rnd.Next(MAX_LEADER_BOARD_LIST_COUNT);

                    ZoomToFrequency((long)leaderBoardSignals[index].frequency);
                }

            }
        }

        private void ZoomToFrequency(long frequency)
        {
            if (recordingSeries1 || !automatedZooming)
            {
                ////Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count - 2);

                Command command = commandBuffer.GetMostRecentCommand();             

                commandBuffer.AddCommand("ZoomToFrequency:"+frequency);

                if (automatedZooming)
                    commandQueue.AddCommand("AutomatedZoomToFrequency");
                else if (recordingSeries1 || recordingSeries2)
                    commandQueue.AddCommand("ZoomToFrequency");

                ////if (automatedZooming)
                analyzingNearFarTransitions = true;                                

                Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);
                graph1FrequencyRanges.Push(frequencyRange);

                this.Invoke(new Action(() =>
                {
                    textBox1.Text = Math.Round(frequencyRange.lower).ToString();
                    textBox2.Text = Math.Round(frequencyRange.upper).ToString();

                    if (button3.Text == "Stop Recording")
                    {
                        this.Cursor = Cursors.WaitCursor;
                        recordingSeries1 = false;
                    }                     
                    else
                        if (button5.Text == "Stop Recording")
                    {
                        this.Cursor = Cursors.WaitCursor;
                        recordingSeries2 = false;
                    }
                    /*////else
                        ActivateSettings(false);
                        */                        
                }));

                if (button3.Text == "Stop Recording")
                {
                    this.Cursor = Cursors.WaitCursor;
                    recordingSeries1 = false;
                }
                else
                 if (button5.Text == "Stop Recording")
                {
                    this.Cursor = Cursors.WaitCursor;
                    recordingSeries2 = false;
                }
                /*////else
                    ActivateSettings(false);
                    */

                /*////eventTimer = new System.Windows.Forms.Timer();

                if (automatedZooming)
                    eventTimer.Tick += new EventHandler(NewSettingsThread2);
                else
                    eventTimer.Tick += new EventHandler(NewSettingsThread);

                eventTimer.Interval = 1000;

                eventTimer.Start();
                */


                if (!automatedZooming && (button3.Text != "Stop Recording" && button5.Text != "Stop Recording"))
                {
                    ActivateSettings(false);

                    ////ZoomGraphsToFrequency(frequency);
                }

                    /*////Thread.Sleep(4000);

                        if (command == null || command.name != "UserSelectedFrequencyForAnalysis")
                        {
                            startRecordingSeries1 = true;
                        }
                        else
                        {
                            startRecordingSeries1 = false;
                            startRecordingSeries2 = false;
                            ////startRecordingSeries2 = true;
                        }

                        ////ActivateSettings(false);

                        if (command != null && command.name == "UserSelectedFrequencyForAnalysis")
                        {
                            series1MinYChart1 = 99999999;
                            series1MaxYChart1 = -99999999;

                            series1MinYChart2 = 99999999;
                            series1MaxYChart2 = -99999999;

                            ZoomGraphsToFrequency(frequency);
                        }

                        /*if (listBox2.SelectedIndex >= 0)
                        {
                            string itemStr = (string)listBox2.Items[listBox2.SelectedIndex];

                            string[] itemStrArray = itemStr.Split(':');

                            long frequency = long.Parse(itemStrArray[0]);

                            ZoomGraphsToFrequency(frequency);
                        }*/


                    /*////eventTimer = new System.Windows.Forms.Timer();

                    eventTimer.Tick += new EventHandler(NewSettingsThread);
                    eventTimer.Interval = 1000;

                    eventTimer.Start();
                    */

                }
        }


        private double RangeChanged(System.Windows.Forms.DataVisualization.Charting.Chart chart, string dataSeries, float[] data, long lowerIndex, long upperIndex, double newLowerFrequency, ref double graphBinFreqInc)
        {
            try
            {
                if (data.Length > 0)
                {
                    long graphBinCount = upperIndex - lowerIndex;

                    long lowerResGraphBinCount;

                    if (graphBinCount > MAXIMUM_GRAPH_BIN_COUNT)
                        lowerResGraphBinCount = MAXIMUM_GRAPH_BIN_COUNT;
                    else
                        lowerResGraphBinCount = graphBinCount;

                    double inc = (double)graphBinCount / lowerResGraphBinCount;

                    graphBinFreqInc = inc * binSize;

                    double index = lowerIndex;

                    double value;

                    double binFrequency = newLowerFrequency;


                    int minYIndex = -1, maxYIndex = -1;

                    double minY = 99999999, maxY = -99999999;

                    chart.Series[dataSeries].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star10;

                    InterestingSignal interestingSignal;

                    int interestingSignalIndex;

                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint1=null;                    

                    for (int i = 0; i < lowerResGraphBinCount; i++)
                    {
                        value = data[(long)index];

                        if (checkBox8.Checked)
                        {
                            if (i < chart.Series[dataSeries].Points.Count)
                            {
                                graphPoint1 = chart.Series[dataSeries].Points.ElementAt(i);

                                graphPoint1.SetValueXY(i, value);

                                graphPoint1.AxisLabel = Utilities.GetFrequencyString(binFrequency);
                            }
                            else
                            {
                                graphPoint1 = new System.Windows.Forms.DataVisualization.Charting.DataPoint(i, value);
                                graphPoint1.AxisLabel = Utilities.GetFrequencyString(binFrequency);

                                chart.Series[dataSeries].Points.Add(graphPoint1);
                            }

                            graphPoint1.Label = "";
                        }

                        if (interestingSignals != null && dataSeries == "Strength Difference")
                        {
                            for (int j = (int)index; j < (int)(index + inc); j++)
                            {
                                interestingSignalIndex = interestingSignals.FindIndex(x => x.index == j);

                                if (interestingSignalIndex >= 0 && interestingSignalIndex < 4)
                                {
                                    interestingSignal = interestingSignals[interestingSignalIndex];

                                    if (checkBox8.Checked)
                                    {
                                        if (graphPoint1 != null)
                                        {
                                            graphPoint1.Label = Utilities.GetFrequencyString(binFrequency);

                                            graphPoint1.LabelForeColor = Waterfall.colors[(int)((float)interestingSignalIndex / 4 * (Waterfall.colors.Count - 1))];
                                        }
                                    }

                                    break;
                                }
                            }
                        }


                        if (i > 0)
                        {
                            if (value < minY)
                            {
                                minY = value;

                                minYIndex = i;
                            }

                            if (value > maxY && index > 0)
                            {
                                maxY = value;

                                maxYIndex = i;
                            }
                        }


                        index += inc;

                        binFrequency += graphBinFreqInc;
                    }


                    double avgStrength = 0;
                    int valueCount = 0;

                    for (long i = lowerIndex + 1; i < upperIndex; i++)
                    {
                        value = data[i];

                        if (Double.IsNaN(value))
                        {
                            value = 0;
                        }
                        else
                        {
                            avgStrength += value;
                            valueCount++;
                        }
                    }

                    avgStrength /= valueCount;

                    ////chart.Refresh();

                    chart.ChartAreas[0].AxisX.ScaleView.Zoom(1, lowerResGraphBinCount - 1);

                    if (chart == chart1)
                    {
                        if (dataSeries == "Far Series")
                        {
                            series1MinYChart1 = minY;
                            series1MaxYChart1 = maxY;
                        }

                        if (dataSeries == "Near Series")
                        {
                            series2MinYChart1 = minY;
                            series2MaxYChart1 = maxY;
                        }

                        minY = Math.Min(series1MinYChart1, series2MinYChart1);
                        maxY = Math.Max(series1MaxYChart1, series2MaxYChart1);
                    }
                    else
                    {
                        if (dataSeries == "Far Series")
                        {
                            series1MinYChart2 = minY;
                            series1MaxYChart2 = maxY;
                        }

                        if (dataSeries == "Near Series")
                        {
                            series2MinYChart2 = minY;
                            series2MaxYChart2 = maxY;
                        }

                        minY = Math.Min(series1MinYChart2, series2MinYChart2);
                        maxY = Math.Max(series1MaxYChart2, series2MaxYChart2);
                    }

                    if (minY == maxY)
                        maxY = minY + 0.01;

                    if (checkBox8.Checked)
                    {
                        ////if (checkBox3.Checked || minY < chart.ChartAreas[0].AxisY.Minimum)
                        if (checkBox3.Checked)
                            chart.ChartAreas[0].AxisY.Minimum = Math.Round(minY, 2);

                        ////if (checkBox3.Checked || maxY > chart.ChartAreas[0].AxisY.Maximum)
                        if (checkBox3.Checked)
                            chart.ChartAreas[0].AxisY.Maximum = Math.Round(maxY, 2);

                        ////chart.ChartAreas[0].AxisY.Maximum = chart.ChartAreas[0].AxisY.Minimum * 2;

                        chart.ChartAreas[0].AxisY.Maximum += 0.01;
                    }


                    if ((dataUsedForTimeBasedGraph == TimeBasedGraphData.AverageGraph && chart == chart2) || (dataUsedForTimeBasedGraph == TimeBasedGraphData.CurrentGraph && chart == chart1))
                    {
                        if (dataSeries == "Far Series")
                        {
                            if (checkBox7.Checked)
                                textBox7.Text = Math.Round(avgStrength, 10).ToString();
                            else
                                textBox7.Text = Math.Round(avgStrength, 3).ToString();
                        }

                        if (dataSeries == "Near Series")
                        {
                            if (checkBox7.Checked)
                                textBox8.Text = Math.Round(avgStrength, 10).ToString();
                            else
                                textBox8.Text = Math.Round(avgStrength, 3).ToString();
                        }
                    }

                    return avgStrength;
                }
            }
            catch (Exception ex)
            {

            }

            return 0;
        }

        private void AxisViewChanged(System.Windows.Forms.DataVisualization.Charting.Chart chart, string dataSeries, float[] data, ref double lowerFrequency, ref double upperFrequency, ref double graphBinFreqInc)
        {
            if (data.Length > 0)
            {
                double min;
                double max;

                if (newData)
                {
                    min = 0;
                    max = totalBinCount;
                }
                else
                {
                    min = chart.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    max = chart.ChartAreas[0].AxisX.ScaleView.ViewMaximum + 1;

                    min--;

                    if (min < 0)
                        min = 0;

                    if (max == 1)
                        max = chart.Series[dataSeries].Points.Count;

                    if (max <= 0 || max > totalBinCount)
                        max = totalBinCount;
                }

                upperFrequency = lowerFrequency + max * graphBinFreqInc;
                lowerFrequency = lowerFrequency + min * graphBinFreqInc;

                long lowerIndex;
                long upperIndex;

                long lowerIndex2;
                long upperIndex2;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);
                if (checkBox11.Checked)
                {
                    lowerIndex = (long)((currentBufferFramesObject.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
                    upperIndex = (long)((currentBufferFramesObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

                    Command mostRecentCommand = commandBuffer.GetMostRecentCommand();

                    ////if (currentBufferFramesObject != zoomedOutBufferObject)
                    if (mostRecentCommand != null && mostRecentCommand.name == "ZoomToFrequency")
                    {
                        lowerIndex2 = (long)((lowerFrequency - currentBufferFramesObject.lowerFrequency) / binSize);
                        upperIndex2 = (long)((upperFrequency - currentBufferFramesObject.lowerFrequency) / binSize);

                        ////long lowerIndex2 = (long)((lowerFrequency - dataLowerFrequency) / binSize);
                        ////long upperIndex2 = (long)((upperFrequency - dataLowerFrequency) / binSize);

                        ////RangeChanged(chart, dataSeries, data, lowerIndex, upperIndex, lowerFrequency, ref graphBinFreqInc);

                        RangeChanged(chart, dataSeries, data, lowerIndex + lowerIndex2, lowerIndex + upperIndex2, lowerFrequency, ref graphBinFreqInc);
                        ////RangeChanged(chart, dataSeries, data, lowerIndex2, upperIndex2, lowerFrequency, ref graphBinFreqInc);
                    }
                    else if (mostRecentCommand != null && mostRecentCommand.name == "ZoomOutOfFrequency")
                    {
                        lowerIndex = (long)((currentBufferFramesObject.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
                        upperIndex = (long)((currentBufferFramesObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

                        RangeChanged(chart, dataSeries, data, lowerIndex, upperIndex, lowerFrequency, ref graphBinFreqInc);                        
                    }
                    else
                    {
                        lowerIndex = (long)((lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
                        upperIndex = (long)((upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

                        RangeChanged(chart, dataSeries, data, lowerIndex, upperIndex, lowerFrequency, ref graphBinFreqInc);
                    }
                        
                }
                else
                {
                    lowerIndex = (long)((lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
                    upperIndex = (long)((upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

                    RangeChanged(chart, dataSeries, data, lowerIndex, upperIndex, lowerFrequency, ref graphBinFreqInc);
                }

                if ((dataSeries == "Far Series" || dataSeries == "Near Series") && (recordingSeries1 || recordingSeries2))
                {
                    if (chart == chart1)
                    {
                        if (checkBox8.Checked)
                        {
                            if (recordingSeries2 && waterFall.GetMode() == WaterFallMode.Strength)
                            {
                                waterFall.RefreshWaterfall(series2BinData.binArray, series1BinData.binArray, lowerIndex + 1, upperIndex);

                                waterFall.CalculateRanges(series2BinData.binArray, series1BinData.binArray);
                            }
                            else
                            {
                                waterFall.RefreshWaterfall(series1BinData.binArray, series2BinData.binArray, lowerIndex + 1, upperIndex);

                                waterFall.CalculateRanges(series1BinData.binArray, series2BinData.binArray);
                            }
                        }
                    }
                    else
                    {
                        if (chart == chart2)
                        {
                            if (recordingSeries2 && waterFallAvg.GetMode() == WaterFallMode.Strength)
                            {
                                if (checkBox8.Checked)
                                {
                                    waterFallAvg.RefreshWaterfall(series2BinData.avgBinArray, series1BinData.avgBinArray, lowerIndex + 1, upperIndex);

                                    waterFallAvg.CalculateRanges(series2BinData.avgBinArray, series1BinData.avgBinArray);
                                }
                            }
                            else
                            {
                                double nearStrengthDeltaRange = 0;

                                if (interestingSignals != null && interestingSignals.Count > 0)
                                {
                                    int i = 0;

                                    while (i < interestingSignals.Count && interestingSignals[i].strengthDif > 0)
                                        nearStrengthDeltaRange = interestingSignals[i++].strengthDif;
                                }

                                if (checkBox8.Checked)
                                {
                                    if (nearStrengthDeltaRange > 0)
                                        waterFallAvg.SetNearStrengthDeltaRange(nearStrengthDeltaRange);
                                    else
                                        waterFallAvg.CalculateRanges(series1BinData.avgBinArray, series2BinData.avgBinArray);

                                    waterFallAvg.RefreshWaterfall(series1BinData.avgBinArray, series2BinData.avgBinArray, lowerIndex + 1, upperIndex);
                                }
                            }

                            if (waterFallAvg.GetMode() == WaterFallMode.Difference && waterFallAvg.GetRangeMode() == WaterFallRangeMode.Auto)
                                textBox10.Text = Math.Round(waterFallAvg.GetNearStrengthDeltaRange(), 2).ToString();
                            else
                                if (waterFallAvg.GetMode() == WaterFallMode.Strength && waterFallAvg.GetRangeMode() == WaterFallRangeMode.Auto)
                            {
                                textBox9.Text = Math.Round(waterFallAvg.GetStrengthMinimum(), 2).ToString();
                                textBox10.Text = Math.Round(waterFallAvg.GetStrengthMaximum(), 2).ToString();
                            }
                        }
                    }
                }
            }
        }

        private void chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            commandBuffer.AddCommand("chart1_AxisViewChanged");

            Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(graph1LowerFrequency, graph1UpperFrequency);
            graph1FrequencyRanges.Push(frequencyRange);

            if (series1BinData != null)
            {
                AxisViewChanged(chart1, series1BinData.dataSeries, series1BinData.binArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
            }

            if (series2BinData != null)
            {
                AxisViewChanged(chart1, series2BinData.dataSeries, series2BinData.binArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (graph1FrequencyRanges.Count > 0)
            {
                Utilities.FrequencyRange frequencyRange = graph1FrequencyRanges.Pop();

                long lowerIndex = (long)((frequencyRange.lower - dataLowerFrequency) / binSize);
                long upperIndex = (long)((frequencyRange.upper - dataLowerFrequency) / binSize);

                graph1LowerFrequency = frequencyRange.lower;
                graph1UpperFrequency = frequencyRange.upper;

                if (series1BinData != null)
                    RangeChanged(chart1, series1BinData.dataSeries, series1BinData.binArray, lowerIndex, upperIndex, graph1LowerFrequency, ref graph1BinFreqInc);

                if (series2BinData != null)
                    RangeChanged(chart1, series2BinData.dataSeries, series2BinData.binArray, lowerIndex, upperIndex, graph1LowerFrequency, ref graph1BinFreqInc);

                if (series1BinData != null && series2BinData != null)
                    GraphDifference(series1BinData, series2BinData);
            }
        }

        private void chart2_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            commandBuffer.AddCommand("chart2_AxisViewChanged");

            Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(graph2LowerFrequency, graph2UpperFrequency);
            graph2FrequencyRanges.Push(frequencyRange);

            if (series1BinData != null)
            {
                AxisViewChanged(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
            }

            if (series2BinData != null)
            {
                AxisViewChanged(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
            }

            if (series1BinData != null && series2BinData != null)
                GraphDifference(series1BinData, series2BinData);


            /*////
            analyzingNearFarTransitions = true;

            textBox1.Text = Math.Round(graph2LowerFrequency).ToString();

            textBox2.Text = Math.Round(graph2UpperFrequency).ToString();


            if (button3.Text == "Stop Recording")
                button3.PerformClick();
            else
                if (button5.Text == "Stop Recording")
                button5.PerformClick();

            eventTimer = new System.Windows.Forms.Timer();

            eventTimer.Tick += new EventHandler(NewSettingsThread);
            eventTimer.Interval = 1000;

            eventTimer.Start();
            */

        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            if (graph2FrequencyRanges.Count > 0)
            {
                Utilities.FrequencyRange frequencyRange = graph2FrequencyRanges.Pop();

                long lowerIndex;
                long upperIndex;

                /*////if (analyzingNearFarTransitions)
                {
                    dataLowerFrequency = (uint)frequencyRange.lower;
                    dataUpperFrequency = (uint)frequencyRange.upper;

                    textBox1.Text = dataLowerFrequency.ToString();
                    textBox2.Text = dataUpperFrequency.ToString();
                }*/

                lowerIndex = (long)((frequencyRange.lower - dataLowerFrequency) / binSize);
                upperIndex = (long)((frequencyRange.upper - dataLowerFrequency) / binSize);

                graph2LowerFrequency = frequencyRange.lower;
                graph2UpperFrequency = frequencyRange.upper;

                if (series1BinData != null)
                    RangeChanged(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, lowerIndex, upperIndex, graph2LowerFrequency, ref graph2BinFreqInc);

                if (series2BinData != null)
                    RangeChanged(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, lowerIndex, upperIndex, graph2LowerFrequency, ref graph2BinFreqInc);

                if (series1BinData != null && series2BinData != null)
                    GraphDifference(series1BinData, series2BinData);

                /*////if (analyzingNearFarTransitions)
                {
                    eventTimer = new System.Windows.Forms.Timer();

                    eventTimer.Tick += new EventHandler(NewSettingsThread);
                    eventTimer.Interval = 1000;

                    eventTimer.Start();
                }*/
            }
        }

        private void ScaleData(ref float[] binArray, double averageTotalFramesStrength1, double averageTotalFramesStrength2)
        {
            float ratio = (float)(averageTotalFramesStrength2 / averageTotalFramesStrength1);

            for (int j = 0; j < binArray.Length; j++)
            {
                binArray[j] *= ratio;
            }
        }

        private void RecordData(ref BinData binData, ref double averageCurrentFrameStrength, ref double averageTotalFramesStrength, ref int totalMagnitude, ref double avgMagnitude, int deviceIndex)
        {
            if (binData.binArray.Length == 0)
                binData = new BinData(totalBinCount, binData.dataSeries, binData.mode);

            averageCurrentFrameStrength = 0;

            averageTotalFramesStrength = 0;


            long currentTime = Environment.TickCount;

            long lowerIndex;
            long upperIndex;

            int i = 0;

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            lowerIndex = (long)((currentBufferFramesObject.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
            upperIndex = (long)((currentBufferFramesObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

            if ((currentBufferFramesObject.bufferFrames.bufferFramesArray.Count > BufferFrames.MIN_BUFFER_SIZE && currentTime - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.startBufferIndex].time > BufferFrames.BUFFER_TIME_LENGTH) || currentBufferFramesObject.bufferFrames.bufferFilled)
            ////if ((currentBufferFramesObject.bufferFrames.bufferFramesArray.Count > BufferFrames.MIN_BUFFER_SIZE && currentTime - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.startBufferIndex].time > BufferFrames.BUFFER_TIME_LENGTH))
            {
                currentBufferFramesObject.bufferFrames.bufferFilled = true;

                currentBufferFramesObject.bufferFrames.currentBufferIndex = currentBufferFramesObject.bufferFrames.startBufferIndex;


                BinData targetBinData = null;

                if (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode == BinDataMode.Far)
                {
                    targetBinData = series1BinData;

                    this.Invoke(new Action(() =>
                    {
                        listBox3.Items.Add("series1BinData totalBinArrayNumberOfFrames[]++\n");
                    }));                    
                }
                else
                    if (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode == BinDataMode.Near)
                {
                    targetBinData = series2BinData;

                    this.Invoke(new Action(() =>
                    {
                        listBox3.Items.Add("series2BinData totalBinArrayNumberOfFrames[]++\n");
                    }));
                    
                }
                else
                {
                    ////targetBinData = null;
                }

                if (targetBinData != null)
                {
                    i = 0;

                    for (long j = lowerIndex; j < upperIndex; j++)
                    {
                        targetBinData.totalBinArray[j] += currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i];

                        targetBinData.totalBinArrayNumberOfFrames[j]++;

                        i++;
                    }

                    ////targetBinData.numberOfFrames++;
                    targetBinData.bufferFrames--;

                    if (checkBox11.Checked && recordingSeries1 && !analyzingNearFarTransitions && series1BinData.GetAverageNumberOfFrames() >= 4 && series2BinData.GetAverageNumberOfFrames() >= 4)
                    {
                        InitializeZoomToFrequencyThread();
                    }
                }

                if (currentBufferFramesObject.bufferFrames.startBufferIndex + 1 >= currentBufferFramesObject.bufferFrames.bufferFramesArray.Count)
                    currentBufferFramesObject.bufferFrames.startBufferIndex = 0;
                else
                    currentBufferFramesObject.bufferFrames.startBufferIndex++;
            }
            else
                currentBufferFramesObject.bufferFrames.currentBufferIndex++;

            if (currentBufferFramesObject.bufferFrames.currentBufferIndex >= currentBufferFramesObject.bufferFrames.bufferFramesArray.Count)
            {
                currentBufferFramesObject.bufferFrames.bufferFramesArray.Add(new BufferFrame(totalBinCount, currentMode));
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    listBox3.Items.Add("buffer mode modified from: " + currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode + " to: " + currentMode + "\n");
                }));                

                currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode = currentMode;                
            }


            currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].time = currentTime;



            if (!checkBox7.Checked)
            {
                uint scanCount = 1;

                ////NativeMethods.GetBins(binData.binArray, deviceIndex, rangeSamplingPercentage, PROXIMITRY_DETECTOR, scanCount);                

                NativeMethods.GetBins(currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray, deviceIndex, rangeSamplingPercentage, PROXIMITRY_DETECTOR, scanCount);

                /*////this.Invoke(new Action(() =>
                {
                    currentBufferFramesObject.bufferFrames.GraphMostRecentAvgStrength(chart5);
                }));*/

                i = 0;

                for (long j = lowerIndex; j < upperIndex; j++)
                {
                    binData.binArray[j] = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i];

                    i++;
                }

                proximitryFrequency.sampleCount++;
            }
            else
            {
                NativeMethods.GetBinsForDevices(binData.device1BinArray, binData.device2BinArray, 0, 1);

                for (int j = 0; j < binData.size; j++)
                {
                    binData.binArray[j] = binData.device1BinArray[j] / binData.device2BinArray[j];
                }
            }

            /*////
            for (int j = 0; j < binData.size; j++)
            {
                value = binData.binArray[j];

                if (Double.IsNaN(value) || value < 0)
                {

                    if (j > 0)
                        value = binData.binArray[j - 1];
                   else
                        if (j < binData.size - 1)
                        value = binData.binArray[j + 1];


                    binData.binArray[j] = (float)value;
                }

                if (Double.IsNaN(value) || value < 0)
                {
                    value = -25;
                    binData.binArray[j] = (float)value;
                }

                averageCurrentFrameStrength += value;
            }

            binData.numberOfFrames++;

            if (recordingIntoBuffer)
                bufferFrames++;      
            */




            if (totalBinBufferArray == null || totalBinBufferArray.Length != totalBinCount)
                totalBinBufferArray = new float[totalBinCount];
            else
            {
                for (int j = 0; j < totalBinBufferArray.Length; j++)
                {
                    totalBinBufferArray[j] = 0;
                }
            }

            binData.bufferFrames = currentBufferFramesObject.bufferFrames.AddBufferRangeIntoArray(totalBinBufferArray, binData.mode);
            binData.bufferFrames += currentBufferFramesObject.bufferFrames.AddBufferRangeIntoArray(totalBinBufferArray, BinDataMode.Indeterminate);

            i = 0;

            for (long j = lowerIndex; j < upperIndex; j++)
            {
                binData.avgBinArray[j] = (binData.totalBinArray[j] + totalBinBufferArray[i]) / (binData.totalBinArrayNumberOfFrames[i] + binData.bufferFrames);

                averageTotalFramesStrength += binData.avgBinArray[j];

                i++;
            }



            averageCurrentFrameStrength /= binData.size;
            averageTotalFramesStrength /= binData.size;

            ////if (binData.numberOfFrames % 100 == 0)
            if (binData.GetAverageNumberOfFrames() % 100 == 0)
            {
                minAvgStrength = 99999999;
                maxAvgStrength = -99999999;
            }

            if (averageTotalFramesStrength > maxAvgStrength)
                maxAvgStrength = averageTotalFramesStrength;

            if (averageTotalFramesStrength < minAvgStrength)
                minAvgStrength = averageTotalFramesStrength;

            if (resetGraph)
                newData = true;
        }

        private void GraphData(BinData binData)
        {
            ////if (binData.numberOfFrames + binData.bufferFrames > 0)
            if (binData.GetAverageNumberOfFrames() + binData.bufferFrames > 0)
            {
                try
                {
                    ////AxisViewChanged(chart1, binData.dataSeries, currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
                    AxisViewChanged(chart1, binData.dataSeries, binData.binArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
                    chart1.Refresh();
                }
                catch
                {
                }
            }

            if (binData.GetAverageNumberOfFrames() + binData.bufferFrames > 0)
            {
                try
                {
                    AxisViewChanged(chart2, binData.dataSeries, binData.avgBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
                    chart2.Refresh();
                }
                catch
                {
                }

            /*////double[] transitionStrengthOverTime = currentBufferFramesObject.transitionBufferFrames.GetStrengthOverTimeForRange((long)graph2LowerFrequency, (long)graph2UpperFrequency);
            currentBufferFramesObject.transitionBufferFrames.GraphData(chart5, transitionStrengthOverTime);
            */
        }

            if (resetGraph && newData)
            {
                difBinArray = new float[totalBinCount];

                resetGraph = false;
                newData = false;
            }
        }    

        private void GraphDifference(BinData series1BinData, BinData series2BinData)
        {
            ////if (checkBox10.Checked)
            ////{
                if (series1BinData != null && series2BinData != null && series1BinData.GetAverageNumberOfFrames() > 0 && series2BinData.GetAverageNumberOfFrames() > 0 && series1BinData.size == series2BinData.size)
                {
                    if (radioButton2.Checked)
                        chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                    else
                        chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    double dif = 0, prevDif, absDif;

                    int i = 0;                   

                    interestingSignals.Clear();
                    leaderBoardSignals.Clear();

                    uint frequency;

                    series2Max = -99999999;

                    for (i = 0; i < totalBinCount; i++)
                    {
                        if (series2BinData.avgBinArray[i] > series2Max)
                            series2Max = series2BinData.avgBinArray[i];
                    }


                    for (i = 0; i < totalBinCount; i++)
                    {
                        prevDif = dif;
                        
                        dif = Waterfall.CalculateStrengthDifference(series1BinData.avgBinArray, series2BinData.avgBinArray, i);

                        absDif = dif;

                        frequency = (uint)(dataLowerFrequency + (i * binSize));
                        
                        if (!checkBox1.Checked || absDif >= difThreshold)
                            difBinArray[i] = (float)chart2.ChartAreas[0].AxisY.Minimum + (float)absDif;
                        else
                            difBinArray[i] = -99999999;

                        if (dif != prevDif)
                        {
                            interestingSignals.Add(new InterestingSignal(i, series2BinData.avgBinArray[i], absDif, frequency));

                            interestingSignals[interestingSignals.Count - 1].maxAvgStrength = series2BinData.avgBinArray[i];
                            interestingSignals[interestingSignals.Count - 1].minAvgStrength = series1BinData.avgBinArray[i];

                            if (dif < 0)
                                interestingSignals[interestingSignals.Count - 1].invertedDif = true;                            
                        }
                    }

                    interestingSignals.Sort(delegate (InterestingSignal x, InterestingSignal y)
                    {
                        if (x.strengthDif < y.strengthDif)
                            return 1;
                        else if (x.strengthDif == y.strengthDif)
                            return 0;
                        else
                            return -1;
                    });

                    for (int j = interestingSignals.Count - 1; j >= MAX_INTERESTING_SIGNAL_LIST_COUNT; j--)
                    {
                        interestingSignals.RemoveAt(j);
                    }

                    listBox1.Items.Clear();

                    string frequencyString;

                    long prevFrequency=-1;

                for (i = 0; i < interestingSignals.Count; i++)
                {
                    if (leaderBoardSignals.Count < MAX_LEADER_BOARD_LIST_COUNT)
                    {
                        if (Math.Abs(interestingSignals[i].frequency - prevFrequency) > 100000 || prevFrequency == -1)
                        {
                            frequencyString = Utilities.GetFrequencyString(interestingSignals[i].frequency);

                            leaderBoardSignals.Add(interestingSignals[i]);
                            listBox1.Items.Add(interestingSignals[i].frequency + ": " + Math.Round(interestingSignals[i].strengthDif));

                            prevFrequency = (long) interestingSignals[i].frequency;
                        }
                    }
                }

                    if (checkBox8.Checked && checkBox10.Checked)
                        AxisViewChanged(chart2, "Strength Difference", difBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
                    else
                        chart2.Series["Strength Difference"].Points.Clear();

                    chart2.Refresh();
                }
            /*////}
            else
                chart2.Series["Strength Difference"].Points.Clear();
                */
        }

        private void AddGradientPoint(System.Windows.Forms.DataVisualization.Charting.Chart chart, TextBox textBox, double gradientValue)
        {
            try
            {
                textBox.Text = gradientValue.ToString();

                System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(chart4.Series["Far Series"].Points.Count, gradientValue);

                if (chart.Series["Far Series"].Points.Count > MAXIMUM_TIME_BASED_GRAPH_POINTS)
                {
                    chart.Series["Far Series"].Points.RemoveAt(0);

                    for (int j = 0; j < chart.Series["Far Series"].Points.Count; j++)
                    {
                        chart.Series["Far Series"].Points[j].XValue--;
                    }
                }

                if (checkBox8.Checked)
                    chart.Series["Far Series"].Points.Add(graphPoint);

                if (chart == chart5)
                {
                    chart.ChartAreas[0].AxisY.Maximum = 4;
                    chart.ChartAreas[0].AxisY.Minimum = 0;
                }

                double totalAvg = 0;
                double avg = 0;


                double minY = 99999999;
                double maxY = -99999999;

                for (int j = 0; j < chart.Series["Far Series"].Points.Count; j++)
                {
                    if (chart.Series["Far Series"].Points[j].YValues[0] < minY)
                        minY = chart.Series["Far Series"].Points[j].YValues[0];

                    if (chart.Series["Far Series"].Points[j].YValues[0] > maxY)
                        maxY = chart.Series["Far Series"].Points[j].YValues[0];

                    totalAvg += chart.Series["Far Series"].Points[j].YValues[0];
                }

                if (minY == maxY)
                {
                    maxY++;
                    minY--;
                }

                if (chart.Series["Far Series"].Points.Count > 0)
                {
                    chart.ChartAreas[0].AxisY.Maximum = maxY;
                    chart.ChartAreas[0].AxisY.Minimum = minY;
                }

                avg = totalAvg / chart.Series["Far Series"].Points.Count;


                if (checkBox6.Checked && chart == chart4)
                {
                    double avgGraphStrengthChange = chart4.Series["Far Series"].Points[chart4.Series["Far Series"].Points.Count - 1].YValues[0] - chart4.Series["Far Series"].Points[chart4.Series["Far Series"].Points.Count - 2].YValues[0];

                    double strengthVSAvg = chart4.Series["Far Series"].Points[chart4.Series["Far Series"].Points.Count - 1].YValues[0] - avg;

                    double graphExtent = chart4.ChartAreas[0].AxisY.Maximum - chart4.ChartAreas[0].AxisY.Minimum;

                    int soundFrequency = (int)((strengthVSAvg / (graphExtent * 500) * 100) * Sound.SOUND_FREQUENCY_MAXIMUM);

                    if (soundFrequency > 0)
                    {
                        Sound.PlaySound(soundFrequency, 1000);
                        form2.BackColor = Color.Red;
                    }
                    else
                        form2.BackColor = Color.Blue;
                }
            }
            catch (Exception ex)
            {

            }
        }



        private void AddPointToTimeBasedGraph(float value)
        {
            System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(chart3.Series["Far Series"].Points.Count, value);
            
            double minY = 999999999999;
            double maxY = -999999999999;

            double totalAvg = 0;
            double avg = 0;

            int minMaxStart = 0;


            for (int j = minMaxStart; j < chart3.Series["Far Series"].Points.Count; j++)
            {
                if (chart3.Series["Far Series"].Points[j].YValues[0] < minY)
                    minY = chart3.Series["Far Series"].Points[j].YValues[0];

                if (chart3.Series["Far Series"].Points[j].YValues[0] > maxY)
                    maxY = chart3.Series["Far Series"].Points[j].YValues[0];

                totalAvg += chart3.Series["Far Series"].Points[j].YValues[0];
            }

            if (minY == maxY)
            {
                maxY++;
                minY--;
            }

            if (chart3.Series["Far Series"].Points.Count > 0)
            {
                chart3.ChartAreas[0].AxisY.Maximum = maxY;
                chart3.ChartAreas[0].AxisY.Minimum = minY;
            }

            avg = totalAvg / chart3.Series["Far Series"].Points.Count;            


            if (chart3.Series["Far Series"].Points.Count > MAXIMUM_TIME_BASED_GRAPH_POINTS)
            {
                chart3.Series["Far Series"].Points.RemoveAt(0);

                for (int j = 0; j < chart3.Series["Far Series"].Points.Count; j++)
                {
                    chart3.Series["Far Series"].Points[j].XValue--;
                }
            }

            if (checkBox8.Checked)
                chart3.Series["Far Series"].Points.Add(graphPoint);



            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint1;
            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint2;
            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint3;

            double x1, y1, x2, y2, l1, l2, dotProduct, angle, trajAngle, totalTrajAngle = 0, avgTrajAngle;

            for (int j = 1; j < chart3.Series["Far Series"].Points.Count - 1; j++)
            {
                prevPoint1 = chart3.Series["Far Series"].Points[j - 1];
                prevPoint2 = chart3.Series["Far Series"].Points[j];
                prevPoint3 = chart3.Series["Far Series"].Points[j + 1];



                x1 = prevPoint1.XValue - prevPoint2.XValue;
                y1 = prevPoint1.YValues[0] - prevPoint2.YValues[0];


                x2 = prevPoint3.XValue - prevPoint2.XValue;
                y2 = prevPoint3.YValues[0] - prevPoint2.YValues[0];


                l1 = Math.Sqrt(x1 * x1 + y1 * y1);

                l2 = Math.Sqrt(x2 * x2 + y2 * y2);


                dotProduct = x1 * x2 + y1 * y2;


                angle = Math.Acos(dotProduct / (l1 * l2));

                if (Double.IsNaN(angle))
                    trajAngle = 0;
                else
                    trajAngle = Math.PI - angle;

                totalTrajAngle += trajAngle;

                if (Double.IsNaN(trajAngle))
                    break;
            }

            avgTrajAngle = totalTrajAngle / chart3.Series["Far Series"].Points.Count;

            /*////if (!Double.IsNaN(avgTrajAngle))
                AddGradientPoint(chart5, textBox13, avgTrajAngle);
                */


            if (checkBox5.Checked)
            {
                double avgGraphStrengthChange = chart3.Series["Far Series"].Points[chart3.Series["Far Series"].Points.Count - 1].YValues[0] - chart3.Series["Far Series"].Points[chart3.Series["Far Series"].Points.Count - 2].YValues[0];

                double strengthVSAvg = chart3.Series["Far Series"].Points[chart3.Series["Far Series"].Points.Count - 1].YValues[0] - avg;

                double graphExtent = chart3.ChartAreas[0].AxisY.Maximum - chart3.ChartAreas[0].AxisY.Minimum;

                int soundFrequency = (int)(strengthVSAvg / (graphExtent * 10) * Sound.SOUND_FREQUENCY_MAXIMUM);

                if (soundFrequency > 0)
                {
                    Sound.PlaySound(soundFrequency, 1000);
                    form2.BackColor = Color.Red;
                }
                else
                    form2.BackColor = Color.Blue;
            }

            
            if (chart3.Series["Far Series"].Points.Count > 1)
            {
                double gradient = 0;
                double totalGradient = 0;
                double avgGradient = 0;

                for (int j = 1; j < chart3.Series["Far Series"].Points.Count; j++)
                {
                    totalGradient += (chart3.Series["Far Series"].Points[j].YValues[0] - chart3.Series["Far Series"].Points[j - 1].YValues[0]);
                }

                avgGradient = totalGradient / (chart3.Series["Far Series"].Points.Count - 1);

                if (chart3.Series["Far Series"].Points.Count > 1)
                {
                    gradient = chart3.Series["Far Series"].Points[chart3.Series["Far Series"].Points.Count - 1].YValues[0] - chart3.Series["Far Series"].Points[chart3.Series["Far Series"].Points.Count - 2].YValues[0];
                }

                AddGradientPoint(chart4, textBox12, avgGradient);
            }            
        }

        private void GraphStrengthToTimeBasedGraph(BinData binData)
        {
            if (binData != null)
            {
                float averageStrength = 0;

                for (int i = 0; i < binData.binArray.Length; i++)
                {
                    averageStrength += binData.binArray[i];
                }

                averageStrength /= binData.binArray.Length;

                if (binData.dataSeries == "Far Series")
                    textBox7.Text = averageStrength.ToString();

                if (binData.dataSeries == "Near Series")
                    textBox8.Text = averageStrength.ToString();
            }
            else
            {
                if (recordingSeries1)
                    AddPointToTimeBasedGraph(float.Parse(textBox7.Text));

                if (recordingSeries2)
                    AddPointToTimeBasedGraph(float.Parse(textBox8.Text));
            }
        }

        private void GraphAverageStrength(BinData binData)
        {
            if (binData != null)
            {
                float averageStrength = 0;

                for (int i = 0; i < binData.avgBinArray.Length; i++)
                {
                    averageStrength += binData.avgBinArray[i];
                }

                averageStrength /= binData.avgBinArray.Length;

                if (binData.dataSeries == "Far Series")
                    textBox7.Text = averageStrength.ToString();

                if (binData.dataSeries == "Near Series")
                    textBox8.Text = averageStrength.ToString();
            }
            else
            {
                if (recordingSeries1)
                    AddPointToTimeBasedGraph(float.Parse(textBox7.Text));

                if (recordingSeries2)
                    AddPointToTimeBasedGraph(float.Parse(textBox8.Text));
            }
        }

        private void GraphMagnitude(double magnitude)
        {            
            magnitudeBuffer[magnitudeBufferCount++] = magnitude;

            double avg;

            ////if (magnitudeBufferCount == 10)
            {
                avg = 0;

                for (int i = 0; i < magnitudeBufferCount; i++)
                    avg += magnitudeBuffer[i];

                avg /= 10;

                magnitudeBufferCount = 0;

                AddPointToTimeBasedGraph((float)avg);
            }
        }


        private void GraphProximitryValue()
        {
            if (proximitryFrequency.totalADCMagnitude > 0 && proximitryFrequency.sampleCount >= 10)
            {
                proximitryFrequency.avgTotalADCMagnitude = proximitryFrequency.totalADCMagnitude / proximitryFrequency.sampleCount;

                proximitryFrequency.totalADCMagnitude = 0;
                proximitryFrequency.sampleCount = 0;

                textBox14.Text = proximitryFrequency.avgTotalADCMagnitude.ToString();

                bool invertedDif = proximitryFrequency.maxStrength < proximitryFrequency.minStrength;

                double graphValue;

                if (!invertedDif)
                    graphValue = proximitryFrequency.avgTotalADCMagnitude;
                else
                    graphValue = proximitryFrequency.minStrength - proximitryFrequency.avgTotalADCMagnitude;

                if (chart6.Series["Current Value"].Points.Count > 0)
                {
                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = chart6.Series["Current Value"].Points.ElementAt(0);

                    graphPoint.SetValueXY(0, graphValue);

                    graphPoint.AxisLabel = Utilities.GetFrequencyString(proximitryFrequency.frequency);
                }
                else
                {
                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(0, graphValue);

                    graphPoint.AxisLabel = Utilities.GetFrequencyString(proximitryFrequency.frequency);

                    chart6.Series["Current Value"].Points.Add(graphPoint);
                }

                if (graphValue > chart6.ChartAreas[0].AxisY.Maximum)
                    chart6.ChartAreas[0].AxisY.Maximum = graphValue;

                if (graphValue < chart6.ChartAreas[0].AxisY.Minimum)
                    chart6.ChartAreas[0].AxisY.Minimum = graphValue;

                if (chart6.ChartAreas[0].AxisY.Maximum == chart6.ChartAreas[0].AxisY.Minimum)
                    chart6.ChartAreas[0].AxisY.Maximum = chart6.ChartAreas[0].AxisY.Minimum + 1;
            }
        }

        private void RecordSeries1()
        {            
            if (button3.Text == "Record Far Series Data")
            {
                while (button5.Text == "Stop Recording")
                {
                    Thread.Sleep(100);
                }

                framesDif = 0;

                recordingSeries2 = false;
                recordingSeries1 = true;

                startRecordingSeries1 = false;

                Command mostRecentCommand = commandBuffer.GetMostRecentCommand();

                ////if (!analyzingNearFarTransitions && !startRecordingSeries1 && !startRecordingSeries2)
                if (mostRecentCommand != null && mostRecentCommand.name == "ZoomToFrequency")
                {
                    BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);
                    zoomedOutBufferObject.bufferFrames.Flush(series1BinData, series2BinData, series1BinData);
                }

                if (series2BinData.GetAverageNumberOfFrames() > 0)
                    radioButton4.Enabled = true;

                if (checkBox9.Checked)
                {
                    notifyIcon1.BalloonTipTitle = "Recording Far";
                    notifyIcon1.BalloonTipText = "Move the mouse or press a key if you're near";

                    notifyIcon1.ShowBalloonTip(10000);
                }               

                Task.Factory.StartNew(() =>
                {                    
                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;

                    while (recordingSeries1)
                    {                        
                        currentMode = BinDataMode.Far;

                        /*////this.Invoke(new Action(() =>
                        {                            
                            currentBufferFramesObject.bufferFrames.GraphMostRecentAvgStrength(chart5);
                        }));
                        */

                        int totalMagnitude = 0;
                        double avgMagnitude = 0;

                        RecordData(ref series1BinData, ref averageSeries1CurrentFrameStrength, ref averageSeries1TotalFramesStrength, ref totalMagnitude, ref avgMagnitude, deviceCount - 1);


                        /*////if (averageSeries2CurrentFrameStrength > 0)
                            ScaleData(ref series1BinData.binArray, averageSeries1CurrentFrameStrength, averageSeries2CurrentFrameStrength);

                        if (averageSeries2TotalFramesStrength > 0)
                            ScaleData(ref series1BinData.avgBinArray, averageSeries1TotalFramesStrength, averageSeries2TotalFramesStrength);
                        */

                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (series1BinData.clearFrames)
                                {
                                    ClearSeries1();

                                    series1BinData.clearFrames = false;
                                }

                                if (series2BinData.clearFrames)
                                {
                                    ClearSeries2();

                                    series2BinData.clearFrames = false;
                                }

                                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                                ////textBox5.Text = (Math.Ceiling(series1BinData.GetAverageNumberOfFrames() + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far))).ToString();


                                long prevFarFrames = long.Parse(textBox5.Text);

                                long farFrames = (long) series1BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far);

                                textBox5.Text = (farFrames).ToString();


                                long prevTextBox6 = long.Parse(textBox6.Text);

                                prevAvgFramesForRegion= avgFramesForRegion;
                                prevNearFrames = nearFrames;
                                prevIndeterminateFrames = indeterminateFrames;

                                long prevFrames = (long)(prevAvgFramesForRegion + prevNearFrames + prevIndeterminateFrames);

                                avgFramesForRegion = series2BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);
                                nearFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near);
                                indeterminateFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);

                                long frames = (long) (avgFramesForRegion + nearFrames + indeterminateFrames);

                                textBox6.Text = frames.ToString();

                                if ((avgFramesForRegion+nearFrames+indeterminateFrames)!=(prevAvgFramesForRegion+prevNearFrames+prevIndeterminateFrames))
                                {
                                    framesDif++;
                                }

                                /*////if (textBox6.Text != "33")
                                {
                                    ///textBox6.Text = textBox6.Text;
                                }*/

                                GraphProximitryValue();

                                ////if (checkBox8.Checked)
                                {
                                    totalADCMagnitudeFar += avgMagnitude;
                                    double avgADCMagnitude = totalADCMagnitudeFar / series1BinData.GetAverageNumberOfFrames();

                                    GraphData(series1BinData);
                                    GraphDifference(series1BinData, series2BinData);

                                    ////////GraphMagnitude(avgADCMagnitude);

                                    ////GraphTotalMagnitude(totalMagnitude);
                                    ////GraphAvgMagnitude(avgMagnitude);

                                    GraphAverageStrength(null);
                                    ////GraphStrengthToTimeBasedGraph(null);
                                }
                            }));
                        }
                        catch (Exception ex)
                        {

                        }                        
                    }

                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            button4.Enabled = true;
                            button5.Enabled = true;
                            button3.Text = "Record Far Series Data";
                            button17.Text = "Record Far";
                            button18.Enabled = true;

                            this.Cursor = Cursors.Arrow;

                            if (recordingSeries2)
                                mainForm.RecordSeries2();
                            /*////else if (analyzingNearFarTransitions)
                            {
                                eventTimer = new System.Windows.Forms.Timer();

                                eventTimer.Tick += new EventHandler(NewSettingsThread);
                                eventTimer.Interval = 1000;

                                eventTimer.Start();
                            }*/

                            Command command = commandQueue.GetMostRecentCommand();

                            if (command != null && (command.name == "AutomatedZoomToFrequency" || command.name == "ZoomToFrequency"))
                            {
                                if (command.name == "AutomatedZoomToFrequency")
                                    startRecordingSeries1 = true;

                                NewSettings(false);

                                commandQueue.RemoveCommand();
                            }

                        }));
                    }
                    catch (Exception ex)
                    {

                    }

                });

                button4.Enabled = false;
                button5.Enabled = false;
                button18.Enabled = false;

                button3.Text = "Stop Recording";
                button17.Text = "Stop";                
            }
            else
            {
                this.Cursor = Cursors.WaitCursor;
                recordingSeries1 = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            RecordSeries1();
        }

        private void RecordSeries2()
        {            
            if (button5.Text == "Record Near Series Data")
            {
                while (button3.Text == "Stop Recording")
                {
                    Thread.Sleep(100);                    
                }

                recordingSeries1 = false;
                recordingSeries2 = true;

                startRecordingSeries2 = false;

                if (series1BinData.GetAverageNumberOfFrames() > 0)
                {
                    radioButton4.Enabled = true;
                    radioButton4.Checked = true;
                }

                Task.Factory.StartNew(() =>
                {
                    bool userNear = true;

                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;

                    BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                    Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);


                    while (recordingSeries2)
                    {
                        currentMode = BinDataMode.Near;

                        ////if (checkBox9.Checked)
                        {
                            if (currentBufferFramesObject.transitionBufferFrames.nearIndex > -1 && currentBufferFramesObject.bufferFrames.currentBufferIndex > -1)
                            {
                                if (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].time - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.transitionBufferFrames.nearIndex].time > BufferFrames.TRANSITION_LENGTH / 2)
                                {
                                    currentBufferFramesObject.transitionBufferFrames.farIndex = currentBufferFramesObject.transitionBufferFrames.nearIndex;

                                    do
                                    {
                                        currentBufferFramesObject.transitionBufferFrames.farIndex--;

                                        if (currentBufferFramesObject.transitionBufferFrames.farIndex < 0)
                                            currentBufferFramesObject.transitionBufferFrames.farIndex = currentBufferFramesObject.bufferFrames.bufferFramesArray.Count - 1;

                                        if (currentBufferFramesObject.transitionBufferFrames.farIndex == currentBufferFramesObject.transitionBufferFrames.nearIndex)
                                        {
                                            currentBufferFramesObject.transitionBufferFrames.farIndex = -1;
                                            break;
                                        }
                                    }
                                    while (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.transitionBufferFrames.nearIndex].time - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.transitionBufferFrames.farIndex].time < BufferFrames.TRANSITION_LENGTH / 2);


                                    if (currentBufferFramesObject.transitionBufferFrames.farIndex > -1)
                                    {
                                        int currentTransitionFrame = currentBufferFramesObject.transitionBufferFrames.farIndex;

                                        long startTransitionTime = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.transitionBufferFrames.farIndex].time;

                                        long index = 0;
                                        while (currentTransitionFrame != currentBufferFramesObject.bufferFrames.currentBufferIndex)
                                        {
                                            currentBufferFramesObject.transitionBufferFrames.AddTransitionBufferFrame(currentBufferFramesObject.bufferFrames.bufferFramesArray[currentTransitionFrame], currentBufferFramesObject.bufferFrames.bufferFramesArray[currentTransitionFrame].time - startTransitionTime, index);

                                            currentTransitionFrame++;

                                            if (currentTransitionFrame >= currentBufferFramesObject.bufferFrames.bufferFramesArray.Count)
                                                currentTransitionFrame = 0;

                                            index++;
                                        }

                                        if (index < currentBufferFramesObject.transitionBufferFrames.minFrameIndex)
                                            currentBufferFramesObject.transitionBufferFrames.minFrameIndex = index;


                                        currentBufferFramesObject.transitionBufferFrames.transitions++;

                                        Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count - 2);

                                        if (analyzingNearFarTransitions && (command == null || command.name != "UserSelectedFrequencyForAnalysis"))
                                        {
                                            recordingSeries2 = false;
                                        }

                                        this.Invoke(new Action(() =>
                                        {
                                            ////double[] transitionStrengthOverTime = currentBufferFramesObject.transitionBufferFrames.GetStrengthOverTimeForRange((long)graph2LowerFrequency, (long)graph2UpperFrequency);

                                            TransitionGradientArray transitionGradientArray = bufferFramesArray.GetStrongestTransitionsFrequencyGradientArray();


                                            if (transitionGradientArray.array.Count > 0)
                                            {
                                                listBox2.Items.Clear();

                                                for (int i = 0; i < transitionGradientArray.array.Count; i++)
                                                {
                                                    listBox2.Items.Add(transitionGradientArray.array[i].frequency + ": " + transitionGradientArray.array[i].strength + ": " + transitionGradientArray.array[i].transitions);
                                                }

                                                textBox17.Text = Utilities.GetFrequencyString(transitionGradientArray.array[0].frequency);

                                                textBox15.Text = transitionGradientArray.array[0].transitions.ToString();

                                                BufferFramesObject bufferFramesObjectContainingStrongestTransitionGradientFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency(transitionGradientArray.array[0].frequency);

                                                ////double[] transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetStrengthOverTimeForFrequency(transitionGradientArray.array[0].frequency);
                                                double[] transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetStrengthOverTimeForIndex(transitionGradientArray.array[0].index);

                                                currentBufferFramesObject.transitionBufferFrames.GraphData(chart5, transitionStrengthOverTime);
                                            }
                                        }));
                                    }
                                    else
                                        if (analyzingNearFarTransitions)
                                    {
                                        recordingSeries2 = false;
                                    }

                                    currentBufferFramesObject.transitionBufferFrames.nearIndex = -1;
                                }
                            }


                            if (Environment.TickCount - GUIInput.lastInputTime >= BufferFrames.TRANSITION_LENGTH / 2)
                            {
                                currentMode = BinDataMode.Indeterminate;
                            }
                            else
                                currentBufferFramesObject.bufferFrames.Change(BinDataMode.Indeterminate, BinDataMode.Near);

                            if (Notifications.currentNotificationTimeIndex >= notificationTime.Length - 1 || Environment.TickCount - GUIInput.lastInputTime >= notificationTime[Notifications.currentNotificationTimeIndex])
                            {
                                double seconds = Math.Ceiling((double)(notificationTime[notificationTime.Length - 1] - (Environment.TickCount - GUIInput.lastInputTime)) / 1000);
                                notifyIcon1.BalloonTipTitle = "Recording Far in " + seconds + " seconds";
                                notifyIcon1.BalloonTipText = "Move the mouse or press a key if you're near";

                                notifyIcon1.ShowBalloonTip(10000);
                                if (seconds <= 0)
                                {
                                    if (series1BinData.binArray.Length == 0)
                                        series1BinData = new BinData(series2BinData.size, series1BinData.dataSeries, series1BinData.mode);

                                    uint indeterminateBufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);

                                    currentBufferFramesObject.bufferFrames.Change(BinDataMode.Indeterminate, BinDataMode.Far);

                                    series1BinData.bufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far);
                                    series2BinData.bufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near);


                                    this.Invoke(new Action(() =>
                                    {
                                        textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();
                                        textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();
                                    }));


                                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;

                                    userNear = false;

                                    recordingSeries1 = true;
                                    recordingSeries2 = false;

                                    Notifications.currentNotificationTimeIndex = 0;


                                    if (totalBinBufferArray == null || totalBinBufferArray.Length != totalBinCount)
                                        totalBinBufferArray = new float[totalBinCount];
                                    else
                                    {
                                        for (int j = 0; j < totalBinBufferArray.Length; j++)
                                        {
                                            totalBinBufferArray[j] = 0;
                                        }
                                    }

                                    series2BinData.bufferFrames = currentBufferFramesObject.bufferFrames.AddBufferRangeIntoArray(totalBinBufferArray, series2BinData.mode);


                                    ////BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                                    ////long lowerIndex = (long)((currentBufferFramesObject.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);
                                    ////long upperIndex = (long)((currentBufferFramesObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / binSize);

                                    int i = 0;

                                    for (long j = (long)frequencyRange.lower; j < (long)frequencyRange.upper; j++)
                                    {
                                        series2BinData.avgBinArray[j] = (series2BinData.totalBinArray[j] + totalBinBufferArray[i]) / (series2BinData.totalBinArrayNumberOfFrames[j] + series2BinData.bufferFrames);

                                        averageSeries2TotalFramesStrength += series2BinData.avgBinArray[j];

                                        i++;
                                    }

                                    averageSeries2TotalFramesStrength /= series2BinData.size;
                                }
                                else
                                    Notifications.currentNotificationTimeIndex++;
                            }
                        }

                        int totalMagnitude = 0;
                        double avgMagnitude = 0;

                        if (userNear)
                            RecordData(ref series2BinData, ref averageSeries2CurrentFrameStrength, ref averageSeries2TotalFramesStrength, ref totalMagnitude, ref avgMagnitude, 0);

                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (series1BinData.clearFrames)
                                {
                                    ClearSeries1();

                                    series1BinData.clearFrames = false;
                                }

                                if (series2BinData.clearFrames)
                                {
                                    ClearSeries2();

                                    series2BinData.clearFrames = false;
                                }

                                ////textBox6.Text = (series2BinData.numberOfFrames + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate)).ToString();

                                textBox5.Text = (series1BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far)).ToString();
                                textBox6.Text = (series2BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate)).ToString();

                                GraphProximitryValue();

                                ////if (checkBox8.Checked)
                                {
                                    totalADCMagnitudeNear += avgMagnitude;
                                    double avgADCMagnitude = totalADCMagnitudeNear / series2BinData.GetAverageNumberOfFrames();

                                    GraphData(series2BinData);
                                    GraphDifference(series1BinData, series2BinData);

                                    ////////GraphMagnitude(avgADCMagnitude);

                                    ////GraphTotalMagnitude(totalMagnitude);
                                    ////GraphAvgMagnitude(avgMagnitude);

                                    GraphAverageStrength(null);
                                    ////GraphStrengthToTimeBasedGraph(null);
                                }
                            }));
                        }
                        catch (Exception ex)
                        {

                        }
                    }


                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                        button4.Enabled = true;
                        button5.Text = "Record Near Series Data";
                        button18.Text = "Record Near";

                        button3.Enabled = true;
                        button17.Enabled = true;

                        this.Cursor = Cursors.Arrow;



                        Command queueCommand = commandQueue.GetMostRecentCommand();

                            if (queueCommand != null)
                            {
                                if (queueCommand.name == "AutomatedZoomToFrequency" || queueCommand.name == "ZoomToFrequency")
                                {
                                    if (queueCommand.name == "AutomatedZoomToFrequency")
                                        startRecordingSeries1 = true;

                                    NewSettings(false);

                                    commandQueue.RemoveCommand();
                                }
                                else
                                {
                                    ZoomOutOfFrequency();

                                    if (!userNear)
                                    {
                                        RecordSeries1();
                                    }
                                }

                            }
                            else
                            {
                                if (analyzingNearFarTransitions && automatedZooming)
                                    ZoomOutOfFrequency();
                                else
                                    if (!userNear)
                                    {
                                        RecordSeries1();
                                    }
                            }

                            ////Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count-2);
                            
                            /*if (analyzingNearFarTransitions && (command == null || command.name != "UserSelectedFrequencyForAnalysis"))
                            {
                                eventTimer = new System.Windows.Forms.Timer();

                                if (graph1FrequencyRanges.Count>0 && automatedZooming)
                                    eventTimer.Tick += new EventHandler(ZoomOutOfFrequencyThread);
                                else
                                    eventTimer.Tick += new EventHandler(NewSettingsThread);                                    
                                
                                eventTimer.Interval = 1000;

                                eventTimer.Start();
                            }
                            else
                                if (!userNear)
                                {
                                    RecordSeries1();
                                }
                                */
                            
                        }));
                    }
                    catch (Exception ex)
                    {

                    }
                });

                button3.Enabled = false;
                button17.Enabled = false;
                button4.Enabled = false;

                button5.Text = "Stop Recording";
                button18.Text = "Stop";
            }
            else
            {
                this.Cursor = Cursors.WaitCursor;
                recordingSeries2 = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            currentBufferFramesObject.transitionBufferFrames.nearIndex = -1;

            RecordSeries2();
        }

        private void LoadConfig()
        {
            TextReader tr = new StreamReader("config.txt");

            textBox1.Text = tr.ReadLine();
            textBox2.Text = tr.ReadLine();
            textBox3.Text = tr.ReadLine();
            textBox4.Text = tr.ReadLine();

            tr.Close();
        }

        private void SaveConfig()
        {
            TextWriter tw = new StreamWriter("config.txt");

            tw.WriteLine(originalStartFrequency);
            tw.WriteLine(originalEndFrequency);
            tw.WriteLine(stepSize);
            tw.WriteLine(difThreshold);

            tw.Close();
        }

        
        private void ZoomGraphsToFrequency(long frequency)
        {
            Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange((long) graph2LowerFrequency, (long) graph2UpperFrequency, dataLowerFrequency, binSize);

            ////long chartXAxisLength = (long)(chart2.ChartAreas[0].AxisX.ScaleView.ViewMaximum - chart2.ChartAreas[0].AxisX.ScaleView.ViewMinimum);\

            long chartXAxisLength = (long) (frequencyRange.upper - frequencyRange.lower);

            long graphFrequencyRange = (long)(graph2UpperFrequency - graph2LowerFrequency);

            double indexStart = ((frequency - 10000 - graph2LowerFrequency) / graphFrequencyRange) * chartXAxisLength;
            double indexEnd = ((frequency + 10000 - graph2LowerFrequency) / graphFrequencyRange) * chartXAxisLength;

            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(indexStart, indexEnd);

            chart2.ChartAreas[0].AxisX.ScaleView.Zoom(indexStart, indexEnd);

            chart1.Refresh();
            chart2.Refresh();


            /*////graph1LowerFrequency = graph2LowerFrequency = frequency - 10000;
            graph1UpperFrequency = graph2UpperFrequency = frequency + 10000;
            */

            if (series1BinData != null)
            {
                AxisViewChanged(chart1, series1BinData.dataSeries, series1BinData.binArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
            }

            if (series2BinData != null)
            {
                AxisViewChanged(chart1, series2BinData.dataSeries, series2BinData.binArray, ref graph1LowerFrequency, ref graph1UpperFrequency, ref graph1BinFreqInc);
            }



            ////chart2.ChartAreas[0].AxisX.ScaleView.Zoom(((frequency - 10000 - graph2LowerFrequency) / graphFrequencyRange) * chartXAxisLength, ((frequency + 10000 - graph2LowerFrequency) / graphFrequencyRange) * chartXAxisLength);

            if (series1BinData != null)
            {
                AxisViewChanged(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
            }

            if (series2BinData != null)
            {
                AxisViewChanged(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, ref graph2LowerFrequency, ref graph2UpperFrequency, ref graph2BinFreqInc);
            }

            if (series1BinData != null && series2BinData != null)
                GraphDifference(series1BinData, series2BinData);                
        }


        private void ActivateSettings(bool clearSettings = true)
        {
            try
            {                
                dataLowerFrequency = uint.Parse(textBox1.Text);
                dataUpperFrequency = uint.Parse(textBox2.Text);

                currentBufferFramesObject = bufferFramesArray.GetBufferFramesObject((long)dataLowerFrequency, (long)dataUpperFrequency);

                if (currentBufferFramesObject == null)
                {
                    currentBufferFramesObject = new BufferFramesObject(mainForm, (long)dataLowerFrequency, (long)dataUpperFrequency);

                    bufferFramesArray.AddBufferFramesObject(currentBufferFramesObject);
                }

                double tune_count = (dataUpperFrequency - dataLowerFrequency) / SAMPLE_RATE;

                if (PROXIMITRY_DETECTOR)
                {
                    MAXIMUM_GRAPH_BIN_COUNT = (long)Math.Ceiling(rangeSamplingPercentage / 100 * tune_count);

                    if (MAXIMUM_GRAPH_BIN_COUNT < 20)
                        MAXIMUM_GRAPH_BIN_COUNT = 20;
                }

                if (!analyzingLeaderBoardSignals)
                {
                    originalStartFrequency = textBox1.Text;
                    originalEndFrequency = textBox2.Text;
                }

                evaluatedFrequencyString = textBox11.Text.ToLower();

                if (dataUpperFrequency <= dataLowerFrequency)
                    MessageBox.Show("End frequency must be greater than start frequency");
                else
                {
                    stepSize = uint.Parse(textBox3.Text);

                    difThreshold = double.Parse(textBox4.Text);

                    SaveConfig();

                    int result = 0;

                    try
                    {
                        deviceCount = NativeMethods.Initialize(dataLowerFrequency, dataUpperFrequency, stepSize, (uint)SAMPLE_RATE);
                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show(ex.ToString());
                    }

                    if (result < 0)
                    {
                        MessageBox.Show("Could not initialize. Is a device connected and not being used by another program?");
                    }
                    else
                    {
                        totalBinCount = NativeMethods.GetBufferSize();

                        binSize = (double)(dataUpperFrequency - dataLowerFrequency) / totalBinCount;

                        graph1BinFreqInc = binSize;
                        graph2BinFreqInc = binSize;

                        graph1LowerFrequency = dataLowerFrequency;
                        graph1UpperFrequency = dataUpperFrequency;

                        graph2LowerFrequency = dataLowerFrequency;
                        graph2UpperFrequency = dataUpperFrequency;

                        if (clearSettings)
                        {
                            if (analyzingNearFarTransitions)
                            {
                                series1BinDataFullRange = series1BinData;

                                series2BinDataFullRange = series2BinData;
                            }

                            Command mostRecentCommand = commandBuffer.GetMostRecentCommand();

                            ////if (!analyzingNearFarTransitions && !startRecordingSeries1 && !startRecordingSeries2)
                            if (mostRecentCommand==null || (mostRecentCommand.name != "ZoomOutOfFrequency" && mostRecentCommand.name != "ZoomToFrequency"))
                            {
                                series1BinData = new BinData(0, "Far Series", BinDataMode.Far);

                                series2BinData = new BinData(0, "Near Series", BinDataMode.Near);
                            }

                            resetGraph = true;

                            if (startRecordingSeries1 || startRecordingSeries2)
                            {
                                newData = true;
                            }

                            radioButton3.Checked = true;
                            radioButton4.Enabled = false;
                            
                            textBox5.Text = "0";
                            textBox6.Text = "0";

                            textBox7.Text = "0";
                            textBox8.Text = "0";

                            chart1.Series["Far Series"].Points.Clear();
                            chart2.Series["Far Series"].Points.Clear();

                            chart1.Series["Near Series"].Points.Clear();
                            chart2.Series["Near Series"].Points.Clear();

                            chart2.Series["Strength Difference"].Points.Clear();

                            ClearSeries1();
                            ClearSeries2();
                        }
                        else
                        {
                            /*////chart1.Series["Far Series"].Points.Clear();
                            chart2.Series["Far Series"].Points.Clear();

                            chart1.Series["Near Series"].Points.Clear();
                            chart2.Series["Near Series"].Points.Clear();

                            chart2.Series["Strength Difference"].Points.Clear();
                            */
                        }

                        if (!analyzingNearFarTransitions)
                        {
                            graph1FrequencyRanges.Clear();
                            graph2FrequencyRanges.Clear();
                        }

                        button3.Enabled = true;
                        button5.Enabled = true;

                        button17.Enabled = true;
                        button18.Enabled = true;

                        if (startRecordingSeries1)
                            RecordSeries1();
                        else
                            if (startRecordingSeries2)
                                RecordSeries2();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            bufferFramesArray.Clear();

            ActivateSettings();
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSeries(saveFarSeriesBufferFileFileName, series1BinData);
            SaveSeries(saveNearSeriesBufferFileFileName, series2BinData);

            recordingSeries1 = false;
            recordingSeries2 = false;

            Thread.Sleep(1000);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
        }

        private void LoadSeries(string filename, ref BinData series, string seriesString, BinDataMode mode)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    dataLowerFrequency = reader.ReadUInt32();
                    dataUpperFrequency = reader.ReadUInt32();
                    stepSize = reader.ReadUInt32();
                    totalBinCount = reader.ReadUInt32();

                    textBox1.Text = dataLowerFrequency.ToString();
                    textBox2.Text = dataUpperFrequency.ToString();
                    textBox3.Text = stepSize.ToString();

                    ActivateSettings(false);

                    series = new BinData(totalBinCount, seriesString, mode);
                    ///////series.GetAverageNumberOfFrames() = reader.ReadUInt32();

                    if (seriesString == "Far Series")
                        textBox5.Text = series.GetAverageNumberOfFrames().ToString();
                    else
                        textBox6.Text = series.GetAverageNumberOfFrames().ToString();

                    binSize = (double)(dataUpperFrequency - dataLowerFrequency) / totalBinCount;

                    graph1LowerFrequency = dataLowerFrequency;
                    graph1UpperFrequency = dataUpperFrequency;

                    graph2LowerFrequency = dataLowerFrequency;
                    graph2UpperFrequency = dataUpperFrequency;

                    graph1BinFreqInc = binSize;
                    graph2BinFreqInc = binSize;

                    double value;
                    for (int i = 0; i < series.avgBinArray.Length; i++)
                    {
                        try
                        {
                            value = reader.ReadSingle();
                        }
                        catch (Exception ex)
                        {
                            value = 0;
                        }

                        series.totalBinArray[i] = (float)value;

                        if (series.GetAverageNumberOfFrames() == 0)
                            value = 0;
                        else
                            value /= series.GetAverageNumberOfFrames();

                        series.binArray[i] = (float)value;
                        series.avgBinArray[i] = (float)value;
                    }

                    reader.Close();
                }
            }

            resetGraph = true;
            newData = true;
        }

        private void LoadData(string filename)
        {
            /*////using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    dataLowerFrequency = reader.ReadUInt32();
                    dataUpperFrequency = reader.ReadUInt32();
                    stepSize = reader.ReadUInt32();
                    totalBinCount = reader.ReadUInt32();

                    textBox1.Text = dataLowerFrequency.ToString();
                    textBox2.Text = dataUpperFrequency.ToString();
                    textBox3.Text = stepSize.ToString();

                    ActivateSettings(false);

                    series2BinData = new BinData(totalBinCount, seriesString, mode);
                    ///////series.GetAverageNumberOfFrames() = reader.ReadUInt32();

                    if (seriesString == "Far Series")
                        textBox5.Text = series.GetAverageNumberOfFrames().ToString();
                    else
                        textBox6.Text = series.GetAverageNumberOfFrames().ToString();

                    binSize = (double)(dataUpperFrequency - dataLowerFrequency) / totalBinCount;

                    graph1LowerFrequency = dataLowerFrequency;
                    graph1UpperFrequency = dataUpperFrequency;

                    graph2LowerFrequency = dataLowerFrequency;
                    graph2UpperFrequency = dataUpperFrequency;

                    graph1BinFreqInc = binSize;
                    graph2BinFreqInc = binSize;

                    double value;
                    for (int i = 0; i < series.avgBinArray.Length; i++)
                    {
                        try
                        {
                            value = reader.ReadSingle();
                        }
                        catch (Exception ex)
                        {
                            value = 0;
                        }

                        series.totalBinArray[i] = (float)value;

                        if (series.GetAverageNumberOfFrames() == 0)
                            value = 0;
                        else
                            value /= series.GetAverageNumberOfFrames();

                        series.binArray[i] = (float)value;
                        series.avgBinArray[i] = (float)value;
                    }

                    reader.Close();
                }
            }

            resetGraph = true;
            newData = true;
            */
        }

        private void SaveData(string filename, BinData nearSeries, BinData farSeries, BufferFramesArray bufferFramesArray)
        {
            if (nearSeries != null && farSeries != null)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(dataLowerFrequency);
                        writer.Write(dataUpperFrequency);
                        writer.Write(stepSize);
                        writer.Write(totalBinCount);

                        nearSeries.SaveData(writer);
                        farSeries.SaveData(writer);

                        bufferFramesArray.SaveData(writer);

                        writer.Close();
                    }
                }
            }
        }

        private void SaveSeries(string filename, BinData series)
        {
            if (series != null)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(dataLowerFrequency);
                        writer.Write(dataUpperFrequency);
                        writer.Write(stepSize);
                        writer.Write(totalBinCount);
                        writer.Write(series.GetAverageNumberOfFrames());

                        for (int i = 0; i < series.avgBinArray.Length; i++)
                        {
                            writer.Write(series.totalBinArray[i]);
                        }

                        writer.Close();
                    }
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                SaveSeries(saveFileDialog1.FileName, series1BinData);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadSeries(openFileDialog1.FileName, ref series1BinData, "Far Series", BinDataMode.Far);

                GraphData(series1BinData);
                GraphDifference(series1BinData, series2BinData);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                SaveSeries(saveFileDialog1.FileName, series2BinData);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadSeries(openFileDialog1.FileName, ref series2BinData, "Near Series", BinDataMode.Near);

                GraphData(series2BinData);
                GraphDifference(series1BinData, series2BinData);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                difThreshold = double.Parse(textBox4.Text);

                SaveConfig();


                GraphDifference(series1BinData, series2BinData);
            }
            catch (Exception)
            {

            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                checkBox1.Checked = true;
                checkBox1.Enabled = false;

                GraphDifference(series1BinData, series2BinData);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                checkBox1.Enabled = true;

                GraphDifference(series1BinData, series2BinData);
            }

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                textBox4.Enabled = false;
                button10.Enabled = false;
            }
            else
            {
                textBox4.Enabled = true;
                button10.Enabled = true;
            }

            GraphDifference(series1BinData, series2BinData);
        }

        private void ClearSeries1()
        {
            if (series1BinData != null)
            {
                chart1.Series["Far Series"].Points.Clear();
                chart2.Series["Far Series"].Points.Clear();

                if (!startRecordingSeries1 && !startRecordingSeries2)
                {
                    currentBufferFramesObject.bufferFrames.Change(BinDataMode.Far, BinDataMode.NotUsed);
                    series1BinData.Clear();
                }
                

                if (chart2.Series["Strength Difference"].Points.Count > 0)
                    chart2.Series["Strength Difference"].Points.Clear();


                series1MinYChart1 = 99999999;
                series1MaxYChart1 = -99999999;

                series1MinYChart2 = 99999999;
                series1MaxYChart2 = -99999999;                

                textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();
                textBox7.Text = "0";

                GraphData(series1BinData);
                GraphDifference(series1BinData, series2BinData);

                ////series1BinData.numberOfFrames = 0;

                nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;
            }

            totalADCMagnitudeFar = 0;

            proximitryFrequency.totalADCMagnitude = 0;
            proximitryFrequency.sampleCount = 0;            
        }

        private void ClearSeries2()
        {
            if (series2BinData != null)
            {
                chart1.Series["Near Series"].Points.Clear();
                chart2.Series["Near Series"].Points.Clear();

                if (!startRecordingSeries1 && !startRecordingSeries2)
                {
                    currentBufferFramesObject.bufferFrames.Change(BinDataMode.Near, BinDataMode.NotUsed);
                    currentBufferFramesObject.bufferFrames.Change(BinDataMode.Indeterminate, BinDataMode.NotUsed);
                    series2BinData.Clear();
                }
                

                if (chart2.Series["Strength Difference"].Points.Count > 0)
                    chart2.Series["Strength Difference"].Points.Clear();

                series2MinYChart1 = 99999999;
                series2MaxYChart1 = -99999999;

                series2MinYChart2 = 99999999;
                series2MaxYChart2 = -99999999;

                textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();
                textBox8.Text = "0";

                GraphData(series2BinData);
                GraphDifference(series1BinData, series2BinData);
                
                nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;
            }

            totalADCMagnitudeNear = 0;

            proximitryFrequency.totalADCMagnitude = 0;
            proximitryFrequency.sampleCount = 0;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (!recordingSeries1)
            {
                radioButton3.Checked = true;
                radioButton4.Enabled = false;                
            }

            if (!recordingSeries1 && !recordingSeries2)
                ClearSeries1();
            else
                series1BinData.clearFrames = true;                        
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (!recordingSeries2)
            {
                radioButton3.Checked = true;
                radioButton4.Enabled = false;                
            }

            if (!recordingSeries1 && !recordingSeries2)
                ClearSeries2();
            else
                series2BinData.clearFrames = true;
        }


        public Form1()
        {
            InitializeComponent();

            mainForm = this;

            notifyIcon1.Icon = SystemIcons.Exclamation;

            notifyIcon1.BalloonTipTitle = "Recording Far";
            notifyIcon1.BalloonTipText = "Move the mouse or press a key if you're near";

            notifyIcon1.Visible = true;

            mouseInput = new MouseInput(this);

            keyboardInput = new KeyboardInput(this);

            if (PROXIMITRY_DETECTOR)
            {
                MAXIMUM_GRAPH_BIN_COUNT = 20;
                rangeSamplingPercentage = 50;
            }

            chart1.Series["Far Series"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            chart1.Series["Near Series"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

            chart1.ChartAreas[0].CursorX.AutoScroll = false;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.IsMarginVisible = false;
            chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false;


            chart2.Series["Far Series"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            chart2.Series["Near Series"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

            chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;

            chart2.Series["Strength Difference"]["PixelPointWidth"] = "1";

            chart2.ChartAreas[0].CursorX.AutoScroll = false;
            chart2.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart2.ChartAreas[0].AxisX.IsMarginVisible = false;
            chart2.ChartAreas[0].AxisX.ScrollBar.Enabled = false;

            chart3.Series["Far Series"].IsValueShownAsLabel = false;

            LoadConfig();

            try
            {
                dataLowerFrequency = uint.Parse(textBox1.Text);
                dataUpperFrequency = uint.Parse(textBox2.Text);
                stepSize = uint.Parse(textBox3.Text);
                difThreshold = double.Parse(textBox4.Text);
            }
            catch (Exception)
            {
                dataLowerFrequency = 87000000;
                dataUpperFrequency = 108000000;
                stepSize = 100;
                difThreshold = 10;

                textBox1.Text = dataLowerFrequency.ToString();
                textBox2.Text = dataUpperFrequency.ToString();
                textBox3.Text = stepSize.ToString();
                textBox4.Text = difThreshold.ToString();
            }

            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox2.Image = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            waterFall = new Waterfall(pictureBox1);
            waterFallAvg = new Waterfall(pictureBox2);

            waterFall.SetStrengthRange(double.Parse(textBox9.Text), double.Parse(textBox10.Text));
        }                

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                waterFall.SetMode(WaterFallMode.Strength);
                waterFallAvg.SetMode(WaterFallMode.Strength);

                textBox9.Text = waterFallAvg.GetStrengthMinimum().ToString();
                textBox9.Enabled = true;
                textBox10.Text = waterFallAvg.GetStrengthMaximum().ToString();
            }            
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                waterFall.SetMode(WaterFallMode.Difference);
                waterFallAvg.SetMode(WaterFallMode.Difference);
                
                textBox9.Text = "0";
                textBox9.Enabled = false;
                textBox10.Text = waterFallAvg.GetNearStrengthDeltaRange().ToString();
            }            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (this.checkBox2.Checked)
            {
                waterFall.SetRangeMode(WaterFallRangeMode.Auto);
                waterFallAvg.SetRangeMode(WaterFallRangeMode.Auto);
            }
            else
            {
                waterFall.SetRangeMode(WaterFallRangeMode.Fixed);
                waterFallAvg.SetRangeMode(WaterFallRangeMode.Fixed);
            }            

            Task.Factory.StartNew(() =>
            {
                int devicesCount, prevDevicesCount = -1;

                devicesCount = NativeMethods.GetConnectedDevicesCount();

                if (devicesCount != prevDevicesCount)
                {
                    if (devicesCount > 0)
                    {
                        this.Invoke(new Action(() =>
                        {
                            button4.Enabled = true;
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            button3.Enabled = false;
                            button4.Enabled = false;
                            button5.Enabled = false;
                        }));
                    }

                    if (devicesCount > 1)
                    {
                        this.Invoke(new Action(() =>
                        {
                            checkBox7.Enabled = true;

                            checkBox7.Visible = true;

                            if (button4.Enabled)
                            {
                                ////button4.PerformClick();
                                ActivateSettings();
                            }
                        }));
                    }
                    else
                        this.Invoke(new Action(() =>
                        {
                            checkBox7.Enabled = false;
                        }));

                    Thread.Sleep(1000);
                }

                prevDevicesCount = devicesCount;

                if (devicesCount == 0)
                    MessageBox.Show("Connect device(s) and restart the application.\nIf using two devices plug in the device used for near signal detection first.");
            });
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            NativeMethods.SetUseDB(radioButton6.Checked ? 1 : 0);

            ClearSeries1();
            ClearSeries2();
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void button14_Click(object sender, EventArgs e)
        {
            leaderBoardSignals.Clear();
            listBox1.Items.Clear();

            currentLeaderBoardSignalIndex = -1;

            button16.Text = "Analyze Leader Board Frequencies";

            textBox1.Text = originalStartFrequency;

            textBox2.Text = originalEndFrequency;

            analyzingLeaderBoardSignals = false;

            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;

            ///button4.PerformClick();
            ActivateSettings();
        }            

        private void button15_Click(object sender, EventArgs e)
        {
            if (form2 == null)
                form2 = new Form2();

            form2.Show();

            form2.Focus();
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            evaluatedFrequencyString = textBox11.Text.ToLower();
        }

        private void NewSettings(bool clearSettings = true)
        {
            Command command = commandBuffer.GetMostRecentCommand();

            ActivateSettings(clearSettings);

            if (command != null && command.name.IndexOf("ZoomToFrequency") == 0)
            {
                series1MinYChart1 = 99999999;
                series1MaxYChart1 = -99999999;

                series1MinYChart2 = 99999999;
                series1MaxYChart2 = -99999999;

                ZoomGraphsToFrequency(long.Parse(command.name.Split(':')[1]));
            }
        }

        private void NewSettingsThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                eventTimer.Stop();

                eventTimer.Interval = 10000000;

                eventTimer.Dispose();

                eventTimer = null;

                NewSettings(false);                
            }            
        }        

        private void ZoomOutOfFrequencyThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                eventTimer.Stop();

                eventTimer.Interval = 10000000;

                eventTimer.Dispose();

                eventTimer = null;

                ZoomOutOfFrequency();
            }            
        }
            
        private void StopRecordingThread()
        {            
            this.Invoke(new Action(() =>
            {
                if (button3.Text == "Stop Recording")
                    button3.PerformClick();
                else
                if (button5.Text == "Stop Recording")
                    button5.PerformClick();


                eventTimer.Tick += new EventHandler(NewSettingsThread);
                eventTimer.Interval = 1000;

                eventTimer.Start();
            }));
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (leaderBoardSignals.Count > 0)
            {
                currentLeaderBoardSignalIndex++;

                if (currentLeaderBoardSignalIndex == 4)
                {
                    button16.Text = "Analyze Leader Board Frequencies";

                    currentLeaderBoardSignalIndex = -1;

                    analyzingLeaderBoardSignals = false;

                    checkBox4.Checked = false;
                    checkBox5.Checked = false;
                    checkBox6.Checked = false;

                    textBox1.Text = originalStartFrequency;

                    textBox2.Text = originalEndFrequency;

                    ActivateSettings();
                }
                else
                {
                    button16.Text = "Next";

                    analyzingLeaderBoardSignals = true;

                    checkBox4.Checked = true;
                    checkBox5.Checked = false;
                    checkBox6.Checked = false;


                    if (currentLeaderBoardSignalIndex == 0)
                    {
                        originalStartFrequency = textBox1.Text;

                        originalEndFrequency = textBox2.Text;
                    }


                    leaderBoardSignals[currentLeaderBoardSignalIndex].minStrength = series1BinData.avgBinArray[leaderBoardSignals[currentLeaderBoardSignalIndex].index];
                    leaderBoardSignals[currentLeaderBoardSignalIndex].maxStrength = series2BinData.avgBinArray[leaderBoardSignals[currentLeaderBoardSignalIndex].index];

                    textBox1.Text = (Math.Round(leaderBoardSignals[currentLeaderBoardSignalIndex].frequency) - 400000).ToString();

                    textBox2.Text = (Math.Round(leaderBoardSignals[currentLeaderBoardSignalIndex].frequency) + 600000).ToString();

                    textBox11.Text = Utilities.GetFrequencyString(leaderBoardSignals[currentLeaderBoardSignalIndex].frequency);


                    proximitryFrequency.frequency = (uint) leaderBoardSignals[currentLeaderBoardSignalIndex].frequency;

                    proximitryFrequency.maxStrength = leaderBoardSignals[currentLeaderBoardSignalIndex].maxAvgStrength;
                    proximitryFrequency.minStrength = leaderBoardSignals[currentLeaderBoardSignalIndex].minAvgStrength;


                    if (proximitryFrequency.maxStrength> proximitryFrequency.minStrength)
                    {
                        chart6.ChartAreas[0].AxisY.Maximum = proximitryFrequency.maxStrength;
                        chart6.ChartAreas[0].AxisY.Minimum = proximitryFrequency.minStrength;
                    }
                    else
                    {
                        chart6.ChartAreas[0].AxisY.Maximum = proximitryFrequency.minStrength;
                        chart6.ChartAreas[0].AxisY.Minimum = proximitryFrequency.maxStrength;
                    }

                    
                    if (button3.Text == "Stop Recording")
                        button3.PerformClick();
                    else
                        if (button5.Text == "Stop Recording")
                        button5.PerformClick();

                    eventTimer = new System.Windows.Forms.Timer();

                    eventTimer.Tick += new EventHandler(NewSettingsThread);
                    eventTimer.Interval = 1000;
                    
                    eventTimer.Start();                    
                }
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            button3.PerformClick();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            currentBufferFramesObject.transitionBufferFrames.nearIndex = -1;

            button5.PerformClick();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                checkBox5.Checked = false;
                checkBox6.Checked = false;
            }

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
            {
                checkBox4.Checked = false;
                checkBox6.Checked = false;
            }

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
            {
                checkBox4.Checked = false;
                checkBox5.Checked = false;
            }

        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            dataUsedForTimeBasedGraph = TimeBasedGraphData.CurrentGraph;            
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            dataUsedForTimeBasedGraph = TimeBasedGraphData.AverageGraph;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            ClearSeries1();
            ClearSeries2();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            mainForm.textBox15.Text = "";
            currentBufferFramesObject.transitionBufferFrames.Clear();

            double[] transitionStrengthOverTime = currentBufferFramesObject.transitionBufferFrames.GetStrengthOverTimeForRange((long)graph2LowerFrequency, (long)graph2UpperFrequency);
            currentBufferFramesObject.transitionBufferFrames.GraphData(chart5, transitionStrengthOverTime);
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                waterFall.SetMode(WaterFallMode.Off);
                waterFallAvg.SetMode(WaterFallMode.Off);
            }            

        }

        private void button20_Click(object sender, EventArgs e)
        {
            currentLeaderBoardSignalIndex++;

            if (currentLeaderBoardSignalIndex >= 4)
                currentLeaderBoardSignalIndex = 0;

            if (leaderBoardSignals.Count>0)
                ZoomToFrequency((long)leaderBoardSignals[currentLeaderBoardSignalIndex].frequency);
            else
                ZoomToFrequency(long.Parse(textBox11.Text));
        }

        private void button21_Click(object sender, EventArgs e)
        {
            ZoomOutOfFrequency();
        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void button24_Click(object sender, EventArgs e)
        {
            ZoomOutOfFrequency();

            button24.Enabled = false;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
            {
                commandBuffer.AddCommand("UserSelectedFrequencyForAnalysis");

                string itemStr = (string)listBox2.Items[listBox2.SelectedIndex];

                string[] itemStrArray = itemStr.Split(':');

                long frequency = long.Parse(itemStrArray[0]);

                automatedZooming = false;

                button24.Enabled = true;

                ////commandQueue.AddCommand("ZoomToFrequency");

                ZoomToFrequency(frequency);
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            SaveData("test.rtl", series2BinData, series1BinData, bufferFramesArray);
        }

        private void button23_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                prevWaterFallMinimum = waterFallAvg.GetStrengthMinimum();
                prevWaterFallMaximum = waterFallAvg.GetStrengthMaximum();
                prevNearStrengthDeltaRange = waterFallAvg.GetNearStrengthDeltaRange();

                waterFall.SetRangeMode(WaterFallRangeMode.Auto);
                waterFallAvg.SetRangeMode(WaterFallRangeMode.Auto);
            }
            else
            {
                waterFall.SetRangeMode(WaterFallRangeMode.Fixed);
                waterFallAvg.SetRangeMode(WaterFallRangeMode.Fixed);

                waterFall.SetStrengthRange(prevWaterFallMinimum, prevWaterFallMaximum);
                waterFall.SetNearStrengthDeltaRange(prevNearStrengthDeltaRange);

                waterFallAvg.SetStrengthRange(prevWaterFallMinimum, prevWaterFallMaximum);
                waterFallAvg.SetNearStrengthDeltaRange(prevNearStrengthDeltaRange);


                if (waterFallAvg.GetMode() == WaterFallMode.Difference)
                {
                    textBox9.Text = "0";
                    textBox10.Text = Math.Round(prevNearStrengthDeltaRange, 2).ToString();
                }
                else
                {
                    textBox9.Text = Math.Round(prevWaterFallMinimum, 2).ToString();
                    textBox10.Text = Math.Round(prevWaterFallMaximum, 2).ToString();
                }
            }
        }

        private void chart3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (waterFall.GetMode() == WaterFallMode.Strength)
            {
                waterFall.SetStrengthRange(double.Parse(textBox9.Text), double.Parse(textBox10.Text));
                waterFallAvg.SetStrengthRange(double.Parse(textBox9.Text), double.Parse(textBox10.Text));
            }
            else
            {
                waterFall.SetNearStrengthDeltaRange(double.Parse(textBox10.Text));
                waterFallAvg.SetNearStrengthDeltaRange(double.Parse(textBox10.Text));
            }
        }

        public bool UsingProximitryDetection()
        {
            return mainForm.checkBox9.Checked;
        }        
    }    
}
