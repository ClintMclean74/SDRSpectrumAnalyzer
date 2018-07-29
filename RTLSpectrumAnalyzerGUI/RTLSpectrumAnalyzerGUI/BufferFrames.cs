using System.Collections.Generic;
using System;
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{
    public class BufferFrames
    {
        public List<BufferFrame> bufferFramesArray = new List<BufferFrame>();

        public List<BufferFrame> currentTransitionBufferFramesArray = new List<BufferFrame>();

        public List<GradientArray> gradients = new List<GradientArray>();

        public bool bufferFilled = false;

        public const uint MIN_BUFFER_SIZE = 10;

        public const uint TRANSITION_LENGTH = 8 * 1000;

#if (SDR_DEBUG)
            public const long BUFFER_TIME_LENGTH = 30 * 1000;        
            public const long TIME_DELAY_BEFORE_ZOOMING_BEFORE_ANALYZING_TRANSITIONS = 10 * 1000;
            public const long TIME_DELAY_BEFORE_ZOOMING = 3 * 1000;
#else
        public const long BUFFER_TIME_LENGTH = 60 * 1000;
            public const long TIME_DELAY_BEFORE_ZOOMING_BEFORE_ANALYZING_TRANSITIONS = 60 * 1000;
            public const long TIME_DELAY_BEFORE_ZOOMING = 10 * 1000;
        #endif

        public const long ZOOMED_IN_TRANSISTION_FRAMES_EXIT = 4;
        public const long ZOOMED_IN_TRANSISTION_FRAMES_STAGE1 = 1;

        public const double MIN_NEAR_FAR_PERCENTAGE_FOR_RERADIATED_FREQUENCY = 105;

        public const long FREQUENCY_SEGMENT_SIZE = 100000;

        public readonly static double[] minStrengthForRankings = { 150, 110 };

        public int currentBufferIndex = -1;
        public int startBufferIndex = 0;

        public int nearIndex;
        public int farIndex;

        public long transitions = 0;

        double avg = 0;

        public long minFrameIndex;

        private Form1 mainForm;
        private BufferFramesObject parent;

        public BufferFrames(Form1 mainForm, BufferFramesObject parent)
        {
            this.mainForm = mainForm;
            this.parent = parent;
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write((UInt32)transitions);

            int bufferFramesArrayCount = 0;

            int i;

            if (bufferFramesArray.Count > 0)
            {                
                i = startBufferIndex;

                do
                {
                    if (i >= bufferFramesArray.Count)
                        i = 0;

                    i++;

                    bufferFramesArrayCount++;                    
                }
                while (i != currentBufferIndex + 1);
            }

            writer.Write((UInt32)bufferFramesArrayCount);

            if (bufferFramesArray.Count > 0)
            {
                writer.Write((UInt32)bufferFramesArray[0].bufferArray.Length);

                i = startBufferIndex;

                do
                {
                    if (i >= bufferFramesArray.Count)
                        i = 0;

                    bufferFramesArray[i].SaveData(writer);

                    i++;
                }
                while (i != currentBufferIndex + 1);
            }            

            writer.Write((UInt32)currentTransitionBufferFramesArray.Count);

            if (currentTransitionBufferFramesArray.Count > 0)
            {
                writer.Write((UInt32)currentTransitionBufferFramesArray[0].bufferArray.Length);

                for (int j = 0; j < currentTransitionBufferFramesArray.Count; j++)
                {
                    currentTransitionBufferFramesArray[j].SaveData(writer);
                }
            }

            writer.Write((UInt32)gradients.Count);

            if (gradients.Count > 0)
            {
                writer.Write((UInt32)gradients[0].gradientArray.Length);

                for (int j = 0; j < gradients.Count; j++)
                {
                    gradients[j].SaveData(writer);
                }
            }
        }

        public void LoadData(BinaryReader reader)
        {
            transitions = reader.ReadUInt32();

            long bufferFramesArrayLength = reader.ReadUInt32();            

            if (bufferFramesArrayLength>0)
            {
                long bufferFramesLength = reader.ReadUInt32();

                for (int i = 0; i < bufferFramesArrayLength; i++)
                {
                    bufferFramesArray.Add(new BufferFrame(bufferFramesLength, BinDataMode.Indeterminate));
                    bufferFramesArray[bufferFramesArray.Count - 1].LoadData(reader);
                }

                currentBufferIndex = bufferFramesArray.Count - 1;
                startBufferIndex = 0;
            }


            long currentBufferFramesArrayLength = reader.ReadUInt32();

            if (currentBufferFramesArrayLength > 0)
            {
                long currentBufferFramesLength = reader.ReadUInt32();

                for (int i = 0; i < currentBufferFramesArrayLength; i++)
                {                    
                    currentTransitionBufferFramesArray.Add(new BufferFrame(currentBufferFramesLength, BinDataMode.Indeterminate));
                    currentTransitionBufferFramesArray[currentTransitionBufferFramesArray.Count - 1].LoadData(reader);
                }               
            }

            long gradientArrayLength = reader.ReadUInt32();

            if (gradientArrayLength > 0)
            {
                long gradientsLength = reader.ReadUInt32();

                for (int i = 0; i < gradientArrayLength; i++)
                {
                    gradients.Add(new GradientArray(gradientsLength));
                    gradients[gradients.Count - 1].LoadData(reader);
                }
            }
        }

        public void GraphMostRecentAvgStrength(System.Windows.Forms.DataVisualization.Charting.Chart chart)
        {
            if (bufferFramesArray.Count > 0)
            {
                try
                {
                    System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint;

                    double totalStrengthValue;

                    totalStrengthValue = 0;

                    for (int j = 0; j < bufferFramesArray[currentBufferIndex].bufferArray.Length; j++)
                    {
                        totalStrengthValue += bufferFramesArray[currentBufferIndex].bufferArray[j];
                    }

                    totalStrengthValue /= bufferFramesArray[currentBufferIndex].bufferArray.Length;

                    totalStrengthValue = Math.Round(totalStrengthValue);

                    avg = totalStrengthValue;


                    if (chart.Series["Far Series"].Points.Count > Form1.MAXIMUM_TIME_BASED_GRAPH_POINTS)
                    {
                        double min = Double.NaN, max = Double.NaN;

                        chart.Series["Far Series"].Points.RemoveAt(0);

                        for (int j = 0; j < chart.Series["Far Series"].Points.Count; j++)
                        {
                            chart.Series["Far Series"].Points[j].XValue--;

                            if (Double.IsNaN(min) || chart.Series["Far Series"].Points[j].YValues[0] < min)
                                min = chart.Series["Far Series"].Points[j].YValues[0];

                            if (Double.IsNaN(max) || chart.Series["Far Series"].Points[j].YValues[0] > max)
                                max = chart.Series["Far Series"].Points[j].YValues[0];

                            if (max == min)
                                max += 0.1;
                        }

                        chart.ChartAreas[0].AxisY.Minimum = min;
                        chart.ChartAreas[0].AxisY.Maximum = max;
                    }

                    graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(chart.Series["Far Series"].Points.Count, avg);

                    chart.Series["Far Series"].Points.Add(graphPoint);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public double[] GetStrengthOverTimeForIndex(long index)
        {
            /*////if (currentTransitionBufferFramesArray.Count > 0)
            {
                int i = startBufferIndex;

                int k = 0;

                do
                {
                    if (i >= currentTransitionBufferFramesArray.Count)
                        i = 0;

                    k++;

                    i++;
                }
                while (i != currentBufferIndex + 1);


                double[] values = new double[k];

                double totalStrengthValue;

                i = startBufferIndex;

                k = 0;
                

                do
                {
                    if (i >= currentTransitionBufferFramesArray.Count)
                        i = 0;

                    totalStrengthValue = currentTransitionBufferFramesArray[i].bufferArray[index];
                    
                    totalStrengthValue = Math.Round(totalStrengthValue);

                    values[k++] = totalStrengthValue;

                    i++;
                }
                while (i != currentBufferIndex + 1);                

                return values;
            }*/


            if (currentTransitionBufferFramesArray.Count > 0)
            {
                double totalStrengthValue;

                double[] values = new double[currentTransitionBufferFramesArray.Count];

                for (int i = 0; i < currentTransitionBufferFramesArray.Count; i++)
                {
                    totalStrengthValue = currentTransitionBufferFramesArray[i].bufferArray[index];

                    totalStrengthValue = Math.Round(totalStrengthValue);

                    values[i] = totalStrengthValue;
                }

                return values;
            }

            return null;
        }

        public double[] GetAveragedStrengthOverTimeForIndex(long index)
        {
            if (bufferFramesArray.Count > 0)
            {
                double[] values = new double[bufferFramesArray.Count];

                double totalStrengthValue;
                
                int i = startBufferIndex;

                int k = 0;

                do
                {
                    if (i >= bufferFramesArray.Count)
                        i = 0;

                    totalStrengthValue = bufferFramesArray[i].bufferArray[index];

                    totalStrengthValue /= bufferFramesArray[i].stackedFrames;

                    totalStrengthValue = Math.Round(totalStrengthValue);

                    values[k++] = totalStrengthValue;

                    i++;
                }
                while (i != currentBufferIndex + 1);

                return values;
            }

            return null;
        }
        
        public double GetAverageStackedFramesForIndex(long index)
        {
            if (bufferFramesArray.Count > 0)
            {                
                long totalStackedFrames = 0;

                int i = startBufferIndex;

                int k = 0;
                while (i != currentBufferIndex + 1)
                {
                    if (i > bufferFramesArray.Count)
                        i = 0;

                    totalStackedFrames += bufferFramesArray[i].stackedFrames;
                                        
                    i++;

                    k++;
                }

                return (double) totalStackedFrames/k;
            }

            return 0;
        }

        public double[] GetStrengthOverTimeForFrequency(long frequency)
        {
            Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(frequency, frequency, parent.lowerFrequency, mainForm.binSize);

            return GetStrengthOverTimeForIndex((long) frequencyRange.lower);            
        }

        public double[] GetStrengthOverTimeForRange(long lowerFrequency, long upperFrequency)
        {
            if (bufferFramesArray.Count > 0)
            {
                double[] values = new double[bufferFramesArray.Count];

                double totalStrengthValue;

                Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(lowerFrequency, upperFrequency, parent.lowerFrequency, mainForm.binSize);
                
                int i = startBufferIndex;

                int k = 0;
                while (i != currentBufferIndex + 1)
                {
                    totalStrengthValue = 0;

                    for (long j = (long) frequencyRange.lower; j < (long) frequencyRange.upper; j++)
                    {
                        totalStrengthValue += bufferFramesArray[i].bufferArray[j];
                    }

                    totalStrengthValue /= (frequencyRange.upper - frequencyRange.lower);

                    totalStrengthValue /= transitions;

                    totalStrengthValue = Math.Round(totalStrengthValue);

                    values[k++] = totalStrengthValue;

                    i++;

                    if (i > bufferFramesArray.Count)
                        i = 0;
                }

                return values;
            }

            return null;
        }

        public TransitionGradientArray GetStrongestTransitionsGradientFrequency()
        {
            TransitionGradientArray transitionGradientArray = new TransitionGradientArray();

            if (bufferFramesArray.Count > 0)
            {
                double maxGradientStrength = Double.NaN, gradientStrength;

                int maxIndex = -1;

                double[] transitionsStrengthArray;

                TransitionGradient strongestTransitionGradient;

                long inc = (long) (FREQUENCY_SEGMENT_SIZE / mainForm.binSize);

                long segmentEnd;                               

                for (long i = 0; i < bufferFramesArray[0].bufferArray.Length; i+=inc)
                {
                    maxGradientStrength = Double.NaN;
                    maxIndex = -1;

                    segmentEnd = i + inc;

                    for (long j = i; j < segmentEnd && j < bufferFramesArray[0].bufferArray.Length; j++)
                    {
                        transitionsStrengthArray = GetAveragedStrengthOverTimeForIndex(j);
                        ////transitionsStrengthArray = GetStrengthOverTimeForIndex(j);                        

                        ////gradientStrength = SignalDataUtilities.Series2ndVS1stHalfAvgStrength(transitionsStrengthArray);

                        gradientStrength = SignalDataUtilities.Series2ndVS1stHalfAvgStrength(transitionsStrengthArray) * transitions;

                        if (Double.IsNaN(maxGradientStrength) || gradientStrength > maxGradientStrength)
                        {
                            maxGradientStrength = gradientStrength;

                            maxIndex = (int)j;
                        }
                    }

                    strongestTransitionGradient = new TransitionGradient(Utilities.GetFrequencyFromIndex((long)(maxIndex), parent.lowerFrequency, mainForm.binSize), maxIndex, maxGradientStrength, this.transitions);
                    transitionGradientArray.Add(strongestTransitionGradient);
                }
            }

            return transitionGradientArray;
        }
        
        public void CalculateGradients()
        {            
            if (bufferFramesArray.Count > 0)
            {                
                gradients.Add(new GradientArray(bufferFramesArray[0].bufferArray.Length));

                gradients[gradients.Count - 1].time = Environment.TickCount;

                double[] transitionsStrengthArray;

                double gradientStrength;

                double avgStackedFrames;

                for (long i = 0; i < bufferFramesArray[0].bufferArray.Length; i++)
                {
                    transitionsStrengthArray = GetStrengthOverTimeForIndex(i);

                    gradientStrength = SignalDataUtilities.Series2ndVS1stHalfAvgStrength(transitionsStrengthArray);

                    avgStackedFrames = GetAverageStackedFramesForIndex(i);

                    gradients[gradients.Count - 1].gradientArray[i] = new Gradient(gradientStrength, avgStackedFrames);
                }                
            }            
        }

        public int EvaluatereradiatedRankingCategory()
        {
            if (gradients.Count > 0)
            {
                int gradientCount;

                for (int j = minStrengthForRankings.Length-1; j >=0; j--)
                {
                    for (int i = 0; i < gradients.Count; i++)
                    {
                        gradientCount = 0;

                        for (int k = 0; k < gradients[i].gradientArray.Length; k++)
                        {
                            if (gradients[i].gradientArray[k].strength < minStrengthForRankings[j])
                            {
                                gradientCount++;
                            }
                        }

                        if (gradientCount == gradients[0].gradientArray.Length)
                        {
                            return j;        
                        }
                    }
                }
            }

            return 1;
        }

        public bool EvaluateWhetherReradiatedFrequencyRange()
        {
            /*////if (gradients.Count>0)
            {
                bool[] possibleReradiatedFrequency = new bool[gradients[0].gradientArray.Length];

                for (int i = 0; i < gradients[0].gradientArray.Length; i++)
                    possibleReradiatedFrequency[i] = true;

                for (int i = 0; i < gradients.Count; i++)
                {
                    for (int j = 0; j < gradients[i].gradientArray.Length; j++)
                    {
                        if (gradients[i].gradientArray[j].strength < BufferFrames.MIN_NEAR_FAR_PERCENTAGE_FOR_RERADIATED_FREQUENCY)
                        {
                            possibleReradiatedFrequency[j] = false;
                        }
                    }
                }


                double gradientForTransition;

                for (int i = 0; i < gradients.Count; i++)
                {
                    for (int j = 0; j < gradients[i].gradientArray.Length; j++)
                    {
                        if (i == 0)
                            gradientForTransition = gradients[i].gradientArray[j].strength;
                        else
                            gradientForTransition = gradients[i].gradientArray[j].strength * (i + 1) /*gradients[i].gradientArray[j].stackedFrames*/ /*////- gradients[i - 1].gradientArray[j].strength * i /*gradients[i-1].gradientArray[j].stackedFrames*/;

            /*                        if (gradientForTransition < BufferFrames.MIN_NEAR_FAR_PERCENTAGE_FOR_RERADIATED_FREQUENCY)
                                    {
                                        possibleReradiatedFrequency[j] = false;
                                    }
                                }
                            }


                            int negativeGradientCount = 0;
                            for (int i = 0; i < gradients[0].gradientArray.Length; i++)
                                if (!possibleReradiatedFrequency[i])
                                    negativeGradientCount++;

                            if (negativeGradientCount == gradients[0].gradientArray.Length)
                                return false;
                        }

                        return true;
                        */

            if (gradients.Count > 0)
            {
                int negativeGradientCount;

                for (int i = 0; i < gradients.Count; i++)
                {
                    negativeGradientCount = 0;

                    for (int j = 0; j < gradients[i].gradientArray.Length; j++)
                    {
                        if (gradients[i].gradientArray[j].strength < BufferFrames.MIN_NEAR_FAR_PERCENTAGE_FOR_RERADIATED_FREQUENCY)
                        {
                            negativeGradientCount++;
                        }
                    }

                    if (negativeGradientCount == gradients[0].gradientArray.Length)
                        return false;
                }
            }

            return true;
        }

        public void GraphData(System.Windows.Forms.DataVisualization.Charting.Chart chart, double[] data)
        {
            try
            {                
                System.Windows.Forms.DataVisualization.Charting.DataPoint graphPoint;                
                
                chart.Series["Far Series"].Points.Clear();

                double min = Double.NaN, max = Double.NaN;
                
                for (int i = 0; i < data.Length; i++)
                {
                    graphPoint = new System.Windows.Forms.DataVisualization.Charting.DataPoint(chart.Series["Far Series"].Points.Count, data[i]);

                    chart.Series["Far Series"].Points.Add(graphPoint);

                    if (Double.IsNaN(min) || data[i] < min)
                        min = data[i];

                    if (Double.IsNaN(max) || data[i] > max)
                        max = data[i];

                    if (max == min)
                        max += 0.1;
                }

                chart.ChartAreas[0].AxisY.Minimum = min;
                chart.ChartAreas[0].AxisY.Maximum = max;
            }
            catch (Exception ex)
            {

            }
        }

        public void GetStrengthOverTimeForfrequency()
        {

        }

        public void AddTransitionBufferFrame(BufferFrame bufferFrame, long transitionTime, long index)
        {
            if (bufferFrame.mode == BinDataMode.Indeterminate || bufferFrame.mode == BinDataMode.NotUsed)
                return;

            if (index >= currentTransitionBufferFramesArray.Count)
                currentTransitionBufferFramesArray.Add(bufferFrame.Clone());
            else
            {
                for (int i = 0; i < currentTransitionBufferFramesArray[(int)index].bufferArray.Length; i++)
                {
                    currentTransitionBufferFramesArray[(int)index].bufferArray[i] = bufferFrame.bufferArray[i];
                }                
            }
            
            if (transitions == 0)
            {
                bufferFramesArray.Add(bufferFrame.Clone());

                minFrameIndex = bufferFramesArray.Count;

                currentBufferIndex = (int) minFrameIndex - 1;
            }
            else
            {
                long startTransitionTime = bufferFramesArray[0].time;

                long minTimeDifference = -1, dif, minIndex = -1;

                for (int i = 0; i < bufferFramesArray.Count; i++)
                {
                    dif = Math.Abs(transitionTime - (bufferFramesArray[i].time - startTransitionTime));

                    if (minTimeDifference == -1 || dif < minTimeDifference)
                    {
                        minTimeDifference = dif;

                        minIndex = i;
                    }
                }

                if (minIndex < 0)
                {
                    bufferFramesArray.Add(bufferFrame.Clone());
                }
                else
                {
                    bufferFramesArray[(int)minIndex].stackedFrames++;
                    
                    for (int i = 0; i < bufferFramesArray[(int)minIndex].bufferArray.Length; i++)
                    {
                        bufferFramesArray[(int)minIndex].bufferArray[i] += bufferFrame.bufferArray[i];                        
                    }
                }
            }
        }        

        public uint GetFramesCountForFrequencyRegion(long lowerFrequency, long upperFrequency, BinDataMode mode)
        {
            BufferFramesObject zoomedOutBufferObject = mainForm.bufferFramesArray.GetBufferFramesObject(0);
            
            long lowerIndex = (long)((lowerFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
            long upperIndex = (long)((upperFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);

            uint frames = 0;

            for (long i = lowerIndex; i < upperIndex; i++)
            {
                if (bufferFramesArray[(int)i].mode == mode)
                {
                    frames++;
                }
            }

            return frames;
        }

        public uint GetFramesCount(BinDataMode mode)
        {
            uint frames = 0;

            for (int i = 0; i < bufferFramesArray.Count; i++)
            {
                if (bufferFramesArray[i].mode == mode)
                {
                    frames++;
                }
            }

            return frames;
        }

        public uint AddBufferRangeIntoArray(float[] array, BinDataMode mode)
        {
            uint frames = 0;

            for (int i = 0; i < bufferFramesArray.Count; i++)
            {
                if (bufferFramesArray[i].mode == mode)
                {
                    for (int j = 0; j < array.Length; j++)
                    {
                        array[j] += bufferFramesArray[i].bufferArray[j];
                    }

                    frames++;
                }
            }

            return frames;
        }

        public void Change(BinDataMode prevMode, BinDataMode newMode)
        {
            for (int i = 0; i < bufferFramesArray.Count; i++)
            {
                try
                {
                    if (bufferFramesArray[i].mode == prevMode)
                    {
                        bufferFramesArray[i].mode = newMode;
                    }
                }
                catch (Exception ex)
                {

                }
            }

        }

        public void Flush(BinData farSeries, BinData nearSeries, BinData indeterminateSeries)
        {
            BinData targetBinData;

            if (farSeries.totalBinArray.Length == 0)
            {
                farSeries.totalBinArray = new float[nearSeries.totalBinArray.Length];
                farSeries.totalBinArrayNumberOfFrames = new float[nearSeries.totalBinArrayNumberOfFrames.Length];
            }

            minFrameIndex = bufferFramesArray.Count;

            BufferFramesObject zoomedOutBufferObject = mainForm.bufferFramesArray.GetBufferFramesObject(0);
            
            long lowerIndex = (long)((parent.lowerFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);
            long upperIndex = (long)((parent.upperFrequency - zoomedOutBufferObject.lowerFrequency) / zoomedOutBufferObject.binSize);

            for (int i = 0; i < bufferFramesArray.Count; i++)
            {
                bufferFramesArray[i].stackedFrames = 0;

                if (bufferFramesArray[i].mode == BinDataMode.Far)
                    targetBinData = farSeries;
                else
                    if (bufferFramesArray[i].mode == BinDataMode.Near)
                        targetBinData = nearSeries;
                else
                    if (bufferFramesArray[i].mode == BinDataMode.Indeterminate)
                        targetBinData = indeterminateSeries;
                else
                    targetBinData = null;

                if (targetBinData != null)
                {
                    int k = 0;                    

                    for (long j = lowerIndex; j < upperIndex; j++)
                    {
                        targetBinData.totalBinArray[j] += bufferFramesArray[i].bufferArray[k];
                        targetBinData.totalBinArrayNumberOfFrames[j]++;

                        k++;
                    }
                    
                    targetBinData.bufferFrames--;
                }
            }

            bufferFramesArray.Clear();

            currentBufferIndex = -1;
            startBufferIndex = 0;

            transitions = 0;

            minFrameIndex = -1;

            bufferFilled = false;
        }

        public void Clear()
        {
            transitions = 0;

            mainForm.textBox15.Text = "";

            bufferFramesArray.Clear();

            minFrameIndex = bufferFramesArray.Count;

            for (int i = 0; i < bufferFramesArray.Count; i++)
            {
                bufferFramesArray[i].stackedFrames = 0;

                for (int j = 0; j < bufferFramesArray[i].bufferArray.Length; j++)
                {
                    bufferFramesArray[i].bufferArray[j] = 0;
                }
            }
        }
    }
}