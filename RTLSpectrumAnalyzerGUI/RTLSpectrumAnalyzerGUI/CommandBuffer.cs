
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

namespace RTLSpectrumAnalyzerGUI
{
    public class Command
    {
        public string name;
        public long time;

        public Command(string name, long time)
        {
            this.name = name;

            this.time = time;
        }        
    }

    public class CommandBuffer
    {
        public const uint MAX_BUFFER_SIZE = 100;

        public List<Command> commandArray = new List<Command>();

        public int startBufferIndex = 0;
        public int commandCurrentBufferIndex = -1;

        public CommandBuffer()
        {
        }

        public int AddCommand(Command command)
        {
            if (commandArray.Count < MAX_BUFFER_SIZE)
            {
                commandArray.Add(command);
                commandCurrentBufferIndex = commandArray.Count - 1;
            }
            else
            {
                if (startBufferIndex >= MAX_BUFFER_SIZE)
                    startBufferIndex = 0;

                commandCurrentBufferIndex = startBufferIndex;

                commandArray[commandCurrentBufferIndex].name = command.name;
                commandArray[commandCurrentBufferIndex].time = command.time;

                startBufferIndex++;
            }


            return commandArray.Count;
        }

        public int AddCommand(string name)
        {
            Command command = new Command(name, (Environment.TickCount & int.MaxValue));
            this.AddCommand(command);

            return commandArray.Count;
        }

        public int RemoveCommand()
        {
            if (commandCurrentBufferIndex!=-1)
            {
                commandArray.RemoveAt(commandCurrentBufferIndex);

                if (commandCurrentBufferIndex == startBufferIndex)
                    commandCurrentBufferIndex = -1;
                else
                    if (commandCurrentBufferIndex == 0)
                        commandCurrentBufferIndex = commandArray.Count-1;
                    else
                        commandCurrentBufferIndex--;
            }

            return commandArray.Count;
        }

        public Command GetMostRecentCommand()
        {
            if (commandArray.Count > 0)
                return commandArray[commandCurrentBufferIndex];
            else
                return null;
        }

        public Command GetCommand(int index)
        {
            if (commandArray.Count > 0 && index>-1)
                return commandArray[index];
            else
                return null;
        }        

        public void Clear()
        {
            commandArray.Clear();

            startBufferIndex = 0;
            commandCurrentBufferIndex = -1;
        }
    }
}