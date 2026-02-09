﻿﻿﻿﻿﻿﻿﻿using System;
using System.Diagnostics;
using VideoTranslator.Config;
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.Services;

namespace VideoTranslator.Interfaces;



public interface ITTSService
{
    Task<TTSSegment?> GenerateSingleTTSAsync(TTSCommand command);

    Task<List<TTSSegment>> GenerateTTSAsync(IEnumerable<TTSCommand> commands, Action<int, int, TTSSegment>? onSegmentCompleted = null, bool cleanOld = false, int limit = -1);
}
public class TTSCommand
{
    public int Index { get; set; }
    public string Text { get; set; }
    public string ReferenceAudio { get; set; }
    public string OutputAudio { get; set; }
}

public class TTSService : ServiceBase, ITTSService
{
    private readonly HttpClient _httpClient;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly ISubtitleService _subtitleService;

    public TTSService(ISpeechRecognitionService speechRecognitionService, ISubtitleService subtitleService) : base()
    {
        _httpClient = new HttpClient();
        _speechRecognitionService = speechRecognitionService;
        _subtitleService = subtitleService;
    }
    
    private async Task<bool> GenerateTTSSegmentAsync(string text, string referenceAudioPath, string outputPath, List<string> ttsUrls, object lockObj, Dictionary<int, int> serviceTaskCounts, int maxServiceAttempts = 3)
    {
        if (!File.Exists(referenceAudioPath))
        {
            progress?.Report($"  警告: 找不到参考音频 {referenceAudioPath}");
            return false;
        }

        var attemptedUrlIndices = new HashSet<int>();

        for (int attempt = 1; attempt <= maxServiceAttempts; attempt++)
        {
            int urlIndex;
            lock (lockObj)
            {
                urlIndex = serviceTaskCounts.OrderBy(kvp => kvp.Value).First().Key;
                if (attemptedUrlIndices.Contains(urlIndex))
                {
                    var availableUrls = serviceTaskCounts.Where(kvp => !attemptedUrlIndices.Contains(kvp.Key)).ToList();
                    if (availableUrls.Count == 0)
                    {
                        progress?.Report($"  没有更多可用服务");
                        return false;
                    }
                    urlIndex = availableUrls.OrderBy(kvp => kvp.Value).First().Key;
                }
                serviceTaskCounts[urlIndex]++;
            }

            attemptedUrlIndices.Add(urlIndex);
            var currentTtsUrl = ttsUrls[urlIndex];

            try
            {
                using var form = new MultipartFormDataContent();
                using var audioFile = File.OpenRead(referenceAudioPath);
                var audioContent = new StreamContent(audioFile);

                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                form.Add(new StringContent(text), "text");
                form.Add(audioContent, "spk_audio", Path.GetFileName(referenceAudioPath));

                progress?.Report($"  尝试服务 [{urlIndex + 1}/{ttsUrls.Count}]: {currentTtsUrl}");

                var response = await _httpClient.PostAsync(currentTtsUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(outputPath, content);
                    lock (lockObj)
                    {
                        serviceTaskCounts[urlIndex]--;
                    }
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    progress?.Error($"  服务 [{urlIndex + 1}] 失败: {response.StatusCode} - {errorContent.Substring(0, Math.Min(100, errorContent.Length))}");
                    lock (lockObj)
                    {
                        serviceTaskCounts[urlIndex]--;
                    }
                    
                    if (attempt < maxServiceAttempts)
                    {
                        progress?.Report($"  切换到其他服务重试...");
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Error($"  服务 [{urlIndex + 1}] 错误: {ex.Message}");
                lock (lockObj)
                {
                    serviceTaskCounts[urlIndex]--;
                }
                
                if (attempt < maxServiceAttempts)
                {
                    progress?.Report($"  切换到其他服务重试...");
                }
            }
        }

        return false;
    }

    public async Task<TTSSegment?> GenerateSingleTTSAsync(TTSCommand command)
    {
        var ttsUrls = Config.VideoTranslator.TTS.Servers;

        if (ttsUrls == null || ttsUrls.Count == 0)
        {
            throw new ArgumentException("TTS服务器列表为空，请先在配置中设置TTSServers");
        }

        progress?.Report($"[TTSService] 生成单个TTS音频");
        progress?.Report($"文本: {command.Text}");

        var lockObj = new object();
        var serviceTaskCounts = new Dictionary<int, int>();

        for (int i = 0; i < ttsUrls.Count; i++)
        {
            serviceTaskCounts[i] = 0;
        }

        var success = await GenerateTTSSegmentAsync(
            command.Text, command.ReferenceAudio, command.OutputAudio, ttsUrls, lockObj, serviceTaskCounts, 3);

        if (success)
        {
            progress?.Report($"成功: {Path.GetFileName(command.OutputAudio)}");
            return new TTSSegment
            {
                Index = command.Index,
                TextTarget = command.Text,
                AudioPath = command.OutputAudio
            };
        }
        else
        {
            progress?.Report($"失败: {Path.GetFileName(command.OutputAudio)}");
            return null;
        }
    }

    public async Task<List<TTSSegment>> GenerateTTSAsync(IEnumerable<TTSCommand> commands, Action<int, int, TTSSegment>? onSegmentCompleted = null, bool cleanOld = false, int limit = -1)
    {
        var sw = Stopwatch.StartNew();
        progress?.Report($"[TTSService] 并行TTS音频生成工具");
        progress?.Report(new string('=', 60));
        progress?.Report($"生成限制: {(limit == -1 ? "全部" : limit.ToString())}");

        var commandList = commands.ToList();
        var totalCommands = commandList.Count;
        var actualLimit = limit == -1 ? totalCommands : Math.Min(limit, totalCommands);

        if (limit != -1 && limit < totalCommands)
        {
            progress?.Report($"限制生成数量: {actualLimit}/{totalCommands}");
        }

        if (cleanOld)
        {
            progress?.Report($"\n清理旧音频文件...");
            var outputDirs = commandList.Select(c => Path.GetDirectoryName(c.OutputAudio)).Where(d => d != null).Distinct();
            foreach (var dir in outputDirs)
            {
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.GetFiles(dir, "*.wav"))
                    {
                        File.Delete(file);
                        progress?.Report($"  删除: {Path.GetFileName(file)}");
                    }
                }
            }
        }

        var ttsUrls = Config.VideoTranslator.TTS.Servers;

        if (ttsUrls == null || ttsUrls.Count == 0)
        {
            throw new ArgumentException("TTS服务器列表为空，请先在配置中设置TTSServers");
        }

        progress?.Report($"\nTTS服务器数量: {ttsUrls.Count}");
        for (int i = 0; i < ttsUrls.Count; i++)
        {
            progress?.Report($"  [{i + 1}] {ttsUrls[i]}");
        }

        var tasks = new List<Task<(TTSSegment? segment, int index, bool success)>>();
        var semaphore = new SemaphoreSlim(ttsUrls.Count);
        var segmentAudioFiles = new List<TTSSegment>();
        var lockObj = new object();
        var serviceTaskCounts = new Dictionary<int, int>();

        for (int i = 0; i < ttsUrls.Count; i++)
        {
            serviceTaskCounts[i] = 0;
        }

        progress?.Report($"\n开始并行生成TTS音频...");
        progress?.Report($"并发数: {ttsUrls.Count}");

        for (int i = 0; i < actualLimit; i++)
        {
            var command = commandList[i];

            if (File.Exists(command.OutputAudio))
            {
                progress?.Report($"  [{i + 1}/{actualLimit}] 跳过已存在: {Path.GetFileName(command.OutputAudio)}");
                var existingSegment = new TTSSegment
                {
                    Index = command.Index,
                    TextTarget = command.Text,
                    AudioPath = command.OutputAudio
                };
                lock (lockObj)
                {
                    segmentAudioFiles.Add(existingSegment);
                }
                onSegmentCompleted?.Invoke(i + 1, actualLimit, existingSegment);
                continue;
            }

            var currentIndex = i;
            var currentCommand = command;

            tasks.Add(Task.Run(async () =>
            {
                var waits = Stopwatch.StartNew();
                await semaphore.WaitAsync();
                waits.Stop();

                if (waits.Elapsed.TotalSeconds > 1)
                    progress?.Report($"{currentIndex}:等待:{waits.Elapsed.TotalSeconds:F2}");

                int serviceIndex = -1;
                try
                {
                    lock (lockObj)
                    {
                        serviceIndex = serviceTaskCounts.OrderBy(kvp => kvp.Value).First().Key;
                        serviceTaskCounts[serviceIndex]++;
                    }

                    var currentTtsUrl = ttsUrls[serviceIndex];

                    progress?.Report($"\n[{currentIndex + 1}/{actualLimit}] 生成中... (服务: [{serviceIndex + 1}/{ttsUrls.Count}])");
                    progress?.Report($"  文本: {currentCommand.Text[..Math.Min(80, currentCommand.Text.Length)]}");
                    progress?.Report($"  URL: {currentTtsUrl}");
                    var ttsw = Stopwatch.StartNew();
                    var success = await GenerateTTSSegmentAsync(
                        currentCommand.Text, currentCommand.ReferenceAudio, currentCommand.OutputAudio, ttsUrls, lockObj, serviceTaskCounts, 3);
                    ttsw.Stop();
                    progress?.Report($"  用时:{ttsw.Elapsed.TotalSeconds:F2}, {Path.GetFileName(currentCommand.OutputAudio)}");

                    if (success)
                    {
                        progress?.Report($"  成功: {Path.GetFileName(currentCommand.OutputAudio)}");
                        var segment = new TTSSegment
                        {
                            Index = currentCommand.Index,
                            TextTarget = currentCommand.Text,
                            AudioPath = currentCommand.OutputAudio
                        };
                        onSegmentCompleted?.Invoke(currentIndex + 1, actualLimit, segment);
                        return (segment, currentIndex, true);
                    }
                    else
                    {
                        progress?.Report($"  失败: {Path.GetFileName(currentCommand.OutputAudio)}");
                        return (null as TTSSegment, currentIndex, false);
                    }
                }
                finally
                {
                    if (serviceIndex >= 0)
                    {
                        lock (lockObj)
                        {
                            serviceTaskCounts[serviceIndex]--;
                        }
                    }
                    semaphore.Release();
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        var successCount = 0;
        var failedCount = 0;

        foreach (var result in results.OrderBy(r => r.index))
        {
            if (result.success && result.segment != null)
            {
                segmentAudioFiles.Add(result.segment);
                successCount++;
            }
            else
            {
                failedCount++;
            }
        }


        sw.Stop();

        progress?.Report($"\n{new string('=', 60)}");
        progress?.Report($"TTS生成完成!");
        progress?.Report($"\n统计:");
        progress?.Report($"  总命令数: {totalCommands}");
        progress?.Report($"  生成数量: {actualLimit}");
        progress?.Report($"  生成成功: {successCount}");
        progress?.Error($"  生成失败: {failedCount}");
        progress?.Report($"总用时:{sw.Elapsed.TotalSeconds:F2}秒");
        progress?.Report($"\n{new string('=', 60)}");

        return segmentAudioFiles;
    }

}