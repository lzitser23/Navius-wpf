using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: the panel disclosed by a Trigger. This is where the contract's separate
/// NaviusNavigationMenuPopup + NaviusNavigationMenuPositioner + NaviusNavigationMenuPortal
/// collapse: the default template (Themes/NavigationMenu.xaml) hosts a
/// <see cref="NaviusAnchoredPopup"/> directly (Positioning/ + Controls/NaviusAnchoredPopup.cs,
/// used as-is per the parity notes), with <see cref="Side"/>/<see cref="Align"/>/
/// <see cref="SideOffset"/>/<see cref="AlignOffset"/> folded on as the only positioning knobs the
/// substrate actually exposes. This is the "per-item popup mode" the M2 scope commits to: every
/// open Content owns its own standalone popup, anchored to its own Trigger, rather than
/// teleporting into one shared, morphing Viewport.
///
/// Not implemented (the substrate has no equivalent): Flip/AvoidCollisions toggle,
/// CollisionPadding, Sticky, HideWhenDetached, ArrowPadding, Container (custom portal mount
/// point -- moot anyway, WPF's own Popup already renders outside the normal visual flow).
/// </summary>
public class NaviusNavigationMenuContent : ContentControl
{
    public static readonly DependencyProperty ForceMountProperty = DependencyProperty.Register(
        nameof(ForceMount), typeof(bool), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(false));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(PlacementAlign.Center));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(0d));

    private static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOpen), typeof(bool), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey AnchorPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Anchor), typeof(UIElement), typeof(NaviusNavigationMenuContent),
        new PropertyMetadata(null));

    public static readonly DependencyProperty AnchorProperty = AnchorPropertyKey.DependencyProperty;

    private NavigationMenuHostBase? _host;
    private NaviusNavigationMenuItem? _owner;

    static NaviusNavigationMenuContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuContent),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuContent)));
    }

    public NaviusNavigationMenuContent()
    {
        Focusable = false;
        PreviewKeyDown += OnPreviewKeyDown;
        MouseEnter += (_, _) => GetTrigger()?.CancelPendingClose();
        MouseLeave += (_, _) => GetTrigger()?.ScheduleClose();
        Loaded += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
    }

    /// <summary>Keep the panel mounted while closed. No exit-animation phase exists in this port
    /// to preserve (see class remarks); accepted for API parity.</summary>
    public bool ForceMount
    {
        get => (bool)GetValue(ForceMountProperty);
        set => SetValue(ForceMountProperty, value);
    }

    public PlacementSide Side
    {
        get => (PlacementSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public PlacementAlign Align
    {
        get => (PlacementAlign)GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    public double SideOffset
    {
        get => (double)GetValue(SideOffsetProperty);
        set => SetValue(SideOffsetProperty, value);
    }

    public double AlignOffset
    {
        get => (double)GetValue(AlignOffsetProperty);
        set => SetValue(AlignOffsetProperty, value);
    }

    public bool IsOpen => (bool)GetValue(IsOpenProperty);

    public UIElement? Anchor => (UIElement?)GetValue(AnchorProperty);

    private NaviusNavigationMenuTrigger? GetTrigger()
    {
        if (_host is null || _owner is null)
        {
            return null;
        }

        _host.TryGetTrigger(_owner.Value, out var trigger);
        return trigger as NaviusNavigationMenuTrigger;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _host is null || _owner is null)
        {
            return;
        }

        _host.RequestClose(_owner.Value);
        GetTrigger()?.Focus();
        e.Handled = true;
    }

    private void Subscribe()
    {
        _host = NavigationMenuHostBase.GetHost(this);
        _owner = NaviusNavigationMenuItem.GetOwner(this);

        if (_host is null || _owner is null)
        {
            return;
        }

        _host.ValueChanged += OnHostValueChanged;

        if (_host.TryGetTrigger(_owner.Value, out var trigger))
        {
            SetValue(AnchorPropertyKey, trigger);
        }

        Refresh();
    }

    private void Unsubscribe()
    {
        if (_host is not null)
        {
            _host.ValueChanged -= OnHostValueChanged;
            _host = null;
        }

        _owner = null;
    }

    private void OnHostValueChanged(object? sender, string? value) => Refresh();

    private void Refresh()
    {
        if (_host is null || _owner is null)
        {
            return;
        }

        var isOpen = string.Equals(_host.Value, _owner.Value, StringComparison.Ordinal);
        SetValue(IsOpenPropertyKey, isOpen);

        if (isOpen && _host.ConsumeKeyboardOpen(_owner.Value))
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(FocusFirstDescendant));
        }
    }

    private void FocusFirstDescendant()
    {
        var target = FindFirstFocusableDescendant(this);
        target?.Focus();
    }

    private static IInputElement? FindFirstFocusableDescendant(DependencyObject node)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is UIElement { Focusable: true, IsVisible: true } focusable)
            {
                return focusable;
            }

            var nested = FindFirstFocusableDescendant(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
