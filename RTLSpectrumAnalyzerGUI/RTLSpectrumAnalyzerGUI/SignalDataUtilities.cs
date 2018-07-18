using System;

namespace RTLSpectrumAnalyzerGUI
{
    public class SignalDataUtilities
    {
        public static double CalculateGradient(double[] data)
        {
            double totalGradients = 0;

            for(int i = 1; i<data.Length; i++)
            {
                totalGradients += (data[i] - data[i - 1]);
            }

            return totalGradients / (data.Length - 1);
        }

        public static double Series2ndVS1stHalfAvgStrength(double[] data)
        {
            int segmentCount = 4;

            double[] segmentStrengths = new double[segmentCount];

            double segmentLength = (double) data.Length / segmentCount;

            segmentLength = Math.Ceiling(segmentLength);

            int j = 0;

            for (int i = 0; i < data.Length; i++)
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

            for (int i = 0; i < segmentStrengths.Length-1; i++)
            {
                percentageIncrements[i] = Math.Round(segmentStrengths [i+1]/ segmentStrengths[i] * 100, 2);
            }


            bool decreasingSignal=false, increasingSignal = false;

            for (int i = 0; i < percentageIncrements.Length-1; i++)
            {
                if (percentageIncrements[i + 1] < percentageIncrements[i] * 0.8)
                    decreasingSignal = true;

                if (percentageIncrements[i + 1] > percentageIncrements[i] * 1.2)
                    increasingSignal=true;
            }

            if (decreasingSignal && increasingSignal)
                return 100;

            double avgStrengthIncrement = 0;

            for (int i = 0; i < percentageIncrements.Length; i++)
            {
                avgStrengthIncrement += percentageIncrements[i];
            }


            avgStrengthIncrement /= percentageIncrements.Length;
            return Math.Round(avgStrengthIncrement, 2);


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

            /*////double Avg1stHalfStrength = 0;

            int i;

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
            */
        }
    }
}
