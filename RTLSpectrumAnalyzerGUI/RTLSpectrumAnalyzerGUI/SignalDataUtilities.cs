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
            double Avg1stHalfStrength = 0;

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
        }
    }
}
