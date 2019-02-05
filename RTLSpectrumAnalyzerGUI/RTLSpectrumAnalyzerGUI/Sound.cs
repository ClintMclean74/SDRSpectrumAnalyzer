
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
using System.Threading;

namespace RTLSpectrumAnalyzerGUI
{
    class Sound
    {
        static Thread _beepThread;
        static AutoResetEvent _signalBeep;

        static int frequency = 1000;
        static int duration = 1000;

        static long soundDelay;
        static long prevSoundTime;

        public static long SOUND_FREQUENCY_MAXIMUM = 24000;

        static Sound()
        {
            _signalBeep = new AutoResetEvent(false);
            _beepThread = new Thread(() =>
            {
                for (; ; )
                {
                    _signalBeep.WaitOne();

                    soundDelay = DateTime.Now.Ticks - prevSoundTime;


                    if (frequency < 100)
                        frequency = 100;
                    else if (frequency > SOUND_FREQUENCY_MAXIMUM)
                        frequency = 10000;

                    Console.Beep(frequency, duration);


                    prevSoundTime = DateTime.Now.Ticks;
                }
            }, 1);
            _beepThread.IsBackground = true;
            _beepThread.Start();
        }

        public static void PlaySound(int soundFrequency, int soundDuration)
        {
            frequency = soundFrequency;
            duration = soundDuration;
            _signalBeep.Set();
        }
    }
}
