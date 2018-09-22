using System.Collections.Generic;
using System;
using System.Data;
using System.Linq;
using System.IO;
using System.Reflection;

namespace RTLSpectrumAnalyzerGUI
{
    public class Utilities
    {
        public class FrequencyRange
        {
            public double lower;
            public double upper;

            public FrequencyRange(double lower, double upper)
            {
                this.lower = lower;
                this.upper = upper;
            }
        }

        public static bool ExistsIn(int[] seriesToBeCleared, int item)
        {
            for(int i=0; i<seriesToBeCleared.Length; i++)
            {
                if (seriesToBeCleared[i] == item)
                    return true;
            }

            return false;
        }

        public static FrequencyRange GetFrequencyRangeFromFrequency(long frequency)
        {
            double lowerFrequency, upperFrequency;

            lowerFrequency = Math.Floor((double)frequency / 1000000) * 1000000;

            upperFrequency = (double)frequency / 1000000;

            if (upperFrequency % 1 == 0)
                upperFrequency += 0.5;


            upperFrequency = Math.Ceiling(upperFrequency) * 1000000;

            FrequencyRange frequencyRange = new FrequencyRange(lowerFrequency, upperFrequency);

            return frequencyRange;
        }

        public static string GetFrequencyString(double frequency, short decimalPlaces = 4)
        {
            return (Math.Round((frequency / 1000000), decimalPlaces)).ToString() + "MHz";
        }

        public static double GetFrequencyValue(string frequency, short decimalPlaces = 4)
        {
            return Math.Round(double.Parse(frequency), decimalPlaces);
        }

        public static double FrequencyToMHz(double frequency, short decimalPlaces = 4)
        {
            return Math.Round(frequency / 1000000, decimalPlaces);
        }

        public static long GetFrequencyFromIndex(long index, long lowerFrequencyOfArray, double binSize)
        {
            return (long)(Math.Round(lowerFrequencyOfArray + (index * binSize)));
        }

        public static FrequencyRange GetIndicesForFrequencyRange(long specifiedLowerFrequency, long specifiedUpperFrequency, long dataLowerFrequency, double binFrequencySize)
        {
            long lowerIndex = (long)(Math.Round((specifiedLowerFrequency - dataLowerFrequency) / binFrequencySize));
            long upperIndex = (long)(Math.Round((specifiedUpperFrequency - dataLowerFrequency) / binFrequencySize));

            FrequencyRange frequencyRange = new FrequencyRange(lowerIndex, upperIndex);

            return frequencyRange;
        }

        public static FrequencyRange GetFrequencyRangeFromString(string frequencyStr)
        {
            string startFrequencyString = frequencyStr.Substring(0, frequencyStr.IndexOf("to") - 4);

            int secondFrequencyStrStartIndex = frequencyStr.IndexOf("to") + 2;
            int secondFrequencyStrLength = frequencyStr.Length - 3 - secondFrequencyStrStartIndex;

            string endFrequencyString = frequencyStr.Substring(secondFrequencyStrStartIndex, secondFrequencyStrLength);

            long startFrequency = (long)(double.Parse(startFrequencyString)) * 1000000;

            long endFrequency = (long)(double.Parse(endFrequencyString)) * 1000000;

            return new FrequencyRange(startFrequency, endFrequency);
        }



        public static bool Equals(double a, double b, double tolerance)
        {
            if (Math.Abs(a - b) <= tolerance)
                return true;

            return false;
        }

        public static int FrequencyInSignals(long frequency, long strongestRange, List<InterestingSignal> signals, bool useRange = false, double tolerance = 10000)
        {
            int closestIndex = -1;
            double minDif = Double.NaN, dif;

            Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);

            if (strongestRange == -1)

                strongestRange = signals.Count;

            for (int i = 0; i < signals.Count && i < strongestRange; i++)
            {
                if (!useRange)
                {
                    dif = Math.Abs(signals[i].frequency - frequency);

                    if (dif < tolerance)
                    {
                        if ((dif < minDif || Double.IsNaN(minDif)) && signals[i].frequency >= frequencyRange.lower && signals[i].frequency <= frequencyRange.upper)
                        {
                            minDif = dif;
                            closestIndex = i;
                        }
                    }
                }
                else
                {
                    if (frequency >= signals[i].lowerFrequency && frequency <= signals[i].upperFrequency)
                    {
                        return i;
                    }
                }
            }

            return closestIndex;
        }

        public static double[] ConvertFloatArrayToDoubleArray(float[] floatArray)
        {
            double[] doubleArray= new double[floatArray.Length];

            for (int i = 0; i < floatArray.Length; i++)
            {
                doubleArray[i] = floatArray[i];
            }

            return doubleArray;
        }

        public static float[] ConvertDoubleArrayToFloatArray(double[] doubleArray)
        {
            float[] floatArray = new float[doubleArray.Length];

            for (int i = 0; i < doubleArray.Length; i++)
            {
                floatArray[i] = (float) doubleArray[i];
            }

            return floatArray;
        }

        public static string GetResourceText(string textResourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = textResourceName;

            ////string[] resources = assembly.GetManifestResourceNames();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();

                return result;
            }
        }

        public static double Interpolate2Points(System.Windows.Forms.DataVisualization.Charting.DataPoint p1, System.Windows.Forms.DataVisualization.Charting.DataPoint p2, double x)
        {
            var x0 = p1.XValue;
            var y0 = p1.YValues[0];
            var x1 = p2.XValue;
            var y1 = p2.YValues[0];
            return y0 + ((x - x0) * y1 - (x - x0) * y0) / (x1 - x0);
        }

        public static void AutoAdjustChartZoom(System.Windows.Forms.DataVisualization.Charting.Chart chart, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e, string series)
        {
            var axisY = chart.ChartAreas[0].AxisY;
            var axisY2 = chart.ChartAreas[0].AxisY2;

            var xRangeStart = e.Axis.ScaleView.ViewMinimum;
            var xRangeEnd = e.Axis.ScaleView.ViewMaximum;

            for (int i = 0; i < chart.Series.Count; i++)
            {
                // compute the Y values for the points crossing the range edges
                double? yRangeStart = null;
                var pointBeforeRangeStart = chart.Series[i].Points.FirstOrDefault(x => x.XValue <= xRangeStart);
                var pointAfterRangeStart = chart.Series[i].Points.FirstOrDefault(x => x.XValue > xRangeStart);
                if (pointBeforeRangeStart != null && pointAfterRangeStart != null)
                    yRangeStart = Interpolate2Points(pointBeforeRangeStart, pointAfterRangeStart, xRangeStart);

                double? yRangeEnd = null;
                var pointBeforeRangeEnd = chart.Series[i].Points.FirstOrDefault(x => x.XValue <= xRangeEnd);
                var pointAfterRangeEnd = chart.Series[i].Points.FirstOrDefault(x => x.XValue > xRangeEnd);
                if (pointBeforeRangeEnd != null && pointAfterRangeEnd != null)
                    yRangeEnd = Interpolate2Points(pointBeforeRangeEnd, pointAfterRangeEnd, xRangeEnd);

                var edgeValues = new[] { yRangeStart, yRangeEnd }.Where(x => x.HasValue).Select(x => x.Value);

                // find the points inside the range
                var valuesInRange = chart.Series[i].Points
                .Where(p => p.XValue >= xRangeStart && p.XValue <= xRangeEnd)
                .Select(x => x.YValues[0]);

                // find the minimum and maximum Y values
                var values = valuesInRange.Concat(edgeValues);
                double yMin;
                double yMax;
                if (values.Any())
                {
                    yMin = values.Min();
                    yMax = values.Max();
                }
                else
                {
                    yMin = chart.Series[i].Points.Min(x => x.YValues[0]);
                    yMax = chart.Series[i].Points.Max(x => x.YValues[0]);
                }


                if (i==0)
                {
                    axisY.ScaleView.Zoom(yMin, yMax);                    
                }
                else
                    if (axisY2 != null)
                        axisY2.ScaleView.Zoom(yMin, yMax);
            }
        }
    }
}
