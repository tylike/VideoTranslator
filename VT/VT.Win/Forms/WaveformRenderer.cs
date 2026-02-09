using System;
using System.Drawing;
using VT.Module.BusinessObjects;

namespace VT.Win.Forms;
[Obsolete("应使用WaveformRenderV2",true)]
public class WaveformRenderer
{
    #region Fields

    private readonly WaveformData data;
    private readonly WaveformLogger logger;
    private Bitmap? waveformBitmap;
    private double zoomLevel = 1.0;

    #endregion

    #region Properties

    public Bitmap? WaveformBitmap => waveformBitmap;

    #endregion

    #region Constructor

    public WaveformRenderer(WaveformData data, WaveformLogger logger)
    {
        this.data = data;
        this.logger = logger;
    }

    #endregion

    #region Public Methods

    public void SetZoomLevel(double zoomLevel)
    {
        this.zoomLevel = zoomLevel;
    }

    public void CreateWaveformBitmap(int height)
    {
        if (data.Samples.Length == 0) return;

        int totalWidth = GetTotalWaveformWidth();
        logger.LogInfo($"Start creating waveform bitmap, totalWidth: {totalWidth}, height: {height}, zoomLevel: {zoomLevel:F2}");

        waveformBitmap = new Bitmap(totalWidth, height);
        using var g = Graphics.FromImage(waveformBitmap);
        g.Clear(Color.White);

        #region Draw Waveform

        DrawWaveform(g, totalWidth, height);

        #endregion

        #region Draw Vad Segments

        DrawVadSegmentsOnBitmap(g, totalWidth, height);

        #endregion

        #region Draw Clips

        DrawClipsOnBitmap(g, totalWidth, height);

        #endregion

        #region Draw Time Markers

        DrawTimeMarkers(g, totalWidth, height);

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

        #region Draw VAD Label

        using var vadBrush = new SolidBrush(Color.DarkGreen);
        g.DrawString("语音活动", SystemFonts.DefaultFont, vadBrush, 5, vadRowY + 5);

        #endregion

        #region Draw SRT Label

        using var srtBrush = new SolidBrush(Color.DarkBlue);
        g.DrawString("源字幕", SystemFonts.DefaultFont, srtBrush, 5, srtRowY + 5);

        #endregion

        #region Draw Source Audio Label

        using var sourceAudioBrush = new SolidBrush(Color.DarkOrange);
        g.DrawString("源音频", SystemFonts.DefaultFont, sourceAudioBrush, 5, sourceAudioRowY + 5);

        #endregion

        #region Draw Target Audio Label

        using var targetAudioBrush = new SolidBrush(Color.DarkRed);
        g.DrawString("目标音频", SystemFonts.DefaultFont, targetAudioBrush, 5, targetAudioRowY + 5);

        #endregion

        #region Draw Adjusted Audio Label

        using var adjustedAudioBrush = new SolidBrush(Color.DarkGoldenrod);
        g.DrawString("调整音频", SystemFonts.DefaultFont, adjustedAudioBrush, 5, adjustedAudioRowY + 5);

        #endregion

        #region Draw Audio Label

        using var audioBrush = new SolidBrush(Color.Gray);
        g.DrawString("完整音频", SystemFonts.DefaultFont, audioBrush, 5, audioRowY + 5);

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

    private void DrawWaveform(Graphics g, int totalWidth, int height)
    {
        int baseWidth = Math.Max(100, data.Samples.Length / 1000);
        int samplesPerPixel = Math.Max(1, (int)(data.Samples.Length / (baseWidth * zoomLevel)));
        int rowHeight = height / 6;
        int audioRowY = rowHeight * 5;
        int centerY = audioRowY + rowHeight / 2;

        logger.LogInfo($"Waveform draw parameters - baseWidth: {baseWidth}, samplesPerPixel: {samplesPerPixel}, centerY: {centerY}, totalWidth: {totalWidth}");

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
            int barHeight = (int)(maxAmplitude * rowHeight * 0.9f);

            g.DrawLine(Pens.Gray, x, centerY - barHeight, x, centerY);
        }
    }

    private void DrawVadSegmentsOnBitmap(Graphics g, int totalWidth, int height)
    {
        if (data.VadSegments == null || data.VadSegments.Length == 0)
        {
            logger.LogInfo($"VAD segments are empty, skip drawing");
            return;
        }

        double totalDurationMS = data.Duration.TotalMilliseconds;

        if (totalDurationMS <= 0)
        {
            logger.LogInfo($"Total duration is 0, skip VAD drawing");
            return;
        }

        logger.LogInfo($"Start drawing VAD segments, total segments: {data.VadSegments.Length}, total duration: {totalDurationMS:F2}ms, total width: {totalWidth}");

        int rowHeight = height / 6;
        int vadRowHeight = rowHeight;

        foreach (var segment in data.VadSegments)
        {
            int startX = (int)((segment.StartMS / totalDurationMS) * totalWidth);
            int endX = (int)((segment.EndMS / totalDurationMS) * totalWidth);

            logger.LogInfo($"VAD segment[{segment.Index}] - StartMS: {segment.StartMS:F2}ms, EndMS: {segment.EndMS:F2}ms, startX: {startX}, endX: {endX}");

            if (startX >= endX)
            {
                logger.LogInfo($"VAD segment[{segment.Index}] skip, startX >= endX");
                continue;
            }

            var segmentRect = new Rectangle(startX, 0, endX - startX, vadRowHeight);

            using var brush = new SolidBrush(Color.FromArgb(50, Color.Green));
            g.FillRectangle(brush, segmentRect);

            g.DrawLine(Pens.Green, startX, 0, startX, vadRowHeight);
            g.DrawLine(Pens.Green, endX, 0, endX, vadRowHeight);

            if (endX - startX > 30)
            {
                var segmentText = segment.Index.ToString();
                var textSize = g.MeasureString(segmentText, SystemFonts.DefaultFont);
                var textX = startX + (endX - startX - (int)textSize.Width) / 2;
                var textY = 5;

                using var textBrush = new SolidBrush(Color.DarkGreen);
                g.DrawString(segmentText, SystemFonts.DefaultFont, textBrush, textX, textY);
            }
        }

        logger.LogInfo($"VAD segments drawing completed");
    }

    private void DrawClipsOnBitmap(Graphics g, int totalWidth, int height)
    {
        if (data.Clips == null || data.Clips.Length == 0)
        {
            logger.LogInfo($"Clips are empty, skip drawing");
            return;
        }

        double totalDurationMS = data.Duration.TotalMilliseconds;

        if (totalDurationMS <= 0)
        {
            logger.LogInfo($"Total duration is 0, skip Clips drawing");
            return;
        }

        logger.LogInfo($"Start drawing Clips, total segments: {data.Clips.Length}, total duration: {totalDurationMS:F2}ms, total width: {totalWidth}");

        int rowHeight = height / 6;
        int vadRowY = 0;
        int srtRowY = rowHeight;
        int sourceAudioRowY = rowHeight * 2;
        int targetAudioRowY = rowHeight * 3;
        int adjustedAudioRowY = rowHeight * 4;
        int audioRowY = rowHeight * 5;

        #region Draw Row Dividers

        for (int i = 1; i < 6; i++)
        {
            int y = i * rowHeight;
            g.DrawLine(Pens.LightGray, 0, y, totalWidth, y);
        }

        #endregion

        foreach (var clip in data.Clips)
        {
            double startMS = clip.SourceSRTClip?.Start.TotalMilliseconds ?? 0;
            double endMS = clip.SourceSRTClip?.End.TotalMilliseconds ?? 0;

            if (startMS >= endMS)
            {
                logger.LogInfo($"Clip[{clip.Index}] skip, startMS >= endMS");
                continue;
            }

            int startX = (int)((startMS / totalDurationMS) * totalWidth);
            int endX = (int)((endMS / totalDurationMS) * totalWidth);

            logger.LogInfo($"Clip[{clip.Index}] - StartMS: {startMS:F2}ms, EndMS: {endMS:F2}ms, startX: {startX}, endX: {endX}");

            var clipWidth = endX - startX;

            #region Draw SRT Clip

            if (clip.SourceSRTClip != null && !string.IsNullOrEmpty(clip.SourceSRTClip.Text))
            {
                var srtRect = new Rectangle(startX, srtRowY, clipWidth, rowHeight - 2);
                using var srtBrush = new SolidBrush(Color.FromArgb(50, Color.Blue));
                g.FillRectangle(srtBrush, srtRect);
                g.DrawLine(Pens.Blue, startX, srtRowY, startX, srtRowY + rowHeight);
                g.DrawLine(Pens.Blue, endX, srtRowY, endX, srtRowY + rowHeight);

                if (clipWidth > 30)
                {
                    var clipText = clip.Index.ToString();
                    var textSize = g.MeasureString(clipText, SystemFonts.DefaultFont);
                    var textX = startX + (clipWidth - (int)textSize.Width) / 2;
                    var textY = srtRowY + 5;

                    using var textBrush = new SolidBrush(Color.DarkBlue);
                    g.DrawString(clipText, SystemFonts.DefaultFont, textBrush, textX, textY);
                }
            }

            #endregion

            #region Draw Source Audio Clip Waveform

            if (clip.SourceAudioClip != null && !string.IsNullOrEmpty(clip.SourceAudioClip.FilePath))
            {
                DrawAudioClipWaveform(g, clip.SourceAudioClip.FilePath, startX, sourceAudioRowY, clipWidth, rowHeight, Color.Orange, clip.Index);
            }

            #endregion

            #region Draw Target Audio Clip Waveform

            if (clip.TargetAudioClip != null && !string.IsNullOrEmpty(clip.TargetAudioClip.FilePath))
            {
                DrawAudioClipWaveform(g, clip.TargetAudioClip.FilePath, startX, targetAudioRowY, clipWidth, rowHeight, Color.Red, clip.Index);
            }

            #endregion

            #region Draw Adjusted Audio Clip Waveform

            if (clip.AdjustedTargetAudioClip != null && !string.IsNullOrEmpty(clip.AdjustedTargetAudioClip.FilePath))
            {
                DrawAudioClipWaveform(g, clip.AdjustedTargetAudioClip.FilePath, startX, adjustedAudioRowY, clipWidth, rowHeight, Color.Purple, clip.Index, clip.SpeedMultiplier);
            }

            #endregion
        }

        logger.LogInfo($"Clips drawing completed");
    }

    private void DrawTimeMarkers(Graphics g, int totalWidth, int height)
    {
        if (data.Duration.TotalMilliseconds <= 0)
        {
            return;
        }

        double totalDurationSeconds = data.Duration.TotalSeconds;
        int markerInterval = TimeFormatter.CalculateMarkerInterval(totalDurationSeconds);

        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8);
        using var brush = new SolidBrush(Color.Gray);

        int minTextWidth = 50;
        int minMarkerIntervalPixels = minTextWidth + 10;

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

        for (int seconds = 0; seconds <= totalDurationSeconds; seconds += markerInterval)
        {
            int x = (int)((seconds / totalDurationSeconds) * totalWidth);

            g.DrawLine(Pens.LightGray, x, 0, x, height);

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

    private void DrawAudioClipWaveform(Graphics g, string filePath, int startX, int startY, int width, int height, Color color, int clipIndex, double speedMultiplier = 1.0)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            logger.LogInfo($"Audio clip file not found: {filePath}");
            return;
        }

        try
        {
            using var audioFile = new NAudio.Wave.AudioFileReader(filePath);
            var samples = new float[audioFile.WaveFormat.SampleRate * 10];
            int samplesRead = audioFile.Read(samples, 0, samples.Length);

            if (samplesRead == 0)
            {
                logger.LogInfo($"Audio clip has no samples: {filePath}");
                return;
            }

            int centerY = startY + height / 2;
            int samplesPerPixel = Math.Max(1, samplesRead / width);

            using var pen = new Pen(color);

            for (int x = 0; x < width; x++)
            {
                int startIndex = x * samplesPerPixel;
                int endIndex = Math.Min(startIndex + samplesPerPixel, samplesRead);

                if (startIndex >= samplesRead) break;

                float maxSample = 0;
                float minSample = 0;

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (samples[i] > maxSample) maxSample = samples[i];
                    if (samples[i] < minSample) minSample = samples[i];
                }

                float maxAmplitude = Math.Max(Math.Abs(maxSample), Math.Abs(minSample));
                int barHeight = (int)(maxAmplitude * height * 0.9f);

                g.DrawLine(pen, startX + x, centerY - barHeight, startX + x, centerY);
            }

            #region Draw Start and End Lines

            g.DrawLine(pen, startX, startY, startX, startY + height);
            g.DrawLine(pen, startX + width - 1, startY, startX + width - 1, startY + height);

            #endregion

            if (width > 30)
            {
                var clipText = clipIndex.ToString();
                var textSize = g.MeasureString(clipText, SystemFonts.DefaultFont);
                var textX = startX + (width - (int)textSize.Width) / 2;
                var textY = startY + 5;

                using var textBrush = new SolidBrush(color);
                g.DrawString(clipText, SystemFonts.DefaultFont, textBrush, textX, textY);

                #region Draw Speed Multiplier

                if (Math.Abs(speedMultiplier - 1.0) > 0.001)
                {
                    var speedText = $"x{speedMultiplier:F2}";
                    var speedSize = g.MeasureString(speedText, SystemFonts.DefaultFont);
                    var speedX = startX + (width - (int)speedSize.Width) / 2;
                    var speedY = startY + height - speedSize.Height - 2;

                    g.DrawString(speedText, SystemFonts.DefaultFont, textBrush, speedX, speedY);
                }

                #endregion
            }

            logger.LogInfo($"Audio clip waveform drawn: {filePath}, samples: {samplesRead}");
        }
        catch (Exception ex)
        {
            logger.LogInfo($"Failed to draw audio clip waveform: {filePath}, error: {ex.Message}");
        }
    }

    #endregion
}
