using System;
using System.Drawing;
using VT.Win.Forms.Styles;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms.Elements;

public class VadSegmentElement : InteractiveElement
{
    #region Fields

    private readonly VadSegmentInfo segment;
    private readonly VadSegmentStyle style;
    private readonly int rowSpacing;

    #endregion

    #region Properties

    public VadSegmentInfo Segment => segment;
    public VadSegmentStyle Style => style;

    #endregion

    #region Constructor

    public VadSegmentElement(VadSegmentInfo segment, VadSegmentStyle? style = null, int rowSpacing = 6) 
        : base($"vad_{segment.Index}")
    {
        this.segment = segment ?? throw new ArgumentNullException(nameof(segment));
        this.style = style ?? new VadSegmentStyle();
        this.rowSpacing = rowSpacing;
        this.Cursor = Cursors.Hand;
    }

    #endregion

    #region Public Methods

    public void UpdateBounds(double totalDurationMS, int totalWidth, int height, int rowSpacing)
    {
        int startX = (int)((segment.StartMS / totalDurationMS) * totalWidth);
        int endX = (int)((segment.EndMS / totalDurationMS) * totalWidth);
        int elementHeight = height - rowSpacing * 2;
        Bounds = new Rectangle(startX, rowSpacing, endX - startX, elementHeight);
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
            var text = segment.Index.ToString();
            var font = style.Font ?? SystemFonts.DefaultFont;
            var textSize = g.MeasureString(text, font);
            var textX = Bounds.X + (Bounds.Width - (int)textSize.Width) / 2;
            var textY = style.TextPadding;

            using var textBrush = new SolidBrush(textColor);
            g.DrawString(text, font, textBrush, textX, textY);
        }

        #endregion
    }

    protected override void OnHoverChanged()
    {
        Cursor = IsHovered ? Cursors.Hand : Cursors.Default;
    }

    #endregion
}
