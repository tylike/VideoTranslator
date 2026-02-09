using System.Windows;
using System.Windows.Controls;

namespace TimeLine.Extensions;

public static class ScrollSyncExtension
{
    private static readonly DependencyPropertyKey SyncModePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "SyncMode",
            typeof(ScrollSyncMode),
            typeof(ScrollSyncExtension),
            new PropertyMetadata(ScrollSyncMode.None));

    public static readonly DependencyProperty SyncModeProperty =
        SyncModePropertyKey.DependencyProperty;

    private static ScrollSyncMode GetSyncMode(DependencyObject obj)
    {
        return (ScrollSyncMode)obj.GetValue(SyncModeProperty);
    }

    private static void SetSyncMode(DependencyObject obj, ScrollSyncMode value)
    {
        obj.SetValue(SyncModePropertyKey, value);
    }

    private static readonly DependencyPropertyKey PartnerScrollViewerPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "PartnerScrollViewer",
            typeof(ScrollViewer),
            typeof(ScrollSyncExtension),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PartnerScrollViewerProperty =
        PartnerScrollViewerPropertyKey.DependencyProperty;

    private static ScrollViewer? GetPartnerScrollViewer(DependencyObject obj)
    {
        return (ScrollViewer?)obj.GetValue(PartnerScrollViewerProperty);
    }

    private static void SetPartnerScrollViewer(DependencyObject obj, ScrollViewer? value)
    {
        obj.SetValue(PartnerScrollViewerPropertyKey, value);
    }

    public static void SetVerticalSync(ScrollViewer primary, ScrollViewer secondary)
    {
        SetSyncMode(primary, ScrollSyncMode.PrimaryVertical);
        SetSyncMode(secondary, ScrollSyncMode.SecondaryVertical);
        SetPartnerScrollViewer(primary, secondary);
        SetPartnerScrollViewer(secondary, primary);

        primary.ScrollChanged += OnPrimaryScrollChanged;
        secondary.ScrollChanged += OnSecondaryScrollChanged;
    }

    private static void OnPrimaryScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer primary && GetSyncMode(primary) == ScrollSyncMode.PrimaryVertical)
        {
            var secondary = GetPartnerScrollViewer(primary);
            if (secondary != null && e.VerticalChange != 0)
            {
                secondary.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }

    private static void OnSecondaryScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer secondary && GetSyncMode(secondary) == ScrollSyncMode.SecondaryVertical)
        {
            var primary = GetPartnerScrollViewer(secondary);
            if (primary != null && e.VerticalChange != 0)
            {
                primary.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }

    private enum ScrollSyncMode
    {
        None,
        PrimaryVertical,
        SecondaryVertical
    }
}