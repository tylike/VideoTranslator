namespace PublishToBilibili.Interfaces
{
    public interface IProcessService
    {
        ProcessInfo? GetProcess(string processName);
        ProcessInfo? GetProcessByPath(string processPath);
        ProcessInfo StartProcess(string processPath);
        bool IsProcessRunning(string processName);
        bool IsProcessRunningByPath(string processPath);
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public IntPtr MainWindowHandle { get; set; }
    }
}
