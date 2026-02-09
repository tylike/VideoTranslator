using Nikse.SubtitleEdit.Core.AudioToText;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.Services;

public class PurfviewFasterWhisperRecognitionService : ServiceBase
{
    private readonly string _whisperPath;
    private readonly string _modelPath;
    private readonly bool _translateToEnglish;
    private readonly bool _usePostProcessing = true;
    private readonly bool _autoAdjustTimings = true;    
    private readonly ISubtitleService _subtitleService;

    public PurfviewFasterWhisperRecognitionService(        
        ISubtitleService subtitleService,
        bool translateToEnglish = false,
        bool usePostProcessing = true,
        bool autoAdjustTimings = true
        ) : base()
    {
        var settings = base.Settings;
        _whisperPath = settings.PurfviewFasterWhisperPath;
        _modelPath = settings.PurfviewFasterWhisperModelPath;
        _translateToEnglish = translateToEnglish;
        _usePostProcessing = usePostProcessing;
        _autoAdjustTimings = autoAdjustTimings;
        
        _subtitleService = subtitleService;
    }

    #region Public Methods

    public async Task<Language> DetectLanguageAsync(string audioPath)
    {
        progress?.Report("[PurfviewFasterWhisperRecognitionService] 开始检测音频语言");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"Whisper路径: {_whisperPath}");
        progress?.Report($"模型路径: {_modelPath}");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        if (!File.Exists(_whisperPath))
        {
            throw new FileNotFoundException($"PurfviewFasterWhisper可执行文件不存在: {_whisperPath}");
        }

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在调用PurfviewFasterWhisper进行语言检测...");

        var detectedLanguage = await Task.Run(() =>
        {
            return DetectLanguageViaWhisper(wavPath);
        });

        progress?.Report($"[PurfviewFasterWhisperRecognitionService] 语言检测完成: {detectedLanguage}");

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
                progress?.Warning($"删除临时文件失败: {ex.Message}");
            }
        }
        #endregion

        return detectedLanguage;
    }

    public async Task<List<ISrtSubtitle>> RecognizeAsync(string audioPath, Language languageCode, string? prompt = null)
    {
        progress?.Report("[PurfviewFasterWhisperRecognitionService] 开始语音识别");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"Whisper路径: {_whisperPath}");
        progress?.Report($"模型路径: {_modelPath}");
        progress?.Report($"语言代码: {languageCode}");
        progress?.Report($"翻译到英语: {_translateToEnglish}");
        progress?.Report($"自动调整时间: {_autoAdjustTimings}");
        if (!string.IsNullOrEmpty(prompt))
        {
            progress?.Report($"提示词: {prompt}");
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        if (!File.Exists(_whisperPath))
        {
            throw new FileNotFoundException($"PurfviewFasterWhisper可执行文件不存在: {_whisperPath}");
        }

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在调用PurfviewFasterWhisper进行识别...");

        var results = await Task.Run(() =>
        {
            return TranscribeViaWhisper(wavPath, audioPath, languageCode, prompt);
        });

        progress?.Report($"[PurfviewFasterWhisperRecognitionService] 识别完成，共识别到 {results.Count} 个片段");

        #region 保存初始识别结果
        var audioDirectory = Path.GetDirectoryName(audioPath);
        var audioFileNameWithoutExt = Path.GetFileNameWithoutExtension(audioPath);
        var initialSrtPath = Path.Combine(audioDirectory, $"{audioFileNameWithoutExt}_未处理识别结果.srt");
        
        progress?.Report($"正在保存初始识别结果: {initialSrtPath}");
        var initialSubtitles = results.Select(x => new SrtSubtitle(0, TimeSpan.FromSeconds((double)x.Start), TimeSpan.FromSeconds((double)x.End), x.Text)).ToList();
        var index = 1;
        var initialSubtitlesWithIndex = initialSubtitles.Select(x => new SrtSubtitle(index++, x.StartTime, x.EndTime, x.Text)).OfType<ISrtSubtitle>().ToList();
        await _subtitleService.SaveSrtAsync(initialSubtitlesWithIndex, initialSrtPath);
        progress?.Report("初始识别结果保存完成");
        #endregion

        #region 使用波形数据调整时间轴
        if (_autoAdjustTimings)
        {
            progress?.Report("正在使用波形数据调整时间轴...");
            var wavePeaks = MakeWavePeaks(wavPath);
            if (wavePeaks != null)
            {
                progress?.Report("正在缩短过长的字幕...");
                var subtitle = new Subtitle();
                subtitle.Paragraphs.AddRange(results.Select(p => new Paragraph(p.Text, (double)p.Start * 1000.0, (double)p.End * 1000.0)).ToList());

                subtitle = WhisperTimingFixer.ShortenLongDuration(subtitle);
                subtitle = WhisperTimingFixer.ShortenViaWavePeaks(subtitle, wavePeaks);

                results = subtitle.Paragraphs.Select(p => new ResultText
                {
                    Start = (decimal)p.StartTime.TotalSeconds,
                    End = (decimal)p.EndTime.TotalSeconds,
                    Text = p.Text
                }).ToList();
                progress?.Report("时间轴调整完成");

                #region 保存波形修复后的结果
                var adjustedSrtPath = Path.Combine(audioDirectory, $"{audioFileNameWithoutExt}_修波形后.srt");
                progress?.Report($"正在保存波形修复后的结果: {adjustedSrtPath}");
                var adjustedSubtitles = results.Select(x => new SrtSubtitle(0, TimeSpan.FromSeconds((double)x.Start), TimeSpan.FromSeconds((double)x.End), x.Text)).ToList();
                index = 1;
                var adjustedSubtitlesWithIndex = adjustedSubtitles.Select(x => new SrtSubtitle(index++, x.StartTime, x.EndTime, x.Text)).OfType<ISrtSubtitle>().ToList();
                await _subtitleService.SaveSrtAsync(adjustedSubtitlesWithIndex, adjustedSrtPath);
                progress?.Report("波形修复后的结果保存完成");
                #endregion
            }
            else
            {
                progress?.Warning("波形数据生成失败，跳过时间轴调整");
            }
        }
        #endregion

        #region 后处理
        if (_usePostProcessing)
        {
            progress?.Report("正在进行后处理...");
            var postProcessor = new AudioToTextPostProcessor(languageCode.ToWhisperCode())
            {
                ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
            };
            var temp = postProcessor.Fix(AudioToTextPostProcessor.Engine.Whisper, results, true, true, true, true, false, false);

            results = temp.Paragraphs.Select(p => new ResultText
            {
                Start = (decimal)p.StartTime.TotalSeconds,
                End = (decimal)p.EndTime.TotalSeconds,
                Text = p.Text
            }).ToList();
        }
        #endregion

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
                progress?.Warning($"删除临时文件失败: {ex.Message}");
            }
        }
        #endregion

        List<ISrtSubtitle> subtitles = new List<ISrtSubtitle>();
        int nindex = 1;
        var isrts = results.Select(x => new SrtSubtitle(nindex++, TimeSpan.FromSeconds((double)x.Start), TimeSpan.FromSeconds((double)x.End), x.Text)).ToArray();
        subtitles.AddRange(isrts);
        return subtitles;
    }

    #endregion

    #region Audio Preprocessing

    private async Task<string> ConvertAudioToWavAsync(string audioPath)
    {
        progress?.Report("[音频预处理] 开始转换音频文件");
        progress?.Report($"  输入文件: {audioPath}");

        var extension = Path.GetExtension(audioPath).ToLower();
        if (extension == ".wav")
        {
            progress?.Report("[音频预处理] 检测到WAV文件，检查格式...");
            var audioInfo = await CheckWavFormatAsync(audioPath);
            if (audioInfo.SampleRate == 16000 && audioInfo.Channels == 1)
            {
                progress?.Report("[音频预处理] WAV格式已符合Whisper要求，无需转换");
                return audioPath;
            }
            progress?.Report($"[音频预处理] WAV格式不符合要求: 采样率={audioInfo.SampleRate}Hz, 声道={audioInfo.Channels}");
        }

        var tempDir = Path.GetTempPath();
        var tempFileName = $"purfview_whisper_temp_{Guid.NewGuid():N}.wav";
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

    private async Task<(int SampleRate, int Channels)> CheckWavFormatAsync(string wavPath)
    {
        var args = $"-i \"{wavPath}\" -f null -";
        try
        {
            var output = await Ffmpeg.ExecuteCommandAsync(args);

            int sampleRate = 0;
            int channels = 0;

            var sampleRateMatch = Regex.Match(output, @"(\d+)\s*Hz");
            if (sampleRateMatch.Success)
            {
                int.TryParse(sampleRateMatch.Groups[1].Value, out sampleRate);
            }

            var channelsMatch = Regex.Match(output, @"(\d+)\s*channel");
            if (channelsMatch.Success)
            {
                int.TryParse(channelsMatch.Groups[1].Value, out channels);
            }

            return (sampleRate, channels);
        }
        catch
        {
            return (0, 0);
        }
    }

    #endregion

    #region Waveform Processing

    private WavePeakData MakeWavePeaks(string _audioPath)
    {
        if (string.IsNullOrEmpty(_audioPath) || !File.Exists(_audioPath))
        {
            progress?.Report("[MakeWavePeaks] 音频文件不存在或路径为空");
            return null;
        }

        progress?.Report("[MakeWavePeaks] 开始生成波形数据");
        progress?.Report($"  音频文件: {_audioPath}");

        var targetFile = _audioPath;
        var delayInMilliseconds = 0;

        try
        {
            if (File.Exists(targetFile))
            {
                progress?.Report("[MakeWavePeaks] 正在生成波形峰值数据...");
                using (var waveFile = new WavePeakGenerator(targetFile))
                {
                    var peakFileName = WavePeakGenerator.GetPeakWaveFileName(_audioPath);
                    progress?.Report($"  波形峰值文件: {peakFileName}");

                    var wavePeaks = waveFile.GeneratePeaks(delayInMilliseconds, peakFileName);
                    progress?.Report("[MakeWavePeaks] 波形数据生成完成");
                    return wavePeaks;
                }
            }
        }
        catch (Exception ex)
        {
            progress?.Error($"[MakeWavePeaks] 生成波形数据失败: {ex.Message}");
            Debug.WriteLine($"[MakeWavePeaks] 错误: {ex}");
        }

        return null;
    }

    #endregion

    #region Transcription

    public Language DetectLanguageViaWhisper(string waveFileName)
    {
        var whisperFolder = Path.GetDirectoryName(_whisperPath);
        var jsonFileName = string.Empty;

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            var inputFileName = Path.GetFileNameWithoutExtension(waveFileName);
            jsonFileName = Path.Combine(whisperFolder, inputFileName + ".json");
        }

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] DetectLanguageViaWhisper - 开始检测语言");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] DetectLanguageViaWhisper - 模型路径: {_modelPath}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] DetectLanguageViaWhisper - 输入文件: {waveFileName}");

        var inputFile = waveFileName;

        var process = GetWhisperProcessForLanguageDetection(inputFile, _modelPath, OutputHandler);

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] DetectLanguageViaWhisper - 可执行文件: {process.StartInfo.FileName}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] DetectLanguageViaWhisper - 参数: {process.StartInfo.Arguments}");

        var sw = Stopwatch.StartNew();
        progress?.Report($"Calling PurfviewFasterWhisper for language detection with: {process.StartInfo.FileName} {process.StartInfo.Arguments}{Environment.NewLine}");

        try
        {
            process.PriorityClass = ProcessPriorityClass.Normal;
        }
        catch
        {
        }

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        progress?.Report($"PurfviewFasterWhisper language detection done in {sw.Elapsed}{Environment.NewLine}");
        process.Dispose();

        if (!string.IsNullOrEmpty(jsonFileName) && File.Exists(jsonFileName))
        {
            var detectedLanguage = LanguageExtensions.TryParseFromJson(jsonFileName);
            if (detectedLanguage.HasValue)
            {
                progress?.Report($"从JSON文件中检测到语言: {detectedLanguage.Value}");
                
                try
                {
                    File.Delete(jsonFileName);
                    progress?.Report($"已删除临时JSON文件: {jsonFileName}");
                }
                catch (Exception ex)
                {
                    progress?.Warning($"删除临时JSON文件失败: {ex.Message}");
                }

                return detectedLanguage.Value;
            }
        }

        progress?.Report("无法从JSON文件中检测语言，返回默认值: English");
        return Language.English;
    }

    public List<ResultText> TranscribeViaWhisper(string waveFileName, string videoFileName, Language languageCode, string prompt)
    {
        var _resultList = new List<ResultText>();
        var _filesToDelete = new List<string>();

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] TranscribeViaWhisper - 开始转录");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] TranscribeViaWhisper - 模型路径: {_modelPath}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] TranscribeViaWhisper - 语言代码: {languageCode}");

        var inputFile = waveFileName;

        var process = GetWhisperProcess(inputFile, _modelPath, languageCode, false, OutputHandler);

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] TranscribeViaWhisper - 可执行文件: {process.StartInfo.FileName}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] TranscribeViaWhisper - 参数: {process.StartInfo.Arguments}");

        var sw = Stopwatch.StartNew();
        progress?.Report($"Calling PurfviewFasterWhisper with: {process.StartInfo.FileName} {process.StartInfo.Arguments}{Environment.NewLine}");

        try
        {
            process.PriorityClass = ProcessPriorityClass.Normal;
        }
        catch
        {
        }

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        progress?.Report($"PurfviewFasterWhisper done in {sw.Elapsed}{Environment.NewLine}");
        process.Dispose();

        if (GetResultFromSrt(waveFileName, videoFileName, out var resultTexts, _filesToDelete))
        {
            return resultTexts;
        }
        progress?.Report("Loading result from STDOUT" + Environment.NewLine);
        return _resultList.OrderBy(p => p.Start).ToList();
    }

    #endregion

    #region Process Creation

    private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        progress.Report(outLine.Data);
    }

    public Process GetWhisperProcessForLanguageDetection(string waveFileName, string modelPath, DataReceivedEventHandler dataReceivedHandler = null)
    {
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - 开始创建语言检测进程");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - Whisper路径: {_whisperPath}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - 输入文件: {waveFileName}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - 模型路径: {modelPath}");

        var outputSrt = "--standard --beep_off --output_format json ";
        var parameters = $"--model \"{modelPath}\" {outputSrt} \"{waveFileName}\"";

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - 完整参数: {_whisperPath} {parameters}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(_whisperPath, parameters)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };

        if (!string.IsNullOrEmpty(Configuration.Settings.General.FFmpegLocation) && process.StartInfo.EnvironmentVariables["Path"] != null)
        {
            process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + Path.GetDirectoryName(Configuration.Settings.General.FFmpegLocation);
        }

        var whisperFolder = Path.GetDirectoryName(_whisperPath);
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - Whisper文件夹: {whisperFolder}");

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            process.StartInfo.WorkingDirectory = whisperFolder;
            Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcessForLanguageDetection - 工作目录: {whisperFolder}");
        }

        if (!string.IsNullOrEmpty(whisperFolder) && process.StartInfo.EnvironmentVariables["Path"] != null)
        {
            process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + whisperFolder;
        }

        process.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        process.StartInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

        if (dataReceivedHandler != null)
        {
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += dataReceivedHandler;
            process.ErrorDataReceived += dataReceivedHandler;
        }

        return process;
    }

    public Process GetWhisperProcess(string waveFileName, string modelPath, Language lang, bool translate, DataReceivedEventHandler dataReceivedHandler = null)
    {
        var language = lang.ToWhisperCode();
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 开始创建Whisper进程");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - Whisper路径: {_whisperPath}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 输入文件: {waveFileName}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 模型路径: {modelPath}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 语言: {language}");
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 是否翻译: {translate}");

        var translateToEnglish = translate ? "--task translate " : string.Empty;
        if (language.ToLowerInvariant() == "english" || language.ToLowerInvariant() == "en")
        {
            language = "en";
            translateToEnglish = string.Empty;
        }

        var outputSrt = "--standard --beep_off ";
        var parameters = $"--language {language} --model \"{modelPath}\" {outputSrt}{translateToEnglish} \"{waveFileName}\"";

        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 完整参数: {_whisperPath} {parameters}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(_whisperPath, parameters)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };

        if (!string.IsNullOrEmpty(Configuration.Settings.General.FFmpegLocation) && process.StartInfo.EnvironmentVariables["Path"] != null)
        {
            process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + Path.GetDirectoryName(Configuration.Settings.General.FFmpegLocation);
        }

        var whisperFolder = Path.GetDirectoryName(_whisperPath);
        Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - Whisper文件夹: {whisperFolder}");

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            process.StartInfo.WorkingDirectory = whisperFolder;
            Debug.WriteLine($"[PurfviewFasterWhisperRecognitionService] GetWhisperProcess - 工作目录: {whisperFolder}");
        }

        if (!string.IsNullOrEmpty(whisperFolder) && process.StartInfo.EnvironmentVariables["Path"] != null)
        {
            process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + whisperFolder;
        }

        process.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        process.StartInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

        if (dataReceivedHandler != null)
        {
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += dataReceivedHandler;
            process.ErrorDataReceived += dataReceivedHandler;
        }

        return process;
    }

    #endregion

    #region Result Parsing

    public bool GetResultFromSrt(string waveFileName, string videoFileName, out List<ResultText> resultTexts, List<string> filesToDelete)
    {
        var whisperFolder = Path.GetDirectoryName(_whisperPath);
        var srtFileName = string.Empty;
        var vttFileName = string.Empty;

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            var inputFileName = Path.GetFileNameWithoutExtension(waveFileName);
            srtFileName = Path.Combine(whisperFolder, inputFileName + ".srt");
            vttFileName = Path.Combine(whisperFolder, inputFileName + ".vtt");
        }

        if (!File.Exists(srtFileName) && !File.Exists(vttFileName))
        {
            resultTexts = new List<ResultText>();
            return false;
        }

        var sub = new Subtitle();
        if (File.Exists(srtFileName))
        {
            var rawText = FileUtil.ReadAllLinesShared(srtFileName, Encoding.UTF8);
            new SubRip().LoadSubtitle(sub, rawText, srtFileName);
            progress?.Report($"Loading result from {srtFileName}{Environment.NewLine}");
        }
        else
        {
            var rawText = FileUtil.ReadAllLinesShared(vttFileName, Encoding.UTF8);
            new WebVTT().LoadSubtitle(sub, rawText, vttFileName);
            progress?.Report($"Loading result from {vttFileName}{Environment.NewLine}");
        }

        sub.RemoveEmptyLines();

        var results = new List<ResultText>();
        foreach (var p in sub.Paragraphs)
        {
            results.Add(new ResultText
            {
                Start = (decimal)p.StartTime.TotalSeconds,
                End = (decimal)p.EndTime.TotalSeconds,
                Text = p.Text
            });
        }

        resultTexts = results;

        if (File.Exists(srtFileName))
        {
            filesToDelete?.Add(srtFileName);
        }

        if (File.Exists(vttFileName))
        {
            filesToDelete?.Add(vttFileName);
        }

        return true;
    }

    #endregion
}

//[--language {
//af,am,ar,as,az,
//ba,be,bg,bn,bo,br,bs,
//ca,cs,cy,da,de,el,en,es,et,eu,fa,fi,fo,fr,gl,gu,ha,haw,
//he,hi,hr,ht,hu,hy,id,is,it,ja,jw,ka,kk,km,kn,ko,la,lb,ln,lo,lt,
//lv,mg,mi,mk,ml,mn,mr,ms,mt,my,ne,nl,nn,no,oc,pa,pl,ps,pt,ro,ru,sa,
//sd,si,sk,sl,sn,so,sq,sr,su,sv,sw,ta,te,tg,th,tk,tl,tr,tt,uk,ur,uz,vi,yi,yo,yue,
//zh,
//Afrikaans,Albanian,Amharic,Arabic,Armenian,Assamese,Azerbaijani,Bashkir,Basque,Belarusian,Bengali,
//Bosnian,Breton,Bulgarian,Burmese,Cantonese,Castilian,Catalan,Chinese,Croatian,Czech,
//Danish,Dutch,English,Estonian,Faroese,Finnish,Flemish,French,Galician,Georgian,German,Greek,
//Gujarati,Haitian,Haitian Creole, Hausa, Hawaiian, Hebrew, Hindi, Hungarian, Icelandic, Indonesian,
//Italian, Japanese, Javanese, Kannada, Kazakh, Khmer, Korean, Lao, Latin, Latvian,
//Letzeburgesch, Lingala, Lithuanian, Luxembourgish, Macedonian, Malagasy, Malay, Malayalam,
//Maltese, Mandarin, Maori, Marathi, Moldavian, Moldovan, Mongolian, Myanmar, Nepali, Norwegian,
//Nynorsk, Occitan, Panjabi, Pashto, Persian, Polish, Portuguese, Punjabi, Pushto, Romanian, Russian,
//Sanskrit, Serbian, Shona, Sindhi, Sinhala, Sinhalese, Slovak, Slovenian, Somali, Spanish, Sundanese,
//Swahili, Swedish, Tagalog, Tajik, Tamil, Tatar, Telugu, Thai, Tibetan, Turkish, Turkmen, Ukrainian, Urdu, Uzbek,
//Valencian, Vietnamese, Welsh, Yiddish, Yoruba
//}]