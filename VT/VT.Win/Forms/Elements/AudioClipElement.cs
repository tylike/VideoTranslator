using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Styles;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms.Elements;

public class AudioClipElement : InteractiveElement
{
    #region Fields

    private readonly AudioClip audioClip;
    private readonly AudioClipStyle style;
    private readonly double? speedMultiplier;
    private readonly int elementSpacing;

    #endregion

    #region Properties

    public AudioClip AudioClip => audioClip;
    public AudioClipStyle Style => style;
    public double? SpeedMultiplier => speedMultiplier;

    #endregion

    #region Constructor

    public AudioClipElement(AudioClip audioClip, AudioClipStyle? style = null, double? speedMultiplier = null, int elementSpacing = 1)
        : base($"audio_{audioClip.Index}")
    {
        this.audioClip = audioClip ?? throw new ArgumentNullException(nameof(audioClip));
        this.style = style ?? new AudioClipStyle();
        this.speedMultiplier = speedMultiplier;
        this.elementSpacing = elementSpacing;
        this.Cursor = Cursors.Hand;
    }

    #endregion

    #region Public Methods

    public void UpdateBounds(double totalDurationMS, int totalWidth, int rowY, int rowHeight, int elementSpacing)
    {
        double startMS = audioClip.Start.TotalMilliseconds;
        double endMS = audioClip.End.TotalMilliseconds;

        if (startMS >= endMS) return;

        int startX = (int)((startMS / totalDurationMS) * totalWidth);
        int endX = (int)((endMS / totalDurationMS) * totalWidth);
        int elementWidth = Math.Max(1, endX - startX - elementSpacing * 2);

        Bounds = new Rectangle(startX + elementSpacing, rowY, elementWidth, rowHeight);
    }

    #endregion

    #region Protected Methods

    protected override void OnRender(Graphics g, WaveformRenderContext context)
    {
        if (Bounds.Width <= 0) return;

        #region Draw Waveform

        if (!string.IsNullOrEmpty(audioClip.FilePath) && System.IO.File.Exists(audioClip.FilePath))
        {
            DrawWaveform(g);
        }

        #endregion

        #region Draw Border

        if (style.ShowBorder)
        {
            DrawBorder(g);
        }

        #endregion

        #region Draw Hover Background

        if (IsHovered && style.EnableHoverEffect)
        {
            using var hoverBrush = new SolidBrush(style.HoverBackgroundColor);
            g.FillRectangle(hoverBrush, Bounds);
        }

        #endregion

        #region Draw Text

        if (Bounds.Width > style.MinWidthForText)
        {
            DrawText(g);
        }

        #endregion
    }

    private void DrawWaveform(Graphics g)
    {
        try
        {
            using var audioFile = new NAudio.Wave.AudioFileReader(audioClip.FilePath);
            var samples = new float[audioFile.WaveFormat.SampleRate * 10];
            int samplesRead = audioFile.Read(samples, 0, samples.Length);

            if (samplesRead == 0) return;

            int centerY = Bounds.Y + Bounds.Height / 2;
            int samplesPerPixel = Math.Max(1, samplesRead / Bounds.Width);

            using var pen = new Pen(style.Color, style.LineWidth);

            for (int x = 0; x < Bounds.Width; x++)
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
                int barHeight = (int)(maxAmplitude * Bounds.Height * 0.9f);

                g.DrawLine(pen, Bounds.X + x, centerY - barHeight, Bounds.X + x, centerY);
            }
        }
        catch (Exception)
        {
        }
    }

    private void DrawBorder(Graphics g)
    {
        #region Determine Border Style

        var borderColor = style.BorderColor;
        var borderWidth = style.BorderWidth;

        if (IsHovered && style.EnableHoverEffect)
        {
            borderColor = style.HoverBorderColor;
            borderWidth = style.HoverBorderWidth;
        }

        #endregion

        using var pen = new Pen(borderColor, borderWidth);

        #region Set Dash Style

        switch (style.BorderStyle)
        {
            case ElementBorderStyle.Dashed:
                pen.DashStyle = DashStyle.Dash;
                break;
            case ElementBorderStyle.Dotted:
                pen.DashStyle = DashStyle.Dot;
                break;
            case ElementBorderStyle.Solid:
            default:
                pen.DashStyle = DashStyle.Solid;
                break;
        }

        #endregion

        g.DrawRectangle(pen, Bounds);
    }

    private void DrawText(Graphics g)
    {
        var clipText = audioClip.Index.ToString();
        var font = style.Font ?? SystemFonts.DefaultFont;
        var textSize = g.MeasureString(clipText, font);
        var textX = Bounds.X + (Bounds.Width - (int)textSize.Width) / 2;
        var textY = Bounds.Y + style.TextPadding;

        using var textBrush = new SolidBrush(style.Color);
        g.DrawString(clipText, font, textBrush, textX, textY);

        #region Draw Speed Multiplier

        if (style.ShowSpeedMultiplier && speedMultiplier.HasValue && Math.Abs(speedMultiplier.Value - 1.0) > 0.001)
        {
            var speedText = $"x{speedMultiplier.Value:F2}";
            var speedSize = g.MeasureString(speedText, font);
            var speedX = Bounds.X + (Bounds.Width - (int)speedSize.Width) / 2;
            var speedY = Bounds.Y + Bounds.Height - speedSize.Height - 2;

            g.DrawString(speedText, font, textBrush, speedX, speedY);
        }

        #endregion
    }

    protected override void OnHoverChanged()
    {
        Cursor = IsHovered ? Cursors.Hand : Cursors.Default;
    }

    #endregion
}
