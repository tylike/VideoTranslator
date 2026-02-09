using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VT.Win
{
    internal static class ConsoleAllocator
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr handle);

        private static bool _consoleAllocated = false;

        public static bool ShowConsole(string[] args)
        {
            bool shouldShow = !ContainsArgument(args, "noconsole") && !ContainsArgument(args, "nc");
            if (shouldShow)
            {
                return Allocate();
            }
            return false;
        }

        public static bool Allocate()
        {
            if (_consoleAllocated)
                return true;

            if (AllocConsole())
            {
                _consoleAllocated = true;
                RedirectConsoleOutput();
                Console.WriteLine("控制台已分配");
                return true;
            }
            return false;
        }

        public static void Free()
        {
            if (_consoleAllocated)
            {
                FreeConsole();
                _consoleAllocated = false;
            }
        }

        private static void RedirectConsoleOutput()
        {
            var handleOut = GetStdHandle(-11);
            var handleErr = GetStdHandle(-12);
            var fsOut = new FileStream(new SafeFileHandle(handleOut, false), FileAccess.Write);
            var fsErr = new FileStream(new SafeFileHandle(handleErr, false), FileAccess.Write);
            var writerOut = new StreamWriter(fsOut, System.Console.OutputEncoding) { AutoFlush = true };
            var writerErr = new StreamWriter(fsErr, System.Console.OutputEncoding) { AutoFlush = true };
            System.Console.SetOut(writerOut);
            System.Console.SetError(writerErr);
        }

        private static bool ContainsArgument(string[] args, string argument)
        {
            return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
        }
    }
}
