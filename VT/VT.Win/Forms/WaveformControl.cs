using System;
using System.Drawing;
using System.Windows.Forms;
using VT.Module.BusinessObjects;

namespace VT.Win.Forms;

public class WaveformControl : Control
{
    #region Constants

    private const int MinWaveformHeight = 360;
    private const int ControlsHeight = 70;
    private const string LogFilePath = @"d:\VideoTranslator\waveform_debug.log";

    #endregion

    #region Fields

    private readonly WaveformData data;
    private readonly WaveformLogger logger;
    private readonly AudioPlayer audioPlayer;
    private readonly WaveformControls controls;
    private readonly WaveformRendererV2 renderer;
    private readonly WaveformTooltip tooltip;

    #endregion

    #region Constructor

    public WaveformControl(WaveformData data)
    {
        this.data = data;
        this.logger = new WaveformLogger(LogFilePath);
        this.audioPlayer = new AudioPlayer();
        this.controls = new WaveformControls(this);
        this.renderer = new WaveformRendererV2(data, logger);
        this.tooltip = new WaveformTooltip(data, this, renderer);

        #region Initialize Control Properties

        DoubleBuffered = true;
        MinimumSize = new Size(100, MinWaveformHeight + ControlsHeight);

        #endregion

        #region Subscribe to Events

        SubscribeToEvents();
        SubscribeToRendererEvents();

        #endregion

        logger.LogInfo($"WaveformControl initialized, file: {data.FileName}, samples: {data.Samples.Length}, VAD segments: {data.VadSegments?.Length ?? 0}, clips: {data.Clips?.Length ?? 0}");
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeToEvents()
    {
        #region Control Events

        controls.PlayButtonClicked += Controls_PlayButtonClicked;
        controls.ScrollBarScrolled += Controls_ScrollBarScrolled;
        controls.ZoomTrackBarChanged += Controls_ZoomTrackBarChanged;

        #endregion

        #region Audio Player Events

        audioPlayer.PlaybackStateChanged += AudioPlayer_PlaybackStateChanged;

        #endregion
    }

    private void Controls_PlayButtonClicked(object? sender, EventArgs e)
    {
        audioPlayer.TogglePlay(data.FilePath);
    }

    private void Controls_ScrollBarScrolled(object? sender, ScrollEventArgs e)
    {
        logger.LogInfo($"Scroll bar scrolled, Value: {controls.HScrollBar.Value}, Maximum: {controls.HScrollBar.Maximum}");
        Invalidate();
    }

    private void Controls_ZoomTrackBarChanged(object? sender, EventArgs e)
    {
        logger.LogInfo($"Zoom track bar changed, zoomLevel: {controls.ZoomLevel:F2}");
        renderer.SetZoomLevel(controls.ZoomLevel);
        UpdateScrollBar();
        Invalidate();
    }

    private void AudioPlayer_PlaybackStateChanged(object? sender, EventArgs e)
    {
        controls.UpdatePlayButton(audioPlayer.IsPlaying);
    }

    private void SubscribeToRendererEvents()
    {
        #region Subscribe to Renderer Events

        renderer.ElementsCreated += Renderer_ElementsCreated;

        #endregion
    }

    private void Renderer_ElementsCreated(object? sender, EventArgs e)
    {
        #region Subscribe to Element Events

        foreach (var element in renderer.Elements)
        {
            if (element is Interactions.IInteractiveElement interactive)
            {
                interactive.Click += Element_Click;
                interactive.DoubleClick += Element_DoubleClick;
            }
        }

        #endregion
    }

    private void Element_Click(object? sender, Interactions.ElementClickEventArgs e)
    {
        logger.LogInfo($"Element clicked: {e.Element.Id}");

        #region Handle SRT Clip Click

        if (e.Element is Elements.SrtClipElement srtElement)
        {
            var srtClip = srtElement.SrtClip;
            logger.LogInfo($"SRT clip clicked - Index: {srtClip.Index}");

            #region Find Parent Clip and Play Source Audio

            var parentClip = FindParentClip(srtClip);
            if (parentClip != null && parentClip.SourceAudioClip != null && !string.IsNullOrEmpty(parentClip.SourceAudioClip.FilePath))
            {
                audioPlayer.Play(parentClip.SourceAudioClip.FilePath);
            }

            #endregion
        }

        #endregion

        #region Handle Audio Clip Click

        if (e.Element is Elements.AudioClipElement audioElement)
        {
            logger.LogInfo($"Audio clip clicked - Index: {audioElement.AudioClip.Index}");

            if (!string.IsNullOrEmpty(audioElement.AudioClip.FilePath))
            {
                audioPlayer.Play(audioElement.AudioClip.FilePath);
            }
        }

        #endregion

        #region Handle VAD Segment Click

        if (e.Element is Elements.VadSegmentElement vadElement)
        {
            var segment = vadElement.Segment;
            logger.LogInfo($"VAD segment clicked - Index: {segment.Index}, StartMS: {segment.StartMS:F2}, EndMS: {segment.EndMS:F2}");
        }

        #endregion
    }

    private void Element_DoubleClick(object? sender, Interactions.ElementDoubleClickEventArgs e)
    {
        logger.LogInfo($"Element double clicked: {e.Element.Id}");

        #region Handle SRT Clip Double Click

        if (e.Element is Elements.SrtClipElement srtElement)
        {
            var srtClip = srtElement.SrtClip;
            logger.LogInfo($"SRT clip double clicked - Index: {srtClip.Index}");

            #region Find Parent Clip and Play Target Audio

            var parentClip = FindParentClip(srtClip);
            if (parentClip != null && parentClip.TargetAudioClip != null && !string.IsNullOrEmpty(parentClip.TargetAudioClip.FilePath))
            {
                audioPlayer.Play(parentClip.TargetAudioClip.FilePath);
            }

            #endregion
        }

        #endregion

        #region Handle Audio Clip Double Click

        if (e.Element is Elements.AudioClipElement audioElement)
        {
            logger.LogInfo($"Audio clip double clicked - Index: {audioElement.AudioClip.Index}");

            if (!string.IsNullOrEmpty(audioElement.AudioClip.FilePath))
            {
                audioPlayer.Play(audioElement.AudioClip.FilePath);
            }
        }

        #endregion
    }

    private TimeLineClip? FindParentClip(SRTClip srtClip)
    {
        if (data.Clips == null) return null;

        foreach (var clip in data.Clips)
        {
            if (clip.SourceSRTClip == srtClip)
            {
                return clip;
            }
        }

        return null;
    }

    #endregion

    #region Override Methods

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        controls.UpdateControlPositions();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        #region Handle Interaction Manager

        int waveformHeight = Math.Max(MinWaveformHeight, Height - ControlsHeight);
        var waveformRect = new Rectangle(50, 10, Width - 60, waveformHeight);

        if (waveformRect.Contains(e.Location))
        {
            var relativePoint = new Point(e.X - 50, e.Y - 10);
            renderer.InteractionManager.HandleMouseMove(relativePoint, e.Button);
            Cursor = renderer.InteractionManager.GetCursor();
        }
        else
        {
            Cursor = Cursors.Default;
        }

        #endregion

        #region Update Tooltip

        if (waveformRect.Contains(e.Location))
        {
            tooltip.UpdateTooltip(e.Location, waveformRect);
        }
        else
        {
            tooltip.Hide();
        }

        #endregion
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        tooltip.Hide();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        #region Handle Interaction Manager

        if (e.Button == MouseButtons.Left)
        {
            int waveformHeight = Math.Max(MinWaveformHeight, Height - ControlsHeight);
            var waveformRect = new Rectangle(50, 10, Width - 60, waveformHeight);

            if (waveformRect.Contains(e.Location))
            {
                var relativePoint = new Point(e.X - 50, e.Y - 10);
                renderer.InteractionManager.HandleMouseDown(relativePoint, e.Button, 1, 0);
                renderer.InteractionManager.HandleMouseUp(relativePoint, e.Button, 1, 0);
            }
        }

        #endregion
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        #region Draw Waveform Background

        int waveformHeight = Math.Max(MinWaveformHeight, Height - ControlsHeight);
        var waveformRect = new Rectangle(50, 10, Width - 60, waveformHeight);
        e.Graphics.FillRectangle(Brushes.White, waveformRect);

        #endregion

        #region Draw Waveform Bitmap

        if (renderer.WaveformBitmap == null || renderer.WaveformBitmap.Width != renderer.GetTotalWaveformWidth() || renderer.WaveformBitmap.Height != waveformRect.Height)
        {
            renderer.CreateWaveformBitmap(waveformRect.Height);
        }

        if (renderer.WaveformBitmap != null)
        {
            var scrollOffset = controls.HScrollBar.Value;
            var sourceRect = new Rectangle(scrollOffset, 0, waveformRect.Width, waveformRect.Height);
            e.Graphics.DrawImage(renderer.WaveformBitmap, waveformRect, sourceRect, GraphicsUnit.Pixel);
        }

        #endregion

        #region Draw Labels

        renderer.DrawLabels(e.Graphics, waveformRect);

        #endregion

        #region Update Play Button

        controls.UpdatePlayButton(audioPlayer.IsPlaying);

        #endregion

        #region Draw Border

        e.Graphics.DrawRectangle(Pens.LightGray, waveformRect);

        #endregion
    }

    #endregion

    #region Private Methods

    private void UpdateScrollBar()
    {
        int totalWidth = renderer.GetTotalWaveformWidth();
        int visibleWidth = Width - 50;

        logger.LogInfo($"Update scroll bar - totalWidth: {totalWidth}, visibleWidth: {visibleWidth}");

        controls.UpdateScrollBar(totalWidth, visibleWidth);
    }

    #endregion
}
