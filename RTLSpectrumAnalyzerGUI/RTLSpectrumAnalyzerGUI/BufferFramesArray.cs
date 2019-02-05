
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
    public class TransitionGradient
    {
        public long index;
        public long frequency;
        public double strength;
        public long transitions;
        public int ranking = -1;
        public Gradient gradient;
        public long rangeWidth;
        public long startFrequency;
        public long endFrequency;

        public TransitionGradient(long frequency, long index, double strength, long transitions, Gradient gradient, long rangeWidth=1, long startFrequency = -1, long endFrequency = -1)
        {
            this.index = index;
            this.frequency = frequency;
            this.strength = strength;
            this.transitions = transitions;
            this.gradient = gradient;

            this.rangeWidth = rangeWidth;

            this.startFrequency = startFrequency;
            this.endFrequency = endFrequency;
        }
    }

    public class TransitionGradientArray
    {        
        public List<TransitionGradient> array;

        public TransitionGradientArray()
        {
            array = new List<TransitionGradient>();
        }

        public long Add(TransitionGradient transitionGradient)
        {
            if (transitionGradient!=null)
                array.Add(transitionGradient);

            return array.Count;
        }

        public long AddArray(TransitionGradientArray transitionGradientArray)
        {
            for (int i = 0; i < transitionGradientArray.array.Count; i++)
            {
                array.Add(transitionGradientArray.array[i]);
            }

            return array.Count;
        }

        public TransitionGradient GetTransitionGradientForFrequency(long startFrequency, long endFrequency, double tolerance = 100)
        {            
            for (int i = 0; i < array.Count; i++)
            {
                if (Math.Abs(startFrequency - array[i].startFrequency) <= tolerance && Math.Abs(endFrequency - array[i].endFrequency) <= tolerance)
                    return array[i];
            }

            return null;
        }

        public TransitionGradient GetTransitionGradientForFrequency(long frequency, double tolerance=100)
        {
            long min = 999999999;
            long dif;
            int minIndex = -1;

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] != null)
                {
                    dif = Math.Abs(frequency - array[i].frequency);

                    if (min == 999999999 || dif < min)
                    {
                        min = dif;

                        minIndex = i;
                    }
                }
            }

            if (minIndex > -1 && min <= tolerance)
            {
                array[minIndex].ranking = minIndex;

                return array[minIndex];
            }
            
            return null;
        }


        public void SortAccordingToFrequency()
        {
            array.Sort(delegate (TransitionGradient x, TransitionGradient y)
            {                
                if (x.frequency > y.frequency)
                    return 1;
                else if (x.frequency == y.frequency)
                    return 0;
                else
                    return -1;
            });
        }


        public void Sort()
        {
            array.Sort(delegate (TransitionGradient x, TransitionGradient y)
            {
                /*////if (x.strength*x.transitions < y.strength * y.transitions)
                    return 1;
                else if (x.strength * x.transitions == y.strength * y.transitions)
                    return 0;
                else
                    return -1;
                    */

                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                if (x==null || x.strength < y.strength)
                    return 1;
                else if (x.strength == y.strength)
                    return 0;
                else
                    return -1;
            });
        }
    }

    public class AnalysisOption
    {
        public int leaderBoardSignalIndex;
        public BufferFramesObject bufferFramesObject;

        public AnalysisOption(int leaderBoardSignalIndex, BufferFramesObject bufferFramesObject)
        {
            this.leaderBoardSignalIndex = leaderBoardSignalIndex;

            this.bufferFramesObject = bufferFramesObject;
        }
    }

    public class BufferFramesObject
    {
        public BufferFrames bufferFrames, transitionBufferFrames;

        public long lowerFrequency;
        public long upperFrequency;

        public double binSize;

        public bool possibleReradiatedFrequencyRange = true;

        public bool[] options = new bool[10];

        public int reradiatedRankingCategory = -1;

        public BufferFramesObject(Form1 mainForm, long lowerFrequency, long upperFrequency, double binSize)
        {
            this.lowerFrequency = lowerFrequency;
            this.upperFrequency = upperFrequency;

            this.binSize = binSize;

            bufferFrames = new BufferFrames(mainForm, this);

            transitionBufferFrames = new BufferFrames(mainForm, this);
        }

        public bool EvaluateWhetherReradiatedFrequency(long frequency)
        {
            return transitionBufferFrames.EvaluateWhetherReradiatedFrequency(frequency);
        }

        public bool EvaluateWhetherReradiatedFrequencyRange()
        {
            /*////possibleReradiatedFrequencyRange = transitionBufferFrames.EvaluateWhetherReradiatedFrequencyRange();

            return possibleReradiatedFrequencyRange;
            */

            reradiatedRankingCategory = transitionBufferFrames.EvaluatereRadiatedRankingCategory();

            return true;
        }

        public void Flush(BinData farSeries, BinData nearSeries, BinData indeterminateSeries)
        {
            bufferFrames.Flush(farSeries, nearSeries, indeterminateSeries);
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write((UInt32)this.lowerFrequency);
            writer.Write((UInt32)this.upperFrequency);

            writer.Write(this.binSize);

            bufferFrames.SaveData(writer);
            transitionBufferFrames.SaveData(writer);            
        }

        public void LoadData(BinaryReader reader, bool accrue = false)
        {
            /*////lowerFrequency = reader.ReadUInt32();
            upperFrequency = reader.ReadUInt32();
            binSize = reader.ReadDouble();
            */

            try
            {
                bufferFrames.LoadData(reader, accrue);
                transitionBufferFrames.LoadData(reader, accrue);
            }
            catch(Exception  ex)
            {

            }
        }

        public void Change(BinDataMode prevMode, BinDataMode newMode)
        {
            bufferFrames.Change(prevMode, newMode);
        }
    }

    public class BufferFramesArray
    {
        List<BufferFramesObject> bufferFramesObjects = new List<BufferFramesObject>();

        public BufferFramesArray()
        {
        }

        public void Flush(BinData farSeries, BinData nearSeries, BinData indeterminateSeries)
        {
            for (int i = 0; i < bufferFramesObjects.Count; i++)
                bufferFramesObjects[i].Flush(farSeries, nearSeries, indeterminateSeries);
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write((UInt32) bufferFramesObjects.Count);

            for (int i = 0; i < bufferFramesObjects.Count; i++)
            {
                bufferFramesObjects[i].SaveData(writer);
            }
        }

        public void LoadData(BinaryReader reader, Form1 mainForm, bool accrue = false)
        {
            long bufferFramesObjectsLength = reader.ReadUInt32();

            long lowerFrequency;
            long upperFrequency;
            double binSize;

            if (accrue)
            {
                BufferFramesObject bufferFramesObject;

                for (int i = 0; i < bufferFramesObjectsLength; i++)
                {
                    lowerFrequency = reader.ReadUInt32();
                    upperFrequency = reader.ReadUInt32();
                    binSize = reader.ReadDouble();

                    bufferFramesObject = GetBufferFramesObject(lowerFrequency, upperFrequency);

                    if (bufferFramesObject == null)
                    {
                        bufferFramesObjects.Add(new BufferFramesObject(mainForm, lowerFrequency, upperFrequency, binSize));

                        bufferFramesObject = bufferFramesObjects[bufferFramesObjects.Count - 1];
                    }

                    bufferFramesObject.LoadData(reader, accrue);

                    bufferFramesObject.EvaluateWhetherReradiatedFrequencyRange();
                }
            }
            else
            {
                for (int i = 0; i < bufferFramesObjectsLength; i++)
                {
                    lowerFrequency = reader.ReadUInt32();
                    upperFrequency = reader.ReadUInt32();
                    binSize = reader.ReadDouble();

                    bufferFramesObjects.Add(new BufferFramesObject(mainForm, lowerFrequency, upperFrequency, binSize));

                    bufferFramesObjects[bufferFramesObjects.Count - 1].LoadData(reader);

                    bufferFramesObjects[bufferFramesObjects.Count - 1].EvaluateWhetherReradiatedFrequencyRange();
                }
            }
        }

        public long AddBufferFramesObject(BufferFramesObject bufferObject)
        {
            bufferFramesObjects.Add(bufferObject);

            return bufferFramesObjects.Count;
        }

        public BufferFramesObject GetBufferFramesObject(int index)
        {
            if (index>=0 && index< bufferFramesObjects.Count)
                return bufferFramesObjects[index];

            return null;
        }

        public BufferFramesObject GetBufferFramesObject(long lowerFrequency, long upperFrequency)
        {
            for(int i=0; i<bufferFramesObjects.Count; i++)
            {
                if (bufferFramesObjects[i].lowerFrequency == lowerFrequency && bufferFramesObjects[i].upperFrequency == upperFrequency)
                {
                    return bufferFramesObjects[i];
                }
            }

            return null;
        }

        public BufferFramesObject GetBufferFramesObjectForFrequency(long frequency)
        {
            int startIndex;

            if (bufferFramesObjects[0].upperFrequency - bufferFramesObjects[0].lowerFrequency <= 1000000)
                startIndex = 0;
            else
                startIndex = 1;

            for (int i = startIndex; i < bufferFramesObjects.Count; i++)
            {
                if (frequency >= bufferFramesObjects[i].lowerFrequency && frequency < bufferFramesObjects[i].upperFrequency)
                {
                    return bufferFramesObjects[i];
                }
            }

            return null;
        }
        

        public bool EvaluateWhetherReradiatedFrequency(long frequency)
        {
            BufferFramesObject bufferFramesObjectForFrequency = GetBufferFramesObjectForFrequency(frequency);

            return bufferFramesObjectForFrequency.EvaluateWhetherReradiatedFrequency(frequency);
        }


        public TransitionGradient GetTransitionsGradientForFrequency(long frequency)
        {
            BufferFramesObject bufferFramesObjectForFrequency = GetBufferFramesObjectForFrequency(frequency);

            return bufferFramesObjectForFrequency.transitionBufferFrames.GetTransitionsGradientForFrequency(frequency);
        }

        public TransitionGradient GetRangeTransitionsGradientForFrequency(long frequency)
        {
            BufferFramesObject bufferFramesObjectForFrequency = GetBufferFramesObjectForFrequency(frequency);

            return bufferFramesObjectForFrequency.transitionBufferFrames.GetTransitionsGradient();
        }

        public TransitionGradientArray GetStrongestTransitionsFrequencyGradientArray(bool frequencyRanges)
        {
            ////frequencyRanges = false;

            TransitionGradientArray transitionGradientArray = new TransitionGradientArray();

            TransitionGradientArray transitionGradientArrayZoomed;

            int startIndex;

            if (bufferFramesObjects[0].upperFrequency-bufferFramesObjects[0].lowerFrequency<=1000000)
                startIndex = 0;
            else
                startIndex = 1;

            for (int i = startIndex; i < bufferFramesObjects.Count; i++)
            {
                ////if (bufferFramesObjects[i].possibleReradiatedFrequencyRange)
                {
                    if (!frequencyRanges)
                    {
                        transitionGradientArrayZoomed = bufferFramesObjects[i].transitionBufferFrames.GetStrongestTransitionsGradientFrequency();

                        transitionGradientArray.AddArray(transitionGradientArrayZoomed);
                    }
                    else
                    {
                        transitionGradientArray.Add(bufferFramesObjects[i].transitionBufferFrames.GetTransitionsGradient());
                    }
                }
            }

            return transitionGradientArray;
        }

        public void Change(BinDataMode prevMode, BinDataMode newMode)
        {
            for (int i = 0; i < bufferFramesObjects.Count; i++)
            {
                bufferFramesObjects[i].Change(prevMode, newMode);
            }
        }

        public void Clear()
        {
            bufferFramesObjects.Clear();
        }
    }
}
