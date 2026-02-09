using System;
using System.Collections.Generic;
using System.Linq;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.SRT.Core.Extensions;
using VT.Core;

namespace VideoTranslator.SRT.Core.Services;

public class SrtTimeFixer
{
    #region 公共属性

    public int FixedCount { get; private set; }

    public List<string> FixLog { get; private set; } = new();

    #endregion

    #region 修复策略枚举

    public enum FixStrategy
    {
        ShortenPrevious,
        ShiftNext,
        AddGap,
        Balanced
    }

    #endregion

    #region 公共方法

    public SrtFile FixOverlaps(SrtFile srtFile, FixStrategy strategy = FixStrategy.Balanced, TimeSpan? minGap = null)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        FixedCount = 0;
        FixLog.Clear();

        var subtitles = srtFile.Subtitles.OrderBy(s => s.StartTime).ToList();
        var fixedSubtitles = new List<ISrtSubtitle>();

        if (!subtitles.Any())
        {
            return new SrtFile();
        }

        var current = subtitles[0];
        fixedSubtitles.Add(current);

        for (int i = 1; i < subtitles.Count; i++)
        {
            var next = subtitles[i];

            if (current.Overlaps(next))
            {
                FixOverlap(current, next, strategy, minGap);
                FixedCount++;
            }

            fixedSubtitles.Add(next);
            current = next;
        }

        var result = new SrtFile(fixedSubtitles);
        result.ReindexSubtitles();
        return result;
    }

    public SrtFile FixAllOverlaps(SrtFile srtFile, FixStrategy strategy = FixStrategy.Balanced, TimeSpan? minGap = null)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var result = srtFile;
        int previousFixedCount = -1;

        while (FixedCount != previousFixedCount)
        {
            previousFixedCount = FixedCount;
            result = FixOverlaps(result, strategy, minGap);
        }

        return result;
    }

    public List<OverlapInfo> DetectOverlaps(SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var overlaps = new List<OverlapInfo>();
        var subtitles = srtFile.Subtitles.OrderBy(s => s.StartTime).ToList();

        for (int i = 0; i < subtitles.Count - 1; i++)
        {
            var current = subtitles[i];
            var next = subtitles[i + 1];

            if (current.Overlaps(next))
            {
                var overlapDuration = current.EndTime - next.StartTime;
                overlaps.Add(new OverlapInfo
                {
                    CurrentIndex = current.Index,
                    NextIndex = next.Index,
                    CurrentText = current.Text,
                    NextText = next.Text,
                    OverlapDuration = overlapDuration,
                    CurrentEndTime = current.EndTime,
                    NextStartTime = next.StartTime
                });
            }
        }

        return overlaps;
    }

    #endregion

    #region 私有方法

    private void FixOverlap(ISrtSubtitle current, ISrtSubtitle next, FixStrategy strategy, TimeSpan? minGap)
    {
        var overlapDuration = current.EndTime - next.StartTime;
        var gap = minGap ?? TimeSpan.Zero;

        switch (strategy)
        {
            case FixStrategy.ShortenPrevious:
                FixByShorteningPrevious(current, next, overlapDuration, gap);
                break;

            case FixStrategy.ShiftNext:
                FixByShiftingNext(current, next, overlapDuration, gap);
                break;

            case FixStrategy.AddGap:
                FixByAddingGap(current, next, overlapDuration, gap);
                break;

            case FixStrategy.Balanced:
                FixByBalanced(current, next, overlapDuration, gap);
                break;

            default:
                throw new ArgumentException($"未知的修复策略: {strategy}");
        }

        FixLog.Add($"修复重叠: 字幕{current.Index}和{next.Index}, 重叠时长: {overlapDuration.TotalMilliseconds:F0}ms, 策略: {strategy}");
    }

    private void FixByShorteningPrevious(ISrtSubtitle current, ISrtSubtitle next, TimeSpan overlapDuration, TimeSpan gap)
    {
        var newEndTime = next.StartTime - gap;
        if (newEndTime > current.StartTime)
        {
            current.EndTime = newEndTime;
        }
        else
        {
            current.EndTime = current.StartTime + TimeSpan.FromMilliseconds(100);
        }
    }

    private void FixByShiftingNext(ISrtSubtitle current, ISrtSubtitle next, TimeSpan overlapDuration, TimeSpan gap)
    {
        var newStartTime = current.EndTime + gap;
        var shiftAmount = newStartTime - next.StartTime;
        next.StartTime = newStartTime;
        next.EndTime = next.EndTime.Add(shiftAmount);
    }

    private void FixByAddingGap(ISrtSubtitle current, ISrtSubtitle next, TimeSpan overlapDuration, TimeSpan gap)
    {
        var midpoint = (current.EndTime + next.StartTime) / 2;
        current.EndTime = midpoint - gap / 2;
        next.StartTime = midpoint + gap / 2;

        if (current.EndTime <= current.StartTime)
        {
            current.EndTime = current.StartTime + TimeSpan.FromMilliseconds(100);
        }
    }

    private void FixByBalanced(ISrtSubtitle current, ISrtSubtitle next, TimeSpan overlapDuration, TimeSpan gap)
    {
        var totalAdjustment = overlapDuration + gap;
        var halfAdjustment = totalAdjustment / 2;

        current.EndTime = current.EndTime - halfAdjustment;
        next.StartTime = next.StartTime + halfAdjustment;

        if (current.EndTime <= current.StartTime)
        {
            var adjustment = current.StartTime + TimeSpan.FromMilliseconds(100) - current.EndTime;
            current.EndTime = current.StartTime + TimeSpan.FromMilliseconds(100);
            next.StartTime = next.StartTime + adjustment;
        }
    }

    #endregion

    #region 辅助类

    public class OverlapInfo
    {
        public int CurrentIndex { get; set; }
        public int NextIndex { get; set; }
        public string CurrentText { get; set; } = string.Empty;
        public string NextText { get; set; } = string.Empty;
        public TimeSpan OverlapDuration { get; set; }
        public TimeSpan CurrentEndTime { get; set; }
        public TimeSpan NextStartTime { get; set; }

        public override string ToString()
        {
            return $"字幕{CurrentIndex}和{NextIndex}重叠 {OverlapDuration.TotalMilliseconds:F0}ms ({CurrentEndTime.ToSrtTimeString()} - {NextStartTime.ToSrtTimeString()})";
        }
    }

    #endregion
}
