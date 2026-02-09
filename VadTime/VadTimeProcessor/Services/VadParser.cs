using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;
using VT.Core;

namespace VadTimeProcessor.Services;

/// <summary>
/// VAD解析器 - 负责解析VAD输出文件并生成语音段落
/// </summary>
public static class VadParser
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
    /// 从文件解析VAD输出
    /// </summary>
    /// <param name="filePath">VAD输出文件路径</param>
    /// <returns>语音段落链表</returns>
    public static LinkedList<SpeechSegment> ParseVadOutput(string filePath)
    {
        #region 文件读取

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"VAD输出文件未找到: {filePath}");
        }

        var lines = File.ReadAllLines(filePath);

        #endregion

        #region 解析段落

        var segments = new List<SpeechSegment>();
        
        foreach (var line in lines)
        {
            var segment = ParseLine(line);
            if (segment != null)
            {
                segments.Add(segment);
            }
        }

        #endregion

        #region 构建链表

        return BuildLinkedList(segments);

        #endregion
    }

    /// <summary>
    /// 从文本内容解析VAD输出
    /// </summary>
    /// <param name="vadText">VAD文本内容</param>
    /// <returns>语音段落链表</returns>
    public static LinkedList<SpeechSegment> ParseVadOutputFromText(string vadText)
    {
        #region 解析段落

        var segments = new List<SpeechSegment>();
        var lines = vadText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var segment = ParseLine(line);
            if (segment != null)
            {
                segments.Add(segment);
            }
        }

        #endregion

        #region 构建链表

        return BuildLinkedList(segments);

        #endregion
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 解析单行VAD输出
    /// </summary>
    /// <param name="line">VAD输出行</param>
    /// <returns>语音段落对象</returns>
    private static SpeechSegment? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            line, 
            @"Speech segment (\d+): start = ([\d.]+), end = ([\d.]+)"
        );

        if (!match.Success)
        {
            return null;
        }

        int index = int.Parse(match.Groups[1].Value);
        double start = double.Parse(match.Groups[2].Value) * 10;
        double end = double.Parse(match.Groups[3].Value) * 10;

        return new SpeechSegment(index, start, end);
    }

    /// <summary>
    /// 构建语音段落链表并建立导航链接
    /// </summary>
    /// <param name="segments">语音段落列表</param>
    /// <returns>语音段落链表</returns>
    private static LinkedList<SpeechSegment> BuildLinkedList(List<SpeechSegment> segments)
    {
        var linkedList = new LinkedList<SpeechSegment>();

        #region 添加段落到链表

        foreach (var segment in segments)
        {
            linkedList.AddLast(segment);
        }

        #endregion

        #region 建立导航链接

        var currentNode = linkedList.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            
            if (currentNode.Previous != null)
            {
                segment.Previous = currentNode.Previous.Value;
            }
            
            if (currentNode.Next != null)
            {
                segment.Next = currentNode.Next.Value;
            }
            
            currentNode = currentNode.Next;
        }

        #endregion

        return linkedList;
    }

    #endregion

    #region 分析方法

    /// <summary>
    /// 打印段落间隔分析
    /// </summary>
    /// <param name="segments">语音段落链表</param>
    public static void PrintSegmentGaps(LinkedList<SpeechSegment> segments)
    {
        _progressService?.Title("段落间隔分析");
        _progressService?.Report();

        var currentNode = segments.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            var gap = segment.GetGapToNext() / 1000;
            
            if (gap > 0)
            {
                _progressService?.Report($"段落 {segment.Index} 和 {segment.Next?.Index} 之间的间隔: {gap:00.00}s");
            }
            
            currentNode = currentNode.Next;
        }

        _progressService?.Report();
    }

    /// <summary>
    /// 打印统计信息
    /// </summary>
    /// <param name="segments">语音段落链表</param>
    public static void PrintStatistics(LinkedList<SpeechSegment> segments)
    {
        #region 计算统计数据

        double totalDuration = 0;
        double totalGap = 0;
        int gapCount = 0;
        double minGap = double.MaxValue;
        double maxGap = double.MinValue;

        var currentNode = segments.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            totalDuration += segment.DurationMS;
            
            var gap = segment.GetGapToNext();
            if (gap > 0)
            {
                totalGap += gap;
                gapCount++;
                
                if (gap < minGap) minGap = gap;
                if (gap > maxGap) maxGap = gap;
            }
            
            currentNode = currentNode.Next;
        }

        double averageGap = gapCount > 0 ? totalGap / gapCount : 0;

        #endregion

        #region 打印结果

        _progressService?.Title("VAD统计信息");
        _progressService?.Report($"总段落数: {segments.Count}");
        _progressService?.Report($"总语音时长: {totalDuration / 1000:00.00}s");
        _progressService?.Report($"总间隔时长: {totalGap / 1000:00.00}s");
        _progressService?.Report($"间隔数量: {gapCount}");
        _progressService?.Report($"最小间隔: {minGap / 1000:00.00}s");
        _progressService?.Report($"最大间隔: {maxGap / 1000:00.00}s");
        _progressService?.Report($"平均间隔: {averageGap / 1000:00.00}s");
        _progressService?.Report();

        #endregion
    }

    /// <summary>
    /// 打印所有段落信息
    /// </summary>
    /// <param name="segments">语音段落链表</param>
    public static void PrintAllSegments(LinkedList<ISpeechSegment> segments)
    {
        _progressService?.Title("所有语音段落");
        _progressService?.Report();

        var currentNode = segments.First;
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            var gap = segment.GetGapToNext() / 1000;
            
            string gapText = "";
            if (gap > 0)
            {
                gapText = $"({gap:0.00}s)";
            }
            
            _progressService?.Report($"[{segment.StartMS / 1000:00.00}s-{segment.DurationMS / 1000:00.00}s-{segment.EndMS / 1000:00.00}s]{gapText}");
            
            currentNode = currentNode.Next;
        }

        _progressService?.Report();
    }

    /// <summary>
    /// 保存VAD段落到文件
    /// </summary>
    /// <param name="segments">语音段落链表</param>
    /// <param name="outputPath">输出文件路径</param>
    public static void SaveVadSegments(LinkedList<SpeechSegment> segments, string outputPath)
    {
        #region 验证参数

        if (segments == null || segments.Count == 0)
        {
            throw new ArgumentException("没有段落需要保存");
        }

        #endregion

        #region 写入文件

        var lines = new List<string>();
        var currentNode = segments.First;
        
        while (currentNode != null)
        {
            var segment = currentNode.Value;
            double startSeconds = segment.StartMS / 1000;
            double endSeconds = segment.EndMS / 1000;
            lines.Add($"Speech segment {segment.Index}: start = {startSeconds:F2}, end = {endSeconds:F2}");
            currentNode = currentNode.Next;
        }

        File.WriteAllLines(outputPath, lines);
        _progressService?.Report($"VAD段落已保存到: {outputPath}");
        _progressService?.Report();

        #endregion
    }

    #endregion
}
