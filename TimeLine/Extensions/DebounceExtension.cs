using System.Windows;
using System.Windows.Threading;

namespace TimeLine.Extensions;

public static class DebounceExtension
{
    private static readonly DependencyPropertyKey DebounceTimerPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "DebounceTimer",
            typeof(DispatcherTimer),
            typeof(DebounceExtension),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DebounceTimerProperty =
        DebounceTimerPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey DebounceActionPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "DebounceAction",
            typeof(Action),
            typeof(DebounceExtension),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DebounceActionProperty =
        DebounceActionPropertyKey.DependencyProperty;

    private static DispatcherTimer GetDebounceTimer(DependencyObject obj)
    {
        return (DispatcherTimer)obj.GetValue(DebounceTimerProperty);
    }

    private static void SetDebounceTimer(DependencyObject obj, DispatcherTimer value)
    {
        obj.SetValue(DebounceTimerPropertyKey, value);
    }

    private static Action? GetDebounceAction(DependencyObject obj)
    {
        return (Action?)obj.GetValue(DebounceActionProperty);
    }

    private static void SetDebounceAction(DependencyObject obj, Action? value)
    {
        obj.SetValue(DebounceActionPropertyKey, value);
    }

    public static void Debounce(this DependencyObject obj, Action action, int milliseconds = 50)
    {
        var timer = GetDebounceTimer(obj);

        if (timer == null)
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(milliseconds)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var currentAction = GetDebounceAction(obj);
                currentAction?.Invoke();
            };
            SetDebounceTimer(obj, timer);
        }

        SetDebounceAction(obj, action);

        timer.Stop();
        timer.Start();
    }
}