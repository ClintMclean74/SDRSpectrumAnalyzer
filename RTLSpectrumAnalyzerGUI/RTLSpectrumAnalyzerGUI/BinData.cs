
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
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{
    public enum BinDataMode { Near, Far, Indeterminate, NotUsed };

    public class BinData
    {
        public string dataSeries;

        public float[] device1BinArray = null;
        public float[] device2BinArray = null;

        public float[] binArray = null;
        public float[] avgBinArray = null;
        public float[] totalBinArray = null;
        public float[] totalBinArrayNumberOfFrames = null;

        public uint bufferFrames = 0;

        public float[] storedTotalBinArray = null;
        public float[] storedTotalBinArrayNumberOfFrames = null;

        public float[] storedAvgBinArray = null;
        public float[] storedBinArray = null;

        public uint storedBufferFrames = 0;                

        public uint size = 0;

        public BinDataMode mode = BinDataMode.Indeterminate;

        public bool clearFrames = false;

        public BinData(uint size, string series, BinDataMode mode)
        {
            this.size = size;

            dataSeries = series;

            this.mode = mode;

            totalBinArray = new float[size];
            totalBinArrayNumberOfFrames = new float[size];

            avgBinArray = new float[size];
            binArray = new float[size];

            device1BinArray = new float[size];
            device2BinArray = new float[size];
        }

        public void SaveData(BinaryWriter writer)
        {            
            writer.Write((UInt32) totalBinArray.Length);

            for (int i = 0; i < totalBinArray.Length; i++)
            {
                writer.Write(totalBinArray[i]);
            }

            for (int i = 0; i < totalBinArray.Length; i++)
            {
                writer.Write(totalBinArrayNumberOfFrames[i]);
            }
        }

        public void LoadData(BinaryReader reader, bool accrue = false)
        {
            long length = reader.ReadUInt32();

            double value;

            if (accrue)
            {
                for (int i = 0; i < this.totalBinArray.Length; i++)
                {
                    try
                    {
                        value = reader.ReadSingle();
                    }
                    catch (Exception ex)
                    {
                        value = 0;
                    }

                    totalBinArray[i] += (float)value;
                }
            }
            else
            {
                for (int i = 0; i < this.totalBinArray.Length; i++)
               {
                    try
                    {
                        value = reader.ReadSingle();
                    }
                    catch (Exception ex)
                    {
                        value = 0;
                    }

                    totalBinArray[i] = (float)value;
                }
            }
                
            if (accrue)
            {
                for (int i = 0; i < this.totalBinArray.Length; i++)
                {
                    totalBinArrayNumberOfFrames[i] += reader.ReadSingle();
                }
            }
            else
                for (int i = 0; i < this.totalBinArray.Length; i++)
                {
                    totalBinArrayNumberOfFrames[i] = reader.ReadSingle();
                }
        }

        public void CalculateAvgBinData()
        {
            for (int i = 0; i < this.size; i++)
            {
                avgBinArray[i] = totalBinArray[i]  / totalBinArrayNumberOfFrames[i];                
            }
        }
        
        public void InitializeBinDataToAvgBinData()
        {
            for (int i = 0; i < this.size; i++)
            {
                binArray[i] = avgBinArray[i];
            }
        }

        public double GetAverageNumberOfFrames()
        {
            double totalNumberOfFrames = 0;

            for (long i = 0; i < totalBinArrayNumberOfFrames.Length; i++)
            {
                totalNumberOfFrames += totalBinArrayNumberOfFrames[i];
            }

            if (totalBinArrayNumberOfFrames.Length > 0)
                return Math.Ceiling(totalNumberOfFrames / totalBinArrayNumberOfFrames.Length);
            else
                return 0;
        }
        
        public double GetAverageNumberOfFramesForFrequencyRegion(long lowerFrequency, long upperFrequency, long dataLowerFrequency, double binFrequencySize)
        {
            Utilities.FrequencyRange frequencyRange = Utilities.GetIndicesForFrequencyRange(lowerFrequency, upperFrequency, dataLowerFrequency, binFrequencySize);

            return GetAverageNumberOfFramesForFrequencyRegion((long)frequencyRange.lower, (long)frequencyRange.upper);
        }

        public double GetAverageNumberOfFramesForFrequencyRegion(long lowerFrequencyIndex, long upperFrequencyIndex)
        {
            if (totalBinArrayNumberOfFrames.Length == 0)
                return 0;

            double totalNumberOfFrames = 0;

            for (long i = lowerFrequencyIndex; i < upperFrequencyIndex; i++)
            {
                totalNumberOfFrames += totalBinArrayNumberOfFrames[i];
            }

            return Math.Ceiling(totalNumberOfFrames / (upperFrequencyIndex - lowerFrequencyIndex));
        }

        public void Restore()
        {
            if (storedTotalBinArray!=null)
            {
                totalBinArray = storedTotalBinArray;
                totalBinArrayNumberOfFrames = storedTotalBinArrayNumberOfFrames;

                avgBinArray = storedAvgBinArray;
                binArray = storedBinArray;

                bufferFrames = storedBufferFrames;
            }
        }

        public void Store()
        {
            storedTotalBinArray = (float[])totalBinArray.Clone();
            storedTotalBinArrayNumberOfFrames = (float[])totalBinArrayNumberOfFrames.Clone();

            storedAvgBinArray = (float[])avgBinArray.Clone();
            storedBinArray = (float[])binArray.Clone();

            storedBufferFrames = bufferFrames;
        }

        public void Clear()
        {            
            for (int i = 0; i < this.size; i++)
            {
                totalBinArray[i] = 0;
                totalBinArrayNumberOfFrames[i] = 0;

                avgBinArray[i] = 0;
                binArray[i] = 0;
            }

            bufferFrames = 0;
        }
    }
}
