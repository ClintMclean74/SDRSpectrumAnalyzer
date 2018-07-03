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
