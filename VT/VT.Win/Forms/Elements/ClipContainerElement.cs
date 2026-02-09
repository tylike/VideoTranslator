using System;
using System.Collections.Generic;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Styles;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms.Elements;

public class ClipContainerElement : WaveformElement
{
    #region Fields

    private readonly TimeLineClip clip;
    private readonly ClipStyle style;
    private readonly List<IWaveformElement> childElements;
    private readonly int rowSpacing;
    private readonly int elementSpacing;

    #endregion

    #region Properties

    public TimeLineClip Clip => clip;
    public ClipStyle Style => style;
    public IReadOnlyList<IWaveformElement> ChildElements => childElements.AsReadOnly();

    #endregion

    #region Constructor

    public ClipContainerElement(TimeLineClip clip, ClipStyle? style = null, int rowSpacing = 6, int elementSpacing = 1)
        : base($"clip_container_{clip.Index}")
    {
        this.clip = clip ?? throw new ArgumentNullException(nameof(clip));
        this.style = style ?? new ClipStyle();
        this.rowSpacing = rowSpacing;
        this.elementSpacing = elementSpacing;
        this.childElements = new List<IWaveformElement>();
    }

    #endregion

    #region Public Methods

    public void UpdateBounds(double totalDurationMS, int totalWidth, int rowHeight, int startRow)
    {
        double startMS = clip.SourceSRTClip?.Start.TotalMilliseconds ?? 0;
        double endMS = clip.SourceSRTClip?.End.TotalMilliseconds ?? 0;

        if (startMS >= endMS) return;

        int startX = (int)((startMS / totalDurationMS) * totalWidth);
        int endX = (int)((endMS / totalDurationMS) * totalWidth);
        int clipWidth = endX - startX;

        Bounds = new Rectangle(startX, startRow * rowHeight, clipWidth, rowHeight * 5);

        #region Update Child Elements

        childElements.Clear();

        #region Create SRT Clip Element

        if (clip.SourceSRTClip != null && !string.IsNullOrEmpty(clip.SourceSRTClip.Text))
        {
            var srtElement = new SrtClipElement(clip.SourceSRTClip, style.SrtClipStyle, elementSpacing);
            srtElement.UpdateBounds(totalDurationMS, totalWidth, startRow * rowHeight + rowHeight, rowHeight, elementSpacing);
            childElements.Add(srtElement);
        }

        #endregion

        #region Create Source Audio Element

        if (clip.SourceAudioClip != null && !string.IsNullOrEmpty(clip.SourceAudioClip.FilePath))
        {
            var sourceAudioElement = new AudioClipElement(clip.SourceAudioClip, style.SourceAudioStyle, null, elementSpacing);
            sourceAudioElement.UpdateBounds(totalDurationMS, totalWidth, startRow * rowHeight + rowHeight * 2, rowHeight, elementSpacing);
            childElements.Add(sourceAudioElement);
        }

        #endregion

        #region Create Target Audio Element

        if (clip.TargetAudioClip != null && !string.IsNullOrEmpty(clip.TargetAudioClip.FilePath))
        {
            var targetAudioElement = new AudioClipElement(clip.TargetAudioClip, style.TargetAudioStyle, null, elementSpacing);
            targetAudioElement.UpdateBounds(totalDurationMS, totalWidth, startRow * rowHeight + rowHeight * 3, rowHeight, elementSpacing);
            childElements.Add(targetAudioElement);
        }

        #endregion

        #region Create Adjusted Audio Element

        if (clip.AdjustedTargetAudioClip != null && !string.IsNullOrEmpty(clip.AdjustedTargetAudioClip.FilePath))
        {
            var adjustedAudioElement = new AudioClipElement(clip.AdjustedTargetAudioClip, style.AdjustedAudioStyle, clip.SpeedMultiplier, elementSpacing);
            adjustedAudioElement.UpdateBounds(totalDurationMS, totalWidth, startRow * rowHeight + rowHeight * 4, rowHeight, elementSpacing);
            childElements.Add(adjustedAudioElement);
        }

        #endregion

        #endregion
    }

    #endregion

    #region Protected Methods

    protected override void OnRender(Graphics g, WaveformRenderContext context)
    {
        #region Render Child Elements

        foreach (var child in childElements)
        {
            child.Render(g, context);
        }

        #endregion
    }

    #endregion
}
