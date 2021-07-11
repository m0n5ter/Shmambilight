using System;
using System.Windows;

namespace Shmambilight.Screen
{
    public class MonitorInfo
    {
        public bool IsPrimary { get; set; }
        public Size ScreenSize { get; set; }
        public Rect MonitorArea { get; set; }
        public Rect WorkArea { get; set; }
        public string DeviceName { get; set; }
        public IntPtr HMon { get; set; }
    }
}