using System;
using System.Collections.Generic;


namespace RTLSpectrumAnalyzerGUI
{
    public class SignalDataUtilities
    {

      
        

        public static List<InterestingSignal> CreateRangeList(List<InterestingSignal> frequencyList)
        {
            List<InterestingSignal> rangeList = new List<InterestingSignal>();

            Utilities.FrequencyRange frequencyRange;

            int rangeIndex;

            InterestingSignal range;

            for (int i = 0; i < frequencyList.Count; i++)
            {
                frequencyRange = Utilities.GetFrequencyRangeFromFrequency((long) frequencyList[i].frequency);

                rangeIndex = rangeList.FindIndex(x => x.lowerFrequency == frequencyRange.lower && x.upperFrequency == frequencyRange.upper);

                if (rangeIndex > -1)
                {
                    rangeList[rangeIndex].rangeTotal += frequencyList[i].rating;

                    rangeList[rangeIndex].rangeTotalCount++;
                }
                else
                {
                    range = new InterestingSignal(0, 0, 0, (frequencyRange.lower + frequencyRange.upper) / 2, frequencyRange.lower, frequencyRange.upper);

                    range.rangeTotal = frequencyList[i].rating;

                    range.rangeTotalCount=1;

                    rangeList.Add(range);
                }
            }

            /*////for (int i = 0; i < rangeList.Count; i++)
            {
                rangeList[i].rating = rangeList[i].rangeTotal / rangeList[i].rangeTotalCount;
            }*/

            return rangeList;
        }


        public static double[] ShiftTo0(double[] data)
        {
            double min = Double.NaN;

            double[] newData = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < min || double.IsNaN(min))
                    min = data[i];
            }

            for (int i = 0; i < data.Length; i++)
            {                
                newData[i] = data[i] - min;
            }

            return newData;
        }

        public static double GetNearestPeakStrength(float[] array, long frequencyIndex, uint width=2)
        {
            long rightIndex = frequencyIndex;
            long leftIndex = frequencyIndex;

            while (rightIndex < array.Length - 2 && rightIndex<(frequencyIndex + width) && array[rightIndex + 1] > array[rightIndex])
                rightIndex++;

            while (leftIndex > 0 && leftIndex > (frequencyIndex - width) && array[leftIndex - 1] > array[leftIndex])
                leftIndex--;

            if (array[rightIndex] >= array[leftIndex])
                return array[rightIndex];
            else
                return array[leftIndex];
        }

        public static double[] GetRollingAverage(double[] data)
        {
            double totalAvg = 0;

            double[] newData = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                totalAvg += data[i];
                newData[i] = totalAvg / (i + 1);

                
                ////newData[i] = data[i]*2;
            }

            return newData;
        }

        public static double[] GetRollingAverage2(double[] data)
        {
            int rollingAvgLength=10;

            List<double> rollingAvg = new List<double>(rollingAvgLength);

            double totalAvg = 0, avgValue;

            double[] newData = new double[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                avgValue = data[i] / rollingAvgLength;

                totalAvg += avgValue;

                rollingAvg.Add(avgValue);

                if (rollingAvg.Count > rollingAvgLength)
                {
                    avgValue = rollingAvg[0];

                    totalAvg -= avgValue;

                    rollingAvg.RemoveAt(0);
                }

                newData[i] = totalAvg;
            }

            return newData;
        }

        public static double CalculateGradient(double[] data)
        {
            double totalGradients = 0;

            for(int i = 1; i<data.Length; i++)
            {
                totalGradients += (data[i] - data[i - 1]);
            }

            return totalGradients / (data.Length - 1);
        }

        public static double[] CalculateGradientArray(double[] data)
        {
            double[] gradientArray = new double[data.Length];            

            for (int j = 1; j < data.Length; j++)
            {
                gradientArray[j] += (data[j] - data[j - 1]);                
            }
            
            gradientArray[0] = gradientArray[1] = gradientArray[2] = gradientArray[3];

            return gradientArray;
        }

        public static double SeriesAvgStrength(double[] data)
        {
            double avgStrength = 0;

            for (int i = 0; i < data.Length; i++)
            {
                avgStrength += data[i];
            }

            avgStrength /= data.Length;

            return avgStrength;
        }

        public static double[] Normalize(double[] data, double maximum)
        {
            double[] newArray = new double[data.Length];

            double max = Double.NaN;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] > max || double.IsNaN(max))
                    max = data[i];                
            }

            for (int i = 0; i < data.Length; i++)
            {
                newArray[i] = data[i] / max * maximum;                    
            }

            return newArray;
        }

        public static Gradient SeriesTransitionGradient(double[] data, int divisionsCount = 1)
        {
            ////////Gradient.divisionsCount = 20;

            ////double[] gradientValues = SegmentSeries(data, 2);
            ////double[] gradientValues = GetRollingAverage2(data);
            ////double[] gradientValues = CalculateGradientArray(GetRollingAverage(data));
            ////double[] gradientValues = CalculateGradientArray(data);

            ////double[] gradientValues = CalculateGradientArray(data);

            ////gradientValues = GetRollingAverage2(gradientValues);

            ////gradientValues = CalculateGradientArray(gradientValues);

            ////gradientValues = GetRollingAverage2(gradientValues);

            ////gradientValues = GetRollingAverage(data);

            ////gradientValues = ShiftTo0(gradientValues);

            ////gradientValues = SegmentSeries(gradientValues, divisionsCount);

            ////gradientValues = SegmentSeries(gradientValues, 5);

            ////double[] gradientValues = data;

            int i;

            /*/////////for (i = 0; i < data.Length; i++)
            {
                data[i] = data[i] * data[i];                
            }*/

            ////gradientValues = Normalize(data, 1000000);


            /////////double[] gradientValues = (double []) data.Clone();

            double[] gradientValues = new double[data.Length];


            double total1 = 0, total2 = 0;
            

            int firstLength = (int)(data.Length * 4 / 10);

            int secondLength = data.Length - firstLength - 1;


            for (i = 0; i < firstLength; i++)
            {

                total1 += data[i];
            }

            total1 /= firstLength;

            for (; i < data.Length-1; i++)
            {
                total2 += data[i];
            }

            total2 /= secondLength;

            double percentageIncrease = Math.Round(total2 / total1 * 100, 2);

            for (i = 0; i < firstLength; i++)
            {
                gradientValues[i] = total1;
                ////gradientValues[i] = 100;
            }

            for (; i < data.Length; i++)
            {
                gradientValues[i] = total2;
                ////gradientValues[i] = percentageIncrease;
            }

            ////gradientValues = (double []) data.Clone();


            /*////
            gradientValues = GetRollingAverage2(gradientValues);
            
            gradientValues = GetRollingAverage2(gradientValues);
            
            gradientValues = CalculateGradientArray(gradientValues);
            gradientValues = CalculateGradientArray(gradientValues);
            */



            /*////gradientValues = SegmentSeries(gradientValues, divisionsCount);
            
            gradientValues = CalculateGradientArray(gradientValues);


            gradientValues = SegmentSeries(gradientValues, divisionsCount);

            gradientValues = CalculateGradientArray(gradientValues);
            

            gradientValues = SegmentSeries(gradientValues, divisionsCount);
            */




            Gradient gradient = new Gradient(0, 1, gradientValues);

            gradient.strength = percentageIncrease;

            return gradient;
        }

        public static double[] Reduce(double[] data, int segmentCount)
        {
            double segmentLength = (double) data.Length / segmentCount;

            double[] newArray = new double[segmentCount];
            
            double total;
            for (int j = 0; j < segmentCount; j++)
            {
                total = 0;
                for (int i = (int) (j * segmentLength); i < (j + 1) * segmentLength; i++)
                {
                    total += data[i];
                }

                newArray[j] = total / segmentLength;
            }

            return newArray;
        }

        public static double[] Expand(double[] data, long length)
        {
            double[] newArray = new double[length];
            
            int j = 0;

            double increment = (double) length / data.Length;

            int segmentIndex = (int) Math.Round(increment);

            for (int i = 0; i < length; i++)
            {
                if (i == segmentIndex)
                {
                    segmentIndex = (int)Math.Round(segmentIndex + increment);

                    if (j<data.Length-1)
                        j++;
                }

                newArray[i] = data[j];
            }

            return newArray;
        }

        public static double[] SegmentSeries(double[] data, int divisionsCount = 1)
        {
            if (divisionsCount == -1)
                divisionsCount = data.Length;

            double[] divisions = new double[divisionsCount];

            double divisionLength = ((double)data.Length / divisionsCount);

            int i;


            for (int j = 0; j < divisionsCount; j++)
            {
                for (i = (int)(divisionLength * j); i < divisionLength * (j+1); i++)
                {
                    divisions[j] += data[i];
                }

                divisions[j] /= (i-(int)(divisionLength * j));
            }

                /*////for (i = divisionLength; i < divisionLength * 2; i++)
                {
                    divisions[1] += (data[i] - data[i - 1]);
                }

                divisions[1] /= divisionLength;

                for (i = divisionLength * 2; i < divisionLength * 3; i++)
                {
                    divisions[2] += (data[i] - data[i - 1]);
                }

                divisions[2] /= divisionLength;


                for (i = divisionLength * 3; i < divisionLength * 4; i++)
                {
                    divisions[3] += (data[i] - data[i - 1]);
                }

                divisions[3] /= divisionLength;

                for (i = divisionLength * 4; i < divisionLength * 5; i++)
                {
                    divisions[4] += (data[i] - data[i - 1]);
                }

                divisions[4] /= divisionLength;
            }
            */

            for (i = 0; i < divisions.Length; i++)
            {
                divisions[i] = Math.Round(divisions[i], 2);
            }


            return Expand(divisions, data.Length);



            /*double[] divisions2 = new double[data.Length];

            i = 0;
            for (int j = 0; j < data.Length; j++)
            {
                if (j % divisionLength == 0 && j > 0)
                {
                    if (i < divisionsCount-1)
                        i++;                    
                }

                divisions2[j] = divisions[i];
            }

            return divisions2;
            */

            ////return divisions;

            /*////Gradient gradient = new Gradient(0, 1, divisions);

            return gradient;
            */
        }

        public static double Series2ndVS1stHalfAvgStrength(double[] data)
        {
            int segmentCount = 4;

            double[] segmentStrengths = new double[segmentCount];

            double segmentLength = (double) data.Length / segmentCount;

            segmentLength = Math.Ceiling(segmentLength);

            int i;

            int j = 0;

            for (i = 0; i < data.Length; i++)
            {
                segmentStrengths[j] += data[i];

                if (i>0 && i % segmentLength == 0)
                {
                    segmentStrengths[j] /= segmentLength;
                    j++;
                }
            }

            segmentStrengths[j] /= (data.Length - segmentLength * (segmentCount - 1));

            double[] percentageIncrements = new double[segmentCount - 1];

            for (i = 0; i < segmentStrengths.Length-1; i++)
            {
                percentageIncrements[i] = Math.Round(segmentStrengths [i+1]/ segmentStrengths[i] * 100, 2);
            }


            bool decreasingSignal=false, increasingSignal = false, invalidSignal = false;

            int centerIndex = segmentStrengths.Length / 2 - 1;

            /*////
            for (int i = 0; i < percentageIncrements.Length-1; i++)
            {                
                if (percentageIncrements[i + 1] < 80)
                    decreasingSignal = true;

                if (percentageIncrements[i + 1] > 120)
                    increasingSignal = true;

                if (i >= centerIndex && percentageIncrements[i + 1] > 150)
                {
                    invalidSignal = true;
                    break;
                }
                else
                    if (i >= centerIndex && percentageIncrements[i+1] < 50)
                    {
                        invalidSignal = true;
                        break;
                    }
            }

            if ((decreasingSignal && increasingSignal) || invalidSignal)
                return 100;
            */

            double avgStrengthIncrement = 0;

            for (i = 0; i < percentageIncrements.Length; i++)
            {
                avgStrengthIncrement += percentageIncrements[i];
            }


            avgStrengthIncrement /= percentageIncrements.Length;
            ////return Math.Round(avgStrengthIncrement, 2);


            /*////double avg1stQuarterStrength = 0;

            int i;

            for (i = 0; i < data.Length / 4; i++)
            {
                avg1stQuarterStrength += data[i];
            }

            avg1stQuarterStrength = avg1stQuarterStrength / (data.Length / 2);


            double avg2ndQuarterStrength = 0;

            for (; i < data.Length/2; i++)
            {
                avg2ndQuarterStrength += data[i];
            }

            avg2ndQuarterStrength = avg2ndQuarterStrength / (data.Length / 4);
            
            return Math.Round(Avg2ndHalfStrength / Avg1stHalfStrength * 100, 2);
            */

            double Avg1stHalfStrength = 0;            

            for (i = 0; i < data.Length/2; i++)
            {
                Avg1stHalfStrength += data[i];            
            }

            Avg1stHalfStrength = Avg1stHalfStrength / (data.Length / 2);


            double Avg2ndHalfStrength = 0;

            for (; i < data.Length; i++)
            {
                Avg2ndHalfStrength += data[i];
            }

            Avg2ndHalfStrength = Avg2ndHalfStrength / (data.Length-(data.Length / 2));

            return Math.Round(Avg2ndHalfStrength/Avg1stHalfStrength*100, 2);                        
        }
    }
}
