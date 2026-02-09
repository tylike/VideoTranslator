using System;
using System.Drawing;
using System.Windows.Forms;
using VT.Module.BusinessObjects;

namespace VT.Win.Forms;

public class WaveformTooltip
{
    #region Fields

    private readonly WaveformData data;
    private readonly Control parent;
    private readonly WaveformRendererV2 renderer;
    private ToolTip tooltip;
    private Point? lastMousePosition;
    private string? lastTooltipText;

    #endregion

    #region Constructor

    public WaveformTooltip(WaveformData data, Control parent, WaveformRendererV2 renderer)
    {
        this.data = data;
        this.parent = parent;
        this.renderer = renderer;
        InitializeTooltip();
    }

    #endregion

    #region Initialization

    private void InitializeTooltip()
    {
        tooltip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 100,
            ReshowDelay = 50,
            ShowAlways = true
        };
    }

    #endregion

    #region Public Methods

    public void UpdateTooltip(Point mouseLocation, Rectangle waveformRect)
    {
        if (!waveformRect.Contains(mouseLocation))
        {
            lastMousePosition = null;
            tooltip.Hide(parent);
            return;
        }

        int scrollOffset = 0;
        int totalWidth = renderer.GetTotalWaveformWidth();
        int visibleWidth = waveformRect.Width;

        if (totalWidth > visibleWidth)
        {
            scrollOffset = 0;
        }

        int relativeX = mouseLocation.X - waveformRect.X + scrollOffset;
        double totalDurationMS = data.Duration.TotalMilliseconds;

        if (totalDurationMS <= 0)
        {
            return;
        }

        double currentMS = (relativeX / (double)totalWidth) * totalDurationMS;
        string timeText = TimeFormatter.FormatTime((int)(currentMS / 1000));

        string tooltipText = $"时间: {timeText}";

        #region Check VAD Segments

        if (data.VadSegments != null)
        {
            foreach (var segment in data.VadSegments)
            {
                if (currentMS >= segment.StartMS && currentMS <= segment.EndMS)
                {
                    tooltipText += $"\n语音活动段: {segment.Index}";
                    break;
                }
            }
        }

        #endregion

        #region Check Clips

        if (data.Clips != null)
        {
            foreach (var clip in data.Clips)
            {
                double startMS = clip.SourceSRTClip?.Start.TotalMilliseconds ?? 0;
                double endMS = clip.SourceSRTClip?.End.TotalMilliseconds ?? 0;

                if (currentMS >= startMS && currentMS <= endMS)
                {
                    tooltipText += $"\n片段: {clip.Index}";

                    if (clip.SourceSRTClip != null && !string.IsNullOrEmpty(clip.SourceSRTClip.Text))
                    {
                        tooltipText += $"\n源字幕: {clip.SourceSRTClip.Text}";
                    }

                    if (clip.SourceAudioClip != null && !string.IsNullOrEmpty(clip.SourceAudioClip.FilePath))
                    {
                        tooltipText += $"\n源音频: {System.IO.Path.GetFileName(clip.SourceAudioClip.FilePath)}";
                    }

                    if (clip.TargetSRTClip != null && !string.IsNullOrEmpty(clip.TargetSRTClip.Text))
                    {
                        tooltipText += $"\n目标字幕: {clip.TargetSRTClip.Text}";
                    }

                    if (clip.TargetAudioClip != null && !string.IsNullOrEmpty(clip.TargetAudioClip.FilePath))
                    {
                        tooltipText += $"\n目标音频: {System.IO.Path.GetFileName(clip.TargetAudioClip.FilePath)}";
                    }

                    if (clip.AdjustedTargetAudioClip != null && !string.IsNullOrEmpty(clip.AdjustedTargetAudioClip.FilePath))
                    {
                        tooltipText += $"\n调整音频: {System.IO.Path.GetFileName(clip.AdjustedTargetAudioClip.FilePath)}";
                        if (Math.Abs(clip.SpeedMultiplier - 1.0) > 0.001)
                        {
                            tooltipText += $" (速度: x{clip.SpeedMultiplier:F2})";
                        }
                    }

                    break;
                }
            }
        }

        #endregion

        if (lastMousePosition == null || lastMousePosition != mouseLocation || lastTooltipText != tooltipText)
        {
            tooltip.SetToolTip(parent, tooltipText);
            lastMousePosition = mouseLocation;
            lastTooltipText = tooltipText;
        }
    }

    public void Hide()
    {
        tooltip.Hide(parent);
        lastMousePosition = null;
    }

    #endregion
}
