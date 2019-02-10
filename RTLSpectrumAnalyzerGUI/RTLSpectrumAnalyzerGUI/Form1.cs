
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
        enum ProgramState { AQUIRING_NEAR_FAR_FRAMES, ANALYZING_TRANSITIONS };

        ProgramState programState = ProgramState.AQUIRING_NEAR_FAR_FRAMES;

        static bool ANALYZING_TRANSITIONS_STAGE_REACHED = false;

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

        public double graph1LowerIndex = double.NaN;
        public double graph1UpperIndex = double.NaN;

        public double graph2LowerIndex = double.NaN;
        public double graph2UpperIndex = double.NaN;

        List<long> nearSignal = new List<long>();

        public uint dataLowerFrequency = 0, dataUpperFrequency = 0, stepSize;

        double rangeSamplingPercentage = 100;

        double difThreshold = 0;
        uint totalBinCount = 0;

        public bool recordingSeries1 = false;
        public bool recordingSeries2 = false;

        bool resumeRecording = false;

        public bool startRecordingSeries1 = false;
        public bool startRecordingSeries2 = false;

        public long recordingSeries1Start = -1;
        public long recordingSeries2Start = -1;


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

        public List<InterestingSignal> reradiatedFrequencies = new List<InterestingSignal>();
        List<InterestingSignal> leaderBoardRanges = new List<InterestingSignal>();
        List<InterestingSignal> leaderBoardSignals = new List<InterestingSignal>();
        List<InterestingSignal> interestingSignals = new List<InterestingSignal>();
        List<InterestingSignal> interestingSignalsForAnalysis = new List<InterestingSignal>();
        List<InterestingSignal> transitionSignalsToBeAnalysed = new List<InterestingSignal>();

        short MAX_LEADER_BOARD_LIST_COUNT = 1000;
        short MAX_INTERESTING_SIGNAL_LIST_COUNT = 1000;
        short MAX_INTERESTING_SIGNAL_LIST_BOX_COUNT = 100;
        short MAX_INTERESTING_SIGNALS_FOR_EVALUATION_COUNT = 8;
        short MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT = 4;

        short STRONGEST_INTERESTING_AND_LEADERBOARD_SIGNALS_FOR_TRANSITIONS = 100;

        short GRAPH_WIDTH_WHEN_ZOOMED = 10000;

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

        public BinDataMode currentMode = BinDataMode.Indeterminate;

        private bool analyzingNearFarTransitions = false;

        public BufferFramesArray bufferFramesArray = new BufferFramesArray();

        public CommandBuffer commandBuffer = new CommandBuffer();
        public CommandBuffer commandQueue = new CommandBuffer();

        public BufferFramesObject currentBufferFramesObject = null;

        private int nearFarBufferIndex;

        public float[] totalBinBufferArray;
        public float[] avgBinBufferArray;

        public bool recordingIntoBuffer = true;

        MouseInput mouseInput;

        KeyboardInput keyboardInput;

        quickStartForm quickStartForm;

        public Form2 form2 = new Form2();

        public LeaderboardGraph leaderboardGraph = new LeaderboardGraph();

        UserAnalysisForm userAnalysisForm;

        BinData series1BinDataFullRange;

        BinData series2BinDataFullRange;

        public BinData series1BinData;
        public BinData series2BinData;

        public double binSize;

        Stack<Utilities.FrequencyRange> graph1FrequencyRanges = new Stack<Utilities.FrequencyRange>();
        Stack<Utilities.FrequencyRange> graph2FrequencyRanges = new Stack<Utilities.FrequencyRange>();

        double avgFramesForRegion, prevAvgFramesForRegion;
        uint nearFrames, prevNearFrames;
        uint indeterminateFrames, prevIndeterminateFrames;
        uint framesDif;

        bool automatedZooming = true;

        public long userSelectedFrequencyForAnalysis = -1;

        long userSelectedFrequencyForZooming = -1;


    #if SDR_DEBUG
        public const long REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS = 100;
        public const long REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT = 1000;
        public const long REQUIRED_FRAMES_BEFORE_USER_ANALYSIS = 10000;
        public const long REQUIRED_TRANSITIONS_BEFORE_USER_ANALYSIS = 4;
        public const uint MAX_TRANSITION_SCANS = 4;
    #else
        public const long REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS = 100;
        public const long REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT = 1000;
        public const long REQUIRED_FRAMES_BEFORE_USER_ANALYSIS = 10000;
        public const long REQUIRED_TRANSITIONS_BEFORE_USER_ANALYSIS = 4;        
        public const uint MAX_TRANSITION_SCANS = 4;
    #endif


        public const string SESSION_PATH = "sessions\\";
        public const string PREVIOUS_SESSIONS_PATH = "sessions\\previous_sessions\\";
        public const string SESSION_FILE_NAME = "session.rtl";
        public const string SESSION_EXTENSION = ".rtl";


        public const int DENSITY_GRAPH_SEGMENT_SIZE = 3000000;

        TransitionGradientArray transitionGradientArray;

        public bool zoomingAnalysis = true;

        public int analyzingTransitionsBeforeSuccessCount = 0;

        public bool showGraphs = true;

        public bool originalUserSettingShowGraphs = true;

        public int currentLeaderboardSignalBeingAnalyzedIndex = -1;

        private long userAnalysisCheckFarFrames = 0;
        private long userAnalysisCheckNearFrames = 0;

        public bool showingCheckForReradiatedFrequencyDialog = false;

        public bool analyzingUserSelectedFrequency = false;

        public bool transitionAnalysesMode = false;

        public bool showUserAnalaysisGraphs = false;

        clsResize _form_resize;

        ////public bool neverShowGraphs = false;

        public long GetAverageNumberOfFramesForFrequencyRegion(BinData binData, BinDataMode binDataMode, long lowerFrequency, long upperFrequency, long dataLowerFrequency, double binSize)
        {
            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);
            BufferFramesObject bufferFramesObject = mainForm.bufferFramesArray.GetBufferFramesObject(lowerFrequency, upperFrequency);

            long frames = 0;

            frames += (long)binData.GetAverageNumberOfFramesForFrequencyRegion(lowerFrequency, upperFrequency, dataLowerFrequency, binSize);

            if (zoomedOutBufferObject != bufferFramesObject)
                frames += zoomedOutBufferObject.bufferFrames.GetFramesCount(binDataMode);

            if (bufferFramesObject != null)
                frames += bufferFramesObject.bufferFrames.GetFramesCount(binDataMode);

            if (binDataMode == BinDataMode.Near)
            {
                if (zoomedOutBufferObject != bufferFramesObject)
                    frames += zoomedOutBufferObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);

                if (bufferFramesObject != null)
                    frames += bufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);
            }


            return frames;
        }

        private bool ZoomOutOfFrequency()
        {
            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            ////if (!automatedZooming || (/*////GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT && */GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT))
            if (!automatedZooming || transitionAnalysesMode || (GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT || GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT))
            {
                commandBuffer.AddCommand("ZoomOutOfFrequency");

                chart1.Series["Far Series"].Points.Clear();
                chart2.Series["Far Series"].Points.Clear();

                analyzingNearFarTransitions = false;

                automatedZooming = true;

                button24.Enabled = false;
                userAnalysisForm.button1.Enabled = button24.Enabled;

                bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);

                textBox1.Text = zoomedOutBufferObject.lowerFrequency.ToString();
                textBox2.Text = zoomedOutBufferObject.upperFrequency.ToString();

                if (button3.Text == "Stop Recording")
                    button3.PerformClick();
                else
                    if (button5.Text == "Stop Recording")
                    button5.PerformClick();

                if (resumeRecording)
                {
                    startRecordingSeries2 = true;

                    resumeRecording = false;
                }

                LaunchNewThread(NewSettingsThread, 1000);

                return true;
            }

            return false;
        }

        private void InitializeZoomToFrequencyThread()
        {
            this.Invoke(new Action(() =>
            {
                Command command = commandBuffer.GetMostRecentCommand();

                if (command == null || command.name != "InitializeZoomToFrequencyThread" || (Environment.TickCount & int.MaxValue) - command.time > 20000)
                {
                    commandBuffer.AddCommand("InitializeZoomToFrequencyThread");

                    LaunchNewThread(DetermineInterestingSignalAndZoomToFrequencyThread, 100);
                }
            }));
        }

        public delegate void DelegateDeclaration(Object myObject, EventArgs myEventArgs);

        public void ShowQuickStartFormThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                DestroyEventTimer();

                ShowQuickStartForm();
            }
        }

        private void LaunchNewThread(DelegateDeclaration target, int delay, string tag = "")
        {
            DestroyEventTimer();

            eventTimer = new System.Windows.Forms.Timer();

            eventTimer.Tag = tag;
            eventTimer.Tick += new EventHandler(target);
            eventTimer.Interval = delay;

            eventTimer.Start();
        }

        private void DestroyEventTimer()
        {
            if (eventTimer != null)
            {
                eventTimer.Stop();

                eventTimer.Interval = 10000000;

                eventTimer.Dispose();

                eventTimer = null;
            }
        }

        private void DetermineInterestingSignalAndZoomToFrequencyThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                DestroyEventTimer();

                ////DetermineInterestingSignalAndZoomToFrequency(interestingSignalsForAnalysis);
                DetermineInterestingSignalAndZoomToFrequency(leaderBoardSignals, leaderBoardRanges);
            }
        }

        private int DetermineSignalForAcquiringFrames(List<InterestingSignal> signals)
        {
            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            ////for (int i = 0; i < interestingSignalsForAnalysis.Count && i < MAX_INTERESTING_SIGNALS_FOR_EVALUATION_COUNT; i++)
            ////for (int i = 0; i < leaderBoardSignals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
            for (int i = 0; i < signals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
            {
                Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signals[i].frequency);

                long framesForRegion;

                if (recordingSeries1)
                {
                    framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);
                }
                else
                    framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);

                if (framesForRegion < REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                    return i;
            }

            return -1;
        }

        private int DetermineInterestingSignalWithLeastFramesForAcquiringFrames(List<InterestingSignal> signals)
        {
            long minFrames = -1;
            int minIndex = -1;

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            ////for (int i = 0; i < interestingSignalsForAnalysis.Count && i < MAX_INTERESTING_SIGNALS_FOR_EVALUATION_COUNT; i++)
            ////for (int i = 0; i < leaderBoardSignals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
            for (int i = 0; i < signals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
            {
                ////Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)leaderBoardSignals[i].frequency);
                ////Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signals[i].frequency);

                Utilities.FrequencyRange frequencyRange;

                if (signals[i].lowerFrequency == -1 || signals[i].upperFrequency == -1)
                    frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signals[i].frequency);
                else
                {
                    frequencyRange = new Utilities.FrequencyRange(signals[i].lowerFrequency, signals[i].upperFrequency);
                }

                long framesForRegion;



                if (recordingSeries1)
                {
                    framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);
                }
                else
                    framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);

                if (minFrames == -1 || framesForRegion < minFrames)
                {
                    minFrames = framesForRegion;

                    minIndex = i;
                }
            }

            return minIndex;
        }

        private int DetermineSignalForAnalysingTransitions(List<InterestingSignal> signals)
        {
            for (int j = 0; j < BufferFrames.minStrengthForRankings.Length; j++)
            {
                ////for (int i = 0; i < interestingSignalsForAnalysis.Count && i < MAX_INTERESTING_SIGNALS_FOR_EVALUATION_COUNT; i++)
                ////for (int i = 0; i < leaderBoardSignals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
                for (int i = 0; i < signals.Count && (i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT || transitionAnalysesMode); i++)
                {
                    ////Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)leaderBoardSignals[i].frequency);                    

                    ////Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signals[i].frequency);

                    Utilities.FrequencyRange frequencyRange;

                    if (signals[i].lowerFrequency == -1 || signals[i].upperFrequency == -1)
                        frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signals[i].frequency);
                    else
                    {
                        frequencyRange = new Utilities.FrequencyRange(signals[i].lowerFrequency, signals[i].upperFrequency);
                    }


                    BufferFramesObject bufferFramesObject = bufferFramesArray.GetBufferFramesObject((long)frequencyRange.lower, (long)frequencyRange.upper);

                    if (bufferFramesObject == null)
                        return i;

                    if (!transitionAnalysesMode)
                    {
                        if (bufferFramesObject.reradiatedRankingCategory <= j)
                            return i;
                    }
                }
            }

            return -1;
        }

        private void DetermineInterestingSignalAndZoomToFrequency(List<InterestingSignal> signals, List<InterestingSignal> signals2 = null)
        {
            List<InterestingSignal> signalsList = new List<InterestingSignal>();

            if (listBox2.Items.Count > 0)
            {
                long transitionFrequency;

                string itemStr = (string)listBox2.Items[0];

                string[] itemStrArray = itemStr.Split(':');


                for (int i = 0; i < listBox2.Items.Count; i++)
                {
                    itemStr = (string)listBox2.Items[i];

                    itemStrArray = itemStr.Split(':');

                    if (itemStrArray[0].IndexOf("to") > -1)
                    {
                        Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromString(itemStrArray[0]);

                        transitionFrequency = (long)frequencyRange.lower;
                    }
                    else
                    {
                        itemStrArray[0] = itemStrArray[0].Substring(0, itemStrArray[0].Length - 3);

                        transitionFrequency = (long)(double.Parse(itemStrArray[0]) * 1000000);
                    }

                    if (bufferFramesArray.EvaluateWhetherReradiatedFrequency(transitionFrequency))
                    {
                        signalsList.Add(new InterestingSignal(0, 0, 0, transitionFrequency));

                        break;
                    }
                }
            }


            int signalsCount = 0;

            if (signals2 != null)
            {
                signalsCount = MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT / 2;
            }


            for (int i = 0; i < signals.Count && i < signalsCount; i++)
            {
                signalsList.Add(signals[i]);
            }

            if (signals2 != null)
            {
                for (int i = 0; i < signals2.Count && i < signalsCount; i++)
                {
                    signalsList.Add(signals2[i]);
                }
            }

            if (transitionAnalysesMode)
            {
                currentLeaderBoardSignalIndex = DetermineSignalForAnalysingTransitions(transitionSignalsToBeAnalysed);

                if (currentLeaderBoardSignalIndex > -1)
                {
                    programState = ProgramState.ANALYZING_TRANSITIONS;

                    ANALYZING_TRANSITIONS_STAGE_REACHED = true;
                }

                ////StopRecording();

                ////recordingSeries1 = true;

                if (currentLeaderBoardSignalIndex > -1)
                    ZoomToFrequency((long)transitionSignalsToBeAnalysed[currentLeaderBoardSignalIndex].frequency);
                else
                {
                    BufferFramesObject bufferFramesObjectForFrequency;

                    double avgGradientStrengthForFrequencyRange;

                    double maxAvgGradientStrengthForFrequencyRange = Double.NaN;

                    int maxIndex = -1;

                    for (int i = 0; i < transitionSignalsToBeAnalysed.Count - 2; i++)
                    {
                        bufferFramesObjectForFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency((long)transitionSignalsToBeAnalysed[i].frequency);

                        /////////transitionSignalsToBeAnalysed[i].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetAverageForGradients();
                        transitionSignalsToBeAnalysed[i].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetTransitionsGradient().strength;


                        bufferFramesObjectForFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency((long)transitionSignalsToBeAnalysed[i + 1].frequency);
                        /////////transitionSignalsToBeAnalysed[i + 1].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetAverageForGradients();
                        transitionSignalsToBeAnalysed[i + 1].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetTransitionsGradient().strength;

                        bufferFramesObjectForFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency((long)transitionSignalsToBeAnalysed[i + 2].frequency);
                        /////////transitionSignalsToBeAnalysed[i + 2].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetAverageForGradients();
                        transitionSignalsToBeAnalysed[i + 2].avgGradientStrength = bufferFramesObjectForFrequency.transitionBufferFrames.GetTransitionsGradient().strength;

                        avgGradientStrengthForFrequencyRange = transitionSignalsToBeAnalysed[i].avgGradientStrength + transitionSignalsToBeAnalysed[i + 1].avgGradientStrength + transitionSignalsToBeAnalysed[i + 2].avgGradientStrength;

                        avgGradientStrengthForFrequencyRange /= 3;

                        if (Double.IsNaN(maxAvgGradientStrengthForFrequencyRange) || avgGradientStrengthForFrequencyRange > maxAvgGradientStrengthForFrequencyRange)
                        {
                            maxAvgGradientStrengthForFrequencyRange = avgGradientStrengthForFrequencyRange;

                            maxIndex = i;
                        }
                    }

                    if (maxIndex > -1)
                    {
                        if (transitionSignalsToBeAnalysed[maxIndex + 2].frequency - transitionSignalsToBeAnalysed[maxIndex].frequency > 10000000)
                        {
                            uint scans = (uint)Math.Ceiling((transitionSignalsToBeAnalysed[maxIndex + 2].frequency - transitionSignalsToBeAnalysed[maxIndex].frequency) / 1000000);

                            if (scans > MAX_TRANSITION_SCANS)
                                scans = MAX_TRANSITION_SCANS;

                            InitializeTransitionSignalsToBeAnalysed(scans, (long)transitionSignalsToBeAnalysed[maxIndex].frequency, (long)transitionSignalsToBeAnalysed[maxIndex + 2].frequency);

                            ////if (transitionSignalsToBeAnalysed[transitionSignalsToBeAnalysed.Count-1].frequency - transitionSignalsToBeAnalysed[0].frequency > 10000000)
                            DetermineInterestingSignalAndZoomToFrequency(transitionSignalsToBeAnalysed);
                        }
                        else
                        {
                            transitionAnalysesMode = false;

                            Utilities.FrequencyRange frequencyRange1 = Utilities.GetFrequencyRangeFromFrequency((long)transitionSignalsToBeAnalysed[maxIndex].frequency);

                            Utilities.FrequencyRange frequencyRange2 = Utilities.GetFrequencyRangeFromFrequency((long)transitionSignalsToBeAnalysed[maxIndex + 2].frequency);

                            textBox1.Text = frequencyRange1.lower.ToString();
                            textBox2.Text = frequencyRange2.upper.ToString();



                            ////BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                            /////////Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(reradiatedFrequency);

                            ////zoomedOutBufferObject.lowerFrequency = (long)frequencyRange.lower;

                            ////zoomedOutBufferObject.upperFrequency = (long)frequencyRange.upper;

                            StopRecording();

                            ////this.Invoke(new Action(() =>
                            ////{
                            ////bufferFramesArray.Clear();

                            /////////textBox1.Text = frequencyRange.lower.ToString();
                            /////////textBox2.Text = frequencyRange.upper.ToString();

                            ////textBox1.Text = "430000000";
                            ////textBox2.Text = "440000000";


                            startRecordingSeries2 = true;

                            LaunchNewThread(NewSettingsThread, 4000, "clearData");



                            /*////if (listBox2.Items.Count > 0)
                            {
                                string itemStr = (string)listBox2.Items[0];

                                string[] itemStrArray = itemStr.Split(':');

                                itemStrArray[0] = itemStrArray[0].Substring(0, itemStrArray[0].Length - 3);

                                long reradiatedFrequency = (long)(double.Parse(itemStrArray[0]) * 1000000);

                                transitionGradientArray = null;

                                ////BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                                Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(reradiatedFrequency);

                                ////zoomedOutBufferObject.lowerFrequency = (long)frequencyRange.lower;

                                ////zoomedOutBufferObject.upperFrequency = (long)frequencyRange.upper;

                                StopRecording();

                                ////this.Invoke(new Action(() =>
                                ////{
                                ////bufferFramesArray.Clear();

                                textBox1.Text = frequencyRange.lower.ToString();
                                textBox2.Text = frequencyRange.upper.ToString();

                                ////textBox1.Text = "430000000";
                                ////textBox2.Text = "440000000";


                                startRecordingSeries2 = true;

                                LaunchNewThread(NewSettingsThread, 4000, "clearData");

                                ////ActivateSettings();

                                ////RecordSeries2();
                                ///}));

                                ////Thread.Sleep(1000);
                            }
                            */
                        }
                    }
                    else
                    {

                    }
                }
            }
            else
            if (leaderBoardSignals.Count > 0 && automatedZooming)
            {
                currentLeaderBoardSignalIndex = DetermineSignalForAcquiringFrames(signalsList);

                if (currentLeaderBoardSignalIndex == -1 && recordingSeries1)
                ////if (currentLeaderBoardSignalIndex == -1)
                {
                    currentLeaderBoardSignalIndex = DetermineSignalForAnalysingTransitions(signalsList);

                    if (currentLeaderBoardSignalIndex > -1)
                    {
                        ////if (recordingSeries1)
                        programState = ProgramState.ANALYZING_TRANSITIONS;
                        ////else
                        ////programState = ProgramState.AQUIRING_NEAR_FAR_FRAMES;                        

                        ANALYZING_TRANSITIONS_STAGE_REACHED = true;
                    }
                }
                else if (currentLeaderBoardSignalIndex == -1)
                {

                    /*////int list = 0;

                    if (signalsList2 != null)
                    {
                        Random rnd = new Random();

                        list = rnd.Next(0, 2);
                    }

                    List<InterestingSignal> signalsList;

                    if (list == 0)
                        signalsList = signalsList1;
                    else
                        signalsList = signalsList2;


                    int listIndex;

                    Random rnd = new Random();

                    listIndex = rnd.Next(0, Math.Min(signalsList.Count, MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT) + 1);


                    BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                    ////for (int i = 0; i < interestingSignalsForAnalysis.Count && i < MAX_INTERESTING_SIGNALS_FOR_EVALUATION_COUNT; i++)
                    ////for (int i = 0; i < leaderBoardSignals.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
                    ////for (int i = 0; i < signalsList.Count && i < MAX_LEADERBOARD_SIGNALS_FOR_EVALUATION_COUNT; i++)
                    {
                        Utilities.FrequencyRange frequencyRange;

                        if (signalsList[listIndex].lowerFrequency == -1 || signalsList[listIndex].upperFrequency == -1)
                            frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)signalsList[listIndex].frequency);
                        else
                        {
                            frequencyRange = new Utilities.FrequencyRange(signalsList[listIndex].lowerFrequency, signalsList[listIndex].upperFrequency);
                        }


                        long framesForRegion;

                        if (recordingSeries1)
                        {
                            framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);
                        }
                        else
                            framesForRegion = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, (long)frequencyRange.lower, (long)frequencyRange.upper, zoomedOutBufferObject.lowerFrequency, binSize);

                        if (framesForRegion < REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                            return listIndex;
                    }
                    */


                    currentLeaderBoardSignalIndex = DetermineInterestingSignalWithLeastFramesForAcquiringFrames(signalsList);

                    programState = ProgramState.AQUIRING_NEAR_FAR_FRAMES;
                }

                if (currentLeaderBoardSignalIndex > -1)
                    ////ZoomToFrequency((long)signals[currentLeaderBoardSignalIndex].frequency);
                    ZoomToFrequency((long)signalsList[currentLeaderBoardSignalIndex].frequency);
            }
        }

        private void ZoomToFrequency(long lowerFrequency, long upperFrequency = -1)
        {
            if (recordingSeries1 || recordingSeries2 || !automatedZooming)
            {
                Command command = commandBuffer.GetMostRecentCommand();

                commandBuffer.AddCommand("ZoomToFrequency:" + lowerFrequency + ":" + upperFrequency);

                if (automatedZooming)
                {
                    string commandStr = "AutomatedZoomToFrequency";

                    if (recordingSeries1)
                        commandStr += ":RecordSeries1";
                    else if (recordingSeries2)
                        commandStr += ":RecordSeries2";

                    commandQueue.AddCommand(commandStr);
                }
                else if (recordingSeries1 || recordingSeries2)
                    commandQueue.AddCommand("ZoomToFrequency");

                analyzingNearFarTransitions = true;

                analyzingTransitionsBeforeSuccessCount++;

                Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(lowerFrequency);
                graph1FrequencyRanges.Push(frequencyRange);

                this.Invoke(new Action(() =>
                {
                    textBox1.Text = Math.Round(frequencyRange.lower).ToString();
                    textBox2.Text = Math.Round(frequencyRange.upper).ToString();
                }));

                StopRecording();

                command = commandQueue.GetMostRecentCommand();
                ////if (!automatedZooming && (button3.Text != "Stop Recording" && button5.Text != "Stop Recording"))
                if (!automatedZooming && (command == null || command.name != "ZoomToFrequency"))
                {
                    ActivateSettings(false);

                    ZoomGraphsToFrequency(lowerFrequency, upperFrequency);
                }
            }
        }

        private void EnableShowGraphButtons(bool enable)
        {
            checkBox8.Enabled = checkBox13.Enabled = checkBox15.Enabled = enable;

            if (enable)
            {
                if (checkBox8.Checked)
                    mainForm.checkBox13.Enabled = mainForm.checkBox15.Enabled = false;
                else
                    mainForm.checkBox13.Enabled = mainForm.checkBox15.Enabled = true;
            }
        }

        private bool ShowGraphs()
        {
            if (showUserAnalaysisGraphs)
                return false;            
            
            if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                    return true;

            return false;            
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

                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint1 = null;

                    if (dataSeries == "Strength Difference")
                        chart.Series[dataSeries].Points.Clear();

                    bool negative = false;

                    for (int i = 0; i < lowerResGraphBinCount; i++)
                    {
                        value = data[(long)index];

                        /*if (i%2==0)
                            value = -100;
                        else
                            value = -110;
                            */

                        if (value < 0)
                            negative = true;

                        ////if (value > 100 || dataSeries != "Strength Difference")
                        {
                            ////if ((checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions)) && value < 0)
                            ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                            if (ShowGraphs())
                            {
                                if (i < chart.Series[dataSeries].Points.Count && dataSeries != "Strength Difference")
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


                            if (chart != leaderboardGraph.chart1)
                            {
                                if (checkBox10.Checked)
                                {
                                    if (interestingSignals != null && dataSeries == "Strength Difference")
                                    {
                                        for (int j = (int)index; j < (int)(index + inc); j++)
                                        {
                                            interestingSignalIndex = interestingSignals.FindIndex(x => x.index == j);

                                            if (interestingSignalIndex >= 0 && interestingSignalIndex < 4)
                                            {
                                                interestingSignal = interestingSignals[interestingSignalIndex];

                                                ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                                                if (ShowGraphs())
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
                                }
                                else
                                if (checkBox12.Checked && transitionGradientArray != null)
                                {
                                    if (graphPoint1 != null)
                                    {
                                        if (FrequencyInInterestingAndLeaderBoardSignals((long)binFrequency, STRONGEST_INTERESTING_AND_LEADERBOARD_SIGNALS_FOR_TRANSITIONS))
                                        {
                                            TransitionGradient transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency((long)binFrequency, binSize / 10);

                                            if (transitionGradient != null)
                                            {

                                                if (transitionGradient.ranking >= 0 && transitionGradient.ranking < 4)
                                                {
                                                    graphPoint1.Label = Utilities.GetFrequencyString(binFrequency);
                                                    graphPoint1.LabelForeColor = Waterfall.colors[(int)((float)transitionGradient.ranking / 4 * (Waterfall.colors.Count - 1))];
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            ////if (i >= 0)
                            {
                                if (value < minY)
                                {
                                    minY = value;

                                    minYIndex = i;
                                }

                                if (value > maxY)//// && index > 0)
                                {
                                    maxY = value;

                                    maxYIndex = i;
                                }
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

                    chart.ChartAreas[0].AxisX.ScaleView.Zoom(0, lowerResGraphBinCount - 1);

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
                    else if (dataSeries != "Series" && dataSeries != "AvgSeries")
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

                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                    if (ShowGraphs())
                    {
                        if (dataSeries != "Series" && dataSeries != "AvgSeries")
                        {
                            if (checkBox3.Checked)
                            {
                                double roundMin = Math.Round(minY, 2);

                                chart.ChartAreas[0].AxisY.Minimum = roundMin;
                                chart.ChartAreas[0].AxisY2.Minimum = roundMin;
                            }

                            if (checkBox3.Checked)
                            {
                                double roundMax = Math.Round(maxY, 2);

                                chart.ChartAreas[0].AxisY.Maximum = roundMax;
                                chart.ChartAreas[0].AxisY2.Maximum = roundMax;
                            }

                            chart.ChartAreas[0].AxisY.Maximum += 0.01;
                            chart.ChartAreas[0].AxisY2.Maximum += 0.01;
                        }
                        else
                        {
                            if (checkBox3.Checked)
                            {
                                double roundMin = Math.Round(minY, 2);

                                if (dataSeries == "Series")
                                    chart.ChartAreas[0].AxisY.Minimum = roundMin;
                                else
                                if (dataSeries == "AvgSeries")
                                    chart.ChartAreas[0].AxisY2.Minimum = roundMin;
                            }

                            if (checkBox3.Checked)
                            {
                                double roundMax = Math.Round(maxY, 2);

                                if (dataSeries == "Series")
                                    chart.ChartAreas[0].AxisY.Maximum = roundMax;
                                else
                                if (dataSeries == "AvgSeries")
                                    chart.ChartAreas[0].AxisY2.Maximum = roundMax;
                            }

                            if (dataSeries == "Series")
                                chart.ChartAreas[0].AxisY.Maximum += 0.01;
                            else
                                if (dataSeries == "AvgSeries")
                                chart.ChartAreas[0].AxisY2.Maximum += 0.01;
                        }
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

        private void GraphDataForRange(System.Windows.Forms.DataVisualization.Charting.Chart chart, string dataSeries, float[] data, double lowerFrequency, double upperFrequency, double graphBinFreqInc)
        {
            if (data.Length > 0)
            {
                long lowerIndex;
                long upperIndex;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                if (zoomedOutBufferObject.binSize == 0)
                    zoomedOutBufferObject.binSize = binSize;

                lowerIndex = (long)((lowerFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
                upperIndex = (long)((upperFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);

                RangeChanged(chart, dataSeries, data, lowerIndex, upperIndex, lowerFrequency, ref graphBinFreqInc);

                if (series1BinData != null && series2BinData != null)
                    if ((dataSeries == "Far Series" || dataSeries == "Near Series"))//// && (recordingSeries1 || recordingSeries2))
                    {
                        if (chart == chart1)
                        {
                            ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                            if (ShowGraphs())
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
                                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                                    if (ShowGraphs())
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

                                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                                    if (ShowGraphs())
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
            else
            {
                chart.ChartAreas[0].AxisY.Minimum = 0;

                chart.ChartAreas[0].AxisY.Maximum = 0.01;
            }
        }

        private void chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            commandBuffer.AddCommand("chart1_AxisViewChanged");

            Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(graph1LowerFrequency, graph1UpperFrequency);
            graph1FrequencyRanges.Push(frequencyRange);

            double min = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
            double max = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum + 1;

            graph1UpperFrequency = graph1LowerFrequency + max * graph1BinFreqInc;
            graph1LowerFrequency = graph1LowerFrequency + min * graph1BinFreqInc;


            if (series1BinData != null)
            {
                GraphDataForRange(chart1, series1BinData.dataSeries, series1BinData.binArray, graph1LowerFrequency, graph1UpperFrequency, graph1BinFreqInc);
            }

            if (series2BinData != null)
            {
                GraphDataForRange(chart1, series2BinData.dataSeries, series2BinData.binArray, graph1LowerFrequency, graph1UpperFrequency, graph1BinFreqInc);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (graph1FrequencyRanges.Count > 0)
            {
                Utilities.FrequencyRange frequencyRange = graph1FrequencyRanges.Pop();

                long lowerIndex;
                long upperIndex;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                lowerIndex = (long)((frequencyRange.lower - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
                upperIndex = (long)((frequencyRange.upper - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);


                graph1LowerFrequency = frequencyRange.lower;
                graph1UpperFrequency = frequencyRange.upper;

                if (series1BinData != null)
                    RangeChanged(chart1, series1BinData.dataSeries, series1BinData.binArray, lowerIndex, upperIndex, graph1LowerFrequency, ref graph1BinFreqInc);

                if (series2BinData != null)
                    RangeChanged(chart1, series2BinData.dataSeries, series2BinData.binArray, lowerIndex, upperIndex, graph1LowerFrequency, ref graph1BinFreqInc);

                if (series1BinData != null && series2BinData != null)
                    GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();

                chart1.Refresh();
            }
        }

        private void chart2_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            ////commandBuffer.AddCommand("chart2_AxisViewChanged");

            Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(graph2LowerFrequency, graph2UpperFrequency);
            graph2FrequencyRanges.Push(frequencyRange);

            double min = chart2.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
            double max = chart2.ChartAreas[0].AxisX.ScaleView.ViewMaximum + 1;

            graph2UpperFrequency = graph2LowerFrequency + max * graph2BinFreqInc;
            graph2LowerFrequency = graph2LowerFrequency + min * graph2BinFreqInc;


            if (series1BinData != null)
            {
                GraphDataForRange(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
            }

            if (series2BinData != null)
            {
                GraphDataForRange(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
            }

            if (series1BinData != null && series2BinData != null)
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            if (graph2FrequencyRanges.Count > 0)
            {
                Utilities.FrequencyRange frequencyRange = graph2FrequencyRanges.Pop();

                long lowerIndex;
                long upperIndex;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                lowerIndex = (long)((frequencyRange.lower - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
                upperIndex = (long)((frequencyRange.upper - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);

                graph2LowerFrequency = frequencyRange.lower;
                graph2UpperFrequency = frequencyRange.upper;

                if (series1BinData != null)
                    RangeChanged(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, lowerIndex, upperIndex, graph2LowerFrequency, ref graph2BinFreqInc);

                if (series2BinData != null)
                    RangeChanged(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, lowerIndex, upperIndex, graph2LowerFrequency, ref graph2BinFreqInc);

                if (series1BinData != null && series2BinData != null)
                    GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset();

                chart2.Refresh();
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

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            if (currentBufferFramesObject != zoomedOutBufferObject)
                analyzingNearFarTransitions = true;
            else
                analyzingNearFarTransitions = false;

            averageCurrentFrameStrength = 0;

            averageTotalFramesStrength = 0;

            long currentTime = (Environment.TickCount & int.MaxValue);

            long lowerIndex;
            long upperIndex;

            int i = 0;

            lowerIndex = (long)((currentBufferFramesObject.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
            upperIndex = (long)((currentBufferFramesObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);

            if ((currentBufferFramesObject.bufferFrames.bufferFramesArray.Count > BufferFrames.MIN_BUFFER_SIZE && currentTime - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.startBufferIndex].time > BufferFrames.BUFFER_TIME_LENGTH) || currentBufferFramesObject.bufferFrames.bufferFilled)
            {
                currentBufferFramesObject.bufferFrames.bufferFilled = true;

                currentBufferFramesObject.bufferFrames.currentBufferIndex = currentBufferFramesObject.bufferFrames.startBufferIndex;


                BinData targetBinData = null;

                if (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode == BinDataMode.Far)
                {
                    targetBinData = series1BinData;
                }
                else
                    if (currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode == BinDataMode.Near)
                {
                    targetBinData = series2BinData;

                    /*////this.Invoke(new Action(() =>
                    {
                        listBox3.Items.Add("series2BinData totalBinArrayNumberOfFrames[]++\n");
                    }));*/
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

                    targetBinData.bufferFrames--;

                    /*////if (checkBox11.Checked && !analyzingNearFarTransitions && series1BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS && series2BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS)
                    {
                        if ((recordingSeries1 && (Environment.TickCount & int.MaxValue) - recordingSeries1Start > BufferFrames.TIME_DELAY_BEFORE_ZOOMING || (recordingSeries2 && (Environment.TickCount & int.MaxValue) - recordingSeries2Start > BufferFrames.TIME_DELAY_BEFORE_ZOOMING)))
                        {
                            InitializeZoomToFrequencyThread();
                        }
                    }*/
                }

                if (currentBufferFramesObject.bufferFrames.startBufferIndex + 1 >= currentBufferFramesObject.bufferFrames.bufferFramesArray.Count)
                    currentBufferFramesObject.bufferFrames.startBufferIndex = 0;
                else
                    currentBufferFramesObject.bufferFrames.startBufferIndex++;
            }
            else
                currentBufferFramesObject.bufferFrames.currentBufferIndex++;


            if (checkBox9.Checked && !analyzingNearFarTransitions)//// && series1BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS && series2BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS)
            {
                if (series1BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS && series2BinData.GetAverageNumberOfFrames() >= REQUIRED_FRAMES_BEFORE_ANALYZING_TRANSITIONS)
                {
                    long delay;

                    if (transitionAnalysesMode)
                        delay = BufferFrames.TRANSITIONS_ANALYSES_MODE_TIME_DELAY_BEFORE_ZOOMING;
                    else
                    if (recordingSeries1 && ANALYZING_TRANSITIONS_STAGE_REACHED && analyzingTransitionsBeforeSuccessCount > 0)
                        delay = BufferFrames.TIME_DELAY_BEFORE_ZOOMING;
                    else
                        delay = BufferFrames.TIME_DELAY_BEFORE_ZOOMING_BEFORE_ANALYZING_TRANSITIONS;

                    automatedZooming = true;

                    int positiveResult = (Environment.TickCount & int.MaxValue) & int.MaxValue;

                    ////if ((transitionAnalysesMode && (zoomedOutBufferObject == currentBufferFramesObject)) || (recordingSeries1 && (Environment.TickCount & int.MaxValue) - recordingSeries1Start > delay || (recordingSeries2 && (Environment.TickCount & int.MaxValue) - recordingSeries2Start > delay)))
                    if (recordingSeries1 && recordingSeries1Start > -1 && (Environment.TickCount & int.MaxValue) - recordingSeries1Start > delay || (recordingSeries2 && recordingSeries2Start > -1 && (Environment.TickCount & int.MaxValue) - recordingSeries2Start > delay))
                    {
                        InitializeZoomToFrequencyThread();
                    }
                }
            }

            if (currentBufferFramesObject.bufferFrames.currentBufferIndex >= currentBufferFramesObject.bufferFrames.bufferFramesArray.Count)
            {
                currentBufferFramesObject.bufferFrames.bufferFramesArray.Add(new BufferFrame(totalBinCount, currentMode));
            }
            else
            {
                /*////this.Invoke(new Action(() =>
                {
                    listBox3.Items.Add("buffer mode modified from: " + currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode + " to: " + currentMode + "\n");
                }));*/

                currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].mode = currentMode;
            }

            currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].time = currentTime;

            if (!checkBox7.Checked)
            {
                uint scanCount = 1;

                NativeMethods.GetBins(currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray, deviceIndex, rangeSamplingPercentage, PROXIMITRY_DETECTOR, scanCount);

                i = 0;

                float value;

                for (long j = lowerIndex; j < upperIndex; j++)
                {
                    value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i];

                    if (Double.IsNaN(value) || value < 0)
                    {
                        if (i > 0)
                            value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i - 1];
                        else
                            if (i < binData.size - 1)
                            value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i + 1];
                    }

                    if (Double.IsNaN(value) || value < 0)
                    {
                        value = 0;
                    }

                    binData.binArray[j] = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i] = value;

                    i++;
                }

                proximitryFrequency.sampleCount++;
            }
            else
            {
                NativeMethods.GetBinsForDevices(binData.device1BinArray, binData.device2BinArray, 0, 1);
                

                for (int j = 0; j < currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray.Length; j++)
                {
                    currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[j] = binData.device1BinArray[j] / binData.device2BinArray[j];
                }


                i = 0;

                float value;

                for (long j = lowerIndex; j < upperIndex; j++)
                {
                    value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i];

                    if (Double.IsNaN(value) || value < 0)
                    {
                        if (i > 0)
                            value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i - 1];
                        else
                            if (i < binData.size - 1)
                            value = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i + 1];
                    }

                    if (Double.IsNaN(value) || value < 0)
                    {
                        value = 0;
                    }

                    binData.binArray[j] = currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i] = value;

                    i++;
                }
            }


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
            uint indeterminateFrames2 = currentBufferFramesObject.bufferFrames.AddBufferRangeIntoArray(totalBinBufferArray, BinDataMode.Indeterminate);
            binData.bufferFrames += indeterminateFrames2;

            i = 0;

            for (long j = lowerIndex; j < upperIndex; j++)
            {
                binData.avgBinArray[j] = (binData.totalBinArray[j] + totalBinBufferArray[i]) / (binData.totalBinArrayNumberOfFrames[j] + binData.bufferFrames);

                averageTotalFramesStrength += binData.avgBinArray[j];

                ////currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].bufferArray[i] = (series1BinData.totalBinArray[j] + totalBinBufferArray[i]) / (series1BinData.totalBinArrayNumberOfFrames[j] + binData.bufferFrames);

                i++;
            }

            averageCurrentFrameStrength /= binData.size;
            averageTotalFramesStrength /= binData.size;

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
            if (binData.GetAverageNumberOfFrames() + binData.bufferFrames > 0)
            {
                try
                {
                    GraphDataForRange(chart1, binData.dataSeries, binData.binArray, graph1LowerFrequency, graph1UpperFrequency, graph1BinFreqInc);
                }
                catch
                {
                }
            }

            if (binData.GetAverageNumberOfFrames() + binData.bufferFrames > 0)
            {
                try
                {
                    GraphDataForRange(chart2, binData.dataSeries, binData.avgBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
                }
                catch
                {
                }
            }

            if (resetGraph && newData)
            {
                difBinArray = new float[totalBinCount];

                resetGraph = false;
                newData = false;
            }
        }

        private void GraphDifferenceOrNearFarTransitionRatios(BinData series1BinData, BinData series2BinData)
        {
            GraphDifference(series1BinData, series2BinData);

            if (checkBox12.Checked)
                GraphNearFarTransitionRatios();
        }

        private void GraphNearFarTransitionRatios()
        {
            if (transitionGradientArray != null && transitionGradientArray.array.Count > 0)
            {
                if (radioButton2.Checked)
                    chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                else
                    chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                chart2.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

                chart2.ChartAreas[0].AxisY2.Minimum = 100;

                chart2.Series["Strength Difference"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;

                transitionGradientArray.SortAccordingToFrequency();


                series2Max = -99999999;

                for (int i = 0; i < series2BinData.avgBinArray.Length; i++)
                {
                    if (series2BinData.avgBinArray[i] > series2Max)
                        series2Max = series2BinData.avgBinArray[i];
                }

                long frequency;

                float[] ratioBinArray = new float[series1BinData.totalBinArray.Length];

                for (int i = 0; i < series1BinData.totalBinArray.Length; i++)
                {
                    ratioBinArray[i] = 100;
                }


                long lowerIndex;
                long upperIndex;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);


                lowerIndex = (long)((graph2LowerFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
                upperIndex = (long)((graph2UpperFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);



                TransitionGradient transitionGradient;
                for (long i = lowerIndex; i < upperIndex; i++)
                {
                    frequency = (uint)(zoomedOutBufferObject.lowerFrequency + (i * binSize));

                    transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(frequency, binSize / 10);

                    if (transitionGradient != null && transitionGradient.strength > BufferFrames.MIN_NEAR_FAR_PERCENTAGE_FOR_RERADIATED_FREQUENCY)
                    {
                        ratioBinArray[i] = (float)(transitionGradient.strength);
                    }
                }

                transitionGradientArray.Sort();

                ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions) && checkBox12.Checked)
                if (ShowGraphs())
                    GraphDataForRange(chart2, "Strength Difference", ratioBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
                else
                    chart2.Series["Strength Difference"].Points.Clear();
            }
        }

        public void CheckForStartRecordingNearSeries()
        {
            if (UsingProximitryDetection())
            {
                if (recordingSeries1 && GUIInput.lastInputTime - recordingSeries1Start > GUIInput.AFTER_RECORD_FAR_INPUT_BUFFER)
                {
                    currentBufferFramesObject.transitionBufferFrames.nearIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex - 1;

                    if (currentBufferFramesObject.transitionBufferFrames.nearIndex < 0)
                        currentBufferFramesObject.transitionBufferFrames.nearIndex = currentBufferFramesObject.transitionBufferFrames.bufferFramesArray.Count - 1;

                    commandQueue.AddCommand("RecordSeries2");

                    EnableShowGraphButtons(true);

                    StopRecording();
                }
            }
        }


        public void AddFrequencyToReradiatedFrequencies(int index, double percentageIncrease)
        {
            long frequency;
            if (checkBox14.Checked)
            {
                reradiatedFrequencies.Add(leaderBoardRanges[index]);

                frequency = (long)leaderBoardRanges[index].frequency;
            }
            else
            {
                reradiatedFrequencies.Add(leaderBoardSignals[index]);

                frequency = (long)leaderBoardSignals[index].frequency;
            }

            if (checkBox14.Checked)
                reradiatedFrequencies[reradiatedFrequencies.Count - 1].rating = reradiatedFrequencies[reradiatedFrequencies.Count - 1].rangeTotal * (percentageIncrease / 100);

            /*////reradiatedFrequencies.Sort(delegate (InterestingSignal x, InterestingSignal y)
            {
                if (x.rating < y.rating)
                    return 1;
                else if (x.rating == y.rating)
                    return 0;
                else
                    return -1;
            });*/

            Utilities.FrequencyRange frequencyRange, existingFrequencyRange;

            frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);

            bool rangeInExistingFrequencyRanges = false;

            listBox6.Items.Clear();

            for (int i = 0; i < reradiatedFrequencies.Count; i++)
            {
                existingFrequencyRange = Utilities.GetFrequencyRangeFromFrequency((long)reradiatedFrequencies[i].frequency);

                if (i != reradiatedFrequencies.Count - 1)
                    if (frequencyRange.lower == existingFrequencyRange.lower && frequencyRange.upper == existingFrequencyRange.upper)
                        rangeInExistingFrequencyRanges = true;

                if (checkBox14.Checked)
                {
                    ////listBox6.Items.Add(Utilities.GetFrequencyString(frequencyRange.lower) + " to " + Utilities.GetFrequencyString(frequencyRange.upper) + ": " + ((long)reradiatedFrequencies[i].rating).ToString());
                    listBox6.Items.Add(Utilities.GetFrequencyString(existingFrequencyRange.lower) + " to " + Utilities.GetFrequencyString(existingFrequencyRange.upper));
                }
                else
                {
                    ////listBox6.Items.Add(Utilities.GetFrequencyString(reradiatedFrequencies[i].frequency) + ": " + ((long)reradiatedFrequencies[i].rating).ToString());
                    listBox6.Items.Add(Utilities.GetFrequencyString(reradiatedFrequencies[i].frequency));
                }
            }

            if (!rangeInExistingFrequencyRanges)
            {
                if (checkBox14.Checked)
                    index = reradiatedFrequencies.FindIndex(x => Math.Abs(x.frequency - leaderBoardRanges[index].frequency) < 10000);
                else
                    index = reradiatedFrequencies.FindIndex(x => Math.Abs(x.frequency - leaderBoardSignals[index].frequency) < 10000);

                string msg;

                if (reradiatedFrequencies.Count == 1)
                    msg = "A frequency has been detected that could be reradiated from you, would you like to do a user analysis of this range?";
                else
                    msg = "A new frequency has been detected that could be reradiated from you, would you like to do a user analysis of this range?";

                DialogResult dialogResult = MessageBox.Show(msg, "Possible reradiated frequency range detected", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    StopRecording();
                    
                    originalUserSettingShowGraphs = showGraphs;
                    showGraphs = false;
                    checkBox8.Checked = checkBox13.Checked = false;

                    resumeRecording = true;

                    ////commandQueue.AddCommand("AnalyzeLeaderboardFrequency:" + reradiatedFrequency);
                    commandQueue.AddCommand("AnalyzeLeaderboardFrequency:" + index);
                }
            }
        }

        public void CheckForReradiatedFrequency()
        {
            this.Invoke(new Action(() =>
            {
                showingCheckForReradiatedFrequencyDialog = true;

                if (listBox2.Items.Count > 0)
                {
                    for (int i = 0; i < listBox2.Items.Count; i++)
                    {
                        string itemStr = (string)listBox2.Items[i];

                        string[] itemStrArray = itemStr.Split(':');

                        long reradiatedFrequency;

                        string[] itemStrArray2 = itemStrArray[1].Split(' ');

                        double percentageIncrease = double.Parse(itemStrArray2[1].Substring(0, itemStrArray2[1].Length - 1));

                        if (percentageIncrease > BufferFrames.minStrengthForRankings[1])
                        {
                            if (itemStrArray[0].IndexOf("to") > -1)
                            {
                                Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromString(itemStrArray[0]);

                                reradiatedFrequency = (long)(frequencyRange.lower + frequencyRange.upper) / 2;
                            }
                            else
                            {
                                itemStrArray[0] = itemStrArray[0].Substring(0, itemStrArray[0].Length - 3);

                                reradiatedFrequency = (long)(double.Parse(itemStrArray[0]) * 1000000);

                            }

                            itemStrArray2 = itemStrArray[1].Split(' ');
                            uint transitions = uint.Parse(itemStrArray2[2]);

                            BufferFramesObject bufferFramesObjectForReradiatedFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency(reradiatedFrequency);

                            if (bufferFramesObjectForReradiatedFrequency != null)
                            {
                                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                                long farFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, bufferFramesObjectForReradiatedFrequency.lowerFrequency, bufferFramesObjectForReradiatedFrequency.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);
                                long nearFrames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, bufferFramesObjectForReradiatedFrequency.lowerFrequency, bufferFramesObjectForReradiatedFrequency.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                ////if (farFrames - userAnalysisCheckFarFrames > REQUIRED_FRAMES_BEFORE_USER_ANALYSIS && nearFrames - userAnalysisCheckNearFrames > REQUIRED_FRAMES_BEFORE_USER_ANALYSIS && listBox2.Items.Count > 0)
                                {
                                    if (transitions >= REQUIRED_TRANSITIONS_BEFORE_USER_ANALYSIS)
                                    {
                                        /*////int interestingSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, STRONGEST_INTERESTING_AND_LEADERBOARD_SIGNALS_FOR_TRANSITIONS, interestingSignals);
                                        int leaderboardSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, STRONGEST_INTERESTING_AND_LEADERBOARD_SIGNALS_FOR_TRANSITIONS, leaderBoardSignals);

                                        if (interestingSignalIndex > -1 && leaderboardSignalIndex > -1)
                                        */

                                        if (bufferFramesArray.EvaluateWhetherReradiatedFrequency(reradiatedFrequency))
                                        {
                                            int interestingSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, MAX_INTERESTING_SIGNAL_LIST_COUNT, interestingSignals, checkBox14.Checked, 100000);

                                            int leaderboardSignalIndex;
                                            if (checkBox14.Checked)
                                                leaderboardSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, MAX_LEADER_BOARD_LIST_COUNT, leaderBoardRanges, checkBox14.Checked, 100000);
                                            else
                                                leaderboardSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, MAX_LEADER_BOARD_LIST_COUNT, leaderBoardSignals, checkBox14.Checked, 100000);

                                            if (leaderboardSignalIndex > -1)
                                            {
                                                int reradiatedSignalIndex = Utilities.FrequencyInSignals(reradiatedFrequency, reradiatedFrequencies.Count, reradiatedFrequencies, checkBox14.Checked, 100000);

                                                if (reradiatedSignalIndex == -1)
                                                {
                                                    userAnalysisCheckFarFrames = farFrames;
                                                    userAnalysisCheckNearFrames = nearFrames;

                                                    AddFrequencyToReradiatedFrequencies(leaderboardSignalIndex, percentageIncrease);

                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                showingCheckForReradiatedFrequencyDialog = false;
            }));
        }

        private void GraphDifference(BinData series1BinData, BinData series2BinData)
        {
            if (series1BinData != null && series2BinData != null && series1BinData.GetAverageNumberOfFrames() > 0 && series2BinData.GetAverageNumberOfFrames() > 0 && series1BinData.size == series2BinData.size)
            {
                if (radioButton2.Checked)
                    chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                else
                    chart2.Series["Strength Difference"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                double dif = 0, prevDif;

                int i = 0;

                interestingSignals.Clear();
                interestingSignalsForAnalysis.Clear();
                ////leaderBoardSignals.Clear();

                uint frequency;

                series2Max = -99999999;

                for (i = 0; i < totalBinCount; i++)
                {
                    if (series2BinData.avgBinArray[i] > series2Max)
                        series2Max = series2BinData.avgBinArray[i];
                }

                int inc = (int)(BufferFrames.FREQUENCY_SEGMENT_SIZE / mainForm.binSize);

                long segmentEnd;

                double maxSignalStrength = Double.NaN;
                int maxIndex;

                uint maxFrequency = 0;

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                ////for (i = 0; i < totalBinCount; i++)
                for (i = (int)frequencyRange.lower; i < frequencyRange.upper; i += inc)
                {
                    maxSignalStrength = Double.NaN;
                    maxIndex = -1;

                    segmentEnd = i + inc;

                    for (long j = i; j < segmentEnd && j < series1BinData.avgBinArray.Length && j < series2BinData.avgBinArray.Length; j++)
                    {
                        frequency = (uint)(zoomedOutBufferObject.lowerFrequency + (j * zoomedOutBufferObject.binSize));

                        dif = Waterfall.CalculateStrengthDifference(series1BinData.avgBinArray, series2BinData.avgBinArray, j);

                        ////absDif = dif;

                        if (!checkBox1.Checked || maxSignalStrength >= difThreshold)
                        {
                            difBinArray[j] = (float)dif;
                        }
                        else
                            difBinArray[j] = -99999999;

                        if (dif > maxSignalStrength || Double.IsNaN(maxSignalStrength))
                        {
                            maxSignalStrength = dif;
                            maxIndex = (int)j;

                            maxFrequency = frequency;
                        }
                    }

                    prevDif = maxSignalStrength;

                    ////if (dif != (float) prevDif)
                    {
                        interestingSignals.Add(new InterestingSignal(maxIndex, series2BinData.avgBinArray[maxIndex], maxSignalStrength, maxFrequency));

                        interestingSignals[interestingSignals.Count - 1].maxAvgStrength = series2BinData.avgBinArray[maxIndex];
                        interestingSignals[interestingSignals.Count - 1].minAvgStrength = series1BinData.avgBinArray[maxIndex];

                        if (maxSignalStrength < 0)
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

                BufferFramesObject bufferFramesObject;

                for (int j = interestingSignals.Count - 1; j >= 0; j--)
                {
                    bufferFramesObject = bufferFramesArray.GetBufferFramesObjectForFrequency((long)interestingSignals[j].frequency);

                    if (bufferFramesObject != null && !bufferFramesObject.possibleReradiatedFrequencyRange)
                       interestingSignals.RemoveAt(j);
                }

                listBox1.Items.Clear();

                string frequencyString;

                long prevFrequency = -1;
                double prevStrengthDif = Double.NaN;

                for (i = 0; i < interestingSignals.Count; i++)
                {
                    if (interestingSignalsForAnalysis.Count < MAX_LEADER_BOARD_LIST_COUNT)
                    {
                        if (Math.Abs(interestingSignals[i].frequency - prevFrequency) > 100000 || interestingSignals[i].strengthDif > prevStrengthDif || prevFrequency == -1)
                        {
                            frequencyString = Utilities.GetFrequencyString(interestingSignals[i].frequency);

                            interestingSignalsForAnalysis.Add(interestingSignals[i]);

                            if (!analyzingNearFarTransitions)
                            {
                                int leaderBoardSignalIndex = leaderBoardSignals.FindIndex(x => Math.Abs(x.frequency - interestingSignals[i].frequency) < 10000);

                                if (leaderBoardSignalIndex == -1)
                                {
                                    interestingSignals[i].rating = ((interestingSignals.Count - i) * 10) - interestingSignals.Count / 2;
                                    ////interestingSignals[i].rating = ((interestingSignals.Count - i) * 10);

                                    leaderBoardSignals.Add(interestingSignals[i]);
                                }
                                else
                                {
                                    leaderBoardSignals[leaderBoardSignalIndex].rating += ((interestingSignals.Count - i) * 10) - interestingSignals.Count / 2;
                                    ////leaderBoardSignals[leaderBoardSignalIndex].rating += ((interestingSignals.Count - i) * 10);
                                }
                            }

                            if (listBox1.Items.Count < MAX_INTERESTING_SIGNAL_LIST_BOX_COUNT)
                                listBox1.Items.Add(Utilities.GetFrequencyString(interestingSignals[i].frequency) + ": " + Math.Round(interestingSignals[i].strengthDif, 2) + "%");

                            prevFrequency = (long)interestingSignals[i].frequency;
                            prevStrengthDif = interestingSignals[i].strengthDif;
                        }
                    }
                }

                if (!analyzingNearFarTransitions && automatedZooming)
                {
                    leaderBoardSignals.Sort(delegate (InterestingSignal x, InterestingSignal y)
                {
                    if (x.rating < y.rating)
                        return 1;
                    else if (x.rating == y.rating)
                        return 0;
                    else
                        return -1;
                });

                    if (leaderBoardSignals.Count > MAX_LEADER_BOARD_LIST_COUNT)
                    {
                        for (int j = leaderBoardSignals.Count - 1; j >= MAX_LEADER_BOARD_LIST_COUNT; j--)
                        {
                            leaderBoardSignals.RemoveAt(j);
                        }
                    }

                    listBox4.Items.Clear();

                    for (i = 0; i < leaderBoardSignals.Count; i++)
                    {
                        if (listBox4.Items.Count < MAX_LEADER_BOARD_LIST_COUNT)
                        {
                            listBox4.Items.Add(Utilities.GetFrequencyString(leaderBoardSignals[i].frequency) + ": " + Math.Round(leaderBoardSignals[i].rating, 2));
                        }
                    }


                    leaderBoardRanges = SignalDataUtilities.CreateRangeList(leaderBoardSignals);


                    leaderBoardRanges.Sort(delegate (InterestingSignal x, InterestingSignal y)
                    {
                        if (x == null)
                            return 1;

                        if (x.rangeTotal < y.rangeTotal)
                            return 1;
                        else if (x.rangeTotal == y.rangeTotal)
                            return 0;
                        else
                            return -1;
                    });

                    if (leaderBoardRanges.Count > MAX_LEADER_BOARD_LIST_COUNT)
                    {
                        for (int j = leaderBoardRanges.Count - 1; j >= MAX_LEADER_BOARD_LIST_COUNT; j--)
                        {
                            leaderBoardRanges.RemoveAt(j);
                        }
                    }

                    listBox5.Items.Clear();

                    for (i = 0; i < leaderBoardRanges.Count; i++)
                    {
                        if (listBox5.Items.Count < MAX_LEADER_BOARD_LIST_COUNT)
                        {
                            listBox5.Items.Add(Utilities.GetFrequencyString(leaderBoardRanges[i].lowerFrequency) + " to " + Utilities.GetFrequencyString(leaderBoardRanges[i].upperFrequency) + ": " + Math.Round(leaderBoardRanges[i].rangeTotal, 2));
                        }
                    }
                }

                ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions) && checkBox10.Checked)
                if (ShowGraphs())
                    GraphDataForRange(chart2, "Strength Difference", difBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
                else
                    chart2.Series["Strength Difference"].Points.Clear();
            }
        }

        private void GraphTransitionData(TransitionGradient transitionGradient)
        {
            if (transitionGradient != null)
            {
                BufferFramesObject bufferFramesObjectContainingStrongestTransitionGradientFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency(transitionGradient.frequency);

                double[] transitionStrengthOverTime;

                if (transitionGradient.rangeWidth > 1)
                {
                    transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetStrengthOverTimeForRange(transitionGradient.index - transitionGradient.rangeWidth, transitionGradient.index + transitionGradient.rangeWidth);
                }
                else
                    transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetStrengthOverTimeForIndex(transitionGradient.index);

                if (transitionStrengthOverTime != null)
                {
                    if (transitionGradient.rangeWidth > 1)
                    {
                        textBox17.Text = Utilities.GetFrequencyString(Utilities.GetFrequencyFromIndex(transitionGradient.index - transitionGradient.rangeWidth, bufferFramesObjectContainingStrongestTransitionGradientFrequency.lowerFrequency, bufferFramesObjectContainingStrongestTransitionGradientFrequency.binSize)) + " to " + Utilities.GetFrequencyString(Utilities.GetFrequencyFromIndex(transitionGradient.index + transitionGradient.rangeWidth, bufferFramesObjectContainingStrongestTransitionGradientFrequency.lowerFrequency, bufferFramesObjectContainingStrongestTransitionGradientFrequency.binSize));
                    }
                    else
                        textBox17.Text = Utilities.GetFrequencyString(transitionGradient.frequency);


                    userAnalysisForm.textBox17.Text = textBox17.Text;

                    textBox15.Text = transitionGradient.transitions.ToString();

                    userAnalysisForm.textBox15.Text = textBox15.Text;


                    /*////BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                    long frequencyIndex = (long)((transitionGradient.frequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);                    

                    series1BinData.avgBinArray[frequencyIndex]
                    */

                    Gradient gradient = SignalDataUtilities.SeriesTransitionGradient(transitionStrengthOverTime, Gradient.divisionsCount);


                    /*////double transitionAvgStrength = SignalDataUtilities.SeriesAvgStrength(transitionStrengthOverTime);

                    double gradientVsStrength = Math.Round((gradient.CalculateTransitionGradient() / transitionAvgStrength)*100, 2);


                    gradientVsStrength += 100;

                    this.mainForm.textBox18.Text = gradientVsStrength.ToString() + "%";
                    */

                    double gradientValue = Math.Round(gradient.CalculateTransitionGradient(), 2);

                    gradient.strength = gradientValue;

                    this.mainForm.textBox18.Text = gradientValue.ToString();

                    userAnalysisForm.textBox18.Text = textBox18.Text;

                    System.Windows.Forms.DataVisualization.Charting.Chart chart;

                    /*////if (userAnalysisForm.Visible)
                    {
                        chart = userAnalysisForm.chart8;                    
                    }
                    else
                    {
                        chart = chart8;                    
                    }*/

                    ////currentBufferFramesObject.transitionBufferFrames.GraphData(chart, transitionStrengthOverTime);

                    currentBufferFramesObject.transitionBufferFrames.GraphData(chart8, transitionStrengthOverTime);
                    currentBufferFramesObject.transitionBufferFrames.GraphData(userAnalysisForm.chart8, transitionStrengthOverTime);


                    currentBufferFramesObject.transitionBufferFrames.GraphData(chart8, gradient.divisions, "Series2", false);
                    currentBufferFramesObject.transitionBufferFrames.GraphData(userAnalysisForm.chart8, gradient.divisions, "Series2", false);

                    /////////chart8.ChartAreas[0].AxisY2.Interval = gradientValue;


                    /*////chart8.ChartAreas[0].AxisY2.Interval = gradientValue * 9/10;
                    userAnalysisForm.chart8.ChartAreas[0].AxisY2.Interval = gradientValue * 9 / 10;
                    */

                    if (transitionGradient.rangeWidth > 1)
                    {
                        transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetAveragedStrengthOverTimeForRange(transitionGradient.index - transitionGradient.rangeWidth, transitionGradient.index + transitionGradient.rangeWidth);
                    }
                    else
                        transitionStrengthOverTime = bufferFramesObjectContainingStrongestTransitionGradientFrequency.transitionBufferFrames.GetAveragedStrengthOverTimeForIndex(transitionGradient.index);

                    if (transitionStrengthOverTime != null)
                    {
                        ////this.mainForm.textBox16.Text = SignalDataUtilities.SeriesTransitionGradient(transitionStrengthOverTime).ToString() + "%";

                        ////gradient = SignalDataUtilities.SeriesTransitionGradient(transitionStrengthOverTime);
                        ////this.mainForm.textBox16.Text = gradient.CalculateTransitionGradient().ToString() + "%";                

                        gradient = SignalDataUtilities.SeriesTransitionGradient(transitionStrengthOverTime, Gradient.divisionsCount);

                        /*////transitionAvgStrength = SignalDataUtilities.SeriesAvgStrength(transitionStrengthOverTime);

                        gradientVsStrength = Math.Round((gradient.CalculateTransitionGradient() / transitionAvgStrength) * 100, 2);

                        gradientVsStrength += 100;

                        this.mainForm.textBox16.Text = gradientVsStrength.ToString() + "%";
                        */

                        gradientValue = Math.Round(gradient.CalculateTransitionGradient(), 2);

                        gradient.strength = gradientValue;

                        this.mainForm.textBox16.Text = gradientValue.ToString();


                        userAnalysisForm.textBox16.Text = textBox16.Text;

                        currentBufferFramesObject.transitionBufferFrames.GraphData(chart5, transitionStrengthOverTime);
                        currentBufferFramesObject.transitionBufferFrames.GraphData(userAnalysisForm.chart5, transitionStrengthOverTime);

                        currentBufferFramesObject.transitionBufferFrames.GraphData(chart5, gradient.divisions, "Series2", false);
                        currentBufferFramesObject.transitionBufferFrames.GraphData(userAnalysisForm.chart5, gradient.divisions, "Series2", false);

                        /*////chart5.ChartAreas[0].AxisY2.Interval = gradientValue * 9 / 10;
                        userAnalysisForm.chart5.ChartAreas[0].AxisY2.Interval = gradientValue * 9 / 10;
                        */
                    }
                }
            }
        }

        private void AddGradientPoint(System.Windows.Forms.DataVisualization.Charting.Chart chart, TextBox textBox, double gradientValue, bool show)
        {
            if (ShowGraphs() || showUserAnalaysisGraphs)
            {
                try
                {
                    textBox.Text = Math.Round(gradientValue, 2).ToString();

                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(chart.Series["Series"].Points.Count, gradientValue);

                    if (chart.Series["Series"].Points.Count > MAXIMUM_TIME_BASED_GRAPH_POINTS)
                    {
                        chart.Series["Series"].Points.RemoveAt(0);

                        for (int j = 0; j < chart.Series["Series"].Points.Count; j++)
                        {
                            chart.Series["Series"].Points[j].XValue--;
                        }
                    }

                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                    if (show)
                        chart.Series["Series"].Points.Add(graphPoint);

                    if (chart == chart5)
                    {
                        chart.ChartAreas[0].AxisY.Maximum = 4;
                        chart.ChartAreas[0].AxisY.Minimum = 0;
                    }

                    double totalAvg = 0;
                    double avg = 0;


                    double minY = 99999999;
                    double maxY = -99999999;

                    for (int j = 0; j < chart.Series["Series"].Points.Count; j++)
                    {
                        if (chart.Series["Series"].Points[j].YValues[0] < minY)
                            minY = chart.Series["Series"].Points[j].YValues[0];

                        if (chart.Series["Series"].Points[j].YValues[0] > maxY)
                            maxY = chart.Series["Series"].Points[j].YValues[0];

                        totalAvg += chart.Series["Series"].Points[j].YValues[0];
                    }

                    if (minY == maxY)
                    {
                        maxY++;
                        minY--;
                    }

                    if (chart.Series["Series"].Points.Count > 0)
                    {
                        chart.ChartAreas[0].AxisY.Maximum = maxY;
                        chart.ChartAreas[0].AxisY.Minimum = minY;
                    }

                    avg = totalAvg / chart.Series["Series"].Points.Count;


                    if (checkBox6.Checked && chart == chart4)
                    {
                        double avgGraphStrengthChange = chart4.Series["Series"].Points[chart4.Series["Series"].Points.Count - 1].YValues[0] - chart4.Series["Series"].Points[chart4.Series["Series"].Points.Count - 2].YValues[0];

                        double strengthVSAvg = chart4.Series["Series"].Points[chart4.Series["Series"].Points.Count - 1].YValues[0] - avg;

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
        }

        private void AddPointToTimeBasedGraph(System.Windows.Forms.DataVisualization.Charting.Chart strengthChart, System.Windows.Forms.DataVisualization.Charting.Chart gradientChart, float value, bool show = true)
        {
            System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(strengthChart.Series["Series"].Points.Count, value);

            double minY = 999999999999;
            double maxY = -999999999999;

            double totalAvg = 0;
            double avg = 0;

            int minMaxStart = 0;

            for (int j = minMaxStart; j < strengthChart.Series["Series"].Points.Count; j++)
            {
                if (strengthChart.Series["Series"].Points[j].YValues[0] < minY)
                    minY = strengthChart.Series["Series"].Points[j].YValues[0];

                if (strengthChart.Series["Series"].Points[j].YValues[0] > maxY)
                    maxY = strengthChart.Series["Series"].Points[j].YValues[0];

                totalAvg += strengthChart.Series["Series"].Points[j].YValues[0];
            }

            if (minY == maxY)
            {
                maxY++;
                minY--;
            }

            if (strengthChart.Series["Series"].Points.Count > 0)
            {
                strengthChart.ChartAreas[0].AxisY.Maximum = maxY;
                strengthChart.ChartAreas[0].AxisY.Minimum = minY;
            }

            avg = totalAvg / strengthChart.Series["Series"].Points.Count;


            if (strengthChart.Series["Series"].Points.Count > MAXIMUM_TIME_BASED_GRAPH_POINTS)
            {
                strengthChart.Series["Series"].Points.RemoveAt(0);

                for (int j = 0; j < strengthChart.Series["Series"].Points.Count; j++)
                {
                    strengthChart.Series["Series"].Points[j].XValue--;
                }
            }

            ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions) || show)
            if ((ShowGraphs() && show) || showUserAnalaysisGraphs)            
                    strengthChart.Series["Series"].Points.Add(graphPoint);


            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint1;
            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint2;
            System.Windows.Forms.DataVisualization.Charting.DataPoint prevPoint3;

            double x1, y1, x2, y2, l1, l2, dotProduct, angle, trajAngle, totalTrajAngle = 0, avgTrajAngle;

            for (int j = 1; j < strengthChart.Series["Series"].Points.Count - 1; j++)
            {
                prevPoint1 = strengthChart.Series["Series"].Points[j - 1];
                prevPoint2 = strengthChart.Series["Series"].Points[j];
                prevPoint3 = strengthChart.Series["Series"].Points[j + 1];


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

            avgTrajAngle = totalTrajAngle / strengthChart.Series["Series"].Points.Count;

            if (checkBox5.Checked && strengthChart.Series["Series"].Points.Count >= 2)
            {
                double avgGraphStrengthChange = strengthChart.Series["Series"].Points[strengthChart.Series["Series"].Points.Count - 1].YValues[0] - strengthChart.Series["Series"].Points[strengthChart.Series["Series"].Points.Count - 2].YValues[0];

                double strengthVSAvg = strengthChart.Series["Series"].Points[strengthChart.Series["Series"].Points.Count - 1].YValues[0] - avg;

                double graphExtent = strengthChart.ChartAreas[0].AxisY.Maximum - strengthChart.ChartAreas[0].AxisY.Minimum;

                int soundFrequency = (int)(strengthVSAvg / (graphExtent * 10) * Sound.SOUND_FREQUENCY_MAXIMUM);

                if (soundFrequency > 0)
                {
                    Sound.PlaySound(soundFrequency, 1000);
                    form2.BackColor = Color.Red;
                }
                else
                    form2.BackColor = Color.Blue;
            }


            if (strengthChart.Series["Series"].Points.Count > 1)
            {
                double gradient = 0;
                double totalGradient = 0;
                double avgGradient = 0;

                for (int j = 1; j < strengthChart.Series["Series"].Points.Count; j++)
                {
                    totalGradient += (strengthChart.Series["Series"].Points[j].YValues[0] - strengthChart.Series["Series"].Points[j - 1].YValues[0]);
                }

                avgGradient = totalGradient / (strengthChart.Series["Series"].Points.Count - 1);

                if (strengthChart.Series["Series"].Points.Count > 1)
                {
                    gradient = strengthChart.Series["Series"].Points[strengthChart.Series["Series"].Points.Count - 1].YValues[0] - strengthChart.Series["Series"].Points[strengthChart.Series["Series"].Points.Count - 2].YValues[0];
                }

                AddGradientPoint(gradientChart, textBox12, avgGradient, show);
            }
        }

        private void GraphStrengthToTimeBasedGraph(BinData binData, bool show)
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
                    AddPointToTimeBasedGraph(chart3, chart4, float.Parse(textBox7.Text), show);

                if (recordingSeries2)
                    AddPointToTimeBasedGraph(chart3, chart4, float.Parse(textBox8.Text), show);
            }
        }

        private void GraphAverageStrength(BinData binData, bool show)
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
                System.Windows.Forms.DataVisualization.Charting.Chart chart1, chart2;

                /*////if (userAnalysisForm.Visible)
                {
                    chart1 = userAnalysisForm.chart3;
                    chart2 = userAnalysisForm.chart4;
                }
                else
                {
                    chart1 = chart3;
                    chart2 = chart4;
                }*/


                if (recordingSeries1)
                {
                    ////AddPointToTimeBasedGraph(chart1, chart2, float.Parse(textBox7.Text));

                    AddPointToTimeBasedGraph(chart3, chart4, float.Parse(textBox7.Text));

                    ////if (userAnalysisForm.Focused)
                        AddPointToTimeBasedGraph(userAnalysisForm.chart3, userAnalysisForm.chart4, float.Parse(textBox7.Text), show);
                }

                if (recordingSeries2)
                {
                    ////AddPointToTimeBasedGraph(chart1, chart2, float.Parse(textBox8.Text));

                    AddPointToTimeBasedGraph(chart3, chart4, float.Parse(textBox8.Text));

                    ////if (showUserAnalaysisGraphs)
                        AddPointToTimeBasedGraph(userAnalysisForm.chart3, userAnalysisForm.chart4, float.Parse(textBox8.Text), show);
                }
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

                AddPointToTimeBasedGraph(chart3, chart4, (float)avg);
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

        private void UserSelectedFrequencyForAnalysisExit()
        {
            ZoomOutOfFrequency();

            series1BinData.Restore();
            series2BinData.Restore();

            button24.Enabled = false;
            userAnalysisForm.button1.Enabled = button24.Enabled;

            if (userSelectedFrequencyForAnalysis > -1)
            {

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                BufferFramesObject bufferFramesObjectForReradiatedFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency(userSelectedFrequencyForAnalysis);

                long farFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, bufferFramesObjectForReradiatedFrequency.lowerFrequency, bufferFramesObjectForReradiatedFrequency.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);
                long nearFrames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, bufferFramesObjectForReradiatedFrequency.lowerFrequency, bufferFramesObjectForReradiatedFrequency.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                userAnalysisCheckFarFrames = farFrames;
                userAnalysisCheckNearFrames = nearFrames;
            }

            userAnalysisForm.Hide();
        }

        private void ProcessCommand()
        {
            Command commandData = commandQueue.GetMostRecentCommand();

            if (commandData != null)
            {
                commandQueue.RemoveCommand();

                string[] commandArray = commandData.name.Split(':');

                switch (commandArray[0])
                {
                    case ("RecordSeries1"):
                        RecordSeries1();
                        break;

                    case ("RecordSeries2"):
                        RecordSeries2();
                        break;

                    case ("AutomatedZoomToFrequency"):
                    case ("ZoomToFrequency"):
                        if (commandArray[0] == "AutomatedZoomToFrequency")
                        {
                            SaveData(SESSION_PATH + SESSION_FILE_NAME, series2BinData, series1BinData, bufferFramesArray);

                            if (commandArray.Length > 1)
                            {
                                if (commandArray[1] == "RecordSeries1")
                                    startRecordingSeries1 = true;
                                else
                                    if (commandArray[1] == "RecordSeries2")
                                    startRecordingSeries2 = true;
                            }
                        }

                        NewSettings(false);

                        ////commandQueue.RemoveCommand();
                        break;

                    case ("ExitOnRequiredZoomedFrames"):
                        bool zoomingOut = ZoomOutOfFrequency();

                        if (commandArray.Length > 1)
                        {
                            if (commandArray[1] == "RecordSeries1")
                                startRecordingSeries1 = true;
                            else
                                if (commandArray[1] == "RecordSeries2")
                                startRecordingSeries2 = true;
                        }

                        ////if (!zoomingOut)
                        ////RecordSeries1();

                        SaveData(SESSION_PATH + SESSION_FILE_NAME, series2BinData, series1BinData, bufferFramesArray);

                        break;

                    case ("UserSelectedFrequencyForAnalysisExit"):
                        UserSelectedFrequencyForAnalysisExit();
                        break;

                    case ("AnalyzeCenterFrequency"):
                        bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);

                        userAnalysisForm.AnalyzingCenterFrequency();
                        userAnalysisForm.Show();

                        userAnalysisForm.WindowState = FormWindowState.Maximized;

                        AnalyzeCenterFrequency();
                        break;


                    case ("AnalyzeLeaderboardFrequency"):
                        bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);

                        if (userAnalysisForm.AnalyzingLeaderboardFrequency())
                        {
                            userAnalysisForm.Show();

                            userAnalysisForm.WindowState = FormWindowState.Maximized;

                            currentLeaderboardSignalBeingAnalyzedIndex = int.Parse(commandArray[1]) - 1;
                            AnalyzeLeaderboardFrequency();
                        }
                        break;

                    case ("SaveSessionDataAndCloseForm"):
                        SaveData(SESSION_PATH + SESSION_FILE_NAME, series2BinData, series1BinData, bufferFramesArray);

                        Close();
                        break;
                }

                ////commandQueue.RemoveCommand();
            }
        }

        public void StopRecording()
        {
            recordingSeries1 = false;
            recordingSeries2 = false;
        }

        private void SetButtonsToNotRecordingState()
        {
            /*////if (showGraphs)
                checkBox8.Checked = true;
                */

            button4.Enabled = true;
            button22.Enabled = button23.Enabled = button24.Enabled = button30.Enabled = button4.Enabled;

            ////button25.Enabled = button26.Enabled = button4.Enabled;

            userAnalysisForm.button1.Enabled = button24.Enabled;

            if (automatedZooming)
            {
                button24.Enabled = false;
                userAnalysisForm.button1.Enabled = button24.Enabled;
            }

            button3.Enabled = true;
            button5.Enabled = true;

            button17.Enabled = true;
            button18.Enabled = true;

            userAnalysisForm.button17.Enabled = true;
            userAnalysisForm.button18.Enabled = true;
            userAnalysisForm.button2.Enabled = true;

            button3.Text = "Record Far Series Data";
            button17.Text = "Record Far";

            button5.Text = "Record Near Series Data";
            button18.Text = "Record Near";

            ////userAnalysisForm.button17.Text = "Record Far";
            userAnalysisForm.button18.Text = "Record";

            this.Cursor = Cursors.Arrow;
        }


        private void CheckForNewSignalsAndRefreshData()
        {
            double dif;

            long frequency;

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            for (long j = 0; j < series1BinData.avgBinArray.Length; j++)
            {
                frequency = (uint)(zoomedOutBufferObject.lowerFrequency + (j * zoomedOutBufferObject.binSize));

                dif = Waterfall.CalculateStrengthDifference(series1BinData.avgBinArray, series2BinData.avgBinArray, j);

                if (!Double.IsInfinity(dif) && (dif < 0.01 || dif > 400) && series1BinData.totalBinArrayNumberOfFrames[j] > 10 && series2BinData.totalBinArrayNumberOfFrames[j] > 10)
                {
                    if (recordingSeries2)
                    {
                        series1BinData.totalBinArray[j] = 0.01f;
                        series1BinData.totalBinArrayNumberOfFrames[j] = 1;
                    }
                    else
                        if (recordingSeries1)
                    {
                        series2BinData.totalBinArray[j] = 0.01f;
                        series2BinData.totalBinArrayNumberOfFrames[j] = 1;
                    }
                }
            }
        }



        private int FrequencyInInterestingSignals(long frequency, long strongestRange)
        {
            int closestIndex = -1;
            double minDif = Double.NaN, dif;

            Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);

            for (int i = 0; i < interestingSignalsForAnalysis.Count && i < strongestRange; i++)
            {
                dif = Math.Abs(interestingSignalsForAnalysis[i].frequency - frequency);

                if ((dif < minDif || Double.IsNaN(minDif)) && interestingSignalsForAnalysis[i].frequency >= frequencyRange.lower && interestingSignalsForAnalysis[i].frequency <= frequencyRange.upper)
                {
                    minDif = dif;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private int FrequencyInLeaderboardSignals(long frequency, long strongestRange)
        {
            int closestIndex = -1;
            double minDif = -1;

            Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);

            for (int i = 0; i < leaderBoardSignals.Count && i < strongestRange; i++)
            {
                if (Utilities.Equals(leaderBoardSignals[i].frequency, frequency, 1000000) && leaderBoardSignals[i].frequency >= frequencyRange.lower && leaderBoardSignals[i].frequency <= frequencyRange.upper)
                {
                    minDif = Math.Abs(leaderBoardSignals[i].frequency - frequency);
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private bool FrequencyInInterestingAndLeaderBoardSignals(long frequency, long strongestRange)
        {
            if (Utilities.FrequencyInSignals(frequency, strongestRange, interestingSignals) > -1)
                return true;

            if (Utilities.FrequencyInSignals(frequency, strongestRange, leaderBoardSignals) > -1)
                return true;

            return false;
        }

        private void GraphAndListTransitionData()
        {
            transitionGradientArray = bufferFramesArray.GetStrongestTransitionsFrequencyGradientArray(checkBox14.Checked);

            transitionGradientArray.Sort();

            if (transitionGradientArray.array.Count > 0)
            {
                listBox2.Items.Clear();

                ////int index = -1;

                for (int i = 0; i < transitionGradientArray.array.Count; i++)
                {
                    ////if (transitionGradientArray.array[i]!=null && (FrequencyInInterestingAndLeaderBoardSignals(transitionGradientArray.array[i].frequency, STRONGEST_INTERESTING_AND_LEADERBOARD_SIGNALS_FOR_TRANSITIONS) || transitionGradientArray.array[i].rangeWidth>1))
                    if (transitionGradientArray.array[i] != null)
                    {
                        ////if (Math.Abs(transitionGradientArray.array[i].frequency - 434936500) < 10000)
                        ////index = i;

                        if (transitionGradientArray.array[i].rangeWidth > 1)
                        {
                            BufferFramesObject bufferFramesObjectForFrequency;

                            bufferFramesObjectForFrequency = bufferFramesArray.GetBufferFramesObjectForFrequency((long)transitionGradientArray.array[i].frequency);

                            listBox2.Items.Add(Utilities.GetFrequencyString(Utilities.GetFrequencyFromIndex(transitionGradientArray.array[i].index - transitionGradientArray.array[i].rangeWidth, bufferFramesObjectForFrequency.lowerFrequency, bufferFramesObjectForFrequency.binSize)) + " to " + Utilities.GetFrequencyString(Utilities.GetFrequencyFromIndex(transitionGradientArray.array[i].index + transitionGradientArray.array[i].rangeWidth, bufferFramesObjectForFrequency.lowerFrequency, bufferFramesObjectForFrequency.binSize)) + ": " + Math.Round(transitionGradientArray.array[i].strength, 2) + "% " + transitionGradientArray.array[i].transitions);
                        }
                        else
                            listBox2.Items.Add(Utilities.GetFrequencyString(transitionGradientArray.array[i].frequency) + ": " + Math.Round(transitionGradientArray.array[i].strength, 2) + "% " + transitionGradientArray.array[i].transitions);
                    }
                }


                if (!automatedZooming && userSelectedFrequencyForAnalysis > -1)
                {
                    ////TransitionGradient transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(userSelectedFrequencyForAnalysis, BufferFrames.FREQUENCY_SEGMENT_SIZE);
                    TransitionGradient transitionGradient;

                    if (userAnalysisForm.radioButton1.Checked)
                        transitionGradient = bufferFramesArray.GetTransitionsGradientForFrequency(userSelectedFrequencyForAnalysis);
                    else
                        transitionGradient = bufferFramesArray.GetRangeTransitionsGradientForFrequency(userSelectedFrequencyForAnalysis);


                    if (transitionGradient != null)
                        GraphTransitionData(transitionGradient);
                }
                else
                    GraphTransitionData(transitionGradientArray.array[0]);
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

                recordingSeries1Start = (Environment.TickCount & int.MaxValue);

                ////Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count - 2);


                ////if (!analyzingNearFarTransitions || automatedZooming || checkBox9.Checked || !analyzingUserSelectedFrequency)
                    ////checkBox8.Checked = false;

                if (showGraphs)
                {
                    if (analyzingNearFarTransitions || !automatedZooming || !checkBox9.Checked || analyzingUserSelectedFrequency)
                        EnableShowGraphButtons(true);
                    ////checkBox8.Enabled = checkBox13.Enabled = checkBox15.Enabled = true;
                    else
                        EnableShowGraphButtons(false);
                    ////checkBox8.Enabled = checkBox13.Enabled = checkBox15.Enabled = false;
                }
                else
                    EnableShowGraphButtons(false);
                ////checkBox8.Enabled = checkBox13.Enabled = checkBox15.Enabled = false;


                framesDif = 0;

                recordingSeries2 = false;
                recordingSeries1 = true;

                startRecordingSeries1 = false;

                Command mostRecentCommand = commandBuffer.GetMostRecentCommand();

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

                    bool exitOnRequiredZoomedFrames = false;

                    BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                    long startRecordingFarFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                    long farFrames;

                    while (recordingSeries1)
                    {
                        currentMode = BinDataMode.Far;

                        int totalMagnitude = 0;
                        double avgMagnitude = 0;

                        if (analyzingNearFarTransitions && !analyzingUserSelectedFrequency)
                        {
                            farFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                            ////if (programState == ProgramState.AQUIRING_NEAR_FAR_FRAMES && farFrames - startRecordingFarFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                            if (!transitionAnalysesMode && automatedZooming && farFrames - startRecordingFarFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                            {
                                ////recordingSeries1 = false;

                                ////exitOnRequiredZoomedFrames = true;                                
                                StopRecording();
                                commandQueue.AddCommand("ExitOnRequiredZoomedFrames:RecordSeries1");
                            }
                        }

                        if (recordingSeries1)
                        {
                            RecordData(ref series1BinData, ref averageSeries1CurrentFrameStrength, ref averageSeries1TotalFramesStrength, ref totalMagnitude, ref avgMagnitude, deviceCount - 1);

                            ////CheckForNewSignalsAndRefreshData();

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

                                    long prevFarFrames = long.Parse(textBox5.Text);

                                    ////long farFrames = (long)series1BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize) + currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far);

                                    farFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                    textBox5.Text = (farFrames).ToString();

                                    long prevTextBox6 = long.Parse(textBox6.Text);

                                    /*////prevAvgFramesForRegion = avgFramesForRegion;
                                    prevNearFrames = nearFrames;
                                    prevIndeterminateFrames = indeterminateFrames;

                                    long prevFrames = (long)(prevAvgFramesForRegion + prevNearFrames + prevIndeterminateFrames);

                                    avgFramesForRegion = series2BinData.GetAverageNumberOfFramesForFrequencyRegion(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);
                                    nearFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near);
                                    indeterminateFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);

                                    long frames = (long)(avgFramesForRegion + nearFrames + indeterminateFrames);
                                    */

                                    long frames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                    textBox6.Text = frames.ToString();

                                    if ((avgFramesForRegion + nearFrames + indeterminateFrames) != (prevAvgFramesForRegion + prevNearFrames + prevIndeterminateFrames))
                                    {
                                        framesDif++;
                                    }

                                    GraphProximitryValue();

                                    totalADCMagnitudeFar += avgMagnitude;
                                    double avgADCMagnitude = totalADCMagnitudeFar / series1BinData.GetAverageNumberOfFrames();

                                    GraphData(series1BinData);
                                    GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                                    ////////GraphMagnitude(avgADCMagnitude);

                                    ////GraphTotalMagnitude(totalMagnitude);
                                    ////GraphAvgMagnitude(avgMagnitude);

                                    GraphAverageStrength(null, checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions) || userAnalysisForm.Visible);
                                    ////GraphStrengthToTimeBasedGraph(null);

                                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                                    if (ShowGraphs())
                                    {
                                        chart1.Refresh();
                                        chart2.Refresh();
                                    }
                                }));
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }

                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            SetButtonsToNotRecordingState();
                            ProcessCommand();

                            recordingSeries1Start = -1;
                        }));
                    }
                    catch (Exception ex)
                    {

                    }

                });

                button4.Enabled = false;
                button22.Enabled = button23.Enabled = button24.Enabled = button30.Enabled = button4.Enabled;

                ////button25.Enabled = button26.Enabled = button4.Enabled;

                userAnalysisForm.button1.Enabled = button24.Enabled;


                if (automatedZooming)
                {
                    button24.Enabled = false;
                    userAnalysisForm.button1.Enabled = button24.Enabled;
                }

                button5.Enabled = false;
                button18.Enabled = false;
                userAnalysisForm.button18.Enabled = false;
                userAnalysisForm.button2.Enabled = false;

                button3.Text = "Stop Recording";
                button17.Text = "Stop";
                ////userAnalysisForm.button17.Text = "Stop";
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

        public void RecordSeries2()
        {
            if (button5.Text == "Record Near Series Data")
            {
                while (button3.Text == "Stop Recording")
                {
                    Thread.Sleep(100);
                }

                recordingSeries2Start = (Environment.TickCount & int.MaxValue);

                recordingSeries1 = false;
                recordingSeries2 = true;

                startRecordingSeries2 = false;

                bool exitOnRequiredZoomedFrames = false;

                if (series1BinData.GetAverageNumberOfFrames() > 0)
                {
                    radioButton4.Enabled = true;
                    ////radioButton4.Checked = true;
                }

                Task.Factory.StartNew(() =>
                {
                    bool userNear = true;

                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;

                    BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                    Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                    long startRecordingNearFrames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                    long nearFrames;

                    while (userNear && recordingSeries2)
                    {
                        if (!showingCheckForReradiatedFrequencyDialog && !analyzingUserSelectedFrequency)
                            CheckForReradiatedFrequency();

                        currentMode = BinDataMode.Near;

                        ////if (programState == ProgramState.ANALYZING_TRANSITIONS && currentBufferFramesObject.transitionBufferFrames.nearIndex > -1 && currentBufferFramesObject.bufferFrames.currentBufferIndex > -1)
                        if (currentBufferFramesObject.transitionBufferFrames.nearIndex > -1 && currentBufferFramesObject.bufferFrames.currentBufferIndex > -1)
                        {
                            ////BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                            if (currentBufferFramesObject != zoomedOutBufferObject && currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.bufferFrames.currentBufferIndex].time - currentBufferFramesObject.bufferFrames.bufferFramesArray[currentBufferFramesObject.transitionBufferFrames.nearIndex].time > BufferFrames.TRANSITION_LENGTH / 2)
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

                                    currentBufferFramesObject.transitionBufferFrames.currentTransitionBufferFramesArray.Clear();

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

                                    analyzingTransitionsBeforeSuccessCount = 0;

                                    currentBufferFramesObject.transitionBufferFrames.CalculateGradients();

                                    currentBufferFramesObject.EvaluateWhetherReradiatedFrequencyRange();

                                    /*////Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count - 2);

                                    if (analyzingNearFarTransitions && (command == null || command.name != "UserSelectedFrequencyForAnalysis"))
                                    {
                                        nearFrames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                        if (nearFrames - startRecordingNearFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                                            recordingSeries2 = false;
                                        else
                                            exitOnRequiredZoomedFrames = true;
                                    }*/

                                    this.Invoke(new Action(() =>
                                    {
                                        GraphAndListTransitionData();
                                    }));

                                    if (transitionAnalysesMode)
                                    {
                                        StopRecording();

                                        commandQueue.AddCommand("ExitOnRequiredZoomedFrames:RecordSeries2");
                                    }
                                }
                                else
                                    if (analyzingNearFarTransitions)
                                {
                                    ////recordingSeries2 = false;
                                }

                                currentBufferFramesObject.transitionBufferFrames.nearIndex = -1;
                            }
                        }
                        /*////else
                        {
                            Command command = commandBuffer.GetCommand(commandBuffer.commandArray.Count - 2);

                            if (analyzingNearFarTransitions && (command == null || command.name != "UserSelectedFrequencyForAnalysis"))
                            {
                                long frames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                if (frames - startRecordingNearFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                                    recordingSeries2 = false;
                                else
                                    exitOnRequiredZoomedFrames = true;
                            }
                        }*/

                        if ((Environment.TickCount & int.MaxValue) - GUIInput.lastInputTime >= BufferFrames.TRANSITION_LENGTH / 2)
                        {
                            currentMode = BinDataMode.Indeterminate;
                        }
                        else
                            currentBufferFramesObject.bufferFrames.Change(BinDataMode.Indeterminate, BinDataMode.Near);

                        if (!transitionAnalysesMode && checkBox9.Checked && (Notifications.currentNotificationTimeIndex >= Notifications.notificationTime.Length - 1 || (Environment.TickCount & int.MaxValue) - GUIInput.lastInputTime >= Notifications.notificationTime[Notifications.currentNotificationTimeIndex]) && !analyzingUserSelectedFrequency)
                        {
                            double seconds = Math.Ceiling((double)(Notifications.notificationTime[Notifications.notificationTime.Length - 1] - ((Environment.TickCount & int.MaxValue) - GUIInput.lastInputTime)) / 1000);
                            notifyIcon1.BalloonTipTitle = "Recording Far in " + seconds + " seconds";
                            notifyIcon1.BalloonTipText = "Move the mouse or press a key if you're near";

                            notifyIcon1.ShowBalloonTip(10000);
                            if (seconds <= 0)
                            {
                                if (series1BinData.binArray.Length == 0)
                                    series1BinData = new BinData(series2BinData.size, series1BinData.dataSeries, series1BinData.mode);

                                uint indeterminateBufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Indeterminate);


                                bufferFramesArray.Change(BinDataMode.Indeterminate, BinDataMode.Far);
                                ////currentBufferFramesObject.bufferFrames.Change(BinDataMode.Indeterminate, BinDataMode.Far);

                                series1BinData.bufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Far);
                                series2BinData.bufferFrames = currentBufferFramesObject.bufferFrames.GetFramesCount(BinDataMode.Near);


                                this.Invoke(new Action(() =>
                                {
                                    textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();
                                    textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();
                                }));


                                nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;

                                userNear = false;

                                /*////recordingSeries1 = true;
                                recordingSeries2 = false;
                                */

                                StopRecording();
                                commandQueue.AddCommand("RecordSeries1");

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

                        int totalMagnitude = 0;
                        double avgMagnitude = 0;

                        if (userNear && recordingSeries2)
                        {
                            RecordData(ref series2BinData, ref averageSeries2CurrentFrameStrength, ref averageSeries2TotalFramesStrength, ref totalMagnitude, ref avgMagnitude, 0);

                            ////CheckForNewSignalsAndRefreshData();

                            if (analyzingNearFarTransitions && automatedZooming && !analyzingUserSelectedFrequency)
                            {
                                long frames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                if (!transitionAnalysesMode && frames - startRecordingNearFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                                {
                                    StopRecording();

                                    commandQueue.AddCommand("ExitOnRequiredZoomedFrames:RecordSeries2");
                                }
                            }

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

                                    long farFrames = GetAverageNumberOfFramesForFrequencyRegion(series1BinData, BinDataMode.Far, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);
                                    nearFrames = GetAverageNumberOfFramesForFrequencyRegion(series2BinData, BinDataMode.Near, currentBufferFramesObject.lowerFrequency, currentBufferFramesObject.upperFrequency, zoomedOutBufferObject.lowerFrequency, binSize);

                                    textBox5.Text = farFrames.ToString();
                                    textBox6.Text = nearFrames.ToString();

                                    GraphProximitryValue();

                                    totalADCMagnitudeNear += avgMagnitude;
                                    double avgADCMagnitude = totalADCMagnitudeNear / series2BinData.GetAverageNumberOfFrames();

                                    ////if (userAnalysisForm.WindowState != FormWindowState.Maximized)
                                    {
                                        GraphData(series2BinData);
                                        GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
                                    }


                                    GraphAverageStrength(null, checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions) || userAnalysisForm.Visible);
                                    ////GraphAverageStrength(null);

                                    ////if (checkBox8.Checked || (checkBox13.Checked && analyzingNearFarTransitions))
                                    if (ShowGraphs())
                                    {
                                        chart1.Refresh();
                                        chart2.Refresh();
                                    }

                                    ////if (exitOnRequiredZoomedFrames && nearFrames - startRecordingNearFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT && long.Parse(textBox6.Text) >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                                    if (automatedZooming && exitOnRequiredZoomedFrames && nearFrames - startRecordingNearFrames >= REQUIRED_ZOOMED_FRAMES_BEFORE_ZOOMING_OUT)
                                        recordingSeries2 = false;
                                }));
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }


                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            SetButtonsToNotRecordingState();
                            ProcessCommand();

                            recordingSeries2Start = -1;
                        }));
                    }
                    catch (Exception ex)
                    {

                    }
                });

                button3.Enabled = false;
                button17.Enabled = false;
                userAnalysisForm.button17.Enabled = false;
                button4.Enabled = false;

                button22.Enabled = button23.Enabled = button24.Enabled = button30.Enabled = button4.Enabled;

                ////button25.Enabled = button26.Enabled = button4.Enabled;

                userAnalysisForm.button1.Enabled = userAnalysisForm.button2.Enabled = button24.Enabled;


                if (automatedZooming)
                    button24.Enabled = false;

                button5.Text = "Stop Recording";
                button18.Text = "Stop";
                userAnalysisForm.button18.Text = "Stop";
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


        public void ZoomGraphsToFrequency(long startFrequency, long endFrequency = -1)
        {
            /*////long frequency;

            if (endFrequency>-1 && startFrequency != endFrequency)
            {
                frequency = (startFrequency + endFrequency) / 2;
            }
            else
                frequency = startFrequency;
                */

            if (transitionAnalysesMode)
            {
                ///StopRecording();

                userAnalysisForm.ShowTransitionDialog();

                commandQueue.Clear();

                commandQueue.AddCommand("RecordSeries1");
                StopRecording();

                ////recordingSeries1 = true;
                ////RecordSeries1();
            }

            Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(graph1LowerFrequency, graph1UpperFrequency);
            graph1FrequencyRanges.Push(frequencyRange);

            frequencyRange = new Utilities.FrequencyRange(graph2LowerFrequency, graph2UpperFrequency);
            graph2FrequencyRanges.Push(frequencyRange);


            if (endFrequency == -1 || endFrequency == startFrequency)
            {
                if (startFrequency - GRAPH_WIDTH_WHEN_ZOOMED / 2 < dataLowerFrequency)
                {
                    graph1LowerFrequency = graph2LowerFrequency = dataLowerFrequency;

                    graph1UpperFrequency = graph2UpperFrequency = startFrequency + GRAPH_WIDTH_WHEN_ZOOMED / 2 + (startFrequency - dataLowerFrequency);
                }
                else
                {
                    if (startFrequency + 10000 > dataUpperFrequency)
                    {
                        graph1LowerFrequency = graph2LowerFrequency = startFrequency - GRAPH_WIDTH_WHEN_ZOOMED / 2 - (dataUpperFrequency - startFrequency);
                        graph1UpperFrequency = graph2UpperFrequency = dataUpperFrequency;
                    }
                    else
                    {
                        graph1LowerFrequency = graph2LowerFrequency = startFrequency - GRAPH_WIDTH_WHEN_ZOOMED / 2;
                        graph1UpperFrequency = graph2UpperFrequency = startFrequency + GRAPH_WIDTH_WHEN_ZOOMED / 2;
                    }
                }
            }
            else
            {
                graph1LowerFrequency = graph2LowerFrequency = startFrequency;
                graph1UpperFrequency = graph2UpperFrequency = endFrequency;
            }

            /*////chart1.Series["Far Series"].Points.Clear();
            chart2.Series["Far Series"].Points.Clear();

            chart1.Series["Near Series"].Points.Clear();
            chart2.Series["Near Series"].Points.Clear();

            chart2.Series["Strength Difference"].Points.Clear();
            */

            if (series1BinData != null)
            {
                GraphDataForRange(chart1, series1BinData.dataSeries, series1BinData.binArray, graph1LowerFrequency, graph1UpperFrequency, graph1BinFreqInc);
            }

            if (series2BinData != null)
            {
                GraphDataForRange(chart1, series2BinData.dataSeries, series2BinData.binArray, graph1LowerFrequency, graph1UpperFrequency, graph1BinFreqInc);
            }

            if (series1BinData != null)
            {
                GraphDataForRange(chart2, series1BinData.dataSeries, series1BinData.avgBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
            }

            if (series2BinData != null)
            {
                GraphDataForRange(chart2, series2BinData.dataSeries, series2BinData.avgBinArray, graph2LowerFrequency, graph2UpperFrequency, graph2BinFreqInc);
            }

            if (series1BinData != null && series2BinData != null)
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

            transitionGradientArray = bufferFramesArray.GetStrongestTransitionsFrequencyGradientArray(endFrequency != -1 && endFrequency != startFrequency);

            if (transitionGradientArray != null)
            {
                TransitionGradient transitionGradient;

                if (startFrequency != endFrequency && endFrequency != -1)
                {
                    transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(startFrequency, endFrequency, binSize / 10);
                }
                else
                    /////////transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(frequency, binSize / 10);
                    transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(startFrequency, binSize * 100);

                if (transitionGradient != null)
                    GraphTransitionData(transitionGradient);
            }
        }

        public void InitializeTransitionSignalsToBeAnalysed(uint scans, long lowerFrequency, long upperFrequency)
        {
            transitionSignalsToBeAnalysed.Clear();

            long inc = (upperFrequency - lowerFrequency) / scans;

            long currentFrequency = lowerFrequency;

            for (int i = 0; i < scans; i++)
            {
                transitionSignalsToBeAnalysed.Add(new InterestingSignal(-1, 0, 0, currentFrequency));

                currentFrequency += inc;
            }

            ////transitionSignalsToBeAnalysed.Add(new InterestingSignal(-1, 0, 0, currentFrequency-1));
        }

        public void ActivateSettings(bool clearSettings = true)
        {
            try
            {
                while (button3.Text == "Stop Recording" || button5.Text == "Stop Recording")
                {
                    Thread.Sleep(100);
                }

                dataLowerFrequency = uint.Parse(textBox1.Text);
                dataUpperFrequency = uint.Parse(textBox2.Text);

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
                            transitionGradientArray = null;

                            if (analyzingNearFarTransitions)
                            {
                                series1BinDataFullRange = series1BinData;

                                series2BinDataFullRange = series2BinData;
                            }

                            Command mostRecentCommand = commandBuffer.GetMostRecentCommand();

                            if (mostRecentCommand == null || (mostRecentCommand.name != "ZoomOutOfFrequency" && mostRecentCommand.name != "ZoomToFrequency"))
                            {
                                series1BinData = new BinData(totalBinCount, "Far Series", BinDataMode.Far);

                                series2BinData = new BinData(totalBinCount, "Near Series", BinDataMode.Near);
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

                            bufferFramesArray.Clear();

                            leaderBoardSignals.Clear();

                            listBox1.Items.Clear();
                            listBox2.Items.Clear();
                            listBox3.Items.Clear();
                            listBox4.Items.Clear();
                        }

                        currentBufferFramesObject = bufferFramesArray.GetBufferFramesObject((long)dataLowerFrequency, (long)dataUpperFrequency);

                        if (currentBufferFramesObject == null)
                        {
                            currentBufferFramesObject = new BufferFramesObject(mainForm, (long)dataLowerFrequency, (long)dataUpperFrequency, binSize);

                            bufferFramesArray.AddBufferFramesObject(currentBufferFramesObject);
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

                        ////listBox3.Items.Add("startRecordingSeries2: " + startRecordingSeries2);

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

            transitionGradientArray = null;

            ActivateSettings();
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Command command = commandQueue.GetMostRecentCommand();

            if (command == null || command.name != "SaveSessionDataAndCloseForm")
            {
                if (recordingSeries1 || recordingSeries2)
                {
                    commandQueue.AddCommand("SaveSessionDataAndCloseForm");

                    recordingSeries1 = false;
                    recordingSeries2 = false;

                    e.Cancel = true;
                }
                else
                    SaveData(SESSION_PATH + SESSION_FILE_NAME, series2BinData, series1BinData, bufferFramesArray);
            }
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

                        series.totalBinArrayNumberOfFrames[i] = 1;

                        /*////if (series.GetAverageNumberOfFrames() == 0)
                            value = 0;
                        else
                            value /= series.GetAverageNumberOfFrames();
                            */

                        series.binArray[i] = (float)value;
                        series.avgBinArray[i] = (float)value;
                    }

                    reader.Close();
                }
            }

            resetGraph = true;
            newData = true;
        }


        public Utilities.FrequencyRange GetFrequencyRangeFromFileData(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    dataLowerFrequency = reader.ReadUInt32();
                    dataUpperFrequency = reader.ReadUInt32();

                    Utilities.FrequencyRange frequencyRange = new Utilities.FrequencyRange(dataLowerFrequency, dataUpperFrequency);

                    return frequencyRange;
                }
            }

            return null;
        }

        public void Flush()
        {
            bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);
        }


        public bool LoadData(string filename, bool accrue=false, long specifiedLowerFrequency=-1, long specifiedUpperFrequency=-1, long specifiedStepSize=-1)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    dataLowerFrequency = reader.ReadUInt32();
                    dataUpperFrequency = reader.ReadUInt32();

                    stepSize = reader.ReadUInt32();

                    if ((specifiedLowerFrequency == -1 || dataLowerFrequency == specifiedLowerFrequency) && (specifiedUpperFrequency == -1 || dataUpperFrequency == specifiedUpperFrequency) && (specifiedStepSize == -1 || stepSize == specifiedStepSize))
                    {
                        textBox1.Text = dataLowerFrequency.ToString();
                        textBox2.Text = dataUpperFrequency.ToString();
                        textBox3.Text = stepSize.ToString();                        

                        ////ActivateSettings();
                        ActivateSettings(!accrue);

                        if (!accrue || series2BinData == null)
                            series2BinData = new BinData(totalBinCount, "Near Series", BinDataMode.Near);

                        ////series2BinData.LoadData(reader);
                        series2BinData.LoadData(reader, accrue);

                        textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();

                        if (!accrue || series1BinData == null)
                            series1BinData = new BinData(totalBinCount, "Far Series", BinDataMode.Far);

                        series1BinData.LoadData(reader, accrue);
                        ////series1BinData.LoadData(reader);

                        textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();

                        if (!accrue)
                            bufferFramesArray.Clear();

                        ////bufferFramesArray.LoadData(reader, mainForm);

                        bufferFramesArray.LoadData(reader, mainForm, accrue);

                        currentBufferFramesObject = bufferFramesArray.GetBufferFramesObject((long)dataLowerFrequency, (long)dataUpperFrequency);

                        try
                        {
                            uint leaderBoardSignalCount = reader.ReadUInt32();

                            long frequency;
                            int index;

                            int leaderBoardSignalIndex;

                            if (accrue)
                            {
                                for (int i = 0; i < leaderBoardSignalCount; i++)
                                {
                                    frequency = reader.ReadUInt32();
                                    index = reader.ReadInt32();

                                    leaderBoardSignalIndex = leaderBoardSignals.FindIndex(x => Math.Abs(x.frequency - frequency) < 10000);

                                    if (leaderBoardSignalIndex > -1)
                                    {
                                        leaderBoardSignals[leaderBoardSignalIndex].LoadData(reader, accrue);
                                    }
                                    else
                                    {
                                        leaderBoardSignals.Add(new InterestingSignal(index, 0, 0, frequency));
                                        leaderBoardSignals[leaderBoardSignals.Count - 1].LoadData(reader);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < leaderBoardSignalCount; i++)
                                {
                                    frequency = reader.ReadUInt32();
                                    index = reader.ReadInt32();

                                    leaderBoardSignals.Add(new InterestingSignal(index, 0, 0, frequency));
                                    leaderBoardSignals[leaderBoardSignals.Count - 1].LoadData(reader);
                                }
                            }

                        }
                        catch (Exception ex)
                        {

                        }

                        reader.Close();

                        resetGraph = true;
                        newData = true;


                        series1BinData.CalculateAvgBinData();
                        series2BinData.CalculateAvgBinData();

                        series1BinData.InitializeBinDataToAvgBinData();
                        series2BinData.InitializeBinDataToAvgBinData();

                        GraphData(series1BinData);
                        GraphData(series2BinData);

                        GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                        chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                        chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset();

                        chart1.Refresh();
                        chart2.Refresh();

                        GraphAndListTransitionData();

                        resetGraph = true;
                        newData = true;

                        return true;
                    }
                }
            }

            return false;
        }

        private void SaveData(string filename, BinData nearSeries, BinData farSeries, BufferFramesArray bufferFramesArray)
        {
            if (nearSeries != null && farSeries != null)
            {
                if (analyzingUserSelectedFrequency)
                {
                    nearSeries.Restore();
                    farSeries.Restore();
                }

                textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();
                textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();

                bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);


                textBox5.Text = series1BinData.GetAverageNumberOfFrames().ToString();
                textBox6.Text = series2BinData.GetAverageNumberOfFrames().ToString();

                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                        writer.Write((UInt32)zoomedOutBufferObject.lowerFrequency);
                        writer.Write((UInt32)zoomedOutBufferObject.upperFrequency);
                        writer.Write((UInt32)stepSize);
                        ////writer.Write((UInt32)totalBinCount);

                        nearSeries.SaveData(writer);
                        farSeries.SaveData(writer);

                        bufferFramesArray.SaveData(writer);

                        writer.Write((UInt32)leaderBoardSignals.Count);

                        for (int i = 0; i < leaderBoardSignals.Count; i++)
                        {
                            leaderBoardSignals[i].SaveData(writer);
                        }

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
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
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
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                difThreshold = double.Parse(textBox4.Text);

                SaveConfig();


                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
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

                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                checkBox1.Enabled = true;

                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
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

            GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);
        }

        public void ClearCharts(int[] seriesToBeCleared)
        {

            ClearChartSeriesPoints(chart1, seriesToBeCleared);
            ClearChartSeriesPoints(chart2, seriesToBeCleared);
            ClearChartSeriesPoints(chart3, seriesToBeCleared);
            ClearChartSeriesPoints(chart4, seriesToBeCleared);
            ClearChartSeriesPoints(chart5, seriesToBeCleared);
            ClearChartSeriesPoints(chart6, seriesToBeCleared);
            ClearChartSeriesPoints(chart7, seriesToBeCleared);
            ClearChartSeriesPoints(chart8, seriesToBeCleared);


            ClearChartSeriesPoints(userAnalysisForm.chart3, seriesToBeCleared);
            ClearChartSeriesPoints(userAnalysisForm.chart4, seriesToBeCleared);
            ClearChartSeriesPoints(userAnalysisForm.chart5, seriesToBeCleared);
            ClearChartSeriesPoints(userAnalysisForm.chart8, seriesToBeCleared);

        }

        public void ClearChartSeriesPoints(System.Windows.Forms.DataVisualization.Charting.Chart chart, int[] seriesToBeCleared)
        {
            for (int i = 0; i < chart.Series.Count; i++)
            {
                if (Utilities.ExistsIn(seriesToBeCleared, i))
                    chart.Series[i].Points.Clear();
            }

            chart.ChartAreas[0].AxisY.Minimum = -99999999;
            chart.ChartAreas[0].AxisY.Maximum = 99999999;

            chart.ChartAreas[0].AxisY2.Minimum = 0;            
            chart.ChartAreas[0].AxisY2.Maximum = 1000;            
        }

        public void ClearSeries1()
        {
            if (series1BinData != null)
            {
                ////bufferFramesArray.Flush(null, null, null);

                int[] seriesToBeCleared = new int[1];

                seriesToBeCleared[0] = 0;

                ClearCharts(seriesToBeCleared);

                if (!startRecordingSeries1 && !startRecordingSeries2 && currentBufferFramesObject!=null)
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
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                if (currentBufferFramesObject != null)
                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;
                else
                    nearFarBufferIndex = -1;
            }

            totalADCMagnitudeFar = 0;

            proximitryFrequency.totalADCMagnitude = 0;
            proximitryFrequency.sampleCount = 0;

            userAnalysisCheckFarFrames = 0;
        }

        public void ClearSeries2()
        {
            if (series2BinData != null)
            {
                ////bufferFramesArray.Flush(null, null, null);

                int[] seriesToBeCleared = new int[1];

                seriesToBeCleared[0] = 1;

                ClearCharts(seriesToBeCleared);

                /*////chart1.Series["Near Series"].Points.Clear();
                chart2.Series["Near Series"].Points.Clear();

                chart1.ChartAreas[0].AxisY.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Maximum = 0.1;

                chart2.ChartAreas[0].AxisY.Minimum = 0;
                chart2.ChartAreas[0].AxisY.Maximum = 0.1;
                */

                if (!startRecordingSeries1 && !startRecordingSeries2 && currentBufferFramesObject!=null)
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
                GraphDifferenceOrNearFarTransitionRatios(series1BinData, series2BinData);

                if (currentBufferFramesObject != null)
                    nearFarBufferIndex = currentBufferFramesObject.bufferFrames.currentBufferIndex;
                else
                    nearFarBufferIndex = -1;
            }

            totalADCMagnitudeNear = 0;

            proximitryFrequency.totalADCMagnitude = 0;
            proximitryFrequency.sampleCount = 0;

            userAnalysisCheckNearFrames = 0;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure? This will clear all your previously recorded data.", "Info", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
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
        }

        private void button12_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure? This will clear all your previously recorded data.", "Info", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
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
        }

        public Form1()
        {
            InitializeComponent();

            ////if (Properties.Settings.Default.DontShowInfoBoxes!=null)
                Properties.Settings.Default.DontShowInfoBoxes = new int[10];

            _form_resize = new clsResize(this);

            _form_resize.SetFormsInitialSize(new Size(1920, 1080));

            _form_resize.StoreControlsInitialSizes();

            mainForm = this;

            userAnalysisForm = new UserAnalysisForm(this);

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

            chart3.Series["Series"].IsValueShownAsLabel = false;

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

        private void ShowQuickStartForm()
        {
            quickStartForm = new quickStartForm(this);

            if (File.Exists(SESSION_PATH + SESSION_FILE_NAME))
            {
                Utilities.FrequencyRange frequencyRange = GetFrequencyRangeFromFileData(SESSION_PATH + SESSION_FILE_NAME);

                if (frequencyRange != null)
                {
                    quickStartForm.textBox1.Text = frequencyRange.lower.ToString();
                    quickStartForm.textBox2.Text = frequencyRange.upper.ToString();
                }
            }
            else
            {
                quickStartForm.checkBox10.Enabled = false;
                quickStartForm.checkBox10.Checked = false;
            }

            quickStartForm.StartPosition = FormStartPosition.CenterScreen;

            quickStartForm.TopMost = true;

            quickStartForm.Show();
            quickStartForm.Focus();
        }

        private void ShowStartInfoDialog()
        {
            if (Properties.Settings.Default.DontShowInfoBoxes[(int)UserInfoDialogs.StartDialog] != 1)
            {
                UserInfoDialog dialog = new UserInfoDialog((int)UserInfoDialogs.StartDialog);
                
                string welcomeText = Utilities.GetResourceText("RTLSpectrumAnalyzerGUI.Resources.Welcome.txt");

                dialog.SetTitle("RTL SDR spectrum analyzer");
                dialog.SetText(welcomeText);

                dialog.EnableShowMessageAgain(false);

                dialog.ShowDialog();
            }
        }

        private void ShowZoomInDialog()
        {
            if (Properties.Settings.Default.DontShowInfoBoxes[(int)UserInfoDialogs.ZoomInDialog] != 1)
            {
                UserInfoDialog dialog = new UserInfoDialog((int)UserInfoDialogs.ZoomInDialog);

                string zoomInText = Utilities.GetResourceText("RTLSpectrumAnalyzerGUI.Resources.ZoomIn.txt");

                dialog.SetTitle("ZoomIn To Frequency Information");
                dialog.SetText(zoomInText);

                dialog.EnableShowMessageAgain(true);

                dialog.ShowDialog();
            }
        }

        public void ShowUserAnalysisDialog()
        {            
            if (Properties.Settings.Default.DontShowInfoBoxes[(int)UserInfoDialogs.UserAnalysisDialog] != 1)
            {
                UserInfoDialog dialog = new UserInfoDialog((int)UserInfoDialogs.UserAnalysisDialog);

                string userAnalysisText = Utilities.GetResourceText("RTLSpectrumAnalyzerGUI.Resources.UserAnalysis.txt");                

                dialog.SetTitle("User Analysis");

                dialog.SetText(userAnalysisText);

                dialog.ShowDialog();
            }
        }

		public void SetStartup(bool set)
		{
			try
			{
				Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

				if (rk != null)
				{
					if (set)
						rk.SetValue("RTLSpectrumAnalyzer", Application.ExecutablePath);
					else
						rk.DeleteValue("RTLSpectrumAnalyzer", false);
				}
			}
			catch(Exception ex)
			{

			}
		}

		private void Form1_Load(object sender, EventArgs e)
        {			
			System.IO.Directory.CreateDirectory(SESSION_PATH);
            System.IO.Directory.CreateDirectory(PREVIOUS_SESSIONS_PATH);            

            this.WindowState = FormWindowState.Maximized;
            
            button26.BringToFront();

            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Minimum = 100;

            leaderboardGraph.chart1.Series["AvgSeries"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;



            chart5.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

            chart5.ChartAreas[0].AxisY2.Minimum = 100;

            chart5.Series["Series2"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;


            userAnalysisForm.chart5.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

            userAnalysisForm.chart5.ChartAreas[0].AxisY2.Minimum = 100;

            userAnalysisForm.chart5.Series["Series2"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;



            chart8.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

            chart8.ChartAreas[0].AxisY2.Minimum = 100;

            chart8.Series["Series2"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;


            userAnalysisForm.chart8.ChartAreas[0].AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;

            userAnalysisForm.chart8.ChartAreas[0].AxisY2.Minimum = 100;

            userAnalysisForm.chart8.Series["Series2"].YAxisType = System.Windows.Forms.DataVisualization.Charting.AxisType.Secondary;





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
                int devicesCount=-1, prevDevicesCount = -1;

				while (devicesCount <= 0)
				{
					devicesCount = NativeMethods.GetConnectedDevicesCount();

					if (devicesCount != prevDevicesCount)
					{
						if (devicesCount > 0)
						{
							this.Invoke(new Action(() =>
							{
								button4.Enabled = true;
								button22.Enabled = button23.Enabled = button24.Enabled = button4.Enabled;
								userAnalysisForm.button1.Enabled = button24.Enabled;

								if (automatedZooming)
								{
									button24.Enabled = false;
									userAnalysisForm.button1.Enabled = button24.Enabled;
								}
							}));
						}
						else
						{
							this.Invoke(new Action(() =>
							{
								button3.Enabled = false;
								button4.Enabled = false;
								button22.Enabled = button23.Enabled = button24.Enabled = button4.Enabled;
								userAnalysisForm.button1.Enabled = button24.Enabled;

								if (automatedZooming)
								{
									button24.Enabled = false;
									userAnalysisForm.button1.Enabled = button24.Enabled;
								}

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
					{
						////MessageBox.Show("Connect device(s) and restart the application.\nIf using two devices plug in the device used for near signal detection first.");
						DialogResult result = MessageBox.Show("Connect device(s) and continue.\nIf using two devices plug in the device used for near signal detection first.", "", MessageBoxButtons.RetryCancel);

						if (result == DialogResult.Cancel)
							break;
					}
				}
            });

            mainForm.WindowState = FormWindowState.Maximized;

            ShowStartInfoDialog();

            LaunchNewThread(ShowQuickStartFormThread, 1000);

            mainForm.Hide();
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

            ActivateSettings();
        }

        public void ShowIncreasingStrengthColorIndicator()
        {
            if (!checkBox5.Checked && !checkBox6.Checked)
                checkBox5.Checked = userAnalysisForm.checkBox5.Checked = true;

            if (form2 == null || mainForm.form2.IsDisposed)
                form2 = new Form2();

            form2.Show();

            form2.Focus();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            ShowIncreasingStrengthColorIndicator();
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

        public class MyEventArgs : EventArgs
        {
            public bool clearData=false;
        }

        private void NewSettingsThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                DestroyEventTimer();

                ////NewSettings(false);
                if (((System.Windows.Forms.Timer)myObject).Tag != "")
                {
                    if (((System.Windows.Forms.Timer)myObject).Tag=="clearData")
                        NewSettings(true);
                    else
                        NewSettings(false);
                }
                else
                    NewSettings(false);
            }
        }

        private void ZoomOutOfFrequencyThread(Object myObject, EventArgs myEventArgs)
        {
            if (eventTimer != null)
            {
                DestroyEventTimer();

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


                    proximitryFrequency.frequency = (uint)leaderBoardSignals[currentLeaderBoardSignalIndex].frequency;

                    proximitryFrequency.maxStrength = leaderBoardSignals[currentLeaderBoardSignalIndex].maxAvgStrength;
                    proximitryFrequency.minStrength = leaderBoardSignals[currentLeaderBoardSignalIndex].minAvgStrength;


                    if (proximitryFrequency.maxStrength > proximitryFrequency.minStrength)
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

                    LaunchNewThread(NewSettingsThread, 1000);
                }
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            button3.PerformClick();
        }

        private void button18_Click(object sender, EventArgs e)
        {
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
            userAnalysisForm.checkBox5.Checked = checkBox5.Checked;

            if (checkBox5.Checked)
            {
                checkBox4.Checked = false;
                checkBox6.Checked = false;

                userAnalysisForm.checkBox6.Checked = false;
            }

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            userAnalysisForm.checkBox6.Checked = checkBox6.Checked;

            if (checkBox6.Checked)
            {
                checkBox4.Checked = false;
                checkBox5.Checked = false;

                userAnalysisForm.checkBox5.Checked = false;
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

            if (leaderBoardSignals.Count > 0)
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

        public void ResumeAutomatedAnalysis()
        {
            mainForm.showGraphs = mainForm.originalUserSettingShowGraphs;

            if (mainForm.showGraphs)
                mainForm.checkBox8.Checked = true;

            analyzingUserSelectedFrequency = false;

            if (recordingSeries1 || recordingSeries2)
            {
                mainForm.StopRecording();
                commandQueue.AddCommand("UserSelectedFrequencyForAnalysisExit");
            }
            else
                UserSelectedFrequencyForAnalysisExit();            
        }

        private void button24_Click(object sender, EventArgs e)
        {
            ResumeAutomatedAnalysis();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {            
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(PREVIOUS_SESSIONS_PATH);
            int fileCount = dir.GetFiles().Length;

            System.IO.File.Copy(SESSION_PATH + SESSION_FILE_NAME, PREVIOUS_SESSIONS_PATH  + "session_" + fileCount + SESSION_EXTENSION);
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            checkBox10.CheckedChanged -= checkBox10_CheckedChanged;

            if (checkBox10.Checked)
            {                
                checkBox10.Checked = false;
            }

            checkBox10.CheckedChanged += checkBox10_CheckedChanged;
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            checkBox12.CheckedChanged -= checkBox12_CheckedChanged;
            
            if (checkBox12.Checked)
            {                
                checkBox12.Checked = false;
            }

            checkBox12.CheckedChanged += checkBox12_CheckedChanged;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZoomToListBoxFrequency(listBox1);            
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox9.Checked)
            {
                checkBox8.Checked = true;
                showGraphs = true;                
            }            
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            /*////checkBox13.Checked = !checkBox8.Checked;

            checkBox15.Checked = !checkBox8.Checked;
            */

            mainForm.checkBox13.Enabled = mainForm.checkBox15.Enabled = !checkBox8.Checked;
        }

        private void AnalyzeFrequency(long lowerFrequency, long upperFrequency = -1)
        {
            if (series1BinData.binArray.Length > 0 && series2BinData.binArray.Length > 0 && !Double.IsNaN(series1BinData.binArray[0]) && !Double.IsNaN(series2BinData.binArray[0]))
            {
                ////if (!analyzingNearFarTransitions)
                {
                    analyzingUserSelectedFrequency = true;

                    commandBuffer.AddCommand("UserSelectedFrequencyForAnalysis");
                    ////commandBuffer.AddCommand("UserSelectedFrequencyForZooming");

                    ////userSelectedFrequencyForAnalysis = (long)(graph2UpperFrequency + graph2LowerFrequency) / 2;

                    if (upperFrequency == -1)
                    {
                        userAnalysisForm.textBox17.Text = Utilities.GetFrequencyString(lowerFrequency);

                        userAnalysisForm.radioButton1.Checked = true;
                    }
                    else
                    {
                        userAnalysisForm.textBox17.Text = Utilities.GetFrequencyString(lowerFrequency) + " to " + Utilities.GetFrequencyString(upperFrequency);

                        userAnalysisForm.radioButton2.Checked = true;
                    }

                    userAnalysisForm.textBox1.Text = (currentLeaderboardSignalBeingAnalyzedIndex + 1).ToString();

                    ////analyzingNearFarTransitions = true;
                    automatedZooming = false;
                    button24.Enabled = true;
                    userAnalysisForm.button1.Enabled = button24.Enabled;

                    ////if (showGraphs)
                    ////checkBox8.Checked = true;

                    /*////originalUserSettingShowGraphs = showGraphs;

                    showGraphs = false;

                    checkBox8.Checked = checkBox13.Checked = false;
                    */


                    programState = ProgramState.ANALYZING_TRANSITIONS;

                    ZoomToFrequency(lowerFrequency, upperFrequency);
                }
            }
            else
            {
                if (series1BinData.binArray.Length == 0 || Double.IsNaN(series1BinData.binArray[0]))
                {
                    MessageBox.Show("Record some far frames first.");
                }
                else
                if (series2BinData.binArray.Length == 0 || Double.IsNaN(series2BinData.binArray[0]))
                {
                    MessageBox.Show("Record some near frames first.");
                }
                else
                    MessageBox.Show("Record some frames first.");

            }            
        }

        public void AnalyzeCenterFrequency()
        {
            userSelectedFrequencyForAnalysis = (long)(graph2UpperFrequency + graph2LowerFrequency) / 2;            

            AnalyzeFrequency(userSelectedFrequencyForAnalysis);
        }

        public void AnalyzeLeaderboardFrequency()
        {
            if (reradiatedFrequencies.Count > 0)
            {
                currentLeaderboardSignalBeingAnalyzedIndex++;

                if (currentLeaderboardSignalBeingAnalyzedIndex >= listBox6.Items.Count)
                    currentLeaderboardSignalBeingAnalyzedIndex = 0;

                userSelectedFrequencyForAnalysis = (long)reradiatedFrequencies[currentLeaderboardSignalBeingAnalyzedIndex].frequency;

                if (checkBox14.Checked)
                {
                    Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(userSelectedFrequencyForAnalysis);


                    AnalyzeFrequency((long)frequencyRange.lower, (long)frequencyRange.upper);
                }
                else
                    AnalyzeFrequency(userSelectedFrequencyForAnalysis);
            }

            ////AnalyzeFrequency(userSelectedFrequencyForAnalysis);            
        }

        private void button25_Click(object sender, EventArgs e)
        {
            originalUserSettingShowGraphs = showGraphs;
            showGraphs = false;
            checkBox8.Checked = checkBox13.Checked = false;            

            if (recordingSeries1 || recordingSeries2)
            {
                resumeRecording = true;

                StopRecording();

                commandQueue.AddCommand("AnalyzeCenterFrequency");
            }
            else
            {
                resumeRecording = false;

                bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);

                userAnalysisForm.AnalyzingCenterFrequency();
                userAnalysisForm.Show();

                userAnalysisForm.WindowState = FormWindowState.Maximized;

                AnalyzeCenterFrequency();
            }
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZoomToListBoxFrequency(listBox4);            
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (mainForm.reradiatedFrequencies.Count > 0)
            {
                originalUserSettingShowGraphs = showGraphs;
                showGraphs = false;
                checkBox8.Checked = checkBox13.Checked = false;

                if (recordingSeries1 || recordingSeries2)
                {
                    resumeRecording = true;

                    StopRecording();

                    commandQueue.AddCommand("AnalyzeLeaderboardFrequency:" + 0);
                }
                else
                {
                    resumeRecording = false;

                    if (userAnalysisForm.AnalyzingLeaderboardFrequency())
                    {
                        bufferFramesArray.Flush(series1BinData, series2BinData, series1BinData);
                        userAnalysisForm.Show();

                        userAnalysisForm.WindowState = FormWindowState.Maximized;

                        currentLeaderboardSignalBeingAnalyzedIndex = -1;
                        AnalyzeLeaderboardFrequency();
                    }
                }
            }
            else
                MessageBox.Show("The code hasn't detected any reradiated frequencies yet.");
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZoomToListBoxFrequency(listBox2);            
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (_form_resize!=null)
                _form_resize._resize();
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked && !checkBox13.Checked)
                checkBox8.Checked = false;

            checkBox15.Checked = !checkBox13.Checked;
        }

        private void button27_Click(object sender, EventArgs e)
        {
            bool prevShowUserAnalaysisGraphs = showUserAnalaysisGraphs;
            bool prevCheckBox8Checked = checkBox8.Checked;

            showUserAnalaysisGraphs = false;
            checkBox8.Checked = true;            

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            float[] leaderboardSignalsArray = new float[series1BinData.binArray.Length];            

            Utilities.FrequencyRange frequencyRange;
            for (int i = 0; i < leaderBoardSignals.Count; i++)
            {
                frequencyRange = Utilities.GetIndicesForFrequencyRange((long) leaderBoardSignals[i].frequency, (long) leaderBoardSignals[i].frequency, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.binSize);

                leaderboardSignalsArray[(int) frequencyRange.lower] = (float) leaderBoardSignals[i].rating;
            }

            leaderboardGraph.chart1.Series["Series"].Points.Clear();
            leaderboardGraph.chart1.Series["AvgSeries"].Points.Clear();

            leaderboardGraph.chart1.Series["Series"].LegendText = "Rating";
            leaderboardGraph.chart1.Series["AvgSeries"].LegendText = "Density Graph";       


            GraphDataForRange(leaderboardGraph.chart1, "Series", leaderboardSignalsArray, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);

            double[] leaderboardSignalsArrayDouble = SignalDataUtilities.SegmentSeries(Utilities.ConvertFloatArrayToDoubleArray(leaderboardSignalsArray), (int)(zoomedOutBufferObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / DENSITY_GRAPH_SEGMENT_SIZE);



            GraphDataForRange(leaderboardGraph.chart1, "AvgSeries", Utilities.ConvertDoubleArrayToFloatArray(leaderboardSignalsArrayDouble), zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);
                    
            /*////leaderboardGraph.chart1.ChartAreas[0].AxisX.Maximum = series1BinData.binArray.Length - 1;
            leaderboardGraph.chart1.ChartAreas[0].AxisX2.Maximum = series1BinData.binArray.Length - 1;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Minimum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Minimum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Maximum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].RecalculateAxesScale();
            */


            leaderboardGraph.WindowState = FormWindowState.Normal;

            leaderboardGraph.Text = "Leaderboard Graph";
            leaderboardGraph.chart1.Titles[0].Text = "";
            leaderboardGraph.Show();


            showUserAnalaysisGraphs = prevShowUserAnalaysisGraphs;
            checkBox8.Checked = prevCheckBox8Checked;
        }

        private void button28_Click(object sender, EventArgs e)
        {
            bool prevShowUserAnalaysisGraphs = showUserAnalaysisGraphs;
            bool prevCheckBox8Checked = checkBox8.Checked;

            showUserAnalaysisGraphs = false;
            checkBox8.Checked = true;

            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            float[] transitionSignalsArray = new float[series1BinData.binArray.Length];

            Utilities.FrequencyRange frequencyRange;
            for (int i = 0; i < transitionGradientArray.array.Count && transitionGradientArray!=null; i++)
            {
                frequencyRange = Utilities.GetIndicesForFrequencyRange((long)transitionGradientArray.array[i].frequency, (long)transitionGradientArray.array[i].frequency, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.binSize);

                transitionSignalsArray[(int)frequencyRange.lower] = (float)(transitionGradientArray.array[i].strength);
            }

            leaderboardGraph.chart1.Series["Series"].Points.Clear();
            leaderboardGraph.chart1.Series["AvgSeries"].Points.Clear();

            leaderboardGraph.chart1.Series["Series"].LegendText = "Near/Far Percentage Increase";
            leaderboardGraph.chart1.Series["AvgSeries"].LegendText = "Density Graph";


            leaderboardGraph.chart1.ChartAreas[0].AxisX.Maximum = series1BinData.binArray.Length - 1;
            leaderboardGraph.chart1.ChartAreas[0].AxisX2.Maximum = series1BinData.binArray.Length - 1;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Minimum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Minimum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Maximum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].RecalculateAxesScale();


            GraphDataForRange(leaderboardGraph.chart1, "Series", transitionSignalsArray, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);


            double[] transitionSignalsArrayDouble = SignalDataUtilities.SegmentSeries(Utilities.ConvertFloatArrayToDoubleArray(transitionSignalsArray), (int)(zoomedOutBufferObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / DENSITY_GRAPH_SEGMENT_SIZE);            


            GraphDataForRange(leaderboardGraph.chart1, "AvgSeries", Utilities.ConvertDoubleArrayToFloatArray(transitionSignalsArrayDouble), zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);


            leaderboardGraph.WindowState = FormWindowState.Normal;

            leaderboardGraph.Text = "Transitions Graph";
            leaderboardGraph.chart1.Titles[0].Text = "";
            leaderboardGraph.Show();

            showUserAnalaysisGraphs = prevShowUserAnalaysisGraphs;
            checkBox8.Checked = prevCheckBox8Checked;
        }

        private void button29_Click(object sender, EventArgs e)
        {
            bool prevShowUserAnalaysisGraphs = showUserAnalaysisGraphs;
            bool prevCheckBox8Checked = checkBox8.Checked;

            showUserAnalaysisGraphs = false;
            checkBox8.Checked = true;


            BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

            float[] interestingSignalsArray = new float[series1BinData.binArray.Length];

            Utilities.FrequencyRange frequencyRange;
            for (int i = 0; i < interestingSignals.Count; i++)
            {
                frequencyRange = Utilities.GetIndicesForFrequencyRange((long)interestingSignals[i].frequency, (long)interestingSignals[i].frequency, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.binSize);

                interestingSignalsArray[(int)frequencyRange.lower] = (float)(interestingSignals[i].strengthDif);
            }

            ////leaderboardGraph.chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            ////leaderboardGraph.chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);


            leaderboardGraph.chart1.Series["Series"].Points.Clear();
            leaderboardGraph.chart1.Series["AvgSeries"].Points.Clear();

            leaderboardGraph.chart1.Series["Series"].LegendText = "Near/Far Percentage Increase";
            leaderboardGraph.chart1.Series["AvgSeries"].LegendText = "Density Graph";


            leaderboardGraph.chart1.ChartAreas[0].AxisX.Maximum = series1BinData.binArray.Length - 1;
            leaderboardGraph.chart1.ChartAreas[0].AxisX2.Maximum = series1BinData.binArray.Length - 1;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Minimum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Minimum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;
            leaderboardGraph.chart1.ChartAreas[0].AxisY2.Maximum = Double.NaN;

            leaderboardGraph.chart1.ChartAreas[0].RecalculateAxesScale();



            GraphDataForRange(leaderboardGraph.chart1, "Series", interestingSignalsArray, zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);



            double[] interestingSignalsArrayDouble = SignalDataUtilities.SegmentSeries(Utilities.ConvertFloatArrayToDoubleArray(interestingSignalsArray), (int)(zoomedOutBufferObject.upperFrequency - zoomedOutBufferObject.lowerFrequency) / DENSITY_GRAPH_SEGMENT_SIZE);


            GraphDataForRange(leaderboardGraph.chart1, "AvgSeries", Utilities.ConvertDoubleArrayToFloatArray(interestingSignalsArrayDouble), zoomedOutBufferObject.lowerFrequency, zoomedOutBufferObject.upperFrequency, zoomedOutBufferObject.binSize);
            


            leaderboardGraph.WindowState = FormWindowState.Normal;

            leaderboardGraph.Text = "Interesting Signals Graph";
            leaderboardGraph.chart1.Titles[0].Text = "";
            leaderboardGraph.Show();

            showUserAnalaysisGraphs = prevShowUserAnalaysisGraphs;

            checkBox8.Checked = prevCheckBox8Checked;
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            GraphAndListTransitionData();
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void button30_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(PREVIOUS_SESSIONS_PATH);
            FileInfo[] fileInfo = dir.GetFiles();

            bool accrue = false;

            for (int i = 0; i < fileInfo.Length; i++)
            {
                if (LoadData(fileInfo[i].FullName, accrue, long.Parse(textBox1.Text), long.Parse(textBox2.Text), long.Parse(textBox3.Text)))
                {
                    accrue = true;
                }
            }
        }

        private void listBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZoomToListBoxFrequency(listBox6);            
        }

        private void ZoomToListBoxFrequency(ListBox listbox)
        {
            if (listbox.SelectedIndex > -1)
            {
                long startFrequency = -1;
                long endFrequency = -1;

                string itemStr = (string)listbox.Items[listbox.SelectedIndex];

                string[] itemStrArray = itemStr.Split(':');

                if (itemStrArray[0].IndexOf("to") > -1)
                {
                    Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromString(itemStrArray[0]);

                    startFrequency = (long)frequencyRange.lower;

                    endFrequency = (long)frequencyRange.upper;
                }
                else
                {
                    itemStrArray[0] = itemStrArray[0].Substring(0, itemStrArray[0].Length - 3);

                    userSelectedFrequencyForZooming = (long)(double.Parse(itemStrArray[0]) * 1000000);

                    startFrequency = userSelectedFrequencyForZooming;

                    endFrequency = startFrequency;
                }

                BufferFramesObject zoomedOutBufferObject = bufferFramesArray.GetBufferFramesObject(0);

                if (zoomedOutBufferObject == currentBufferFramesObject)
                {
                    if (!analyzingNearFarTransitions && listbox.SelectedIndex > -1)
                    {
                        commandBuffer.AddCommand("UserSelectedFrequencyForZooming");

                        automatedZooming = false;

                        ZoomGraphsToFrequency(startFrequency, endFrequency);
                    }
                }
                else
                    ShowZoomInDialog();

                transitionGradientArray = bufferFramesArray.GetStrongestTransitionsFrequencyGradientArray(endFrequency != -1 && endFrequency != startFrequency);

                if (transitionGradientArray != null)
                {
                    TransitionGradient transitionGradient;

                    if (startFrequency != endFrequency && endFrequency != -1)
                    {
                        transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(startFrequency, endFrequency, binSize / 10);
                    }
                    else
                        transitionGradient = transitionGradientArray.GetTransitionGradientForFrequency(startFrequency, binSize * 100);

                    if (transitionGradient != null)
                        GraphTransitionData(transitionGradient);
                }
            }
        }

        private void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZoomToListBoxFrequency(listBox5);
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            checkBox13.Checked = !checkBox15.Checked;
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        public void ShowSaveDataDialogAndSaveData()
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                SaveData(saveFileDialog1.FileName, series2BinData, series1BinData, bufferFramesArray);
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            ShowSaveDataDialogAndSaveData();
        }

        private void button23_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                ////LoadData(openFileDialog1.FileName);
                LoadData(openFileDialog1.FileName, false);
            }
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
