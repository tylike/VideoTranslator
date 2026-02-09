using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VT.Win.Forms.Interactions;

namespace VT.Win.Forms.Interactions;

public class WaveformInteractionManager
{
    #region Fields

    private readonly List<IInteractiveElement> elements;
    private IInteractiveElement? hoveredElement;
    private IInteractiveElement? pressedElement;
    private Point lastMousePosition;

    #endregion

    #region Properties

    public IReadOnlyList<IInteractiveElement> Elements => elements.AsReadOnly();
    public IInteractiveElement? HoveredElement => hoveredElement;
    public IInteractiveElement? PressedElement => pressedElement;

    #endregion

    #region Constructor

    public WaveformInteractionManager()
    {
        elements = new List<IInteractiveElement>();
    }

    #endregion

    #region Public Methods

    public void AddElement(IInteractiveElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (!elements.Contains(element))
        {
            elements.Add(element);
        }
    }

    public void RemoveElement(IInteractiveElement element)
    {
        elements.Remove(element);
    }

    public void ClearElements()
    {
        elements.Clear();
        hoveredElement = null;
        pressedElement = null;
    }

    public void HandleMouseMove(Point location, MouseButtons button)
    {
        lastMousePosition = location;

        var hitElement = elements.FirstOrDefault(e => e.HitTest(location));

        if (hitElement != hoveredElement)
        {
            if (hoveredElement != null)
            {
                hoveredElement.OnMouseLeave(location, button);
            }

            hoveredElement = hitElement;

            if (hoveredElement != null)
            {
                hoveredElement.OnMouseEnter(location, button);
            }
        }
    }

    public void HandleMouseDown(Point location, MouseButtons button, int clicks, int delta)
    {
        var hitElement = elements.FirstOrDefault(e => e.HitTest(location));

        if (hitElement != null)
        {
            pressedElement = hitElement;
            hitElement.OnMouseDown(location, button, clicks, delta);
        }
    }

    public void HandleMouseUp(Point location, MouseButtons button, int clicks, int delta)
    {
        if (pressedElement != null)
        {
            pressedElement.OnMouseUp(location, button, clicks, delta);
            pressedElement = null;
        }
    }

    public Cursor GetCursor()
    {
        return hoveredElement?.GetCursor() ?? Cursors.Default;
    }

    public IInteractiveElement? GetElementAt(Point location)
    {
        return elements.FirstOrDefault(e => e.HitTest(location));
    }

    #endregion
}
