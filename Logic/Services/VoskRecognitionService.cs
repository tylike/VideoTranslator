using Nikse.SubtitleEdit.Core.AudioToText;
using Nikse.SubtitleEdit.Core.Common;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;
using Vosk;
using VT.Core;

using Configuration = Nikse.SubtitleEdit.Core.Common.Configuration;

namespace VideoTranslator.Services;

public class VoskRecognitionService : ServiceBase
{
    private readonly string _modelPath;
    private string _currentLanguageCode = "en";
    private string? _spkModelPath;
    private string? _prompt;

    public VoskRecognitionService() : base()
    {
        _modelPath = Settings.VoskModelPath;        
    }

    #region Public Methods

    public async Task<List<ISrtSubtitle>> RecognizeAsync(string audioPath, string languageCode, string? spkModelPath = null, string? prompt = null)
    {
        _currentLanguageCode = languageCode;
        _spkModelPath = spkModelPath;
        _prompt = prompt;
        progress?.Report($"[VoskRecognitionService] 开始语音识别");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"模型路径: {_modelPath}");
        progress?.Report($"语言代码: {languageCode}");
        if (!string.IsNullOrEmpty(_spkModelPath))
        {
            progress?.Report($"说话人模型路径: {_spkModelPath}");
        }
        if (!string.IsNullOrEmpty(_prompt))
        {
            progress?.Report($"提示词: {_prompt}");
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        if (!Directory.Exists(_modelPath))
        {
            throw new DirectoryNotFoundException($"Vosk模型目录不存在: {_modelPath}");
        }

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToVoskFormatAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在加载Vosk模型...");



        var results = await Task.Run(() =>
        {
            return this.TranscribeViaVosk(wavPath, _modelPath,_prompt);
        });

        progress?.Report($"[VoskRecognitionService] 识别完成，共识别到 {results.Count} 个片段");

        var postProcessor = new AudioToTextPostProcessor(languageCode)
        {
            ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
        };
        var temp = postProcessor.Fix(AudioToTextPostProcessor.Engine.Vosk, results, true, true, true, true, false, false);


        #region 清理临时文件
        if (wavPath != audioPath && File.Exists(wavPath))
        {
            try
            {
                File.Delete(wavPath);
                progress?.Report($"已删除临时音频文件: {wavPath}");
            }
            catch (Exception ex)
            {
                progress?.Error($"删除临时文件失败: {ex.Message}");
            }
        }
        #endregion

        List<ISrtSubtitle> subtitles = new List<ISrtSubtitle>();
        var isrts = temp.Paragraphs.Select(x => new SrtSubtitle(x.Number, x.StartTime.TimeSpan,x.EndTime.TimeSpan,x.Text)).ToArray();
        subtitles.AddRange(isrts);
        return subtitles;
    }

    #endregion

    #region Audio Preprocessing

    private async Task<string> ConvertAudioToVoskFormatAsync(string audioPath)
    {
        progress?.Report($"[音频预处理] 开始转换音频文件");
        progress?.Report($"  输入文件: {audioPath}");

        var extension = Path.GetExtension(audioPath).ToLower();
        if (extension == ".wav")
        {
            progress?.Report("[音频预处理] 检测到WAV文件，检查格式...");
            var audioInfo = await CheckWavFormatAsync(audioPath);
            if (audioInfo.IsVoskCompatible)
            {
                progress?.Report("[音频预处理] WAV格式已符合Vosk要求，无需转换");
                return audioPath;
            }
            progress?.Report($"[音频预处理] WAV格式不符合要求: 采样率={audioInfo.SampleRate}Hz, 声道={audioInfo.Channels}");
        }

        var tempDir = Path.GetTempPath();
        var tempFileName = $"vosk_temp_{Guid.NewGuid():N}.wav";
        var outputPath = Path.Combine(tempDir, tempFileName);

        progress?.Report($"[音频预处理] 输出文件: {outputPath}");

        var args = $"-i \"{audioPath}\" " +
                   $"-vn " +
                   $"-ar 16000 " +
                   $"-ac 1 " +
                   $"-ab 32k " +
                   $"-af volume=1.75 " +
                   $"-f wav " +
                   $"-y \"{outputPath}\"";

        progress?.Report($"[音频预处理] FFmpeg参数: {args}");

        try
        {
            await Ffmpeg.ExecuteCommandAsync(args);
            progress?.Report("[音频预处理] 音频转换成功");
            return outputPath;
        }
        catch (Exception ex)
        {
            progress?.Error($"[音频预处理] 音频转换失败: {ex.Message}");
            throw;
        }
    }

    private async Task<(bool IsVoskCompatible, int SampleRate, int Channels)> CheckWavFormatAsync(string wavPath)
    {
        var args = $"-i \"{wavPath}\" -f null -";
        try
        {
            var output = await Ffmpeg.ExecuteCommandAsync(args);

            int sampleRate = 0;
            int channels = 0;

            var sampleRateMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*Hz");
            if (sampleRateMatch.Success)
            {
                int.TryParse(sampleRateMatch.Groups[1].Value, out sampleRate);
            }

            var channelsMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*channel");
            if (channelsMatch.Success)
            {
                int.TryParse(channelsMatch.Groups[1].Value, out channels);
            }

            var isCompatible = sampleRate == 16000 && channels == 1;
            return (isCompatible, sampleRate, channels);
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    #endregion


    #region Cleanup

    public void Dispose()
    {
        //_model?.Dispose();
        //_model = null;
    }

    #endregion

    public List<ResultText> TranscribeViaVosk(string waveFileName, string modelFileName,string prompt)
    {
        Directory.SetCurrentDirectory(modelFileName);
        var _model = new Model(modelFileName);

        var rec = string.IsNullOrEmpty(prompt) ? new VoskRecognizer(_model, 16000.0f) : new VoskRecognizer(_model, 16000.0f, prompt);

        rec.SetMaxAlternatives(0);
        rec.SetWords(true);

        #region 设置说话人识别模型
        if (!string.IsNullOrEmpty(_spkModelPath) && Directory.Exists(_spkModelPath))
        {
            try
            {
                progress?.Report($"正在加载说话人识别模型: {_spkModelPath}");
                var spkModel = new SpkModel(_spkModelPath);
                rec.SetSpkModel(spkModel);
                progress?.Report("说话人识别模型加载成功");
            }
            catch (Exception ex)
            {
                progress?.Error($"说话人识别模型加载失败: {ex.Message}");
            }
        }
        #endregion

        var list = new List<ResultText>();
        var buffer = new byte[4096];
        var _bytesWavTotal = new FileInfo(waveFileName).Length;
        var _bytesWavRead = 0;
        var _startTicks = Stopwatch.GetTimestamp();
        using (var source = File.OpenRead(waveFileName))
        {
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                _bytesWavRead += bytesRead;

                var p = (int)(_bytesWavRead * 100.0 / _bytesWavTotal);
                progress.ReportProgress(p);
                if (rec.AcceptWaveform(buffer, bytesRead))
                {
                    var res = rec.Result();
                    var results = ParseJsonToResult(res);
                    list.AddRange(results);
                }
                else
                {
                    var res = rec.PartialResult();
                    //textBoxLog.AppendText(res.RemoveChar('\r', '\n'));
                }
            }
        }

        var finalResult = rec.FinalResult();
        var finalResults = ParseJsonToResult(finalResult);
        list.AddRange(finalResults);
        return list;
    }

    public static List<ResultText> ParseJsonToResult(string result)
    {
        var list = new List<ResultText>();
        var jsonParser = new SeJsonParser();
        var root = jsonParser.GetArrayElementsByName(result, "result");
        foreach (var item in root)
        {
            var conf = jsonParser.GetFirstObject(item, "conf");
            var start = jsonParser.GetFirstObject(item, "start");
            var end = jsonParser.GetFirstObject(item, "end");
            var word = jsonParser.GetFirstObject(item, "word");
            if (!string.IsNullOrWhiteSpace(word) &&
                decimal.TryParse(conf, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var confidence) &&
                decimal.TryParse(start, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var startSeconds) &&
                decimal.TryParse(end, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var endSeconds))
            {
                var rt = new ResultText { Confidence = confidence, Text = word, Start = startSeconds, End = endSeconds };
                list.Add(rt);
            }
        }

        return list;
    }

}

