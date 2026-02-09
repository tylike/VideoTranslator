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
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.Services;

public class WhisperRecognitionService : ServiceBase
{
    private readonly string _whisperPath;
    private readonly string _modelPath;
    private readonly string _smallModelPath;
    private readonly bool _translateToEnglish;
    private readonly bool _usePostProcessing = true;
    private readonly bool _autoAdjustTimings = true;
    private Language? _detectedLanguage;

    public WhisperRecognitionService(
        bool translateToEnglish = false,
        bool usePostProcessing = true,
        bool autoAdjustTimings = true) : base()
    {
        var settings = base.Settings;
        _whisperPath = settings.WhisperPath;
        _modelPath = settings.WhisperModelPath;
        _translateToEnglish = translateToEnglish;
        _usePostProcessing = usePostProcessing;
        _autoAdjustTimings = autoAdjustTimings;
    }

    #region Public Methods

    public async Task<VadDetectionResult> DetectVadSegmentsAsync(string audioPath, decimal threshold = 0.5m, int minSpeechDurationMs = 250, int minSilenceDurationMs = 100)
    {
        progress?.Report("[WhisperRecognitionService] 开始VAD检测");
        progress?.Report($"音频文件: {audioPath}");
        progress?.Report($"VAD阈值: {threshold}");
        progress?.Report($"最小语音时长: {minSpeechDurationMs}ms");
        progress?.Report($"最小静音时长: {minSilenceDurationMs}ms");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        #region 获取VAD工具路径
        var whisperDir = Path.GetDirectoryName(_whisperPath);
        var vadToolPath = Path.Combine(whisperDir ?? "", "whisper-vad-speech-segments.exe");
        var vadModelPath = Path.Combine(whisperDir ?? "", "Models", "silero-v6.2.0-vad.bin");

        if (!File.Exists(vadToolPath))
        {
            throw new FileNotFoundException($"VAD工具不存在: {vadToolPath}");
        }

        if (!File.Exists(vadModelPath))
        {
            throw new FileNotFoundException($"VAD模型不存在: {vadModelPath}");
        }
        #endregion

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在调用VAD工具进行检测...");

        var result = await Task.Run(() =>
        {
            return DetectVadSegmentsViaTool(vadToolPath, vadModelPath, wavPath, threshold, minSpeechDurationMs, minSilenceDurationMs);
        });

        progress?.Report($"[WhisperRecognitionService] VAD检测完成，共检测到 {result.Segments.Count} 个片段");
        progress?.Report($"语音片段: {result.SpeechSegmentCount} 个，总时长: {result.TotalSpeechDuration:F2}秒");
        progress?.Report($"静音片段: {result.SilenceSegmentCount} 个，总时长: {result.TotalSilenceDuration:F2}秒");

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

        return result;
    }

    public async Task<Language> DetectLanguageAsync(string audioPath)
    {
        progress?.Report("[WhisperRecognitionService] 开始检测音频语言");
        progress?.Report($"音频文件: {audioPath}");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        if (!File.Exists(_whisperPath))
        {
            throw new FileNotFoundException($"Whisper可执行文件不存在: {_whisperPath}");
        }

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在调用Whisper进行语言检测...");

        var detectedLanguage = await Task.Run(() =>
        {
            return DetectLanguageViaWhisper(wavPath);
        });

        progress?.Report($"[WhisperRecognitionService] 语言检测完成: {detectedLanguage}");

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

    public async Task<List<ISrtSubtitle>> RecognizeAsync(string audioPath, string languageCode, string? prompt = null)
    {   
        progress?.Report("[WhisperRecognitionService] 开始语音识别");
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
            throw new FileNotFoundException($"Whisper可执行文件不存在: {_whisperPath}");
        }

        #region 音频预处理
        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");
        #endregion

        progress?.Report("正在调用Whisper进行识别...");

        var results = await Task.Run(() =>
        {
            return TranscribeViaWhisper(wavPath, languageCode, prompt);
        });

        progress?.Report($"[WhisperRecognitionService] 识别完成，共识别到 {results.Count} 个片段");

        #region 使用波形数据调整时间轴
        if (_autoAdjustTimings)
        {
            progress?.Report("正在使用波形数据调整时间轴...");
            var wavePeaks = MakeWavePeaks(audioPath);
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
            var postProcessor = new AudioToTextPostProcessor(languageCode)
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
        int index = 1;
        var isrts = results.Select(x => new SrtSubtitle(index++, TimeSpan.FromSeconds((double)x.Start), TimeSpan.FromSeconds((double)x.End), x.Text)).ToArray();
        subtitles.AddRange(isrts);
        return subtitles;
    }

    #endregion

    #region VAD Detection

    private VadDetectionResult DetectVadSegmentsViaTool(string vadToolPath, string vadModelPath, string wavPath, decimal threshold, int minSpeechDurationMs, int minSilenceDurationMs)
    {
        var result = new VadDetectionResult
        {
            AudioPath = wavPath
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var args = $"-f \"{wavPath}\" " +
                   $"--vad-model \"{vadModelPath}\" " +
                   $"--vad-threshold {threshold} " +
                   $"--vad-min-speech-duration-ms {minSpeechDurationMs} " +
                   $"--vad-min-silence-duration-ms {minSilenceDurationMs}";

        Debug.WriteLine($"[WhisperRecognitionService] DetectVadSegmentsViaTool - VAD工具: {vadToolPath}");
        Debug.WriteLine($"[WhisperRecognitionService] DetectVadSegmentsViaTool - 参数: {args}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(vadToolPath, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };

        var sw = Stopwatch.StartNew();
        progress?.Report($"Calling VAD tool: {vadToolPath} {args}{Environment.NewLine}");

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            progress?.Report($"VAD detection done in {sw.Elapsed}{Environment.NewLine}");

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                throw new Exception($"VAD检测失败，退出代码: {process.ExitCode}, 错误信息: {error}");
            }

            var output = outputBuilder.ToString();
            if (string.IsNullOrEmpty(output))
            {
                output = errorBuilder.ToString();
            }
            ParseVadOutput(output, result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WhisperRecognitionService] DetectVadSegmentsViaTool - 错误: {ex}");
            throw;
        }
        finally
        {
            process.Dispose();
        }

        return result;
    }

    private void ParseVadOutput(string output, VadDetectionResult result)
    {
        var speechSegments = new List<(int start, int end)>();
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"Speech segment\s+(\d+):\s+start\s+=\s+([\d.]+),\s+end\s+=\s+([\d.]+)");
            if (match.Success)
            {
                var index = int.Parse(match.Groups[1].Value);
                var start = (int)decimal.Parse(match.Groups[2].Value);
                var end = (int)decimal.Parse(match.Groups[3].Value);
                speechSegments.Add((start, end));
            }
        }

        if (speechSegments.Count == 0)
        {
            progress?.Report("[VAD] 未检测到语音片段");
            return;
        }

        var allSegments = new List<VadSegment>();
        var currentIndex = 0;

        foreach (var (startCs, endCs) in speechSegments)
        {
            var startSec = startCs / 100m;
            var endSec = endCs / 100m;
            var durationSec = endSec - startSec;

            allSegments.Add(new VadSegment
            {
                Index = currentIndex++,
                Start = startSec,
                End = endSec,
                Duration = durationSec,
                IsSpeech = true
            });
        }

        var sortedSegments = allSegments.OrderBy(s => s.Start).ToList();
        var finalSegments = new List<VadSegment>();
        var finalIndex = 0;

        for (int i = 0; i < sortedSegments.Count; i++)
        {
            var currentSegment = sortedSegments[i];

            if (i == 0 && currentSegment.Start > 0)
            {
                finalSegments.Add(new VadSegment
                {
                    Index = finalIndex++,
                    Start = 0,
                    End = currentSegment.Start,
                    Duration = currentSegment.Start,
                    IsSpeech = false
                });
            }

            finalSegments.Add(new VadSegment
            {
                Index = finalIndex++,
                Start = currentSegment.Start,
                End = currentSegment.End,
                Duration = currentSegment.Duration,
                IsSpeech = true
            });

            if (i < sortedSegments.Count - 1)
            {
                var nextSegment = sortedSegments[i + 1];
                var silenceStart = currentSegment.End;
                var silenceEnd = nextSegment.Start;

                if (silenceEnd > silenceStart)
                {
                    finalSegments.Add(new VadSegment
                    {
                        Index = finalIndex++,
                        Start = silenceStart,
                        End = silenceEnd,
                        Duration = silenceEnd - silenceStart,
                        IsSpeech = false
                    });
                }
            }
        }

        var lastSegment = sortedSegments.LastOrDefault();
        if (lastSegment != null)
        {
            result.AudioDuration = lastSegment.End;
        }

        result.Segments = finalSegments;
        result.SpeechSegmentCount = finalSegments.Count(s => s.IsSpeech);
        result.SilenceSegmentCount = finalSegments.Count(s => !s.IsSpeech);
        result.TotalSpeechDuration = finalSegments.Where(s => s.IsSpeech).Sum(s => s.Duration);
        result.TotalSilenceDuration = finalSegments.Where(s => !s.IsSpeech).Sum(s => s.Duration);
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
        var tempFileName = $"whisper_temp_{Guid.NewGuid():N}.wav";
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

    private Language DetectLanguageViaWhisper(string waveFileName)
    {
        Debug.WriteLine($"[WhisperRecognitionService] DetectLanguageViaWhisper - 开始检测语言");
        Debug.WriteLine($"[WhisperRecognitionService] DetectLanguageViaWhisper - 输入文件: {waveFileName}");

        var process = GetWhisperProcessForLanguageDetection(waveFileName, this.Settings.WhisperSmallModelPath, LanguageDetectionOutputHandler);

        Debug.WriteLine($"[WhisperRecognitionService] DetectLanguageViaWhisper - 可执行文件: {process.StartInfo.FileName}");
        Debug.WriteLine($"[WhisperRecognitionService] DetectLanguageViaWhisper - 参数: {process.StartInfo.Arguments}");

        var sw = Stopwatch.StartNew();
        progress?.Report($"Calling whisper with: {process.StartInfo.FileName} {process.StartInfo.Arguments}{Environment.NewLine}");

        try
        {
            process.PriorityClass = ProcessPriorityClass.Normal;
        }
        catch
        {
            // ignored
        }

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        progress?.Report($"Whisper language detection done in {sw.Elapsed}{Environment.NewLine}");
        process.Dispose();

        return _detectedLanguage ?? Language.English;
    }

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
            progress?.Warning($"[MakeWavePeaks] 生成波形数据失败: {ex.Message}");
            Debug.WriteLine($"[MakeWavePeaks] 错误: {ex}");
        }

        return null;
    }



    public List<ResultText> TranscribeViaWhisper(string waveFileName, string languageCode, string? prompt = null)
    {
        var _resultList = new List<ResultText>();
        var _filesToDelete = new List<string>();

        Debug.WriteLine($"[WhisperRecognitionService] TranscribeViaWhisper - 开始转录");
        Debug.WriteLine($"[WhisperRecognitionService] TranscribeViaWhisper - 模型路径: {_modelPath}");
        Debug.WriteLine($"[WhisperRecognitionService] TranscribeViaWhisper - 语言代码: {languageCode}");

        var inputFile = waveFileName;

        var process = GetWhisperProcess(inputFile, _modelPath, languageCode, false, OutputHandler);

        Debug.WriteLine($"[WhisperRecognitionService] TranscribeViaWhisper - 可执行文件: {process.StartInfo.FileName}");
        Debug.WriteLine($"[WhisperRecognitionService] TranscribeViaWhisper - 参数: {process.StartInfo.Arguments}");

        var sw = Stopwatch.StartNew();
        progress?.Report($"Calling whisper with: {process.StartInfo.FileName} {process.StartInfo.Arguments}{Environment.NewLine}");
        
        try
        {
            process.PriorityClass = ProcessPriorityClass.Normal;
        }
        catch
        {
            // ignored
        }

        process.Start();
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        process.WaitForExit();
        
        progress?.Report($"Whisper done in {sw.Elapsed}{Environment.NewLine}");
        process.Dispose();
        
        if (GetResultFromSrt(waveFileName, languageCode, out var resultTexts, _filesToDelete))
        {
            return resultTexts;
        }
        progress?.Report("Loading result from STDOUT" + Environment.NewLine);
        return _resultList.OrderBy(p => p.Start).ToList();
    }

    #endregion

    private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        progress.Report(outLine.Data);
    }

    private void LanguageDetectionOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
        var line = outLine.Data;
        progress?.Report(line);

        if (!string.IsNullOrEmpty(line))
        {
            var match = Regex.Match(line, @"auto-detected language:\s*(\w+)\s*\(p\s*=\s*[\d.]+\)");
            if (match.Success)
            {
                var languageCode = match.Groups[1].Value.ToLowerInvariant();
                _detectedLanguage = LanguageExtensions.FromWhisperCode(languageCode);
                Debug.WriteLine($"[WhisperRecognitionService] 检测到语言代码: {languageCode}, 映射为: {_detectedLanguage}");
            }
        }
    }

    public bool GetResultFromSrt(string waveFileName, string videoFileName, out List<ResultText> resultTexts, List<string> filesToDelete)
    {
        var srtFileName = waveFileName + ".srt";
        if (!File.Exists(srtFileName) && waveFileName.EndsWith(".wav"))
        {
            srtFileName = waveFileName.Remove(waveFileName.Length - 4) + ".srt";
        }

        var vttFileName = Path.ChangeExtension(waveFileName, ".vtt");

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
    public Process GetWhisperProcess(string waveFileName, string modelPath, string language, bool translate, DataReceivedEventHandler dataReceivedHandler = null)
    {
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 开始创建Whisper进程");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - Whisper路径: {_whisperPath}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 输入文件: {waveFileName}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 模型路径: {modelPath}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 语言: {language}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 是否翻译: {translate}");

        var translateToEnglish = translate ? "--task translate " : string.Empty;
        if (language.ToLowerInvariant() == "english" || language.ToLowerInvariant() == "en")
        {
            language = "en";
            translateToEnglish = string.Empty;
        }

        var outputSrt = "--output-srt ";
        var parameters = $"--language {language} --model \"{modelPath}\" {outputSrt}{translateToEnglish} \"{waveFileName}\"";

        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 完整参数: {_whisperPath} {parameters}");

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
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - Whisper文件夹: {whisperFolder}");

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            process.StartInfo.WorkingDirectory = whisperFolder;
            Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcess - 工作目录: {whisperFolder}");
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

    private Process GetWhisperProcessForLanguageDetection(string waveFileName, string modelPath, DataReceivedEventHandler dataReceivedHandler)
    {
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - 开始创建语言检测进程");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - Whisper路径: {_whisperPath}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - 输入文件: {waveFileName}");
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - 模型路径: {modelPath}");

        var parameters = $"--model \"{modelPath}\" --detect-language --file \"{waveFileName}\"";

        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - 完整参数: {_whisperPath} {parameters}");

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
        Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - Whisper文件夹: {whisperFolder}");

        if (!string.IsNullOrEmpty(whisperFolder))
        {
            process.StartInfo.WorkingDirectory = whisperFolder;
            Debug.WriteLine($"[WhisperRecognitionService] GetWhisperProcessForLanguageDetection - 工作目录: {whisperFolder}");
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
}
