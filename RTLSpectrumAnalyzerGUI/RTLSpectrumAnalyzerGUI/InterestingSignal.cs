using System;
using System.Collections.Generic;
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{
    public class InterestingSignal
    {
        public int index = -1;
        public double strength = 0;
        public double strengthDif = 0;
        public double avgGradientStrength;
        public double rating = 0;
        public double frequency = 0;

        public double lowerFrequency = 0;
        public double upperFrequency = 0;

        public double rangeTotal = 0;
        public double rangeTotalCount = 0;


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

        public InterestingSignal(int index, double strength, double strengthDif, double frequency, double lowerFrequency = -1, double upperFrequency = -1)
        {
            this.index = index;
            this.strength = strength;
            this.strengthDif = strengthDif;

            this.frequency = frequency;

            this.lowerFrequency = lowerFrequency;

            this.upperFrequency = upperFrequency;
        }

        public void LoadData(BinaryReader reader, bool accrue = false)
        {
            ////this.frequency = reader.ReadUInt32();
            ////this.index = reader.ReadInt32();

            if (accrue)
            {
                this.strength += reader.ReadDouble();
                this.strengthDif += reader.ReadDouble();
                this.rating += reader.ReadDouble();
                this.totalChange += reader.ReadDouble();
                this.avgChange += reader.ReadDouble();

                this.avgCount += reader.ReadInt32();
            }
            else
            {
                this.strength = reader.ReadDouble();
                this.strengthDif = reader.ReadDouble();
                this.rating = reader.ReadDouble();
                this.totalChange = reader.ReadDouble();
                this.avgChange = reader.ReadDouble();

                this.avgCount = reader.ReadInt32();
            }

            this.maxStrength = reader.ReadDouble();
            this.minStrength = reader.ReadDouble();
            this.maxAvgStrength = reader.ReadDouble();
            this.minAvgStrength = reader.ReadDouble();

            this.invertedDif = reader.ReadBoolean();
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write((UInt32)this.frequency);
            writer.Write((UInt32)this.index);
            writer.Write(this.strength);
            writer.Write(this.strengthDif);

            writer.Write(this.rating);

            writer.Write(this.totalChange);
            writer.Write(this.avgChange);
            writer.Write((UInt32)this.avgCount);
            writer.Write(this.maxStrength);
            writer.Write(this.minStrength);
            writer.Write(this.maxAvgStrength);
            writer.Write(this.minAvgStrength);

            writer.Write(this.invertedDif);
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
