using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Shmambilight.Utils
{
    public class ProcessUtils
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static Process GetForegroundProcess()
        {
            var hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out var processID);
            var process = Process.GetProcessById(Convert.ToInt32(processID));
            return process;
        }
    }
}