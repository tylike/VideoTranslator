using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

public class AudioTimelineComposer
{
    private class AudioInfo
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public double DurationMs { get; set; }
    }

    private class ProcessedSegment
    {
        public AudioClipWithTime Original { get; set; } = new();
        public string TempPath { get; set; } = string.Empty;
        public int StartTimeMs { get; set; }
        public int DurationMs { get; set; }
        public int EndTimeMs => StartTimeMs + DurationMs;
    }



    IProgressService progress;
    readonly IFFmpegService ffmpeg;
    public AudioTimelineComposer(IFFmpegService ffmpeg, IProgressService progress)
    {
        this.progress = progress;
        if (ffmpeg == null)
            throw new AudioCompositionException("FFmpeg服务不能为空");
        this.ffmpeg = ffmpeg;

    }

    private async Task<string> ExecuteFfmpeg(string arguments)
    {
        return await ffmpeg.ExecuteCommandAsync(arguments);       
    }

    private async Task<string> ExecuteFfmpegViaBatchFile(string batchFilePath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(batchFilePath),
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = processInfo };
        progress?.Report($"[AudioTimelineComposer] 执行批处理文件: {batchFilePath}");

        process.Start();

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        var outputTask = Task.Run(() =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                if (line != null)
                {
                    outputBuilder.AppendLine(line);
                    progress?.Report($"[FFmpeg] {line}");
                }
            }
        });

        var errorTask = Task.Run(() =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = process.StandardError.ReadLine();
                if (line != null)
                {
                    errorBuilder.AppendLine(line);
                    progress?.Report($"[FFmpeg] {line}");
                }
            }
        });

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            progress?.Report($"[FFmpeg] 退出代码: {process.ExitCode}");
            throw new Exception($"FFmpeg command failed with exit code {process.ExitCode}. Error: {errorBuilder}");
        }

        return outputBuilder.ToString() + errorBuilder.ToString();
    }

    //实现目标:
    //1. 将segments中的每个音频片段插入到backgroundAudio的指定时间点。
    //2. 如果segments之间有间隔，则backgroundAudio在这些间隔时间内播放。
    //在播放segments时，backgroundAudio的是静音的。
    //3. 如果segments重叠，则抛出异常。
    //4. 输出结果应验证音频的长度最终与backgroundAudio长度一致，允许较小的误差。
    //这里的注释永远不要删除。
    //segments中的音频采样率是22K的，而backgroundAudio是44K,先将backgroundAudio重采样到22K,因为segments中的文件较多，这样快。
    //方法签名不能修改,实现完成后，应在控制台程序中测试是否通过。
    //为了方便测试，假设所有音频文件格式均为WAV。
    //为了方便测试，可以记录一些中间文件 或 日志 到backgroundAudio所在目录，但最终输出文件必须是outputPath指定的路径。
    //如果输出的中间文件、日志较多，可以建立一个子目录来存放。
    public async Task<ComposeResult> Compose(string backgroundAudio, List<AudioClipWithTime> segments, string outputPath)
    {
        progress?.Report($"[AudioTimelineComposer] 开始合成音频");
        progress?.Report($"  背景音频: {backgroundAudio}");
        progress?.Report($"  片段数量: {segments.Count}");
        progress?.Report($"  输出路径: {outputPath}");

        var debugDir = Path.GetDirectoryName(backgroundAudio) ?? Directory.GetCurrentDirectory();
        var debugSubDir = Path.Combine(debugDir, "composer_debug");
        Directory.CreateDirectory(debugSubDir);

        var logFile = Path.Combine(debugSubDir, "composer_log.txt");
        var logBuilder = new StringBuilder();
        Action<string> log = (msg) =>
        {
            logBuilder.AppendLine(msg);
            progress?.Report($"  [Composer] {msg}");
        };

        try
        {
            #region 验证参数
            if (!File.Exists(backgroundAudio))
            {
                throw new AudioCompositionException($"背景音频文件不存在: {backgroundAudio}");
            }

            if (segments == null || segments.Count == 0)
            {
                throw new AudioCompositionException("没有可合成的音频片段");
            }
            
            #endregion

            #region 获取背景音频信息
            log("获取背景音频信息...");
            var bgInfo =await GetAudioInfo(backgroundAudio);
            var bgSampleRate = bgInfo.SampleRate;
            var bgChannels = bgInfo.Channels;
            var bgDurationMs = bgInfo.DurationMs;
            log($"  背景音频: 采样率={bgSampleRate}Hz, 声道数={bgChannels}, 时长={bgDurationMs:F2}ms");

            var segInfo =await GetAudioInfo(segments[0].FilePath);
            var segSampleRate = segInfo.SampleRate;
            var segChannels = segInfo.Channels;
            log($"  片段音频: 采样率={segSampleRate}Hz, 声道数={segChannels}");
            #endregion

            #region 重采样背景音频到片段采样率
            string bgResampledPath;
            if (bgSampleRate != segSampleRate || bgChannels != segChannels)
            {
                bgResampledPath = Path.Combine(debugSubDir, "background_resampled.wav");
                log($"重采样背景音频从 {bgSampleRate}Hz 到 {segSampleRate}Hz...");
                await ExecuteFfmpeg($"-i \"{backgroundAudio}\" -ar {segSampleRate} -ac {segChannels} -y \"{bgResampledPath}\"");
                log($"  已保存到: {bgResampledPath}");
            }
            else
            {
                bgResampledPath = backgroundAudio;
                log($"背景音频采样率({bgSampleRate}Hz)和声道数({bgChannels})与片段一致，无需重采样");
            }
            #endregion

            #region 检查片段重叠
            log("检查片段重叠...");
            var sortedSegments = segments.OrderBy(s => s.Start.TotalMilliseconds).ToList();
            sortedSegments.ValidateNoOverlaps();
            log("  无重叠，检查通过");
            #endregion

            #region 期望效果:
            log("片段时间安排:");
            foreach (var item in sortedSegments)
            {
                log($"  {item.Index}:[PlayBg:{item.StartBgPlayTime}ms][play this{item.Start.TotalMilliseconds:F2}ms, 结束={item.AudioDurationMs:F2}ms,文件={Path.GetFileName(item.FilePath)}[playbg:{item.EndBgPlayTimeMs}]");
            }

            #endregion

            #region 准备各片段
            log("准备音频片段...");

            var processedSegments = new List<string>();
            for (int i = 0; i < sortedSegments.Count; i++)
            {
                var seg = sortedSegments[i];
                log($"  处理片段{i}: {Path.GetFileName(seg.FilePath)}");
                log($"    开始时间: {seg.Start.TotalMilliseconds:F2}ms, 结束时间: {seg.End.TotalMilliseconds:F2}ms");
                processedSegments.Add(seg.FilePath);
            }
            #endregion

            #region 混合所有音频
            log("混合所有音频轨道...");

            // 采用分段处理策略，避免filter链过长
            const int batchSize = 50; // 每批最多处理10个片段
            var currentMixedFile = bgResampledPath;
            var totalBatches = (int)Math.Ceiling((double)processedSegments.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var startIndex = batchIndex * batchSize;
                var endIndex = Math.Min(startIndex + batchSize, processedSegments.Count);
                var currentBatchSize = endIndex - startIndex;

                log($"  处理批次 {batchIndex + 1}/{totalBatches}，包含 {currentBatchSize} 个片段");

                var batchInputs = new List<string> { $"-i \"{currentMixedFile}\"" };
                var delayFilters = new List<string>();

                for (int i = startIndex; i < endIndex; i++)
                {
                    batchInputs.Add($"-i \"{processedSegments[i]}\"");
                    var startMs = sortedSegments[i].Start.TotalMilliseconds;
                    delayFilters.Add($"[{i - startIndex + 1}:a]adelay={startMs:F0}|{startMs:F0}[seg{i - startIndex}];");
                }

                var batchInputsCmd = string.Join(" ", batchInputs);
                var filterChain = string.Join("", delayFilters);
                filterChain += $"[0:a]volume=1[bg];[bg]";
                for (int i = 0; i < currentBatchSize; i++)
                {
                    filterChain += $"[seg{i}]";
                }
                filterChain += $"amix=inputs={currentBatchSize + 1}:duration=longest:normalize=0[outa]";

                log($"  批次{batchIndex + 1} Filter: {filterChain}");

                var batchOutput = batchIndex == totalBatches - 1 
                    ? outputPath 
                    : Path.Combine(debugSubDir, $"mixed_batch_{batchIndex}.wav");

                var batchCmd = $"\"{ffmpeg.FfmpegPath}\" {batchInputsCmd} -filter_complex \"{filterChain}\" -map \"[outa]\" -ar {segSampleRate} -ac {segChannels} -y \"{batchOutput}\"";
                
                var batchFile = Path.Combine(debugSubDir, $"ffmpeg_batch_{batchIndex}_{Guid.NewGuid()}.cmd");
                await File.WriteAllTextAsync(batchFile, $"@echo off{Environment.NewLine}{batchCmd}{Environment.NewLine}");
                log($"  创建批处理文件: {batchFile}");

                try
                {
                    await ExecuteFfmpegViaBatchFile(batchFile);
                    log($"  批次{batchIndex + 1}混合完成: {batchOutput}");
                }
                finally
                {
                    if (File.Exists(batchFile))
                    {
                        File.Delete(batchFile);
                    }
                }

                // 更新当前混合文件为本次批次的输出
                if (batchIndex < totalBatches - 1)
                {
                    currentMixedFile = batchOutput;
                }
            }

            log($"  输出已保存到: {outputPath}");
            #endregion

            #region 验证输出时长
            log("验证输出时长...");
            var outputInfo = await GetAudioInfo(outputPath);
            var outputDurationMs = outputInfo.DurationMs;
            var durationDiff = Math.Abs(outputDurationMs - bgDurationMs);
            var durationTolerance = bgDurationMs * 0.01;

            log($"  输出时长: {outputDurationMs:F2}ms");
            log($"  背景时长: {bgDurationMs:F2}ms");
            log($"  误差: {durationDiff:F2}ms, 允许误差: {durationTolerance:F2}ms");

            if (durationDiff > durationTolerance)
            {
                log($"  ⚠️ 警告: 输出时长与背景时长差异较大 ({durationDiff:F2}ms > {durationTolerance:F2}ms)");
            }
            else
            {
                log("  ✓ 时长验证通过");
            }
            #endregion

            File.WriteAllText(logFile, logBuilder.ToString());
            log("合成完成");

            return new ComposeResult
            {
                Success = true,
                OutputPath = outputPath,
                Output = $"Successfully composed audio with {segments.Count} segments"
            };
        }
        catch (Exception ex)
        {
            log($"错误: {ex.Message}");
            File.WriteAllText(logFile, logBuilder.ToString());

            return new ComposeResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }

    private string GenerateVolumeFilterChain(double bgDurationMs, List<AudioClipWithTime> segments, int sampleRate, int channels)
    {
        if (segments == null || segments.Count == 0)
        {
            return "-af \"volume=1[outa]\"";
        }

        var sortedSegments = segments.OrderBy(s => s.Start.TotalMilliseconds).ToList();
        var volumeExpr = new StringBuilder("volume='");

        for (int i = 0; i < sortedSegments.Count; i++)
        {
            var seg = sortedSegments[i];
            var startSec = seg.Start.TotalMilliseconds / 1000.0;
            var endSec = seg.End.TotalMilliseconds / 1000.0;

            if (i == 0)
            {
                volumeExpr.Append($"if(between(t,{startSec:F3},{endSec:F3}),0,1)");
            }
            else
            {
                volumeExpr.Append($"*if(between(t,{startSec:F3},{endSec:F3}),0,1)");
            }
        }

        volumeExpr.Append("'[outa]");
        return $"-af \"{volumeExpr}\"";
    }

    private async Task<AudioInfo> GetAudioInfo(string audioPath)
    {
        var output =await  ExecuteFfmpeg($"-i \"{audioPath}\" -f null -");

        var sampleRateMatch = Regex.Match(output, @"(\d+) Hz");
        var channelsMatch = Regex.Match(output, @"(\d+) channels?");
        var durationMatch = Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");

        var info = new AudioInfo
        {
            SampleRate = sampleRateMatch.Success ? int.Parse(sampleRateMatch.Groups[1].Value) : 44100,
            Channels = channelsMatch.Success ? int.Parse(channelsMatch.Groups[1].Value) : 2
        };

        if (durationMatch.Success)
        {
            var hours = int.Parse(durationMatch.Groups[1].Value);
            var minutes = int.Parse(durationMatch.Groups[2].Value);
            var seconds = int.Parse(durationMatch.Groups[3].Value);
            var centis = int.Parse(durationMatch.Groups[4].Value);
            info.DurationMs = (hours * 3600 + minutes * 60 + seconds) * 1000.0 + centis * 10;
        }

        return info;
    }
}
