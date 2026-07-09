using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Primitives.Controls.OverlaySurface;

/// <summary>
/// Shared Tier B base for the Dialog / AlertDialog / Drawer families. Owns the open/close state
/// machine (IsOpen DP two-way, Opened/Closed CLR events, cancelable Closing forwarded from the
/// underlying <see cref="OverlaySession"/>), the handshake with <see cref="NaviusOverlayLayer"/>
/// and <see cref="Overlays.OverlayStack"/>, and the enter/exit fade. Each family is itself the
/// popup surface (backdrop + panel are both part of its own ControlTemplate) rather than being
/// decomposed into separate Trigger/Portal/Backdrop/Popup/Close control classes: this mirrors how
/// NaviusRadioGroup/NaviusCheckboxGroup already fold the web's multi-part anatomy into one
/// lookless ContentControl in this codebase.
///
/// Mirrors the web's OverlayPresence Entering/Exiting/Rendered choreography: opening makes the
/// control Visible and fades it in; closing fades it out and only removes it from the
/// NaviusOverlayLayer (and reports IsOpen = false) once the exit animation completes, so the exit
/// transition is never truncated by an instant unmount.
/// </summary>
public abstract class NaviusOverlaySurfaceBase : ContentControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(NaviusOverlaySurfaceBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(NaviusOverlaySurfaceBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(NaviusOverlaySurfaceBase),
        new PropertyMetadata(null));

    /// <summary>Any element inside a surface's Content bound to this command requests a close, mirroring NaviusDialogClose/NaviusAlertDialogCancel.</summary>
    public static readonly RoutedCommand CloseCommand = new(nameof(CloseCommand), typeof(NaviusOverlaySurfaceBase));

    private NaviusOverlayLayer? _layer;
    private OverlaySession? _session;

    static NaviusOverlaySurfaceBase()
    {
        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusOverlaySurfaceBase),
            new CommandBinding(CloseCommand, OnCloseCommandExecuted));
    }

    protected NaviusOverlaySurfaceBase()
    {
        Visibility = Visibility.Collapsed;
        Focusable = true;
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Raised after the overlay has engaged (pushed onto the OverlayStack, enter animation started).</summary>
    public event EventHandler? Opened;

    /// <summary>Raised after the overlay has fully disengaged (exit animation complete, removed from its layer).</summary>
    public event EventHandler? Closed;

    /// <summary>Forwarded from the underlying <see cref="OverlaySession"/>; set Cancel to keep the overlay open.</summary>
    public event EventHandler<OverlayClosingEventArgs>? Closing;

    /// <summary>Whether this instance is currently modal. Dialog/Drawer expose this via their own Modal DP; AlertDialog is always true.</summary>
    protected abstract bool ModalEffective { get; }

    /// <summary>Whether an outside press dismisses the overlay. Dialog/Drawer expose this via their own DP; AlertDialog is always false.</summary>
    protected virtual bool CloseOnOutsideClickEffective => true;

    /// <summary>Whether Escape dismisses the overlay. Fixed true for all three families per their parity contracts.</summary>
    protected virtual bool CloseOnEscapeEffective => true;

    /// <summary>Focus trapping tracks modality, matching the web contract's TrapFocus = OverlayContext.Modal.</summary>
    protected virtual bool TrapFocusEffective => ModalEffective;

    protected virtual TimeSpan EnterDuration => TimeSpan.FromMilliseconds(150);

    protected virtual TimeSpan ExitDuration => TimeSpan.FromMilliseconds(150);

    /// <summary>Opens the overlay. Equivalent to setting <see cref="IsOpen"/> to true, but respects an existing two-way binding via SetCurrentValue.</summary>
    public void Open() => SetCurrentValue(IsOpenProperty, true);

    /// <summary>Closes the overlay. Equivalent to setting <see cref="IsOpen"/> to false, but respects an existing two-way binding via SetCurrentValue.</summary>
    public void Close() => SetCurrentValue(IsOpenProperty, false);

    /// <summary>Override to focus a specific element once the overlay engages (e.g. AlertDialog's Cancel button). Null keeps the OverlayStack's own default (first focusable descendant).</summary>
    protected virtual FrameworkElement? ResolveInitialFocusElement() => null;

    /// <summary>Starts the enter transition. Base implementation fades Opacity 0 -> 1; Drawer additionally slides its panel.</summary>
    protected virtual void PlayEnterAnimation()
    {
        BeginOpacityAnimation(0d, 1d, EnterDuration, onComplete: null);
    }

    /// <summary>Starts the exit transition; MUST call <paramref name="onComplete"/> exactly once when finished so disengage can unmount.</summary>
    protected virtual void PlayExitAnimation(Action onComplete)
    {
        BeginOpacityAnimation(1d, 0d, ExitDuration, onComplete);
    }

    protected void BeginOpacityAnimation(double from, double to, TimeSpan duration, Action? onComplete)
    {
        if (PresentationSource.FromVisual(this) is null)
        {
            // No live render surface (design-time / not yet part of a shown window): skip the
            // animation and resolve synchronously so the open/close state machine still completes.
            Opacity = to;
            onComplete?.Invoke();
            return;
        }

        var animation = new DoubleAnimation(from, to, duration) { FillBehavior = FillBehavior.HoldEnd };
        if (onComplete is not null)
        {
            animation.Completed += (_, _) => onComplete();
        }

        BeginAnimation(OpacityProperty, animation);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var surface = (NaviusOverlaySurfaceBase)d;
        var isOpen = (bool)e.NewValue;

        if (isOpen)
        {
            if (surface._session is null)
            {
                surface.Engage();
            }
        }
        else
        {
            if (surface._session is not null)
            {
                var closed = surface._session.RequestClose(OverlayCloseReason.Programmatic);
                if (!closed)
                {
                    // A Closing handler set Cancel = true, so the session stayed open and the
                    // surface is still visible. Revert IsOpen back to true so the DP does not
                    // report a false "closed" state that is out of sync with the live overlay
                    // (otherwise a later Open() is silently swallowed because _session != null).
                    surface.SetCurrentValue(IsOpenProperty, true);
                }
            }
        }
    }

    private static void OnCloseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusOverlaySurfaceBase surface)
        {
            surface.Close();
        }
    }

    private void Engage()
    {
        var window = Window.GetWindow(this);
        var layer = window is not null ? NaviusOverlayLayer.GetFor(window) : null;

        if (window is null || layer is null)
        {
            Trace.TraceWarning(
                $"{GetType().Name}: no NaviusOverlayLayer found in the current window's visual " +
                "tree. Place a Controls.OverlaySurface.NaviusOverlayLayer (stretched to fill the " +
                "window) so Dialog/AlertDialog/Drawer surfaces have somewhere to render. Reverting IsOpen to false.");
            SetCurrentValue(IsOpenProperty, false);
            return;
        }

        _layer = layer;

        if (!string.IsNullOrEmpty(Title))
        {
            AutomationProperties.SetName(this, Title);
        }

        if (!string.IsNullOrEmpty(Description))
        {
            AutomationProperties.SetHelpText(this, Description);
        }

        layer.AddSurface(this);
        Visibility = Visibility.Visible;
        Opacity = 0d;

        var options = new OverlayOptions
        {
            Modal = ModalEffective,
            CloseOnEscape = CloseOnEscapeEffective,
            CloseOnOutsideClick = CloseOnOutsideClickEffective,
            TrapFocus = TrapFocusEffective,
            RestoreFocus = true,
        };

        var session = OverlayStack.GetFor(window).Push(this, options);
        _session = session;
        session.Closing += OnSessionClosing;
        session.Closed += OnSessionClosed;

        var initialFocus = ResolveInitialFocusElement();
        initialFocus?.Focus();

        PlayEnterAnimation();
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionClosing(object? sender, OverlayClosingEventArgs e) => Closing?.Invoke(this, e);

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        var session = _session;
        if (session is not null)
        {
            session.Closing -= OnSessionClosing;
            session.Closed -= OnSessionClosed;
        }

        _session = null;

        var layer = _layer;
        _layer = null;

        PlayExitAnimation(() =>
        {
            layer?.RemoveSurface(this);
            Visibility = Visibility.Collapsed;
            Closed?.Invoke(this, EventArgs.Empty);
            SetCurrentValue(IsOpenProperty, false);
        });
    }
}
