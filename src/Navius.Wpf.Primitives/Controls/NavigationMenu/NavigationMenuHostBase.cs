using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Shared implementation for <see cref="NaviusNavigationMenu"/> (the root, "nav") and
/// <see cref="NaviusNavigationMenuSub"/> (a nested submenu-in-a-panel), mirroring the web
/// contract's shared NavigationMenuContext: owns the single open-item Value, hover
/// Delay/CloseDelay, and cascades itself to descendants via an inherited attached property
/// (<see cref="HostProperty"/>) so Item/Trigger/Content/List never need explicit wiring, the
/// same role Blazor's CascadingValue plays for NavigationMenuContext.
///
/// Dismissal (outside-press and Escape) is wired at the owning Window level here, mirroring
/// OverlayStack's PreviewMouseDown/PreviewKeyDown pattern (Overlays/OverlayStack.cs), but is
/// not that same shared service: NaviusAnchoredPopup-backed content renders through a separate,
/// transparent child HWND, so Window-level hooks never see presses that land inside an open
/// Content panel (or a nested Sub's own Content panel) at all -- only presses elsewhere in the
/// window. That is sufficient for "click outside the whole menu" but Escape while focus is
/// inside a Content panel needs its own local handler (see NaviusNavigationMenuContent), since
/// key-tunneling from the Window does not cross into the popup's own PresentationSource either.
/// See docs/parity/navigation-menu.md "WPF implementation notes" for the full delta.
/// </summary>
public abstract class NavigationMenuHostBase : ContentControl
{
    public static readonly DependencyProperty HostProperty = DependencyProperty.RegisterAttached(
        "Host", typeof(NavigationMenuHostBase), typeof(NavigationMenuHostBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    public static NavigationMenuHostBase? GetHost(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (NavigationMenuHostBase?)element.GetValue(HostProperty);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NavigationMenuHostBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(string), typeof(NavigationMenuHostBase),
        new PropertyMetadata("horizontal"));

    public static readonly DependencyProperty DelayProperty = DependencyProperty.Register(
        nameof(Delay), typeof(int), typeof(NavigationMenuHostBase),
        new PropertyMetadata(50));

    public static readonly DependencyProperty CloseDelayProperty = DependencyProperty.Register(
        nameof(CloseDelay), typeof(int), typeof(NavigationMenuHostBase),
        new PropertyMetadata(50));

    private readonly Dictionary<string, FrameworkElement> _triggers = new(StringComparer.Ordinal);
    private Window? _window;

    protected NavigationMenuHostBase()
    {
        SetValue(HostProperty, this);
        Loaded += (_, _) => AttachWindowHooks();
        Unloaded += (_, _) => DetachWindowHooks();
    }

    /// <summary>The value of the currently open item; null = nothing open.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Open => Value is not null;

    /// <summary>"horizontal" or "vertical"; drives the List's roving-focus axis.</summary>
    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Milliseconds before hover opens an item's content.</summary>
    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    /// <summary>Milliseconds before leaving the menu closes open content.</summary>
    public int CloseDelay
    {
        get => (int)GetValue(CloseDelayProperty);
        set => SetValue(CloseDelayProperty, value);
    }

    /// <summary>Raised whenever <see cref="Value"/> changes, for any reason.</summary>
    public event EventHandler<string?>? ValueChanged;

    /// <summary>Fires once the open/close transition "settles". This port has no separate
    /// presence-transition phase, so it fires synchronously right after <see cref="Value"/> changes.</summary>
    public event EventHandler<bool>? OpenChangeComplete;

    public void RequestOpen(string value)
    {
        if (!string.Equals(Value, value, StringComparison.Ordinal))
        {
            Value = value;
        }
    }

    public void RequestClose(string value)
    {
        if (string.Equals(Value, value, StringComparison.Ordinal))
        {
            Value = null;
        }
    }

    public void Toggle(string value)
    {
        if (string.Equals(Value, value, StringComparison.Ordinal))
        {
            RequestClose(value);
        }
        else
        {
            RequestOpen(value);
        }
    }

    internal void RegisterTrigger(string value, FrameworkElement trigger) => _triggers[value] = trigger;

    internal void UnregisterTrigger(string value) => _triggers.Remove(value);

    internal bool TryGetTrigger(string value, out FrameworkElement? trigger) => _triggers.TryGetValue(value, out trigger);

    private string? _keyboardOpenPending;

    /// <summary>
    /// Opens the item (like <see cref="RequestOpen"/>) and marks it for the APG "enter content"
    /// keyboard pattern: the owning Content panel should move focus to its first focusable
    /// descendant once open, instead of leaving focus on the trigger (the hover/click default).
    /// </summary>
    public void RequestOpenViaKeyboard(string value)
    {
        _keyboardOpenPending = value;
        RequestOpen(value);
    }

    /// <summary>
    /// Called once by the Content panel that just became open, to check (and clear) whether it
    /// should move focus to its first focusable descendant per <see cref="RequestOpenViaKeyboard"/>.
    /// </summary>
    internal bool ConsumeKeyboardOpen(string value)
    {
        if (!string.Equals(_keyboardOpenPending, value, StringComparison.Ordinal))
        {
            return false;
        }

        _keyboardOpenPending = null;
        return true;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (NavigationMenuHostBase)d;
        host.ValueChanged?.Invoke(host, (string?)e.NewValue);
        host.OpenChangeComplete?.Invoke(host, e.NewValue is not null);
    }

    private void AttachWindowHooks()
    {
        _window = Window.GetWindow(this);
        if (_window is null)
        {
            return;
        }

        _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        _window.PreviewKeyDown += OnWindowPreviewKeyDown;
    }

    private void DetachWindowHooks()
    {
        if (_window is null)
        {
            return;
        }

        _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
        _window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        _window = null;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Value is not { } openValue || e.OriginalSource is not DependencyObject pressTarget)
        {
            return;
        }

        var list = FindDescendants<NaviusNavigationMenuList>(this).FirstOrDefault();
        if (list is not null && IsDescendant(pressTarget, list))
        {
            return;
        }

        RequestClose(openValue);
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || Value is not { } openValue)
        {
            return;
        }

        RequestClose(openValue);

        if (TryGetTrigger(openValue, out var trigger))
        {
            trigger?.Focus();
        }
    }

    /// <summary>Walks the logical tree so descendants are discoverable without a layout pass.</summary>
    private protected static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(childObj))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsDescendant(DependencyObject candidate, DependencyObject ancestor)
    {
        var current = candidate;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = GetVisualOrLogicalParent(current);
        }

        return false;
    }

    private static DependencyObject? GetVisualOrLogicalParent(DependencyObject child)
    {
        if (child is Visual or Visual3D)
        {
            var visualParent = VisualTreeHelper.GetParent(child);
            if (visualParent is not null)
            {
                return visualParent;
            }
        }

        return LogicalTreeHelper.GetParent(child);
    }
}
