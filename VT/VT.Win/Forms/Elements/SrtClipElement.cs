using System;
using System.Drawing;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Styles;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms.Elements;

public class SrtClipElement : InteractiveElement
{
    #region Fields

    private readonly SRTClip srtClip;
    private readonly SrtClipStyle style;
    private readonly int elementSpacing;

    #endregion

    #region Properties

    public SRTClip SrtClip => srtClip;
    public SrtClipStyle Style => style;

    #endregion

    #region Constructor

    public SrtClipElement(SRTClip srtClip, SrtClipStyle? style = null, int elementSpacing = 1)
        : base($"srt_{srtClip.Index}")
    {
        this.srtClip = srtClip ?? throw new ArgumentNullException(nameof(srtClip));
        this.style = style ?? new SrtClipStyle();
        this.elementSpacing = elementSpacing;
        this.Cursor = Cursors.Hand;
    }

    #endregion

    #region Public Methods

    public void UpdateBounds(double totalDurationMS, int totalWidth, int rowY, int rowHeight, int elementSpacing)
    {
        double startMS = srtClip.Start.TotalMilliseconds;
        double endMS = srtClip.End.TotalMilliseconds;

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

        #region Determine Colors

        var fillColor = style.FillColor;
        var borderColor = style.BorderColor;
        var textColor = style.TextColor;

        if (IsHovered && style.EnableHoverEffect)
        {
            fillColor = style.HoverFillColor;
            borderColor = style.HoverBorderColor;
            textColor = style.HoverTextColor;
        }

        #endregion

        #region Draw Fill

        using var fillBrush = new SolidBrush(fillColor);
        g.FillRectangle(fillBrush, Bounds);

        #endregion

        #region Draw Borders

        using var borderPen = new Pen(borderColor);
        g.DrawLine(borderPen, Bounds.Left, Bounds.Top, Bounds.Left, Bounds.Bottom);
        g.DrawLine(borderPen, Bounds.Right, Bounds.Top, Bounds.Right, Bounds.Bottom);

        #endregion

        #region Draw Text

        if (Bounds.Width > style.MinWidthForText)
        {
            var clipText = srtClip.Index.ToString();
            var font = style.Font ?? SystemFonts.DefaultFont;
            var textSize = g.MeasureString(clipText, font);
            var textX = Bounds.X + (Bounds.Width - (int)textSize.Width) / 2;
            var textY = style.TextPadding;

            using var textBrush = new SolidBrush(textColor);
            g.DrawString(clipText, font, textBrush, textX, textY);
        }

        #endregion
    }

    protected override void OnHoverChanged()
    {
        Cursor = IsHovered ? Cursors.Hand : Cursors.Default;
    }

    #endregion
}
