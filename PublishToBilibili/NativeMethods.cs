using System;
using System.Runtime.InteropServices;

namespace PublishToBilibili
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
