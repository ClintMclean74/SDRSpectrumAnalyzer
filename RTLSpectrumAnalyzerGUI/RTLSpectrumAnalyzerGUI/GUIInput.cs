using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;

namespace RTLSpectrumAnalyzerGUI
{
    public class WindowsHookHelper
    {
        public delegate IntPtr HookDelegate(
            Int32 Code, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr CallNextHookEx(
            IntPtr hHook, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hHook);


        [DllImport("User32.dll")]
        public static extern IntPtr SetWindowsHookEx(
            Int32 idHook, HookDelegate lpfn, IntPtr hmod,
            Int32 dwThreadId);
    }

    public class GUIInput
    {
        public static long lastInputTime;

        public const long AFTER_RECORD_FAR_INPUT_BUFFER = 4 * 1000;
    }

    [StructLayout(LayoutKind.Sequential)]

    public struct POINT
    {
        public int x;

        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]

    struct MSLLHOOKSTRUCT
    {
        public POINT pt;

        public uint mouseData;

        public uint flags;

        public uint time;

        public IntPtr dwExtraInfo;
    }

    public class MouseCoord
    {
        public Point point;
        public long time;

        public MouseCoord(Point point, long time)
        {
            this.point = point;

            this.time = time;
        }
    }

    public class MouseInput : IDisposable
    {
        private WindowsHookHelper.HookDelegate mouseDelegate;
        private IntPtr mouseHandle;
        private const Int32 WH_MOUSE_LL = 14;

        private bool disposed;

        Form1 mainForm;

        Point prevCoord, currentCoord;

        MSLLHOOKSTRUCT hookStruct;

        List<MouseCoord> mouseCoords = new List<MouseCoord>();

        public MouseInput(Form1 mainForm)
        {
            this.mainForm = mainForm;
            mouseDelegate = MouseHookDelegate;
            mouseHandle = WindowsHookHelper.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
        }

        public double GetMovementOverTime(long time)
        {
            int i = mouseCoords.Count - 1;

            MouseCoord currentMouseCoord = mouseCoords[i];

            MouseCoord prevMouseCoord = currentMouseCoord;

            i--;
            while (i>0)
            {
                prevMouseCoord = mouseCoords[i];

                if (currentMouseCoord.time - prevMouseCoord.time > time)
                {
                    if (i< mouseCoords.Count)
                        prevMouseCoord = mouseCoords[i+1];

                    break;
                }

                i--;
            }

            double distance = Math.Sqrt(Math.Pow((currentMouseCoord.point.X - prevMouseCoord.point.X), 2) + Math.Pow((currentMouseCoord.point.Y - prevMouseCoord.point.Y), 2));

            return distance;
        }
        
        private IntPtr MouseHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            GUIInput.lastInputTime = Environment.TickCount;

            Notifications.currentNotificationTimeIndex = 0;

            hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            currentCoord.X = hookStruct.pt.x;
            currentCoord.Y = hookStruct.pt.y;

            mouseCoords.Add(new MouseCoord(currentCoord, GUIInput.lastInputTime));

            if (prevCoord.X==0 && prevCoord.Y==0)
                prevCoord = currentCoord;

            /*////if (!mainForm.showingCheckForReradiatedFrequencyDialog)
                mainForm.CheckForReradiatedFrequency();
                */

            double distance = this.GetMovementOverTime(1000);

            if (distance > 100)
            {            
                mainForm.CheckForStartRecordingNearSeries();
            }

            prevCoord.X = currentCoord.X;
            prevCoord.Y = currentCoord.Y;

            WindowsHookHelper.UnhookWindowsHookEx(mouseHandle);

            mouseHandle = WindowsHookHelper.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);

            return WindowsHookHelper.CallNextHookEx(mouseHandle, Code, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (mouseHandle != IntPtr.Zero)
                    WindowsHookHelper.UnhookWindowsHookEx(mouseHandle);

                disposed = true;
            }
        }

        ~MouseInput()
        {
            Dispose(false);
        }
    }

    public class KeyboardInput : IDisposable
    {
        private WindowsHookHelper.HookDelegate keyBoardDelegate;
        private IntPtr keyBoardHandle;
        private const Int32 WH_KEYBOARD_LL = 13;
        private bool disposed;

        Form1 mainForm;

        public KeyboardInput(Form1 mainForm)
        {
            this.mainForm = mainForm;

            keyBoardDelegate = KeyboardHookDelegate;

            keyBoardHandle = WindowsHookHelper.SetWindowsHookEx(WH_KEYBOARD_LL, keyBoardDelegate, IntPtr.Zero, 0);
        }

        private IntPtr KeyboardHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            GUIInput.lastInputTime = Environment.TickCount;

            Notifications.currentNotificationTimeIndex = 0;

            /*////if (!mainForm.showingCheckForReradiatedFrequencyDialog)
                mainForm.CheckForReradiatedFrequency();
                */

            if (mainForm.UsingProximitryDetection())
            {
                mainForm.CheckForStartRecordingNearSeries();
            }


            WindowsHookHelper.UnhookWindowsHookEx(keyBoardHandle);

            keyBoardHandle = WindowsHookHelper.SetWindowsHookEx(WH_KEYBOARD_LL, keyBoardDelegate, IntPtr.Zero, 0);

            return WindowsHookHelper.CallNextHookEx(keyBoardHandle, Code, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (keyBoardHandle != IntPtr.Zero)
                {
                    WindowsHookHelper.UnhookWindowsHookEx(keyBoardHandle);
                }

                disposed = true;
            }
        }

        ~KeyboardInput()
        {
            Dispose(false);
        }
    }
}
