using System;
using System.Drawing;

namespace VT.Win.Forms.Styles;

public class WaveformStyle
{
    #region Properties

    public VadSegmentStyle VadSegmentStyle { get; set; } = new VadSegmentStyle();
    public ClipStyle ClipStyle { get; set; } = new ClipStyle();
    public TimeMarkerStyle TimeMarkerStyle { get; set; } = new TimeMarkerStyle();
    public LabelStyle LabelStyle { get; set; } = new LabelStyle();
    public WaveformLineStyle WaveformLineStyle { get; set; } = new WaveformLineStyle();

    #region Layout

    public int RowSpacing { get; set; } = 6;
    public int ElementSpacing { get; set; } = 1;

    #endregion

    #endregion
}

public class VadSegmentStyle
{
    #region Properties

    public Color FillColor { get; set; } = Color.FromArgb(50, Color.Green);
    public Color BorderColor { get; set; } = Color.Green;
    public Color TextColor { get; set; } = Color.DarkGreen;
    public Font? Font { get; set; }
    public int MinWidthForText { get; set; } = 30;
    public int TextPadding { get; set; } = 5;

    #endregion

    #region Hover Style

    public bool EnableHoverEffect { get; set; } = true;
    public Color HoverFillColor { get; set; } = Color.FromArgb(80, Color.LightGreen);
    public Color HoverBorderColor { get; set; } = Color.DarkGreen;
    public Color HoverTextColor { get; set; } = Color.Black;

    #endregion
}

public class ClipStyle
{
    #region Properties

    public SrtClipStyle SrtClipStyle { get; set; } = new SrtClipStyle();
    public AudioClipStyle SourceAudioStyle { get; set; } = new AudioClipStyle
    {
        Color = Color.Orange
    };
    public AudioClipStyle TargetAudioStyle { get; set; } = new AudioClipStyle
    {
        Color = Color.Red
    };
    public AudioClipStyle AdjustedAudioStyle { get; set; } = new AudioClipStyle
    {
        Color = Color.Purple
    };
    public int MinWidthForText { get; set; } = 30;
    public int TextPadding { get; set; } = 5;

    #region Border Style

    public bool ShowBorder { get; set; } = true;
    public Color BorderColor { get; set; } = Color.Gray;
    public float BorderWidth { get; set; } = 1.0f;
    public ElementBorderStyle BorderStyle { get; set; } = ElementBorderStyle.Solid;

    #endregion

    #region Hover Style

    public bool EnableHoverEffect { get; set; } = true;
    public Color HoverBorderColor { get; set; } = Color.Blue;
    public Color HoverBackgroundColor { get; set; } = Color.FromArgb(50, Color.LightBlue);
    public float HoverBorderWidth { get; set; } = 2.0f;

    #endregion

    #endregion
}

public enum ElementBorderStyle
{
    Solid,
    Dashed,
    Dotted
}

public class SrtClipStyle
{
    #region Properties

    public Color FillColor { get; set; } = Color.FromArgb(50, Color.Blue);
    public Color BorderColor { get; set; } = Color.Blue;
    public Color TextColor { get; set; } = Color.DarkBlue;
    public Font? Font { get; set; }
    public int MinWidthForText { get; set; } = 30;
    public int TextPadding { get; set; } = 5;

    #endregion

    #region Hover Style

    public bool EnableHoverEffect { get; set; } = true;
    public Color HoverFillColor { get; set; } = Color.FromArgb(80, Color.CornflowerBlue);
    public Color HoverBorderColor { get; set; } = Color.DarkBlue;
    public Color HoverTextColor { get; set; } = Color.White;

    #endregion
}

public class AudioClipStyle
{
    #region Properties

    public Color Color { get; set; } = Color.Orange;
    public Font? Font { get; set; }
    public bool ShowSpeedMultiplier { get; set; } = true;
    public float LineWidth { get; set; } = 1.0f;
    public int MinWidthForText { get; set; } = 30;
    public int TextPadding { get; set; } = 5;

    #endregion

    #region Border Style

    public bool ShowBorder { get; set; } = true;
    public Color BorderColor { get; set; } = Color.Gray;
    public float BorderWidth { get; set; } = 1.0f;
    public ElementBorderStyle BorderStyle { get; set; } = ElementBorderStyle.Solid;

    #endregion

    #region Hover Style

    public bool EnableHoverEffect { get; set; } = true;
    public Color HoverBorderColor { get; set; } = Color.Blue;
    public Color HoverBackgroundColor { get; set; } = Color.FromArgb(50, Color.LightBlue);
    public float HoverBorderWidth { get; set; } = 2.0f;

    #endregion
}

public class TimeMarkerStyle
{
    #region Properties

    public Color LineColor { get; set; } = Color.LightGray;
    public Color TextColor { get; set; } = Color.Gray;
    public Font? Font { get; set; }
    public int MinTextWidth { get; set; } = 50;
    public int MinMarkerIntervalPixels { get; set; } = 60;

    #endregion
}

public class LabelStyle
{
    #region Properties

    public VadLabelStyle VadLabel { get; set; } = new VadLabelStyle();
    public SrtLabelStyle SrtLabel { get; set; } = new SrtLabelStyle();
    public SourceAudioLabelStyle SourceAudioLabel { get; set; } = new SourceAudioLabelStyle();
    public TargetAudioLabelStyle TargetAudioLabel { get; set; } = new TargetAudioLabelStyle();
    public AdjustedAudioLabelStyle AdjustedAudioLabel { get; set; } = new AdjustedAudioLabelStyle();
    public AudioLabelStyle AudioLabel { get; set; } = new AudioLabelStyle();
    public Font? Font { get; set; }
    public int Padding { get; set; } = 5;

    #endregion
}

public class VadLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.DarkGreen;
    public string Text { get; set; } = "语音活动";

    #endregion
}

public class SrtLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.DarkBlue;
    public string Text { get; set; } = "源字幕";

    #endregion
}

public class SourceAudioLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.DarkOrange;
    public string Text { get; set; } = "源音频";

    #endregion
}

public class TargetAudioLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.DarkRed;
    public string Text { get; set; } = "目标音频";

    #endregion
}

public class AdjustedAudioLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.DarkGoldenrod;
    public string Text { get; set; } = "调整音频";

    #endregion
}

public class AudioLabelStyle
{
    #region Properties

    public Color Color { get; set; } = Color.Gray;
    public string Text { get; set; } = "完整音频";

    #endregion
}

public class WaveformLineStyle
{
    #region Properties

    public Color Color { get; set; } = Color.Gray;
    public float Width { get; set; } = 1.0f;
    public float AmplitudeScale { get; set; } = 0.9f;

    #endregion
}
