using VadTimeProcessor.Models;
using VadTimeProcessor.Services;
using VideoTranslator.Config;
using VideoTranslator.Services;
using VT.Core;

namespace VideoTranslator.Interfaces;
public interface ISpeechRecognitionServiceVad : ISpeechRecognitionService
{
    /// <summary>
    /// 使用预检测的VAD段落进行语音识别
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出SRT文件路径</param>
    /// <param name="language">语言代码</param>
    /// <param name="vadSegments">预检测的VAD段落列表</param>
    /// <returns>输出的SRT文件路径</returns>
    Task<string> RecognizeAudioAsync(string audioPath, string outputPath, string language, IEnumerable<ISpeechSegment> vadSegments);
}
public class WhisperRecognitionServiceVad : ServiceBase, ISpeechRecognitionServiceVad
{    
    private readonly string _whisperServerUrl;

    public WhisperRecognitionServiceVad()
        : base()
    {        
        _whisperServerUrl = Settings.WhisperServerUrl ?? throw new Exception("没有配置WhisperServerUrl!");
    }

    public async Task<string> RecognizeAudioAsync(string audioPath, string outputPath, string language)
    {
        progress?.Report($"[SpeechRecognitionServiceVad] 开始VAD增强识别");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"输出文件: {outputPath}");
        progress?.Report($"Whisper服务器: {_whisperServerUrl}");
        progress?.Report();

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        WhisperServerClient.SetServerUrl(_whisperServerUrl);

        var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
        var transcriber = AudioTranscriber.Create(audioPath);

        var result = await transcriber.TranscribeAsync();

        if (!result.IsSuccess)
        {
            throw new Exception($"转录失败: {result.ErrorMessage}");
        }

        var sourceSrtPath = result.MergedSrtPath;
        if (File.Exists(sourceSrtPath))
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(sourceSrtPath, outputPath, overwrite: true);
            progress?.Report($"SRT已复制到: {outputPath}");
        }
        else
        {
            throw new FileNotFoundException($"未找到生成的SRT文件: {sourceSrtPath}");
        }

        progress?.Report("[OK] 识别完成");
        return outputPath;
    }

    public async Task<string> RecognizeAudioAsync(string audioPath, string outputPath, string language, IEnumerable<ISpeechSegment> vadSegments)
    {
        progress?.Report($"[SpeechRecognitionServiceVad] 开始VAD增强识别（使用预检测的VAD段落）");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"输出文件: {outputPath}");
        progress?.Report($"Whisper服务器: {_whisperServerUrl}");
        progress?.Report($"VAD段落数量: {vadSegments.Count()}");
        progress?.Report();

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        WhisperServerClient.SetServerUrl(_whisperServerUrl);
        WhisperServerClient.SetProgressService(this.progress);
        var options = TranscribeOptions.CreateDefault(audioPath);
        options.Whisper.ServerUrl = _whisperServerUrl;
        var transcriber = AudioTranscriber.Create(options, vadSegments);

        var result = await transcriber.TranscribeAsync();

        if (!result.IsSuccess)
        {
            throw new Exception($"转录失败: {result.ErrorMessage}");
        }

        var sourceSrtPath = result.MergedSrtPath;
        if (File.Exists(sourceSrtPath))
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(sourceSrtPath, outputPath, overwrite: true);
            progress?.Report($"SRT已复制到: {outputPath}");
        }
        else
        {
            throw new FileNotFoundException($"未找到生成的SRT文件: {sourceSrtPath}");
        }

        progress?.Report("[OK] 识别完成");
        return outputPath;
    }
}
