using System;
using System.Collections.Generic;
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

        public static bool Equals(double a, double b, double tolerance)
        {
            if (Math.Abs(a - b) <= tolerance)
                return true;

            return false;
        }

        public static int FrequencyInSignals(long frequency, long strongestRange, List<InterestingSignal> signals)
        {
            int closestIndex = -1;
            double minDif = Double.NaN, dif;

            Utilities.FrequencyRange frequencyRange = Utilities.GetFrequencyRangeFromFrequency(frequency);

            for (int i = 0; i < signals.Count && i < strongestRange; i++)
            {
                dif = Math.Abs(signals[i].frequency - frequency);

                if ((dif < minDif || Double.IsNaN(minDif)) && signals[i].frequency >= frequencyRange.lower && signals[i].frequency <= frequencyRange.upper)
                {
                    minDif = dif;
                    closestIndex = i;
                }
            }

            return closestIndex;
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
    }
}
