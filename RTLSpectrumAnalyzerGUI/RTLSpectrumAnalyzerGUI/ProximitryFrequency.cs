namespace RTLSpectrumAnalyzerGUI
{
    class ProximitryFrequency
    {
        public uint frequency = 420000000;
        public double totalADCMagnitude = 0;
        public double avgTotalADCMagnitude = 0;
        public double maxStrength = -9999999999999999;
        public double minStrength = 9999999999999999;

        public uint sampleCount = 0;

        public ProximitryFrequency()
        {

        }
    }    
}
