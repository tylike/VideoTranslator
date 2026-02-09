using System;
using System.Collections.Generic;
using System.Drawing;

namespace VT.Win.Forms.Models
{
    public class SubtitleTrack
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public Color Color { get; set; }
        public List<SubtitleItem> Subtitles { get; set; }
    }

    public class SubtitleItem
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; }
    }

    public class TimelineTrackData
    {
        public string Label { get; set; } = "";
        public List<TimelineBarData> Bars { get; set; } = new();
    }

    public class TimelineBarData
    {
        public int Index { get; set; }
        public double LeftPercentage { get; set; }
        public double WidthPercentage { get; set; }
        public string CssClass { get; set; } = "";
        public string Tooltip { get; set; } = "";
    }

    public class RulerMark
    {
        public double Percentage { get; set; }
        public string Label { get; set; } = "";
        public bool IsMajor { get; set; }
    }
}
