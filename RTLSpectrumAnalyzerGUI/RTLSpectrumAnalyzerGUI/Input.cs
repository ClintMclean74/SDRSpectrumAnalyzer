using System;

using System.Runtime.InteropServices;

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
        public long lastInputTime;
    }

    public class MouseInput : IDisposable
    {
        private WindowsHookHelper.HookDelegate mouseDelegate;
        private IntPtr mouseHandle;
        private const Int32 WH_MOUSE_LL = 14;

        private bool disposed;

        Form1 mainForm;

        public MouseInput(Form1 mainForm)
        {
            this.mainForm = mainForm;
            mouseDelegate = MouseHookDelegate;
            mouseHandle = WindowsHookHelper.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
        }

        private IntPtr MouseHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            mainForm.lastInputTime = Environment.TickCount;

            mainForm.currentNotificationTimeIndex = 0;

            if (mainForm.checkBox9.Checked)
            {
                if (mainForm.recordingSeries1)
                {
                    /*////mainForm.series1BinData.numberOfFrames += mainForm.bufferFrames.AddBufferRangeIntoArray(mainForm.bufferFrames.bufferFramesArray, mainForm.series1BinData.totalBinArray, Mode.Far);
                    mainForm.series1BinData.bufferFrames = 0;
                    */

                    ////mainForm.RecordSeries1();
                    mainForm.recordingSeries2 = true;
                    mainForm.recordingSeries1 = false;

                    mainForm.transitionBufferNearIndex = mainForm.bufferFrames.currentBufferIndex;


                    ////mainForm.RecordSeries2();
                }
                else if (mainForm.recordingSeries2 && mainForm.recordingIntoBuffer)
                {
                    ////mainForm.bufferFrames.Change(Mode.Indeterminate, Mode.Near);
                }
            }

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
            mainForm.lastInputTime = Environment.TickCount;

            mainForm.currentNotificationTimeIndex = 0;

            if (mainForm.checkBox9.Checked)
            {
                if (mainForm.recordingSeries1)
                {
                    /*////mainForm.series1BinData.numberOfFrames += mainForm.bufferFrames.AddBufferRangeIntoArray(mainForm.bufferFrames.bufferFramesArray, mainForm.series1BinData.totalBinArray, Mode.Far);
                    mainForm.series1BinData.bufferFrames = 0;
                    */

                    mainForm.recordingSeries2 = true;
                    mainForm.recordingSeries1 = false;
                }
                else if (mainForm.recordingSeries2 && mainForm.recordingIntoBuffer)
                {
                    ////mainForm.bufferFrames.Change(Mode.Indeterminate, Mode.Near);
                }
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
