using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier A-ish: derives from the native <see cref="Button"/> for free focus/click/automation,
/// then layers on hover-intent open/close (Delay/CloseDelay, via DispatcherTimer) and the APG
/// "enter content" keyboard pattern. Disabled maps directly onto the inherited IsEnabled (no
/// separate DP, same delta as the Menubar family). NativeButton is accepted for render-contract
/// parity with Base UI's nativeButton but has no behavioral effect (this is always a native
/// WPF Button).
/// </summary>
public class NaviusNavigationMenuTrigger : Button
{
    public static readonly DependencyProperty NativeButtonProperty = DependencyProperty.Register(
        nameof(NativeButton), typeof(bool), typeof(NaviusNavigationMenuTrigger),
        new PropertyMetadata(true));

    private DispatcherTimer? _openTimer;
    private DispatcherTimer? _closeTimer;

    static NaviusNavigationMenuTrigger()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuTrigger),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuTrigger)));
    }

    public NaviusNavigationMenuTrigger()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    /// <summary>Render-contract parity flag only; see class remarks.</summary>
    public bool NativeButton
    {
        get => (bool)GetValue(NativeButtonProperty);
        set => SetValue(NativeButtonProperty, value);
    }

    /// <summary>The owning Item's Value, or null if not nested inside one.</summary>
    public string? OwningValue => NaviusNavigationMenuItem.GetOwner(this)?.Value;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusNavigationMenuTriggerAutomationPeer(this);

    protected override void OnClick()
    {
        base.OnClick();

        var host = NavigationMenuHostBase.GetHost(this);
        if (host is not null && OwningValue is { } value)
        {
            CancelTimers();
            host.Toggle(value);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var host = NavigationMenuHostBase.GetHost(this);
        if (host is null || OwningValue is not { } value || e.Handled)
        {
            return;
        }

        var isVertical = string.Equals(host.Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        var enterContentKey = isVertical ? Key.Right : Key.Down;

        if (e.Key == enterContentKey)
        {
            CancelTimers();
            host.RequestOpenViaKeyboard(value);
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var host = NavigationMenuHostBase.GetHost(this);
        if (host is not null && OwningValue is { } value)
        {
            host.RegisterTrigger(value, this);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CancelTimers();
        var host = NavigationMenuHostBase.GetHost(this);
        if (host is not null && OwningValue is { } value)
        {
            host.UnregisterTrigger(value);
        }
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        var host = NavigationMenuHostBase.GetHost(this);
        if (host is null || OwningValue is not { } value)
        {
            return;
        }

        _closeTimer?.Stop();

        if (string.Equals(host.Value, value, StringComparison.Ordinal))
        {
            return;
        }

        _openTimer?.Stop();
        _openTimer = StartTimer(Math.Max(0, host.Delay), () => host.RequestOpen(value));
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var host = NavigationMenuHostBase.GetHost(this);
        _openTimer?.Stop();

        if (host is null || OwningValue is not { } value)
        {
            return;
        }

        _closeTimer?.Stop();
        _closeTimer = StartTimer(Math.Max(0, host.CloseDelay), () => host.RequestClose(value));
    }

    /// <summary>
    /// Called by this trigger's Content panel when the pointer enters it, so moving from the
    /// trigger into its own open content doesn't trigger the pending close.
    /// </summary>
    internal void CancelPendingClose() => _closeTimer?.Stop();

    /// <summary>Called by this trigger's Content panel when the pointer leaves it.</summary>
    internal void ScheduleClose()
    {
        var host = NavigationMenuHostBase.GetHost(this);
        if (host is null || OwningValue is not { } value)
        {
            return;
        }

        _closeTimer?.Stop();
        _closeTimer = StartTimer(Math.Max(0, host.CloseDelay), () => host.RequestClose(value));
    }

    private DispatcherTimer StartTimer(int milliseconds, Action callback)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            callback();
        };
        timer.Start();
        return timer;
    }

    private void CancelTimers()
    {
        _openTimer?.Stop();
        _closeTimer?.Stop();
    }
}

internal sealed class NaviusNavigationMenuTriggerAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
    public NaviusNavigationMenuTriggerAutomationPeer(NaviusNavigationMenuTrigger owner) : base(owner)
    {
    }

    private NaviusNavigationMenuTrigger Trigger => (NaviusNavigationMenuTrigger)Owner;

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

    public ExpandCollapseState ExpandCollapseState
    {
        get
        {
            var host = NavigationMenuHostBase.GetHost(Trigger);
            var isOpen = host is not null && Trigger.OwningValue is { } value
                && string.Equals(host.Value, value, StringComparison.Ordinal);
            return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }
    }

    public void Expand()
    {
        ThrowIfDisabled();
        var host = NavigationMenuHostBase.GetHost(Trigger);
        if (host is not null && Trigger.OwningValue is { } value)
        {
            host.RequestOpen(value);
        }
    }

    public void Collapse()
    {
        ThrowIfDisabled();
        var host = NavigationMenuHostBase.GetHost(Trigger);
        if (host is not null && Trigger.OwningValue is { } value)
        {
            host.RequestClose(value);
        }
    }

    // A disabled trigger must not be operable through UIA. ButtonAutomationPeer.Invoke already
    // throws when disabled; the custom ExpandCollapse provider must match (Disabled maps onto the
    // inherited IsEnabled for this control, per the type remarks).
    private void ThrowIfDisabled()
    {
        if (!Trigger.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }
    }
}
