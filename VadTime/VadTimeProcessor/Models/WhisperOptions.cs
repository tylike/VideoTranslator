using System.IO;

namespace VadTimeProcessor.Models;

/// <summary>
/// Whisper转录选项 - 封装Whisper语音识别相关配置
/// </summary>
public class WhisperOptions
{
    #region 构造函数

    public WhisperOptions()
    {
        ServerUrl = "http://127.0.0.1:8080";
        ServerExecutablePath = @"d:\VideoTranslator\whisper.cpp\whisper-server.exe";
        ServerModelPath = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin";
        Temperature = 0.0;
        AutoStartServer = true;
        StopServerAfterTranscribe = false;
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// Whisper服务器URL（默认：http://127.0.0.1:8080）
    /// </summary>
    public string ServerUrl { get; set; }

    /// <summary>
    /// Whisper服务器可执行文件路径（默认：d:\VideoTranslator\whisper.cpp\whisper-server.exe）
    /// </summary>
    public string ServerExecutablePath { get; set; }

    /// <summary>
    /// Whisper服务器模型路径（默认：d:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin）
    /// </summary>
    public string ServerModelPath { get; set; }

    /// <summary>
    /// 温度参数（默认0.0）
    /// 控制转录的随机性，值越低结果越稳定
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// 是否自动启动Whisper服务器（默认true）
    /// 如果检测到服务器未运行，则自动启动
    /// </summary>
    public bool AutoStartServer { get; set; }

    /// <summary>
    /// 转录完成后是否停止Whisper服务器（默认false）
    /// 如果为true，则转录完成后会停止服务器进程
    /// </summary>
    public bool StopServerAfterTranscribe { get; set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 验证选项是否有效
    /// </summary>
    public void Validate()
    {
        if (AutoStartServer)
        {
            if (string.IsNullOrEmpty(ServerExecutablePath))
            {
                throw new ArgumentException("Whisper服务器可执行文件路径不能为空", nameof(ServerExecutablePath));
            }

            if (!File.Exists(ServerExecutablePath))
            {
                throw new FileNotFoundException($"Whisper服务器可执行文件不存在: {ServerExecutablePath}");
            }

            if (string.IsNullOrEmpty(ServerModelPath))
            {
                throw new ArgumentException("Whisper服务器模型路径不能为空", nameof(ServerModelPath));
            }

            if (!File.Exists(ServerModelPath))
            {
                throw new FileNotFoundException($"Whisper服务器模型文件不存在: {ServerModelPath}");
            }
        }

        if (Temperature < 0.0)
        {
            throw new ArgumentException("温度参数不能为负数", nameof(Temperature));
        }
    }

    /// <summary>
    /// 创建默认选项
    /// </summary>
    public static WhisperOptions CreateDefault()
    {
        return new WhisperOptions();
    }

    #endregion
}
