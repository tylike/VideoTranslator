using System;
using System.Drawing;

namespace VT.Win.Forms.Elements;

public interface IWaveformElement
{
    string Id { get; }
    Rectangle Bounds { get; set; }
    bool IsVisible { get; set; }
    bool IsEnabled { get; set; }
    object? Tag { get; set; }
    
    void Render(Graphics g, WaveformRenderContext context);
    bool HitTest(Point point);
}

public class WaveformRenderContext
{
    public double ZoomLevel { get; set; }
    public double TotalDurationMS { get; set; }
    public int TotalWidth { get; set; }
    public int TotalHeight { get; set; }
    public WaveformLogger? Logger { get; set; }
}

public abstract class WaveformElement : IWaveformElement
{
    #region Fields

    private string id;
    private Rectangle bounds;
    private bool isVisible = true;
    private bool isEnabled = true;
    private object? tag;

    #endregion

    #region Properties

    public string Id => id;
    public Rectangle Bounds
    {
        get => bounds;
        set => bounds = value;
    }
    public bool IsVisible
    {
        get => isVisible;
        set => isVisible = value;
    }
    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }
    public object? Tag
    {
        get => tag;
        set => tag = value;
    }

    #endregion

    #region Constructor

    protected WaveformElement(string id)
    {
        this.id = id ?? throw new ArgumentNullException(nameof(id));
    }

    #endregion

    #region Public Methods

    public virtual void Render(Graphics g, WaveformRenderContext context)
    {
        if (!IsVisible || !IsEnabled) return;
        OnRender(g, context);
    }

    public virtual bool HitTest(Point point)
    {
        if (!IsVisible || !IsEnabled) return false;
        return Bounds.Contains(point);
    }

    #endregion

    #region Protected Methods

    protected abstract void OnRender(Graphics g, WaveformRenderContext context);

    #endregion
}
