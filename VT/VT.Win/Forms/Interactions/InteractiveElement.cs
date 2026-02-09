using System;
using System.Drawing;
using VT.Win.Forms.Elements;

namespace VT.Win.Forms.Interactions;

public interface IInteractiveElement : IWaveformElement
{
    event EventHandler<ElementMouseEventArgs>? MouseEnter;
    event EventHandler<ElementMouseEventArgs>? MouseLeave;
    event EventHandler<ElementMouseEventArgs>? MouseDown;
    event EventHandler<ElementMouseEventArgs>? MouseUp;
    event EventHandler<ElementClickEventArgs>? Click;
    event EventHandler<ElementDoubleClickEventArgs>? DoubleClick;
    
    bool IsHovered { get; }
    bool IsPressed { get; }
    Cursor GetCursor();
    
    void OnMouseEnter(Point location, MouseButtons button);
    void OnMouseLeave(Point location, MouseButtons button);
    void OnMouseDown(Point location, MouseButtons button, int clicks, int delta);
    void OnMouseUp(Point location, MouseButtons button, int clicks, int delta);
}

public class ElementMouseEventArgs : EventArgs
{
    #region Properties

    public IWaveformElement Element { get; }
    public Point Location { get; }
    public MouseButtons Button { get; }
    public int Clicks { get; }
    public int Delta { get; }

    #endregion

    #region Constructor

    public ElementMouseEventArgs(IWaveformElement element, Point location, MouseButtons button, int clicks, int delta)
    {
        Element = element;
        Location = location;
        Button = button;
        Clicks = clicks;
        Delta = delta;
    }

    #endregion
}

public class ElementClickEventArgs : ElementMouseEventArgs
{
    #region Constructor

    public ElementClickEventArgs(IWaveformElement element, Point location, MouseButtons button)
        : base(element, location, button, 1, 0)
    {
    }

    #endregion
}

public class ElementDoubleClickEventArgs : ElementMouseEventArgs
{
    #region Constructor

    public ElementDoubleClickEventArgs(IWaveformElement element, Point location, MouseButtons button)
        : base(element, location, button, 2, 0)
    {
    }

    #endregion
}

public abstract class InteractiveElement : WaveformElement, IInteractiveElement
{
    #region Fields

    private bool isHovered;
    private bool isPressed;
    private Cursor cursor = Cursors.Default;

    #endregion

    #region Events

    public event EventHandler<ElementMouseEventArgs>? MouseEnter;
    public event EventHandler<ElementMouseEventArgs>? MouseLeave;
    public event EventHandler<ElementMouseEventArgs>? MouseDown;
    public event EventHandler<ElementMouseEventArgs>? MouseUp;
    public event EventHandler<ElementClickEventArgs>? Click;
    public event EventHandler<ElementDoubleClickEventArgs>? DoubleClick;

    #endregion

    #region Properties

    public bool IsHovered
    {
        get => isHovered;
        protected set
        {
            if (isHovered != value)
            {
                isHovered = value;
                OnHoverChanged();
            }
        }
    }

    public bool IsPressed
    {
        get => isPressed;
        protected set
        {
            if (isPressed != value)
            {
                isPressed = value;
                OnPressedChanged();
            }
        }
    }

    public Cursor Cursor
    {
        get => cursor;
        protected set => cursor = value;
    }

    #endregion

    #region Constructor

    protected InteractiveElement(string id) : base(id)
    {
    }

    #endregion

    #region Public Methods

    public Cursor GetCursor()
    {
        return cursor;
    }

    public virtual void OnMouseEnter(Point location, MouseButtons button)
    {
        IsHovered = true;
        MouseEnter?.Invoke(this, new ElementMouseEventArgs(this, location, button, 0, 0));
    }

    public virtual void OnMouseLeave(Point location, MouseButtons button)
    {
        IsHovered = false;
        MouseLeave?.Invoke(this, new ElementMouseEventArgs(this, location, button, 0, 0));
    }

    public virtual void OnMouseDown(Point location, MouseButtons button, int clicks, int delta)
    {
        IsPressed = true;
        MouseDown?.Invoke(this, new ElementMouseEventArgs(this, location, button, clicks, delta));
    }

    public virtual void OnMouseUp(Point location, MouseButtons button, int clicks, int delta)
    {
        IsPressed = false;
        MouseUp?.Invoke(this, new ElementMouseEventArgs(this, location, button, clicks, delta));

        if (clicks == 1)
        {
            Click?.Invoke(this, new ElementClickEventArgs(this, location, button));
        }
        else if (clicks == 2)
        {
            DoubleClick?.Invoke(this, new ElementDoubleClickEventArgs(this, location, button));
        }
    }

    #endregion

    #region Protected Methods

    protected virtual void OnHoverChanged()
    {
    }

    protected virtual void OnPressedChanged()
    {
    }

    #endregion
}
