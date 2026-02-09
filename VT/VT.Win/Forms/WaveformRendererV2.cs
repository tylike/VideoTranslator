using System;
using System.Collections.Generic;
using System.Drawing;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Elements;
using VT.Win.Forms.Styles;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms;

public class WaveformRendererV2
{
    #region Fields

    private readonly WaveformData data;
    private readonly WaveformLogger logger;
    private readonly WaveformInteractionManager interactionManager;
    private readonly WaveformStyle style;
    private Bitmap? waveformBitmap;
    private double zoomLevel = 1.0;

    #endregion

    #region Properties
    public Bitmap? WaveformBitmap => waveformBitmap;
    public WaveformStyle Style => style;
    public WaveformInteractionManager InteractionManager => interactionManager;
    public List<IWaveformElement> Elements { get; private set; }

    #endregion

    #region Events

    public event EventHandler? ElementsCreated;

    #endregion

    #region Constructor

    public WaveformRendererV2(WaveformData data, WaveformLogger logger, WaveformStyle? style = null)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.style = style ?? new WaveformStyle();
        this.interactionManager = new WaveformInteractionManager();
        this.Elements = new List<IWaveformElement>();
    }

    #endregion

    #region Public Methods

    public void SetZoomLevel(double zoomLevel)
    {
        this.zoomLevel = zoomLevel;
    }

    public void SetStyle(WaveformStyle style)
    {
        this.style.VadSegmentStyle = style.VadSegmentStyle;
        this.style.ClipStyle = style.ClipStyle;
        this.style.TimeMarkerStyle = style.TimeMarkerStyle;
        this.style.LabelStyle = style.LabelStyle;
        this.style.WaveformLineStyle = style.WaveformLineStyle;
    }

    public void CreateWaveformBitmap(int height)
    {
        if (data.Samples.Length == 0) return;

        int totalWidth = GetTotalWaveformWidth();
        logger.LogInfo($"Start creating waveform bitmap, totalWidth: {totalWidth}, height: {height}, zoomLevel: {zoomLevel:F2}");

        waveformBitmap = new Bitmap(totalWidth, height);
        using var g = Graphics.FromImage(waveformBitmap);
        g.Clear(Color.White);

        #region Create Elements

        CreateElements(totalWidth, height);

        #endregion

        #region Render Elements

        var context = new WaveformRenderContext
        {
            ZoomLevel = zoomLevel,
            TotalDurationMS = data.Duration.TotalMilliseconds,
            TotalWidth = totalWidth,
            TotalHeight = height,
            Logger = logger
        };

        foreach (var element in Elements)
        {
            element.Render(g, context);
        }

        #endregion

        #region Draw Waveform

        DrawWaveform(g, totalWidth, height);

        #endregion

        #region Draw Time Markers

        DrawTimeMarkers(g, totalWidth, height);

        #endregion

        #region Draw Labels

        DrawLabels(g, new Rectangle(0, 0, totalWidth, height));

        #endregion
    }

    public void DrawLabels(Graphics g, Rectangle waveformRect)
    {
        if (data.VadSegments == null || data.VadSegments.Length == 0) return;

        int rowHeight = waveformRect.Height / 6;
        int vadRowY = 0;
        int srtRowY = rowHeight;
        int sourceAudioRowY = rowHeight * 2;
        int targetAudioRowY = rowHeight * 3;
        int adjustedAudioRowY = rowHeight * 4;
        int audioRowY = rowHeight * 5;

        var labelStyle = style.LabelStyle;
        var font = labelStyle.Font ?? SystemFonts.DefaultFont;
        var padding = labelStyle.Padding;

        #region Draw VAD Label

        using var vadBrush = new SolidBrush(labelStyle.VadLabel.Color);
        g.DrawString(labelStyle.VadLabel.Text, font, vadBrush, padding, vadRowY + padding);

        #endregion

        #region Draw SRT Label

        using var srtBrush = new SolidBrush(labelStyle.SrtLabel.Color);
        g.DrawString(labelStyle.SrtLabel.Text, font, srtBrush, padding, srtRowY + padding);

        #endregion

        #region Draw Source Audio Label

        using var sourceAudioBrush = new SolidBrush(labelStyle.SourceAudioLabel.Color);
        g.DrawString(labelStyle.SourceAudioLabel.Text, font, sourceAudioBrush, padding, sourceAudioRowY + padding);

        #endregion

        #region Draw Target Audio Label

        using var targetAudioBrush = new SolidBrush(labelStyle.TargetAudioLabel.Color);
        g.DrawString(labelStyle.TargetAudioLabel.Text, font, targetAudioBrush, padding, targetAudioRowY + padding);

        #endregion

        #region Draw Adjusted Audio Label

        using var adjustedAudioBrush = new SolidBrush(labelStyle.AdjustedAudioLabel.Color);
        g.DrawString(labelStyle.AdjustedAudioLabel.Text, font, adjustedAudioBrush, padding, adjustedAudioRowY + padding);

        #endregion

        #region Draw Audio Label

        using var audioBrush = new SolidBrush(labelStyle.AudioLabel.Color);
        g.DrawString(labelStyle.AudioLabel.Text, font, audioBrush, padding, audioRowY + padding);

        #endregion
    }

    public int GetTotalWaveformWidth()
    {
        if (data.Samples.Length == 0) return 0;
        int baseWidth = Math.Max(100, data.Samples.Length / 1000);
        return (int)(baseWidth * zoomLevel);
    }

    #endregion

    #region Private Methods

    private void CreateElements(int totalWidth, int height)
    {
        Elements.Clear();
        interactionManager.ClearElements();

        double totalDurationMS = data.Duration.TotalMilliseconds;

        #region Create VAD Elements

        if (data.VadSegments != null && data.VadSegments.Length > 0)
        {
            int rowHeight = height / 6;

            foreach (var segment in data.VadSegments)
            {
                var element = new VadSegmentElement(segment, style.VadSegmentStyle, style.RowSpacing);
                element.UpdateBounds(totalDurationMS, totalWidth, rowHeight, style.RowSpacing);
                Elements.Add(element);
                interactionManager.AddElement(element);
            }
        }

        #endregion

        #region Create Clip Elements

        if (data.Clips != null && data.Clips.Length > 0)
        {
            int rowHeight = height / 6;

            foreach (var clip in data.Clips)
            {
                var container = new ClipContainerElement(clip, style.ClipStyle, style.RowSpacing, style.ElementSpacing);
                container.UpdateBounds(totalDurationMS, totalWidth, rowHeight, 1);
                Elements.Add(container);

                #region Add Child Elements to Interaction Manager

                foreach (var child in container.ChildElements)
                {
                    if (child is Interactions.IInteractiveElement interactive)
                    {
                        interactionManager.AddElement(interactive);
                    }
                }

                #endregion
            }
        }

        #endregion

        #region Trigger Elements Created Event

        ElementsCreated?.Invoke(this, EventArgs.Empty);

        #endregion
    }

    private void DrawWaveform(Graphics g, int totalWidth, int height)
    {
        int baseWidth = Math.Max(100, data.Samples.Length / 1000);
        int samplesPerPixel = Math.Max(1, (int)(data.Samples.Length / (baseWidth * zoomLevel)));
        int rowHeight = height / 6;
        int audioRowY = rowHeight * 5;
        int centerY = audioRowY + rowHeight / 2;

        logger.LogInfo($"Waveform draw parameters - baseWidth: {baseWidth}, samplesPerPixel: {samplesPerPixel}, centerY: {centerY}, totalWidth: {totalWidth}");

        var lineStyle = style.WaveformLineStyle;
        using var pen = new Pen(lineStyle.Color, lineStyle.Width);

        for (int x = 0; x < totalWidth; x++)
        {
            int startIndex = x * samplesPerPixel;
            int endIndex = Math.Min(startIndex + samplesPerPixel, data.Samples.Length);

            if (startIndex >= data.Samples.Length) break;

            float maxSample = 0;
            float minSample = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (data.Samples[i] > maxSample) maxSample = data.Samples[i];
                if (data.Samples[i] < minSample) minSample = data.Samples[i];
            }

            float maxAmplitude = Math.Max(Math.Abs(maxSample), Math.Abs(minSample));
            int barHeight = (int)(maxAmplitude * rowHeight * lineStyle.AmplitudeScale);

            g.DrawLine(pen, x, centerY - barHeight, x, centerY);
        }
    }

    private void DrawTimeMarkers(Graphics g, int totalWidth, int height)
    {
        if (data.Duration.TotalMilliseconds <= 0) return;

        double totalDurationSeconds = data.Duration.TotalSeconds;
        int markerInterval = TimeFormatter.CalculateMarkerInterval(totalDurationSeconds);

        var markerStyle = style.TimeMarkerStyle;
        var font = markerStyle.Font ?? new Font(SystemFonts.DefaultFont.FontFamily, 8);
        using var brush = new SolidBrush(markerStyle.TextColor);

        int minTextWidth = markerStyle.MinTextWidth;
        int minMarkerIntervalPixels = markerStyle.MinMarkerIntervalPixels;

        int baseInterval = markerInterval;
        while (true)
        {
            int pixelsPerSecond = totalWidth / (int)totalDurationSeconds;
            int pixelsPerMarker = baseInterval * pixelsPerSecond;

            if (pixelsPerMarker >= minMarkerIntervalPixels)
            {
                markerInterval = baseInterval;
                break;
            }

            baseInterval = TimeFormatter.GetNextInterval(baseInterval);
            if (baseInterval > 3600)
            {
                break;
            }
        }

        logger.LogInfo($"Start drawing time markers, total duration: {totalDurationSeconds:F2}s, marker interval: {markerInterval}s");

        using var linePen = new Pen(markerStyle.LineColor);

        for (int seconds = 0; seconds <= totalDurationSeconds; seconds += markerInterval)
        {
            int x = (int)((seconds / totalDurationSeconds) * totalWidth);

            g.DrawLine(linePen, x, 0, x, height);

            string timeText = TimeFormatter.FormatTime(seconds);
            var textSize = g.MeasureString(timeText, font);
            var textX = x + 2;
            var textY = height - textSize.Height - 2;

            if (textX + textSize.Width <= totalWidth)
            {
                g.DrawString(timeText, font, brush, textX, textY);
            }
        }

        logger.LogInfo($"Time markers drawing completed");
    }

    #endregion
}
