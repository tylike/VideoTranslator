using System;
using System.Text.RegularExpressions;
using VideoTranslator.Config;
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.Services;
using VT.Core;

namespace VideoTranslator.Interfaces;

public interface IAudioService
{
    Task<AudioInfo> GetAudioInfoAsync(string audioPath);
    Task<double> GetAudioDurationSecondsAsyn(string audioPath);
    Task<string> GenerateSilenceAsync(double duration, int sampleRate, int channels, string outputPath);
    Task<string> AdjustAudioSpeedAsync(string inputPath, double tempo, int sampleRate, int channels, string outputPath);
    Task<string> AdjustAudioToDurationAsync(string inputPath, double targetDurationMs, int? sampleRate = null, int? channels = null, string outputPath = "");
    Task<string> ConcatenateAudiosAsync(List<string> audioPaths, int sampleRate, int channels, string outputPath);
    Task<string> MergeAudioSegmentsAsync(List<string> audioPaths, int sampleRate, int channels, string outputPath);
    Task<string> MergeAudioSegmentsOnTimelineAsync(List<AudioClipWithTime> audioClips, string backgroundAudioPath, string outputPath);
    Task<string> SegmentAudioBySubtitleAsync(string audioPath, IEnumerable<ISrtSubtitle> subtitles, string outputDirectory);
    Task<string> SegmentAudioBySubtitleSinglePassAsync(string audioPath, IEnumerable<ISrtSubtitle> subtitles, string outputDirectory);
    Task<string> RemoveLeadingTrailingSilenceAsync(string inputPath, string outputPath, double silenceThresholdMs = 30);
}
public class AudioService : ServiceBase, IAudioService
{    
    public AudioService()
    {
    }
    public async Task<AudioInfo> GetAudioInfoAsync(string audioPath)
    {
        var args = $"-i \"{audioPath}\" -f null -";
        var output = await base.Ffmpeg.ExecuteCommandAsync(args);

        var info = new AudioInfo
        {
            FilePath = audioPath,
            DurationSeconds = ParseDurationSeconds(output),
            SampleRate = ParseSampleRate(output),
            Channels = ParseChannels(output)
        };
        if (info.DurationSeconds <= 0)
        {
            progress?.Error("在执行GetAudioInfoAsync时解析时长为0");
            progress?.Error($"参数:{args}");
            progress?.Error($"输出:{output}");
            throw new Exception($"错误:{audioPath},时长为:{info.DurationSeconds}");
        }
        return info;
    }

    public async Task<double> GetAudioDurationSecondsAsyn(string audioPath)
    {
        var info = await GetAudioInfoAsync(audioPath);
        return info.DurationSeconds;
    }

    public async Task<string> GenerateSilenceAsync(double duration, int sampleRate, int channels, string outputPath)
    {
        var args = $"-f lavfi -i anullsrc=r={sampleRate}:d={duration} " +
                   $"-threads 12 -acodec pcm_s16le -ac {channels} -ar {sampleRate} -y \"{outputPath}\"";
        await Ffmpeg.ExecuteCommandAsync(args);
        return outputPath;
    }

    public async Task<string> AdjustAudioSpeedAsync(string inputPath, double tempo, int sampleRate, int channels, string outputPath)
    {
        var filterChain = BuildTempoFilter(tempo);
        var args = $"-i \"{inputPath}\" -filter:a \"{filterChain}\" " +
                   $"-threads 12 -acodec pcm_s16le -ac {channels} -ar {sampleRate} -y \"{outputPath}\"";
        await Ffmpeg.ExecuteCommandAsync(args);
        return outputPath;
    }

    public async Task<string> AdjustAudioToDurationAsync(string inputPath, double targetDurationMs, int? sampleRate = null, int? channels = null, string outputPath = "")
    {
        var audioInfo = await GetAudioInfoAsync(inputPath);
        var actualSampleRate = sampleRate ?? audioInfo.SampleRate;
        var actualChannels = channels ?? audioInfo.Channels;

        var inputDurationSeconds = audioInfo.DurationSeconds;
        var inputDurationMs = inputDurationSeconds * 1000;
        var tempo = inputDurationSeconds / (targetDurationMs / 1000.0);

        progress?.Report($"[AudioService] AdjustAudioToDurationAsync:");
        progress?.Report($"  原始时长: {inputDurationSeconds:F4}s ({inputDurationMs:F2}ms)");
        progress?.Report($"  目标时长: {targetDurationMs:F2}ms");
        progress?.Report($"  计算的 tempo: {tempo:F6}");
        progress?.Report($"  采样率: {actualSampleRate}Hz ({(sampleRate.HasValue ? "指定" : "原始")})");
        progress?.Report($"  声道数: {actualChannels} ({(channels.HasValue ? "指定" : "原始")})");

        if (targetDurationMs < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDurationMs),
                $"目标时长 {targetDurationMs:F2}ms 太小，无法进行有效的速度调整。最小目标时长为 100ms。");
        }

        if (tempo < 0.01 || tempo > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDurationMs),
                $"输入路径:{inputPath}" +
                $"输出路径:{outputPath}" +
                $"目标时长超出可调整范围。原始时长 {inputDurationSeconds:F4}s ({inputDurationMs:F2}ms)，目标时长 {targetDurationMs:F2}ms，" +
                $"tempo={tempo:F6} 超出 [0.01, 100.0] 范围。请确保目标时长在 {inputDurationSeconds / 100.0 * 1000:F0}ms 到 {inputDurationSeconds * 100.0 * 1000:F0}ms 之间。");
        }

        var filterChain = $"rubberband=tempo={tempo:F10}";
        progress.Report($"  使用的滤镜: {filterChain}");

        var args = $"-i \"{inputPath}\" -filter:a \"{filterChain}\" " +
                   $"-threads 12 -acodec pcm_s16le -ac {actualChannels} -ar {actualSampleRate} -y \"{outputPath}\"";
        await Ffmpeg.ExecuteCommandAsync(args);

        var outputDuration = await GetAudioDurationSecondsAsyn(outputPath);
        var outputDurationMs = outputDuration * 1000;
        var diffMs = Math.Abs(outputDurationMs - targetDurationMs);
        var isShortEnough = outputDurationMs <= targetDurationMs + 1.0;

        progress.Report($"  输出时长: {outputDuration:F4}s ({outputDurationMs:F2}ms), 误差: {diffMs:F2}ms");
        progress.Report($"  验证: 输出 <= 目标? {(isShortEnough ? "是" : "否")}");

        if (!isShortEnough)
        {
            progress.Report($"  ⚠️ 警告: 输出时长 {outputDurationMs:F2}ms 超过了目标 {targetDurationMs:F2}ms");
        }

        return outputPath;
    }

    public async Task<string> ConcatenateAudiosAsync(List<string> audioPaths, int sampleRate, int channels, string outputPath)
    {
        progress.Report($"[AudioService] 准备合并 {audioPaths.Count} 个音频文件");

        var tempFile = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid()}.txt");

        progress.Report($"[AudioService] 创建concat文件: {tempFile}");

        var concatList = string.Join("\n", audioPaths.Select(p => $"file '{p.Replace("\\", "/")}'"));
        await File.WriteAllTextAsync(tempFile, concatList);

        progress.Report($"[AudioService] Concat文件内容（前5行）:");
        var lines = await File.ReadAllLinesAsync(tempFile);
        foreach (var line in lines.Take(5))
        {
            progress.Report($"  {line}");
        }
        if (lines.Length > 5)
        {
            progress.Report($"  ... 还有 {lines.Length - 5} 行");
        }

        try
        {
            var args = $"-f concat -safe 0 -i \"{tempFile}\" " +
                       $"-threads 12 -acodec pcm_s16le -ac {channels} -ar {sampleRate} -y \"{outputPath}\"";

            progress.Report($"[AudioService] 执行合并命令...");
            await Ffmpeg.ExecuteCommandAsync(args);

            return outputPath;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
                progress.Report($"[AudioService] 已删除临时concat文件");
            }
        }
    }

    public async Task<string> MergeAudioSegmentsAsync(List<string> audioPaths, int sampleRate, int channels, string outputPath)
    {
        if (audioPaths.Count == 0)
        {
            throw new ArgumentException("No audio paths provided for merging");
        }

        return await ConcatenateAudiosAsync(audioPaths, sampleRate, channels, outputPath);
    }

    /// <summary>
    /// 合并音频片段到时间线
    /// </summary>
    /// <param name="audioClips">目标时间线，以及要使用的片断</param>
    /// <param name="backgroundAudioPath">当片断音频太短时,尾部的空白应使用此文件中的对应时间的内容进行填充</param>
    /// <param name="outputPath">输出文件路径,与backgroundAudioPath是同路径</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> MergeAudioSegmentsOnTimelineAsync(
        List<AudioClipWithTime> audioClips,
        string backgroundAudioPath,
        string outputPath)
    {
        #region 验证参数
        if (string.IsNullOrWhiteSpace(backgroundAudioPath) || !File.Exists(backgroundAudioPath))
        {
            throw new FileNotFoundException("背景音频文件不存在", backgroundAudioPath);
        }

        if (audioClips == null || audioClips.Count == 0)
        {
            throw new ArgumentException("没有可合并的音频片段");
        }

        foreach (var clip in audioClips)
        {
            if (clip == null)
            {
                throw new InvalidOperationException("片段列表中包含null元素");
            }
            clip.Validate();
            var startMs = clip.Start.TotalMilliseconds;
            var endMs = clip.End.TotalMilliseconds;
            var durationMs = clip.DurationMs;            
        }
        #endregion

        #region 准备输出
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        #endregion

        var composer = new AudioTimelineComposer(this.Ffmpeg, this.progress);

        var result =await composer.Compose(
            backgroundAudio: backgroundAudioPath,
            segments: audioClips,
            outputPath: outputPath
        );

        if (!result.Success)
        {
            throw new InvalidOperationException($"音频时间线合并失败: {result.Error}");
        }
        await Task.CompletedTask;
        return outputPath;
    }

    public async Task<string> SegmentAudioBySubtitleAsync(string audioPath, IEnumerable<ISrtSubtitle> subtitles, string outputDirectory)
    {
        progress.Report($"[AudioService] 根据字幕分段音频");
        progress.Report($"  音频文件: {audioPath}");
        progress.Report($"  输出目录: {outputDirectory}");
        progress.Report($"  字幕数量: {subtitles.Count()}");

        Directory.CreateDirectory(outputDirectory);

        var audioInfo = await GetAudioInfoAsync(audioPath);
        progress.Report($"  音频时长: {audioInfo.DurationSeconds:F2}s");
        progress.Report($"  采样率: {audioInfo.SampleRate} Hz");
        progress.Report($"  声道数: {audioInfo.Channels}");
        
        var successCount = 0;
        var failedCount = 0;

        foreach (var subtitle in subtitles)
        {
            var outputFile = Path.Combine(outputDirectory, $"segment_{subtitle.Index:0000}.wav");
            var startTime = subtitle.StartSeconds;
            var endTime = subtitle.EndSeconds;
            var duration = endTime - startTime;

            if (duration <= 0)
            {
                progress.Report($"  [{subtitle.Index}] 跳过: 时长无效 ({duration:F2}s)");
                failedCount++;
                continue;
            }

            if (startTime >= audioInfo.DurationSeconds)
            {
                progress.Report($"  [{subtitle.Index}] 跳过: 开始时间超出音频时长 ({startTime:F2}s >= {audioInfo.DurationSeconds:F2}s)");
                failedCount++;
                continue;
            }

            var args = $"-i \"{audioPath}\" " +
                       $"-ss {startTime:F3} " +
                       $"-t {duration:F3} " +
                       $"-acodec pcm_s16le " +
                       $"-ac {audioInfo.Channels} " +
                       $"-ar {audioInfo.SampleRate} " +
                       $"-y \"{outputFile}\"";

            try
            {
                await Ffmpeg.ExecuteCommandAsync(args);
                successCount++;

                if (successCount % 10 == 0)
                {
                    progress.Report($"  进度: {successCount}/{subtitles.Count()} ({successCount * 100.0 / subtitles.Count():F1}%)");
                }
            }
            catch (Exception ex)
            {
                progress.Error($"  [{subtitle.Index}] 失败: {ex.Message}");
                failedCount++;
            }
        }

        progress.Report($"[OK] 分段完成: 成功 {successCount}, 失败 {failedCount}");
        return outputDirectory;
    }

    public async Task<string> SegmentAudioBySubtitleSinglePassAsync(string audioPath, IEnumerable<ISrtSubtitle> subtitles, string outputDirectory)
    {
        progress.Report($"[AudioService] 单次Pass根据字幕分段音频");
        progress.Report($"  音频文件: {audioPath}");
        progress.Report($"  输出目录: {outputDirectory}");
        progress.Report($"  字幕数量: {subtitles.Count()}");

        Directory.CreateDirectory(outputDirectory);

        var audioInfo = await GetAudioInfoAsync(audioPath);
        progress.Report($"  音频时长: {audioInfo.DurationSeconds:F2}s");
        progress.Report($"  采样率: {audioInfo.SampleRate} Hz");
        progress.Report($"  声道数: {audioInfo.Channels}");

        var validSubtitles = subtitles
            .Where(s =>
            {
                var duration = s.EndSeconds - s.StartSeconds;
                return duration > 0 && s.StartSeconds < audioInfo.DurationSeconds;
            })
            .ToList();

        if (validSubtitles.Count == 0)
        {
            progress.Error("  没有有效的字幕段");
            return outputDirectory;
        }

        progress.Report($"  有效字幕段: {validSubtitles.Count}");

        var filterParts = new List<string>();
        var cmdParts = new List<string> { "-i", $"\"{audioPath}\"" };

        for (int i = 0; i < validSubtitles.Count; i++)
        {
            var sub = validSubtitles[i];
            var duration = (sub.EndSeconds - sub.StartSeconds)+0.2;
            var outputFile = Path.Combine(outputDirectory, $"segment_{sub.Index:0000}.wav");

            filterParts.Add($"[0:a]atrim=start={sub.StartSeconds:F3}:end={(sub.EndSeconds+0.2):F3},asetpts=PTS-STARTPTS[out{i}]");
            cmdParts.Add("-map");
            cmdParts.Add($"[out{i}]");
            cmdParts.Add($"-t");
            cmdParts.Add($"{duration:F3}");
            cmdParts.Add($"\"{outputFile}\"");
        }

        var filterComplex = string.Join(";", filterParts);
        var allArgs = new List<string>();
        allArgs.Add("-filter_complex");
        allArgs.Add($"\"{filterComplex}\"");
        allArgs.AddRange(cmdParts);
        allArgs.Add("-y");

        progress.Report("  执行单次ffmpeg命令...");

        try
        {
            await Ffmpeg.ExecuteCommandAsync(allArgs);
            progress.Report($"[OK] 单次Pass分段完成: {validSubtitles.Count} 个文件");
            return outputDirectory;
        }
        catch (Exception ex)
        {
            progress.Error($"[ERROR] 单次Pass失败: {ex.Message}");
            progress.Report("  回退到逐段处理...");
            return await SegmentAudioBySubtitleAsync(audioPath, subtitles, outputDirectory);
        }
    }

    public async Task<string> RemoveLeadingTrailingSilenceAsync(string inputPath, string outputPath, double silenceThresholdMs = 30)
    {
        #region 参数验证
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("输入音频文件不存在", inputPath);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空", nameof(outputPath));
        }

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        #endregion

        progress.Report($"[AudioService] 检测并去除音频静音");
        progress.Report($"  输入文件: {inputPath}");
        progress.Report($"  输出文件: {outputPath}");
        progress.Report($"  静音阈值: {silenceThresholdMs}ms");

        #region 获取音频信息
        var audioInfo = await GetAudioInfoAsync(inputPath);
        var totalDuration = audioInfo.DurationSeconds;
        progress.Report($"  音频时长: {totalDuration:F3}s");
        #endregion

        #region 检测静音
        var silenceDurationMs = silenceThresholdMs / 1000.0;
        var args = $"-i \"{inputPath}\" -af silencedetect=noise=-50dB:d={silenceDurationMs:F6} -f null -";
        var output = await Ffmpeg.ExecuteCommandAsync(args);
        #endregion

        #region 解析静音检测结果
        double? trimStartTime = null;
        double? trimEndTime = null;

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        double? currentSilenceStart = null;

        foreach (var line in lines)
        {
            if (line.Contains("silence_start"))
            {
                var match = Regex.Match(line, @"silence_start:\s*(\d+\.?\d*)");
                if (match.Success)
                {
                    currentSilenceStart = double.Parse(match.Groups[1].Value);
                }
            }
            else if (line.Contains("silence_end") && currentSilenceStart.HasValue)
            {
                var match = Regex.Match(line, @"silence_end:\s*(\d+\.?\d*)");
                if (match.Success)
                {
                    var detectedEndTime = double.Parse(match.Groups[1].Value);
                    
                    if (currentSilenceStart.Value < 0.1 && !trimStartTime.HasValue)
                    {
                        trimStartTime = detectedEndTime;
                        progress.Report($"  检测到开头静音: {currentSilenceStart.Value:F3}s - {detectedEndTime:F3}s");
                    }
                    
                    if (detectedEndTime > totalDuration - 0.1)
                    {
                        trimEndTime = currentSilenceStart.Value;
                        progress.Report($"  检测到结尾静音: {currentSilenceStart.Value:F3}s - {detectedEndTime:F3}s");
                    }
                    
                    currentSilenceStart = null;
                }
            }
        }
        #endregion

        #region 检查是否需要去除静音
        if (!trimStartTime.HasValue && !trimEndTime.HasValue)
        {
            progress.Report($"  未检测到超过 {silenceThresholdMs}ms 的静音，直接复制文件");
            File.Copy(inputPath, outputPath, true);
            return outputPath;
        }
        #endregion

        #region 计算裁剪范围
        var startTime = trimStartTime.HasValue ? trimStartTime.Value : 0;
        var endTime = trimEndTime.HasValue ? trimEndTime.Value : totalDuration;
        var duration = endTime - startTime;

        progress.Report($"  裁剪范围: {startTime:F3}s - {endTime:F3}s");
        progress.Report($"  裁剪后时长: {duration:F3}s");

        if (duration <= 0)
        {
            progress.Error($"  裁剪后时长无效: {duration:F3}s，跳过处理");
            File.Copy(inputPath, outputPath, true);
            return outputPath;
        }
        #endregion

        #region 执行裁剪
        var trimArgs = $"-i \"{inputPath}\" -ss {startTime:F3} -t {duration:F3} -c copy -y \"{outputPath}\"";
        await Ffmpeg.ExecuteCommandAsync(trimArgs);
        #endregion

        #region 验证输出
        var outputDuration = await GetAudioDurationSecondsAsyn(outputPath);
        progress.Report($"  输出文件时长: {outputDuration:F3}s");
        progress.Report($"[OK] 静音去除完成");
        #endregion

        return outputPath;
    }

    private static double ParseDurationSeconds(string ffmpegOutput)
    {
        var match = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
        if (match.Success)
        {
            var hours = double.Parse(match.Groups[1].Value);
            var minutes = double.Parse(match.Groups[2].Value);
            var seconds = double.Parse(match.Groups[3].Value);
            return hours * 3600 + minutes * 60 + seconds;
        }
        return 0;
    }

    private static int ParseSampleRate(string ffmpegOutput)
    {
        var match = Regex.Match(ffmpegOutput, @"(\d+) Hz");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return 44100;
    }

    private static int ParseChannels(string ffmpegOutput)
    {
        var match = Regex.Match(ffmpegOutput, @"(\d+) channels?");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return 2;
    }

    private static string BuildTempoFilter(double tempo)
    {
        if (tempo >= 0.25 && tempo <= 4.0)
        {
            return $"atempo={tempo:F10}";
        }

        var filters = new List<string>();
        var currentTempo = tempo;

        while (currentTempo > 4.0)
        {
            filters.Add("2.0");
            currentTempo /= 2.0;
        }

        while (currentTempo < 0.25)
        {
            filters.Add("0.5");
            currentTempo /= 0.5;
        }

        filters.Add($"{currentTempo:F10}");

        return string.Join(",", filters.Select(f => $"atempo={f}"));
    }
}