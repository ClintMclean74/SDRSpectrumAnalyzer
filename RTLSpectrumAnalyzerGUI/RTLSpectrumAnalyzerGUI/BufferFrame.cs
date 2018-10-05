using System;
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{   
    public class BufferFrame
    {
        public long time;
        public long transitionTime;
        
        public float[] bufferArray;

        public BinDataMode mode = BinDataMode.Indeterminate;

        public long stackedFrames = 1;

        public BufferFrame(long size, BinDataMode mode)
        {
            bufferArray = new float[size];

            this.mode = mode;
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write((UInt32)stackedFrames);
            writer.Write((UInt32)time);

            writer.Write((UInt32)transitionTime);

            writer.Write((int)mode);            

            writer.Write((UInt32)bufferArray.Length);

            for (int j = 0; j < bufferArray.Length; j++)
            {
                writer.Write(bufferArray[j]);
            }            
        }

        public void LoadData(BinaryReader reader)
        {
            stackedFrames = reader.ReadUInt32();
            time = reader.ReadUInt32();

            transitionTime = reader.ReadUInt32();

            mode = (BinDataMode) reader.ReadUInt32();            

            uint length = reader.ReadUInt32();

            for (int j = 0; j < bufferArray.Length; j++)
            {
                bufferArray[j] = reader.ReadSingle();
            }
        }

        public BufferFrame Clone()
        {
            BufferFrame newBufferFrame = new BufferFrame(bufferArray.Length, this.mode);

            for (int j = 0; j < bufferArray.Length; j++)
            {
                newBufferFrame.bufferArray[j] = bufferArray[j];
            }

            newBufferFrame.stackedFrames = stackedFrames;

            newBufferFrame.time = time;

            newBufferFrame.transitionTime = transitionTime;            

            return newBufferFrame;
        }
    }
}
