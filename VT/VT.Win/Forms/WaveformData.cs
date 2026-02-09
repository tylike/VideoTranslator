using VT.Module.BusinessObjects;

namespace VT.Win.Forms;

public class WaveformData
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public float[] Samples { get; set; } = Array.Empty<float>();
    public TimeSpan Duration { get; set; }
    public int SampleRate { get; set; }
    public VadSegmentInfo[] VadSegments { get; set; } = Array.Empty<VadSegmentInfo>();
    public TimeLineClip[] Clips { get; set; } = Array.Empty<TimeLineClip>();
}

public class VadSegmentInfo
{
    public double StartMS { get; set; }
    public double EndMS { get; set; }
    public int Index { get; set; }
}
