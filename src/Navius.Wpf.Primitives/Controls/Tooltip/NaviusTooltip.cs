using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B: hover/focus tooltip. <see cref="ContentControl.Content"/> is the trigger (the web
/// Trigger part); <see cref="TooltipContent"/> and the placement properties below are the web
/// Positioner part, collapsed onto this root control per the WPF port's honest-adaptation
/// precedent (see docs/parity/tooltip.md, "WPF implementation notes"). Built on
/// <see cref="NaviusAnchoredPopup"/> for placement; registers its popup content as an
/// <see cref="OverlaySession"/> input root so Escape works while the popup's own HwndSource has
/// focus/hit-test priority.
/// </summary>
[TemplatePart(Name = PartTrigger, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
public class NaviusTooltip : ContentControl
{
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";

    /// <summary>Bridges the trigger→popup gap on pointer leave before closing, when hoverable content is enabled.</summary>
    private const int HoverGraceMs = 60;

    public static readonly DependencyProperty TooltipContentProperty = DependencyProperty.Register(
        nameof(TooltipContent), typeof(object), typeof(NaviusTooltip), new PropertyMetadata(null));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusTooltip), new PropertyMetadata(PlacementSide.Top));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusTooltip), new PropertyMetadata(PlacementAlign.Center));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusTooltip), new PropertyMetadata(6d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusTooltip), new PropertyMetadata(0d));

    /// <summary>Falls back to <see cref="NaviusTooltipService.DelayDuration"/> when null (collapses the web's DelayDuration/legacy OpenDelay pair into one nullable override).</summary>
    public static readonly DependencyProperty DelayDurationProperty = DependencyProperty.Register(
        nameof(DelayDuration), typeof(int?), typeof(NaviusTooltip), new PropertyMetadata(null));

    public static readonly DependencyProperty DisableHoverableContentProperty = DependencyProperty.Register(
        nameof(DisableHoverableContent), typeof(bool), typeof(NaviusTooltip), new PropertyMetadata(false));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusTooltip),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    private static readonly DependencyPropertyKey IsInstantPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsInstant), typeof(bool), typeof(NaviusTooltip), new PropertyMetadata(false));

    public static readonly DependencyProperty IsInstantProperty = IsInstantPropertyKey.DependencyProperty;

    private FrameworkElement? _triggerPart;
    private FrameworkElement? _popupContentPart;
    private DispatcherTimer? _openTimer;
    private DispatcherTimer? _closeGraceTimer;
    private OverlaySession? _session;
    private bool _pointerInContent;

    static NaviusTooltip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTooltip), new FrameworkPropertyMetadata(typeof(NaviusTooltip)));
    }

    /// <summary>Content of the tooltip bubble (the web Popup's ChildContent).</summary>
    public object? TooltipContent
    {
        get => GetValue(TooltipContentProperty);
        set => SetValue(TooltipContentProperty, value);
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

    public int? DelayDuration
    {
        get => (int?)GetValue(DelayDurationProperty);
        set => SetValue(DelayDurationProperty, value);
    }

    public bool DisableHoverableContent
    {
        get => (bool)GetValue(DisableHoverableContentProperty);
        set => SetValue(DisableHoverableContentProperty, value);
    }

    /// <summary>Controlled/uncontrolled open state (bindable both ways, mirroring the web's Open/OpenChanged pair).</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>True when the tooltip opened without its hover-intent delay (keyboard focus, or the cross-tooltip skip-delay window). Mirrors the web's data-instant.</summary>
    public bool IsInstant => (bool)GetValue(IsInstantProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UnhookTrigger();
        UnhookPopupContent();

        _triggerPart = GetTemplateChild(PartTrigger) as FrameworkElement;
        _popupContentPart = GetTemplateChild(PartPopupContent) as FrameworkElement;

        HookTrigger();
        HookPopupContent();
    }

    private void HookTrigger()
    {
        if (_triggerPart is null)
        {
            return;
        }

        _triggerPart.MouseEnter += OnTriggerMouseEnter;
        _triggerPart.MouseLeave += OnTriggerMouseLeave;
        _triggerPart.GotKeyboardFocus += OnTriggerGotKeyboardFocus;
        _triggerPart.LostKeyboardFocus += OnTriggerLostKeyboardFocus;
        _triggerPart.PreviewKeyDown += OnTriggerPreviewKeyDown;
        _triggerPart.PreviewMouseDown += OnTriggerPreviewMouseDown;
    }

    private void UnhookTrigger()
    {
        if (_triggerPart is null)
        {
            return;
        }

        _triggerPart.MouseEnter -= OnTriggerMouseEnter;
        _triggerPart.MouseLeave -= OnTriggerMouseLeave;
        _triggerPart.GotKeyboardFocus -= OnTriggerGotKeyboardFocus;
        _triggerPart.LostKeyboardFocus -= OnTriggerLostKeyboardFocus;
        _triggerPart.PreviewKeyDown -= OnTriggerPreviewKeyDown;
        _triggerPart.PreviewMouseDown -= OnTriggerPreviewMouseDown;
    }

    private void HookPopupContent()
    {
        if (_popupContentPart is null)
        {
            return;
        }

        _popupContentPart.MouseEnter += OnPopupContentMouseEnter;
        _popupContentPart.MouseLeave += OnPopupContentMouseLeave;
    }

    private void UnhookPopupContent()
    {
        if (_popupContentPart is null)
        {
            return;
        }

        _popupContentPart.MouseEnter -= OnPopupContentMouseEnter;
        _popupContentPart.MouseLeave -= OnPopupContentMouseLeave;
    }

    private void OnTriggerMouseEnter(object sender, MouseEventArgs e)
    {
        if (IsOpen)
        {
            return;
        }

        _closeGraceTimer?.Stop();

        if (NaviusTooltipService.ShouldSkipDelay())
        {
            OpenNow(instant: true);
            return;
        }

        StartOpenTimer(DelayDuration ?? NaviusTooltipService.DelayDuration);
    }

    private void OnTriggerMouseLeave(object sender, MouseEventArgs e)
    {
        _openTimer?.Stop();

        if (!IsOpen)
        {
            return;
        }

        if (DisableHoverableContent)
        {
            RequestClose(OverlayCloseReason.Programmatic);
            return;
        }

        StartCloseGraceTimer();
    }

    private void OnTriggerGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _openTimer?.Stop();
        OpenNow(instant: true);
    }

    private void OnTriggerLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        if (DisableHoverableContent)
        {
            RequestClose(OverlayCloseReason.Programmatic);
            return;
        }

        StartCloseGraceTimer();
    }

    private void OnTriggerPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            RequestClose(OverlayCloseReason.EscapeKey);
            e.Handled = true;
        }
        else if (e.Key is Key.Space or Key.Enter)
        {
            // Activating the trigger dismisses the tooltip so it doesn't linger over the activated control.
            RequestClose(OverlayCloseReason.Programmatic);
        }
    }

    private void OnTriggerPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsOpen)
        {
            RequestClose(OverlayCloseReason.Programmatic);
        }
    }

    private void OnPopupContentMouseEnter(object sender, MouseEventArgs e)
    {
        if (DisableHoverableContent)
        {
            return;
        }

        _pointerInContent = true;
        _closeGraceTimer?.Stop();
    }

    private void OnPopupContentMouseLeave(object sender, MouseEventArgs e)
    {
        if (DisableHoverableContent)
        {
            return;
        }

        _pointerInContent = false;
        RequestClose(OverlayCloseReason.Programmatic);
    }

    private void StartOpenTimer(int delayMs)
    {
        _openTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(0, delayMs)) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            OpenNow(instant: false);
        };
        _openTimer = timer;
        timer.Start();
    }

    private void StartCloseGraceTimer()
    {
        _closeGraceTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HoverGraceMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!_pointerInContent)
            {
                RequestClose(OverlayCloseReason.Programmatic);
            }
        };
        _closeGraceTimer = timer;
        timer.Start();
    }

    private void OpenNow(bool instant)
    {
        SetValue(IsInstantPropertyKey, instant);
        IsOpen = true;
    }

    private void RequestClose(OverlayCloseReason reason) => _session?.RequestClose(reason);

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusTooltip)d;
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
            CloseOnEscape = true,
            CloseOnOutsideClick = false,
            TrapFocus = false,
            RestoreFocus = false,
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

        NaviusTooltipService.NotifyClosed();
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
            PlacementSide.Top => 4,
            PlacementSide.Bottom => -4,
            _ => 0,
        };
        translate.Y = offset;

        var duration = TimeSpan.FromMilliseconds(130);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(offset, 0, duration) { EasingFunction = ease });
        _popupContentPart.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
    }
}
