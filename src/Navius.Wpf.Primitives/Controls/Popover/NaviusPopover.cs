using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B: click-toggle popover with a trapped, focus-moved-in popup. <see cref="ContentControl.Content"/>
/// is the trigger (the web Trigger part); the placement properties below are the web Positioner part,
/// collapsed onto this root control per the WPF port's honest-adaptation precedent (see
/// docs/parity/popover.md, "WPF implementation notes"). Built on <see cref="NaviusAnchoredPopup"/> for
/// placement; registers its popup content as an <see cref="OverlaySession"/> input root so Escape and
/// outside-press dismissal work once focus (or a click) lands inside the popup's own HwndSource.
/// </summary>
[TemplatePart(Name = PartTrigger, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
public class NaviusPopover : ContentControl
{
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";

    /// <summary>Bound to any button inside the popover's content to close it, mirroring the web's NaviusPopoverClose part.</summary>
    public static readonly RoutedCommand CloseCommand = new(nameof(CloseCommand), typeof(NaviusPopover));

    public static readonly DependencyProperty PopoverContentProperty = DependencyProperty.Register(
        nameof(PopoverContent), typeof(object), typeof(NaviusPopover), new PropertyMetadata(null));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(object), typeof(NaviusPopover), new PropertyMetadata(null, OnTitleOrDescriptionChanged));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(object), typeof(NaviusPopover), new PropertyMetadata(null, OnTitleOrDescriptionChanged));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusPopover), new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusPopover), new PropertyMetadata(PlacementAlign.Center));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusPopover), new PropertyMetadata(6d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusPopover), new PropertyMetadata(0d));

    /// <summary>
    /// When true, the popup content traps focus/scroll like a dialog. The WPF port always moves
    /// focus into the popup and cycles Tab within it while open (see implementation notes: the
    /// web only traps when Modal, but <see cref="OverlayOptions.TrapFocus"/> conflates "move focus
    /// in" and "cycle Tab" into one flag, and splitting them is out of scope for this batch).
    /// </summary>
    public static readonly DependencyProperty ModalProperty = DependencyProperty.Register(
        nameof(Modal), typeof(bool), typeof(NaviusPopover), new PropertyMetadata(false));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusPopover),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    private ButtonBase? _triggerPart;
    private FrameworkElement? _popupContentPart;
    private OverlaySession? _session;

    static NaviusPopover()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusPopover), new FrameworkPropertyMetadata(typeof(NaviusPopover)));
    }

    /// <summary>Content of the popover panel (the web Popup's ChildContent).</summary>
    public object? PopoverContent
    {
        get => GetValue(PopoverContentProperty);
        set => SetValue(PopoverContentProperty, value);
    }

    /// <summary>Labels the popup (web NaviusPopoverTitle); wired to AutomationProperties.Name on the popup content when set.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Describes the popup (web NaviusPopoverDescription); wired to AutomationProperties.HelpText on the popup content when set.</summary>
    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
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

    public bool Modal
    {
        get => (bool)GetValue(ModalProperty);
        set => SetValue(ModalProperty, value);
    }

    /// <summary>Controlled/uncontrolled open state (bindable both ways, mirroring the web's Open/OpenChanged pair).</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_triggerPart is not null)
        {
            _triggerPart.Click -= OnTriggerClick;
        }

        _triggerPart = GetTemplateChild(PartTrigger) as ButtonBase;

        // The popup content lives inside a Popup, whose own root breaks the visual/logical tree
        // that RoutedCommand execution normally bubbles through, so CloseCommand is bound directly
        // on the popup content itself rather than class-registered on NaviusPopover.
        _popupContentPart = GetTemplateChild(PartPopupContent) as FrameworkElement;

        if (_triggerPart is not null)
        {
            _triggerPart.Click += OnTriggerClick;
        }

        if (_popupContentPart is not null)
        {
            _popupContentPart.CommandBindings.Add(new CommandBinding(CloseCommand, OnCloseCommandExecuted));
        }

        ApplyAutomationLabels();
    }

    private void OnTriggerClick(object sender, RoutedEventArgs e) => IsOpen = !IsOpen;

    private void OnCloseCommandExecuted(object sender, ExecutedRoutedEventArgs e) =>
        RequestClose(OverlayCloseReason.Programmatic);

    private static void OnTitleOrDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusPopover)d).ApplyAutomationLabels();

    private void ApplyAutomationLabels()
    {
        if (_popupContentPart is null)
        {
            return;
        }

        AutomationProperties.SetName(_popupContentPart, Title as string ?? string.Empty);
        AutomationProperties.SetHelpText(_popupContentPart, Description as string ?? string.Empty);
    }

    private void RequestClose(OverlayCloseReason reason) => _session?.RequestClose(reason);

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusPopover)d;
        if ((bool)e.NewValue)
        {
            control.OpenCore();
        }
        else
        {
            control.RequestClose(OverlayCloseReason.Programmatic);
        }
    }

    private void OpenCore()
    {
        if (_session is not null)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        var stack = OverlayStack.GetFor(window);
        _session = stack.Push(_popupContentPart ?? this, new OverlayOptions
        {
            Modal = Modal,
            CloseOnEscape = true,
            CloseOnOutsideClick = true,
            TrapFocus = true,
            RestoreFocus = true,
        });

        if (_popupContentPart is not null)
        {
            _session.RegisterInputRoot(_popupContentPart);
        }

        _session.Closed += OnSessionClosed;
        PlayEnterAnimation();
    }

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        IsOpen = false;
    }

    private void PlayEnterAnimation()
    {
        if (_popupContentPart is null)
        {
            return;
        }

        var translate = new TranslateTransform();
        _popupContentPart.RenderTransform = translate;
        _popupContentPart.Opacity = 0;

        var offset = Side switch
        {
            PlacementSide.Top => 6,
            PlacementSide.Bottom => -6,
            _ => 0,
        };
        translate.Y = offset;

        var duration = TimeSpan.FromMilliseconds(150);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(offset, 0, duration) { EasingFunction = ease });
        _popupContentPart.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
    }
}
