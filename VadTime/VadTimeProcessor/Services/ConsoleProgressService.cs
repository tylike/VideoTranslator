using VideoTranslator.Interfaces;

namespace VadTimeProcessor.Services;

/// <summary>
/// 控制台进度服务实现 - 使用控制台输出进度和状态信息
/// </summary>
public class ConsoleProgressService : IProgressService
{
    #region 公共属性

    /// <summary>
    /// 应用程序对象（用于GUI应用程序）
    /// </summary>
    public object Application { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置状态消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="type">消息类型</param>
    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        if (!log)
        {
            return;
        }

        var output = message + (newline ? Environment.NewLine : "");
        switch (type)
        {
            case MessageType.Info:
                Console.Write(output);
                break;
            case MessageType.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[成功] {message}{(newline ? Environment.NewLine : "")}");
                Console.ResetColor();
                break;
            case MessageType.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"[警告] {message}{(newline ? Environment.NewLine : "")}");
                Console.ResetColor();
                break;
            case MessageType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write($"[错误] {message}{(newline ? Environment.NewLine : "")}");
                Console.ResetColor();
                break;
            case MessageType.Debug:
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[调试] {message}{(newline ? Environment.NewLine : "")}");
                Console.ResetColor();
                break;
            case MessageType.Title:
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"=== {message} ==={(newline ? Environment.NewLine : "")}");
                Console.ResetColor();
                break;
        }
    }

    /// <summary>
    /// 显示进度条（控制台模式暂不支持）
    /// </summary>
    /// <param name="marquee">是否使用滚动模式</param>
    public void ShowProgress(bool marquee = false)
    {
    }

    /// <summary>
    /// 隐藏进度条（控制台模式暂不支持）
    /// </summary>
    public void HideProgress()
    {
    }

    /// <summary>
    /// 重置进度（控制台模式暂不支持）
    /// </summary>
    public void ResetProgress()
    {
    }

    public void ReportProgress(double value)
    {
        Console.WriteLine($"进度: {value:F2}%");
    }

    public void SetProgressMaxValue(double value)
    {
    }

    #endregion
}
