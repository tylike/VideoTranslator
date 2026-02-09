using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// VAD段落合并器 - 负责合并相邻的语音段落
/// </summary>
public static class VadSegmentMerger
{
    #region 私有字段

    /// <summary>
    /// 进度服务实例
    /// </summary>
    private static IProgressService? _progressService;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置进度服务实例
    /// </summary>
    /// <param name="progressService">进度服务实例</param>
    public static void SetProgressService(IProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>
    /// 根据间隔时间和最小段落时长智能合并段落
    /// </summary>
    /// <param name="segments">原始语音段落链表</param>
    /// <param name="maxGapSeconds">最大静音间隔（秒），大于此值的段落不会合并</param>
    /// <param name="minDurationSeconds">最小段落时长（秒），小于此值的段落会尝试与后面的合并</param>
    /// <returns>合并后的语音段落链表</returns>
    public static LinkedList<ISpeechSegment> MergeSegmentsByGap(
        LinkedList<ISpeechSegment> segments, 
        double maxGapSeconds = 2.0,
        double minDurationSeconds = 1.0)
    {
        #region 初始化合并列表

        var mergedList = new LinkedList<ISpeechSegment>();
        
        if (segments.Count == 0)
        {
            return mergedList;
        }

        #endregion

        #region 合并段落

        var currentNode = segments.First;
        int mergedIndex = 0;
        
        while (currentNode != null)
        {
            var currentSegment = currentNode.Value;
            var mergedStart = currentSegment.StartMS;
            var mergedEnd = currentSegment.EndMS;
            double mergedDurationMs = currentSegment.DurationMS;
            
            #region 查找所有需要合并的段落

            while (currentNode.Next != null)
            {
                var nextSegment = currentNode.Next.Value;
                var gap = currentNode.Value.GetGapToNext() / 1000;
                double nextDurationMs = nextSegment.DurationMS;
                double currentDurationMs = mergedEnd - mergedStart;
                
                #region 判断是否合并

                bool shouldMerge = false;

                if (currentDurationMs < minDurationSeconds * 1000)
                {
                    #region 当前段落太短，即使gap较大也尝试合并

                    if (gap < maxGapSeconds * 2)
                    {
                        shouldMerge = true;
                        _progressService?.Report($"  段落 {currentNode.Value.Index} 太短 ({currentDurationMs/1000:F2}s < {minDurationSeconds}s)，强制合并（间隔 {gap:F2}s）");
                    }

                    #endregion
                }
                else if (gap < maxGapSeconds)
                {
                    #region 段落足够长，检查间隔是否小于阈值

                    shouldMerge = true;

                    #endregion
                }

                if (shouldMerge)
                {
                    mergedEnd = nextSegment.EndMS;
                    currentNode = currentNode.Next;
                }
                else
                {
                    break;
                }

                #endregion
            }

            #endregion

            #region 创建合并后的段落

            var mergedSegment = new SpeechSegment(mergedIndex, mergedStart, mergedEnd);
            
            if (mergedList.Count > 0)
            {
                var lastNode = mergedList.Last;
                if (lastNode != null)
                {
                    mergedSegment.Previous = lastNode.Value;
                    lastNode.Value.Next = mergedSegment;
                }
            }
            
            mergedList.AddLast(mergedSegment);
            mergedIndex++;

            #endregion

            #region 移动到下一个未合并的段落

            currentNode = currentNode.Next;

            #endregion
        }

        #endregion

        return mergedList;
    }

    /// <summary>
    /// 生成合并后的音频文件
    /// </summary>
    /// <param name="inputAudioPath">输入音频文件路径</param>
    /// <param name="mergedSegments">合并后的语音段落链表</param>
    /// <param name="outputPath">输出音频文件路径</param>
    public static void GenerateMergedAudio(string inputAudioPath, LinkedList<SpeechSegment> mergedSegments, string outputPath)
    {
        #region 验证参数

        if (!File.Exists(inputAudioPath))
        {
            throw new FileNotFoundException($"输入音频文件未找到: {inputAudioPath}");
        }

        if (mergedSegments.Count == 0)
        {
            throw new ArgumentException("没有段落需要合并");
        }

        #endregion

        #region 构建FFmpeg滤镜复杂命令

        var filterParts = new List<string>();
        var mapParts = new List<string>();
        int segmentCount = mergedSegments.Count;

        #region 分割输入流

        var splitOutputs = new List<string>();
        for (int i = 0; i < segmentCount; i++)
        {
            splitOutputs.Add($"[s{i}]");
        }
        string splitFilter = $"[0:a]asplit={segmentCount}{string.Join("", splitOutputs)}";

        #endregion

        #region 裁剪每个段落

        int segmentIndex = 0;
        foreach (var segment in mergedSegments)
        {
            double startSeconds = segment.StartMS / 1000;
            double endSeconds = segment.EndMS / 1000;

            filterParts.Add($"[s{segmentIndex}]atrim={startSeconds:F3}:{endSeconds:F3},asetpts=PTS-STARTPTS[a{segmentIndex}]");
            mapParts.Add($"[a{segmentIndex}]");
            segmentIndex++;
        }

        #endregion

        #region 拼接所有段落

        string filterComplex = $"{splitFilter};{string.Join(";", filterParts)}";
        string concatFilter = $"{string.Join("", mapParts)}concat=n={segmentCount}:v=0:a=1[out]";
        string fullFilter = $"{filterComplex};{concatFilter}";

        #endregion

        #endregion

        #region 构建FFmpeg命令

        var ffmpegPath = @"d:\VideoTranslator\ffmpeg\ffmpeg.exe";
        var arguments = $"-y -i \"{inputAudioPath}\" -filter_complex \"{fullFilter}\" -map \"[out]\" -c:a pcm_s16le \"{outputPath}\"";

        #endregion

        #region 执行FFmpeg命令

        _progressService?.Title("生成合并音频");
        _progressService?.Report($"输入: {inputAudioPath}");
        _progressService?.Report($"输出: {outputPath}");
        _progressService?.Report($"段落数: {mergedSegments.Count}");
        _progressService?.Report();

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("启动FFmpeg进程失败");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            _progressService?.Error($"FFmpeg错误: {error}");
            throw new InvalidOperationException($"FFmpeg执行失败，退出码: {process.ExitCode}");
        }

        _progressService?.Success("合并音频生成成功！");
        _progressService?.Report();

        #endregion
    }

    #endregion
}
