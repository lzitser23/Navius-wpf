using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native <see cref="ScrollViewer"/>, inheriting native keyboard
/// scrolling (arrow/page/home/end), mouse-wheel handling, and ScrollViewerAutomationPeer's UIA
/// ScrollPattern support for free (part-naming alone, no OnApplyTemplate override needed: the
/// base class discovers PART_ScrollContentPresenter/PART_VerticalScrollBar/PART_HorizontalScrollBar
/// by name in whatever template is applied).
///
/// Re-templated (see Themes/ScrollArea.xaml) with overlay scrollbars: thin, floating over the
/// content edge rather than reserving layout space, per the one-ink brand's overlay-scrollbar
/// rule. <see cref="IsHovering"/>/<see cref="IsScrolling"/> mirror the web contract's
/// data-hovering/data-scrolling state so the template can fade the bars in on hover/scroll and
/// back out after <see cref="ScrollHideDelay"/> milliseconds of inactivity.
/// </summary>
[TemplatePart(Name = PartVerticalScrollBar, Type = typeof(ScrollBar))]
[TemplatePart(Name = PartHorizontalScrollBar, Type = typeof(ScrollBar))]
[TemplatePart(Name = PartCorner, Type = typeof(FrameworkElement))]
public class NaviusScrollArea : ScrollViewer
{
    private const string PartVerticalScrollBar = "PART_VerticalScrollBar";
    private const string PartHorizontalScrollBar = "PART_HorizontalScrollBar";
    private const string PartCorner = "PART_Corner";

    private static readonly DependencyPropertyKey IsHoveringPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsHovering), typeof(bool), typeof(NaviusScrollArea),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsHoveringProperty = IsHoveringPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsScrollingPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsScrolling), typeof(bool), typeof(NaviusScrollArea),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsScrollingProperty = IsScrollingPropertyKey.DependencyProperty;

    /// <summary>Milliseconds of scroll inactivity before <see cref="IsScrolling"/> resets to false. Mirrors the web contract's ScrollHideDelay (default 600).</summary>
    public static readonly DependencyProperty ScrollHideDelayProperty = DependencyProperty.Register(
        nameof(ScrollHideDelay), typeof(int), typeof(NaviusScrollArea),
        new PropertyMetadata(600));

    private readonly DispatcherTimer _scrollHideTimer;

    static NaviusScrollArea()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusScrollArea),
            new FrameworkPropertyMetadata(typeof(NaviusScrollArea)));
    }

    public NaviusScrollArea()
    {
        _scrollHideTimer = new DispatcherTimer();
        _scrollHideTimer.Tick += OnScrollHideTimerTick;
    }

    /// <summary>True while the pointer is over the scroll area. Mirrors the web root's data-hovering (real-time, no delay).</summary>
    public bool IsHovering => (bool)GetValue(IsHoveringProperty);

    /// <summary>True while a scroll is in progress or within <see cref="ScrollHideDelay"/> ms of the last one.</summary>
    public bool IsScrolling => (bool)GetValue(IsScrollingProperty);

    public int ScrollHideDelay
    {
        get => (int)GetValue(ScrollHideDelayProperty);
        set => SetValue(ScrollHideDelayProperty, value);
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        SetValue(IsHoveringPropertyKey, true);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        SetValue(IsHoveringPropertyKey, false);
    }

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);
        HandleScrollActivity(e.VerticalChange, e.HorizontalChange);
    }

    /// <summary>
    /// Split out of <see cref="OnScrollChanged"/> so tests can drive it with plain doubles
    /// instead of constructing a <see cref="ScrollChangedEventArgs"/> (its constructor is
    /// internal to PresentationFramework).
    /// </summary>
    private void HandleScrollActivity(double verticalChange, double horizontalChange)
    {
        if (verticalChange == 0 && horizontalChange == 0)
        {
            // Extent/viewport-only recalculation (e.g. content resize), not an actual scroll.
            return;
        }

        SetValue(IsScrollingPropertyKey, true);
        _scrollHideTimer.Stop();
        _scrollHideTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(0, ScrollHideDelay));
        _scrollHideTimer.Start();
    }

    private void OnScrollHideTimerTick(object? sender, EventArgs e)
    {
        _scrollHideTimer.Stop();
        SetValue(IsScrollingPropertyKey, false);
    }
}
