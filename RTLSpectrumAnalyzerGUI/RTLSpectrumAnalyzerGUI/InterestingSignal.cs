
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
