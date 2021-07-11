using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Shmambilight.Screen
{
    public static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("({Left}, {Top}), ({Width} x {Height})")]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        private class MONITORINFOEX
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("User32.dll", CharSet=CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out]MONITORINFOEX info);

        public static IList<MonitorInfo> GetScreens()
        {
            var screens = new List<MonitorInfo>();

            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Native.RECT lprcMonitor, IntPtr dwData)
            {
                var mi = new MONITORINFOEX();

                if (GetMonitorInfo(hMonitor, mi))
                {
                    var deviceName = new string(mi.szDevice.Where(_ => _ != (char) 0).ToArray());

                    screens.Add(new MonitorInfo
                    {
                        ScreenSize = new Size(mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                        MonitorArea = new Rect(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                        WorkArea = new Rect(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right - mi.rcWork.Left, mi.rcWork.Bottom - mi.rcWork.Top),
                        IsPrimary = mi.dwFlags > 0,
                        HMon = hMonitor,
                        DeviceName = deviceName.StartsWith("\\\\.\\") ? deviceName.Substring(4) : deviceName
                    });
                }

                return true;
            }, IntPtr.Zero);

            return screens;
        }
    }
}