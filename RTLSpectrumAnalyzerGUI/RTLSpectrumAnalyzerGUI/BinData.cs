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

        public void LoadData(BinaryReader reader)
        {
            long length = reader.ReadUInt32();

            double value;

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

            for (int i = 0; i < this.totalBinArray.Length; i++)
            {
                totalBinArrayNumberOfFrames[i] = reader.ReadSingle();
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
