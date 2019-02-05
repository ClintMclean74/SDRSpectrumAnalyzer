
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
using System.Runtime.InteropServices;
using System.Text;

namespace RTLSpectrumAnalyzerGUI
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void RtlSdrReadAsyncDelegate(byte* buf, uint len, IntPtr ctx);

    public enum RtlSdrTunerType
    {
        Unknown = 0,
        E4000,
        FC0012,
        FC0013,
        FC2580,
        R820T
    }

    public class NativeMethods
    {
        private const string LibRtlSdr = "RTLSDRDevice.dll";

        [DllImport(LibRtlSdr, EntryPoint = "GetConnectedDevicesCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetConnectedDevicesCount();        

        [DllImport(LibRtlSdr, EntryPoint = "Initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Initialize(uint startFrequency, uint endFrequency, uint stepSize, uint maxSamplingRate);

        [DllImport(LibRtlSdr, EntryPoint = "GetBufferSize", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetBufferSize();
        
        [DllImport(LibRtlSdr, EntryPoint = "GetTotalADCValue", CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetTotalADCValue(int deviceIndex);

        [DllImport(LibRtlSdr, EntryPoint = "GetBins", CallingConvention = CallingConvention.Cdecl)]        
        public static extern void GetBins(float[] buffer, int deviceIndex, double rangeSamplingPercentage, bool usetotalADCMagnitude = false, uint scanCount = 1);

        [DllImport(LibRtlSdr, EntryPoint = "GetBinsForDevices", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetBinsForDevices(float[] binArray1, float[] binArray2, int deviceIndex1, int deviceIndex2);

        [DllImport(LibRtlSdr, EntryPoint = "ScanAndGetTotalADCMagnitudeForFrequency", CallingConvention = CallingConvention.Cdecl)]
        public static extern double ScanAndGetTotalADCMagnitudeForFrequency(uint frequency);        

        [DllImport(LibRtlSdr, EntryPoint = "GetTotalMagnitude", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetTotalMagnitude();

        [DllImport(LibRtlSdr, EntryPoint = "GetAvgMagnitude", CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetAvgMagnitude();

        [DllImport(LibRtlSdr, EntryPoint = "SetUseDB", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetUseDB(int value);

        [DllImport(LibRtlSdr, EntryPoint = "get_device_name", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get_device_name_native(uint index);

        public static string get_device_name(uint index)
        {
            var strptr = get_device_name_native(index);
            return Marshal.PtrToStringAnsi(strptr);
        }

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_device_usb_strings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_device_usb_strings(uint index, StringBuilder manufact, StringBuilder product, StringBuilder serial);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_open(out IntPtr dev, uint index);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_close(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_xtal_freq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_xtal_freq(IntPtr dev, uint rtlFreq, uint tunerFreq);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_xtal_freq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_xtal_freq(IntPtr dev, out uint rtlFreq, out uint tunerFreq);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_usb_strings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_usb_strings(IntPtr dev, StringBuilder manufact, StringBuilder product, StringBuilder serial);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_center_freq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_center_freq(IntPtr dev, uint freq);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_center_freq", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint rtlsdr_get_center_freq(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_freq_correction", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_freq_correction(IntPtr dev, int ppm);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_freq_correction", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_freq_correction(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_tuner_gains", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_tuner_gains(IntPtr dev, [In, Out] int[] gains);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_tuner_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern RtlSdrTunerType rtlsdr_get_tuner_type(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_tuner_gain", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain(IntPtr dev, int gain);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_tuner_gain", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_tuner_gain(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_tuner_gain_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain_mode(IntPtr dev, int manual);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_agc_mode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_agc_mode(IntPtr dev, int on);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_direct_sampling", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_direct_sampling(IntPtr dev, int on);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_offset_tuning", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_offset_tuning(IntPtr dev, int on);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_sample_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_sample_rate(IntPtr dev, uint rate);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_get_sample_rate", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint rtlsdr_get_sample_rate(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_set_testmode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_testmode(IntPtr dev, int on);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_reset_buffer", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_reset_buffer(IntPtr dev);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_read_sync", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_read_sync(IntPtr dev, IntPtr buf, int len, out int nRead);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_wait_async", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_wait_async(IntPtr dev, RtlSdrReadAsyncDelegate cb, IntPtr ctx);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_read_async", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_read_async(IntPtr dev, RtlSdrReadAsyncDelegate cb, IntPtr ctx, uint bufNum, uint bufLen);

        [DllImport(LibRtlSdr, EntryPoint = "rtlsdr_cancel_async", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_cancel_async(IntPtr dev);
    }
}
