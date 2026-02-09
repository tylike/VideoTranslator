//global using MessageType = MT;
using System.Runtime.CompilerServices;
namespace NewConsole;
public class Console
{
    static DateTime? last;
    public static void WriteLine(string message = "",MT type = MT.Info,[CallerMemberName]string from = "",[CallerFilePath]string filePath = "",[CallerLineNumber]int lineNumber = 0)
    {
        var now = DateTime.Now;
        if (last.HasValue)
        { var sw = now - last.Value;
            if(sw > TimeSpan.FromSeconds(1))
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                var fileLink = $"file:///{filePath.Replace('\\', '/')}#L{lineNumber}";
                System.Console.WriteLine($"用时长:{sw.TotalMilliseconds} - [{from}]({fileLink})");
            }
        }
        last = now;

        #region 设置消息颜色
        switch (type)
        {
            case MT.Info:
                System.Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case MT.Error:
                System.Console.ForegroundColor = ConsoleColor.Red;
                break;
            case MT.Warning:
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case MT.Success:
                System.Console.ForegroundColor = ConsoleColor.Green;
                break;
        }
        #endregion

        System.Console.WriteLine($"[{DateTime.Now:mm:ss.fff}]{message}");

        System.Console.ForegroundColor = ConsoleColor.White;
    }

}
public enum MT
{
    Info,
    Error,
    Warning,
    Success
}
