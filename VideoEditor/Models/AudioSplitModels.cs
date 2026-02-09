using System.Collections.ObjectModel;
using VideoTranslator.Models;

namespace VideoEditor.Models;

public class AudioSplitResult
{
    public int SegmentCount { get; set; }
    public List<AudioSplitSegment> Segments { get; set; } = new();
    public List<SplitPointInfo> SplitPoints { get; set; } = new();
    public decimal AverageDuration { get; set; }
    public decimal MinSilenceDuration { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AudioSplitSegment
{
    public int Index { get; set; }
    public decimal Start { get; set; }
    public decimal End { get; set; }
    public decimal Duration { get; set; }
    public string DurationText { get; set; } = string.Empty;
}

public class SplitPointInfo
{
    public int Index { get; set; }
    public decimal Time { get; set; }
    public decimal SilenceDuration { get; set; }
    public int SilenceSegmentIndex { get; set; }
    public string Reason { get; set; } = string.Empty;
}
