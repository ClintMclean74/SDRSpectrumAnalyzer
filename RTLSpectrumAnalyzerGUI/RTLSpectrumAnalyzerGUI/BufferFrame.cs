
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
