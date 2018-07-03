using System;
using System.Collections.Generic;

namespace RTLSpectrumAnalyzerGUI
{
    class Command
    {
        public string name;
        public long time;

        public Command(string name, long time)
        {
            this.name = name;

            this.time = time;
        }        
    }

    class CommandBuffer
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
            Command command = new Command(name, Environment.TickCount);
            this.AddCommand(command);

            return commandArray.Count;
        }

        public int RemoveCommand()
        {
            if (commandArray.Count>0)
                commandArray.RemoveAt(commandArray.Count-1);

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
    }
}