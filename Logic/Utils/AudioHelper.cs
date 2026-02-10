using NAudio.Wave;

namespace VideoTranslator.Utils;

public static class AudioHelper
{
    #region 获取音频时长（秒）

    public static double GetAudioDurationSeconds(string audioPath)
    {
        #region 参数验证

        if (string.IsNullOrWhiteSpace(audioPath))
        {
            throw new ArgumentException("音频路径不能为空", nameof(audioPath));
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("音频文件不存在", audioPath);
        }

        #endregion

        #region 根据文件扩展名选择合适的读取方式

        var extension = Path.GetExtension(audioPath).ToLower();

        try
        {
            return extension switch
            {
                ".wav" => GetWavDuration(audioPath),
                ".mp3" => GetMp3Duration(audioPath),
                ".flac" => GetFlacDuration(audioPath),
                ".m4a" or ".aac" => GetM4aDuration(audioPath),
                _ => GetGenericAudioDuration(audioPath)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"获取音频时长失败: {audioPath}", ex);
        }

        #endregion
    }

    #endregion

    #region 获取音频时长（TimeSpan）

    public static TimeSpan GetAudioDuration(string audioPath)
    {
        return TimeSpan.FromSeconds(GetAudioDurationSeconds(audioPath));
    }

    #endregion

    #region 获取音频时长（毫秒）

    public static long GetAudioDurationMilliseconds(string audioPath)
    {
        return (long)(GetAudioDurationSeconds(audioPath) * 1000);
    }

    #endregion

    #region WAV文件时长获取

    private static double GetWavDuration(string wavPath)
    {
        using var audioFile = new AudioFileReader(wavPath);
        return audioFile.TotalTime.TotalSeconds;
    }

    #endregion

    #region MP3文件时长获取

    private static double GetMp3Duration(string mp3Path)
    {
        using var mp3Reader = new Mp3FileReader(mp3Path);
        return mp3Reader.TotalTime.TotalSeconds;
    }

    #endregion

    #region FLAC文件时长获取

    private static double GetFlacDuration(string flacPath)
    {
        using var audioFile = new AudioFileReader(flacPath);
        return audioFile.TotalTime.TotalSeconds;
    }

    #endregion

    #region M4A/AAC文件时长获取

    private static double GetM4aDuration(string m4aPath)
    {
        using var audioFile = new AudioFileReader(m4aPath);
        return audioFile.TotalTime.TotalSeconds;
    }

    #endregion

    #region 通用音频文件时长获取

    private static double GetGenericAudioDuration(string audioPath)
    {
        using var audioFile = new AudioFileReader(audioPath);
        return audioFile.TotalTime.TotalSeconds;
    }

    #endregion

    #region 获取音频信息

    public static AudioFileInfo GetAudioInfo(string audioPath)
    {
        #region 参数验证

        if (string.IsNullOrWhiteSpace(audioPath))
        {
            throw new ArgumentException("音频路径不能为空", nameof(audioPath));
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("音频文件不存在", audioPath);
        }

        #endregion

        #region 读取音频信息

        using var audioFile = new AudioFileReader(audioPath);

        var info = new AudioFileInfo
        {
            FilePath = audioPath,
            Duration = audioFile.TotalTime,
            DurationSeconds = audioFile.TotalTime.TotalSeconds,
            SampleRate = audioFile.WaveFormat.SampleRate,
            Channels = audioFile.WaveFormat.Channels,
            BitsPerSample = audioFile.WaveFormat.BitsPerSample,
            AverageBytesPerSecond = audioFile.WaveFormat.AverageBytesPerSecond
        };

        return info;

        #endregion
    }

    #endregion

    #region 格式化时长显示

    public static string FormatDuration(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalHours >= 1)
        {
            return timeSpan.ToString(@"hh\:mm\:ss\.fff");
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return timeSpan.ToString(@"mm\:ss\.fff");
        }
        else
        {
            return timeSpan.ToString(@"ss\.fff");
        }
    }

    public static string FormatDuration(TimeSpan duration)
    {
        return FormatDuration(duration.TotalSeconds);
    }

    #endregion

    #region 检查是否为支持的音频格式

    public static bool IsSupportedAudioFormat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLower();
        var supportedExtensions = new[] { ".wav", ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".wma", ".aiff" };

        return supportedExtensions.Contains(extension);
    }

    #endregion
}

#region 音频信息类

public class AudioFileInfo
{
    public string FilePath { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public double DurationSeconds { get; set; }

    public int SampleRate { get; set; }

    public int Channels { get; set; }

    public int BitsPerSample { get; set; }

    public int AverageBytesPerSecond { get; set; }

    public override string ToString()
    {
        return $"文件: {Path.GetFileName(FilePath)} | " +
               $"时长: {AudioHelper.FormatDuration(Duration)} | " +
               $"采样率: {SampleRate}Hz | " +
               $"声道: {Channels} | " +
               $"位深: {BitsPerSample}bit";
    }
}

#endregion
