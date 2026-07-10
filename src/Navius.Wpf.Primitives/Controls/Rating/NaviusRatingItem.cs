using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>Mirrors the contract's data-state (full/half/empty) computed per-star by the owning NaviusRating.</summary>
public enum NaviusRatingFillState
{
    Empty,
    Half,
    Full,
}

/// <summary>
/// One visual star. A lookless Control (not a native RadioButton, unlike NaviusRadioGroupItem)
/// since it needs bespoke half-star fill state and, when the owning NaviusRating has AllowHalf
/// set, left/right half-zone pointer hit-testing that WPF's native button types have no
/// equivalent for (see docs/parity/rating.md "WPF strategy": "splitting each star's Grid column
/// in half"). Index/FillState/IsHighlighted are pushed by the owning NaviusRating via
/// SetCurrentValue, not set directly by the consumer.
/// </summary>
public class NaviusRatingItem : Control
{
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index), typeof(int), typeof(NaviusRatingItem), new PropertyMetadata(1));

    public static readonly DependencyProperty FillStateProperty = DependencyProperty.Register(
        nameof(FillState), typeof(NaviusRatingFillState), typeof(NaviusRatingItem),
        new PropertyMetadata(NaviusRatingFillState.Empty));

    public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
        nameof(IsHighlighted), typeof(bool), typeof(NaviusRatingItem), new PropertyMetadata(false));

    public static readonly RoutedEvent SelectRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectRequested), RoutingStrategy.Bubble,
        typeof(EventHandler<NaviusRatingSelectEventArgs>), typeof(NaviusRatingItem));

    public static readonly RoutedEvent HoverChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(HoverChanged), RoutingStrategy.Bubble,
        typeof(EventHandler<NaviusRatingSelectEventArgs>), typeof(NaviusRatingItem));

    static NaviusRatingItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusRatingItem), new FrameworkPropertyMetadata(typeof(NaviusRatingItem)));
        FocusableProperty.OverrideMetadata(typeof(NaviusRatingItem), new FrameworkPropertyMetadata(true));
    }

    /// <summary>1-based position, assigned by the owning NaviusRating's registration order.</summary>
    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public NaviusRatingFillState FillState
    {
        get => (NaviusRatingFillState)GetValue(FillStateProperty);
        set => SetValue(FillStateProperty, value);
    }

    /// <summary>True while under the group's hover preview (contract's data-highlighted).</summary>
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>Raised on click; IsHalf reflects which half-zone was hit (always false when AllowHalf is off).</summary>
    public event EventHandler<NaviusRatingSelectEventArgs> SelectRequested
    {
        add => AddHandler(SelectRequestedEvent, value);
        remove => RemoveHandler(SelectRequestedEvent, value);
    }

    /// <summary>Raised on pointer enter/leave; IsHalf is null on leave (clears hover preview).</summary>
    public event EventHandler<NaviusRatingSelectEventArgs> HoverChanged
    {
        add => AddHandler(HoverChangedEvent, value);
        remove => RemoveHandler(HoverChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusRatingItemAutomationPeer(this);

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        var isHalf = ActualWidth > 0 && e.GetPosition(this).X < ActualWidth / 2;
        RaiseEvent(new NaviusRatingSelectEventArgs(SelectRequestedEvent, this, isHalf));
        e.Handled = true;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        var isHalf = ActualWidth > 0 && Mouse.GetPosition(this).X < ActualWidth / 2;
        RaiseEvent(new NaviusRatingSelectEventArgs(HoverChangedEvent, this, isHalf));
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        RaiseEvent(new NaviusRatingSelectEventArgs(HoverChangedEvent, this, null));
    }
}

/// <summary>IsHalf is null only on hover-leave (clears hover); otherwise true/false per half-zone.</summary>
public class NaviusRatingSelectEventArgs : RoutedEventArgs
{
    public NaviusRatingSelectEventArgs(RoutedEvent routedEvent, object source, bool? isHalf)
        : base(routedEvent, source)
    {
        IsHalf = isHalf;
    }

    public bool? IsHalf { get; }
}
