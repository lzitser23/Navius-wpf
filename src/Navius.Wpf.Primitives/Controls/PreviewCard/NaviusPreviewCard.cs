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
/// Tier B: hover/focus preview card (the web PreviewCard, née HoverCard). <see cref="ContentControl.Content"/>
/// is the trigger (the web Trigger part); the placement properties below are the web Positioner
/// part, collapsed onto this root control per the WPF port's honest-adaptation precedent (see
/// docs/parity/preview-card.md, "WPF implementation notes"). Non-modal end to end: never traps or
/// moves focus, matching <c>MoveFocusInside = false</c> on the web's PreviewCardPopup. Built on
/// <see cref="NaviusAnchoredPopup"/> for placement; registers its popup content as an
/// <see cref="OverlaySession"/> input root so Escape and outside-press dismissal work inside the
/// popup's own HwndSource.
/// </summary>
[TemplatePart(Name = PartTrigger, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
public class NaviusPreviewCard : ContentControl
{
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";

    public static readonly DependencyProperty PreviewContentProperty = DependencyProperty.Register(
        nameof(PreviewContent), typeof(object), typeof(NaviusPreviewCard), new PropertyMetadata(null));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusPreviewCard), new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusPreviewCard), new PropertyMetadata(PlacementAlign.Center));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusPreviewCard), new PropertyMetadata(6d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusPreviewCard), new PropertyMetadata(0d));

    /// <summary>Hover-intent delay (ms) before opening. Web default: 600.</summary>
    public static readonly DependencyProperty OpenDelayProperty = DependencyProperty.Register(
        nameof(OpenDelay), typeof(int), typeof(NaviusPreviewCard), new PropertyMetadata(600));

    /// <summary>Grace delay (ms) before closing after leave/blur. Web default: 300.</summary>
    public static readonly DependencyProperty CloseDelayProperty = DependencyProperty.Register(
        nameof(CloseDelay), typeof(int), typeof(NaviusPreviewCard), new PropertyMetadata(300));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusPreviewCard),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    private FrameworkElement? _triggerPart;
    private FrameworkElement? _popupContentPart;
    private DispatcherTimer? _openTimer;
    private DispatcherTimer? _closeTimer;
    private OverlaySession? _session;

    static NaviusPreviewCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusPreviewCard), new FrameworkPropertyMetadata(typeof(NaviusPreviewCard)));
    }

    /// <summary>Content of the preview panel (the web Popup's ChildContent).</summary>
    public object? PreviewContent
    {
        get => GetValue(PreviewContentProperty);
        set => SetValue(PreviewContentProperty, value);
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

    public int OpenDelay
    {
        get => (int)GetValue(OpenDelayProperty);
        set => SetValue(OpenDelayProperty, value);
    }

    public int CloseDelay
    {
        get => (int)GetValue(CloseDelayProperty);
        set => SetValue(CloseDelayProperty, value);
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
    }

    private void HookPopupContent()
    {
        if (_popupContentPart is null)
        {
            return;
        }

        // The popup content extends the trigger's "stay open" hover region: entering it keeps the
        // card open (cancels any pending close), leaving it closes after CloseDelay, exactly like
        // leaving the trigger.
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
        _closeTimer?.Stop();

        if (IsOpen)
        {
            return;
        }

        StartOpenTimer();
    }

    private void OnTriggerMouseLeave(object sender, MouseEventArgs e)
    {
        _openTimer?.Stop();

        if (IsOpen)
        {
            StartCloseTimer();
        }
    }

    private void OnTriggerGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _openTimer?.Stop();
        IsOpen = true;
    }

    private void OnTriggerLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (IsOpen)
        {
            StartCloseTimer();
        }
    }

    private void OnPopupContentMouseEnter(object sender, MouseEventArgs e) => _closeTimer?.Stop();

    private void OnPopupContentMouseLeave(object sender, MouseEventArgs e)
    {
        if (IsOpen)
        {
            StartCloseTimer();
        }
    }

    private void StartOpenTimer()
    {
        _openTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(0, OpenDelay)) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            IsOpen = true;
        };
        _openTimer = timer;
        timer.Start();
    }

    private void StartCloseTimer()
    {
        _closeTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(0, CloseDelay)) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            RequestClose(OverlayCloseReason.Programmatic);
        };
        _closeTimer = timer;
        timer.Start();
    }

    private void RequestClose(OverlayCloseReason reason) => _session?.RequestClose(reason);

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusPreviewCard)d;
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
            CloseOnOutsideClick = true,
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
