using System;
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{
    public class Gradient
    {
        public double strength;
        public double stackedFrames;

        public Gradient(double strength, double stackedFrames)
        {
            this.strength = strength;

            this.stackedFrames = stackedFrames;
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write(strength);

            writer.Write(stackedFrames);            
        }

        public void LoadData(BinaryReader reader)
        {
            strength = reader.ReadDouble();

            stackedFrames = reader.ReadDouble();
        }
    }

    public class GradientArray
    {
        public long time;
        public Gradient[] gradientArray;
        
        public GradientArray(long size)
        {
            gradientArray = new Gradient[size];
        }

        public void SaveData(BinaryWriter writer)
        {            
            writer.Write((UInt32)time);

            writer.Write((UInt32)gradientArray.Length);

            for (int j = 0; j < gradientArray.Length; j++)
            {
                gradientArray[j].SaveData(writer);                
            }
        }

        public void LoadData(BinaryReader reader)
        {
            time = reader.ReadUInt32();
            
            uint length  = reader.ReadUInt32();

            for (int j = 0; j < gradientArray.Length; j++)
            {
                gradientArray[j] = new Gradient(0, 0);
                gradientArray[j].LoadData(reader);
            }
        }
    }
}
