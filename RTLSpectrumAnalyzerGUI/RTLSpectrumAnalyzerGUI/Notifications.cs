namespace RTLSpectrumAnalyzerGUI
{
    class Notifications
    {
        #if SDR_DEBUG
            ////public static long[] notificationTime = { 10000, 20000 };
            public static long[] notificationTime = { 50000, 51000, 52000, 53000, 54000, 55000, 56000, 57000, 58000, 59000, 60000 };
        #else
            public static long[] notificationTime = { 50000, 51000, 52000, 53000, 54000, 55000, 56000, 57000, 58000, 59000, 60000 };       
        #endif

        public static uint currentNotificationTimeIndex = 0;
    }
}
