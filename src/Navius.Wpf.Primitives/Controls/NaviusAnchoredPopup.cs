using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B: composes a native <see cref="Popup"/> and drives its position from
/// <see cref="PlacementMath"/>. Not itself a lookless control (no DefaultStyleKey/theme
/// dictionary) since a bare Popup has no chrome of its own; Popover/Tooltip/etc. build
/// their styled surfaces as the <see cref="Child"/> content.
/// </summary>
[ContentProperty(nameof(Child))]
public class NaviusAnchoredPopup : FrameworkElement
{
    private readonly Popup _popup;

    public static readonly DependencyProperty AnchorProperty = DependencyProperty.Register(
        nameof(Anchor), typeof(UIElement), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(PlacementSide.Bottom, OnPlacementInputChanged));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(PlacementAlign.Center, OnPlacementInputChanged));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(0d, OnPlacementInputChanged));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(0d, OnPlacementInputChanged));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusAnchoredPopup),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty ChildProperty = DependencyProperty.Register(
        nameof(Child), typeof(UIElement), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(null, OnChildChanged));

    private static readonly DependencyPropertyKey EffectiveSidePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(EffectiveSide), typeof(PlacementSide), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty EffectiveSideProperty = EffectiveSidePropertyKey.DependencyProperty;

    /// <summary>
    /// Attached read-only property mirroring <see cref="EffectiveSide"/>, as a string
    /// (e.g. "Bottom"), onto <see cref="Child"/> so a popup-content template can react to
    /// it (e.g. an arrow glyph) without a reference back to the owning control.
    /// </summary>
    private static readonly DependencyPropertyKey EffectiveSideTextPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "EffectiveSideText", typeof(string), typeof(NaviusAnchoredPopup),
        new PropertyMetadata(null));

    public static readonly DependencyProperty EffectiveSideTextProperty = EffectiveSideTextPropertyKey.DependencyProperty;

    public static string? GetEffectiveSideText(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string?)element.GetValue(EffectiveSideTextProperty);
    }

    public NaviusAnchoredPopup()
    {
        _popup = new Popup
        {
            Placement = PlacementMode.Absolute,
            PlacementTarget = this,
            AllowsTransparency = true,
        };

        _popup.Opened += (_, _) => UpdatePlacement();
    }

    public UIElement? Anchor
    {
        get => (UIElement?)GetValue(AnchorProperty);
        set => SetValue(AnchorProperty, value);
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

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public UIElement? Child
    {
        get => (UIElement?)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <summary>The side actually used for the current/last placement, after flip resolution.</summary>
    public PlacementSide EffectiveSide => (PlacementSide)GetValue(EffectiveSideProperty);

    private static void OnPlacementInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAnchoredPopup)d).UpdatePlacement();

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusAnchoredPopup)d;
        var isOpen = (bool)e.NewValue;

        if (isOpen)
        {
            control.UpdatePlacement();
        }

        control._popup.IsOpen = isOpen;
    }

    private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusAnchoredPopup)d;

        if (e.OldValue is FrameworkElement oldChild)
        {
            oldChild.SizeChanged -= control.OnChildSizeChanged;
        }

        control._popup.Child = (UIElement?)e.NewValue;

        if (e.NewValue is FrameworkElement newChild)
        {
            newChild.SizeChanged += control.OnChildSizeChanged;
        }
    }

    private void OnChildSizeChanged(object sender, SizeChangedEventArgs e) => UpdatePlacement();

    private void UpdatePlacement()
    {
        if (Anchor is null || Child is not FrameworkElement child || !IsOpen)
        {
            return;
        }

        if (PresentationSource.FromVisual(Anchor) is null)
        {
            // Anchor isn't in the visual tree yet (e.g. still loading); nothing to measure against.
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(Anchor);
        var topLeftDevice = Anchor.PointToScreen(new Point(0, 0));
        var anchorTopLeft = new Point(topLeftDevice.X / dpi.DpiScaleX, topLeftDevice.Y / dpi.DpiScaleY);
        var anchorRect = new Rect(anchorTopLeft, new Size(Anchor.RenderSize.Width, Anchor.RenderSize.Height));

        var popupSize = new Size(
            child.ActualWidth > 0 ? child.ActualWidth : child.DesiredSize.Width,
            child.ActualHeight > 0 ? child.ActualHeight : child.DesiredSize.Height);

        // TODO(multi-monitor): SystemParameters.WorkArea is the primary screen's work area
        // only. A multi-monitor-correct implementation should resolve the work area for the
        // monitor under anchorRect via the Win32 MonitorFromPoint/GetMonitorInfo APIs.
        var workArea = SystemParameters.WorkArea;

        var options = new AnchoredPlacementOptions
        {
            Side = Side,
            Align = Align,
            SideOffset = SideOffset,
            AlignOffset = AlignOffset,
        };

        var result = PlacementMath.Place(anchorRect, popupSize, workArea, options);

        _popup.HorizontalOffset = result.Origin.X;
        _popup.VerticalOffset = result.Origin.Y;

        SetValue(EffectiveSidePropertyKey, result.EffectiveSide);
        child.SetValue(EffectiveSideTextPropertyKey, result.EffectiveSide.ToString());
    }
}
