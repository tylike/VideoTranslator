using System.Diagnostics;
using PublishToBilibili.Interfaces;

namespace PublishToBilibili.Services
{
    public class ProcessService : IProcessService
    {
        public ProcessInfo? GetProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return null;

            var process = processes[0];
            return new ProcessInfo
            {
                Id = process.Id,
                Name = process.ProcessName,
                Path = process.MainModule?.FileName ?? string.Empty,
                MainWindowHandle = process.MainWindowHandle
            };
        }

        public ProcessInfo? GetProcessByPath(string processPath)
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainModule?.FileName?.Equals(processPath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return new ProcessInfo
                        {
                            Id = process.Id,
                            Name = process.ProcessName,
                            Path = process.MainModule.FileName,
                            MainWindowHandle = process.MainWindowHandle
                        };
                    }
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        public ProcessInfo StartProcess(string processPath)
        {
            var process = Process.Start(processPath);
            if (process == null)
            {
                throw new InvalidOperationException($"Failed to start process: {processPath}");
            }

            process.WaitForInputIdle(5000);

            return new ProcessInfo
            {
                Id = process.Id,
                Name = process.ProcessName,
                Path = processPath,
                MainWindowHandle = process.MainWindowHandle
            };
        }

        public bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public bool IsProcessRunningByPath(string processPath)
        {
            var process = GetProcessByPath(processPath);
            return process != null;
        }
    }
}
