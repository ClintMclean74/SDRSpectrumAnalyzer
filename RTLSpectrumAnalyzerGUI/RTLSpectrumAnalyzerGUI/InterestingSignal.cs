using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTLSpectrumAnalyzerGUI
{
    class InterestingSignal
    {
        public int index = -1;
        public double strength = 0;
        public double strengthDif = 0;
        public double rating = 0;
        public double frequency = 0;

        public double totalChange = 0;
        public double avgChange = 0;

        private int MAX_AVG_COUNT = 10;
        private int avgCount = 0;

        public double maxStrength = -1;
        public double minStrength = -1;

        public double maxAvgStrength = -1;
        public double minAvgStrength = -1;

        public bool invertedDif = false;

        List<Double> prevStrengths = new List<Double>();

        public InterestingSignal(int index, double strength, double strengthDif, double frequency)
        {
            this.index = index;
            this.strength = strength;
            this.strengthDif = strengthDif;

            this.frequency = frequency;
        }

        public void Set(int index, double strength, double strengthDif, double frequency)
        {
            this.index = index;
            this.strength = strength;
            this.strengthDif = strengthDif;

            this.frequency = frequency;
        }

        public void SetStrength(double strength)
        {
            totalChange += (strength - this.strength);

            this.strength = strength;

            if (prevStrengths.Count > 100)
                prevStrengths.RemoveAt(0);

            prevStrengths.Add(this.strength);

            maxStrength = -1;
            minStrength = -1;

            for (int i = 0; i < prevStrengths.Count; i++)
            {
                if (prevStrengths[i] > maxStrength)
                    maxStrength = prevStrengths[i];

                if (minStrength == -1 || prevStrengths[i] < minStrength)
                    minStrength = prevStrengths[i];
            }

            avgCount++;

            if (avgCount >= MAX_AVG_COUNT)
            {
                avgChange = totalChange / MAX_AVG_COUNT;

                totalChange = 0;

                avgCount = 0;
            }
        }

        public void SetStrengthDif(double strengthDif)
        {
            this.strengthDif = strengthDif;
        }

        public double AvgChange()
        {
            return avgChange;
        }

        public void ResetAvgChange()
        {
            avgChange = 0;
        }
    }
}
