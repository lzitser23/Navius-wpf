using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// Per-Window overlay z-order and dismiss-routing service. Obtain the instance for a window via
/// <see cref="GetFor"/>. Pushing the first overlay attaches a single Window-level PreviewKeyDown
/// (Escape) and PreviewMouseDown (outside-press) hook; popping the last overlay detaches them.
/// The stack does not create or own any visuals: consumers supply their own root elements (and,
/// for modal overlays, place an <see cref="OverlayBackdrop"/> themselves).
/// </summary>
public sealed class OverlayStack
{
    private static readonly ConditionalWeakTable<Window, OverlayStack> Instances = new();

    private readonly Window _window;
    private readonly List<OverlaySession> _sessions = new();
    private bool _hooksAttached;

    private OverlayStack(Window window)
    {
        _window = window;
    }

    /// <summary>Gets (creating if needed) the overlay stack for the given window.</summary>
    public static OverlayStack GetFor(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return Instances.GetValue(window, w => new OverlayStack(w));
    }

    /// <summary>Current sessions, bottommost first. Index in this list matches <see cref="OverlaySession.StackIndex"/>.</summary>
    public IReadOnlyList<OverlaySession> Sessions => _sessions;

    public OverlaySession? Topmost => _sessions.Count > 0 ? _sessions[^1] : null;

    /// <summary>Opens a new overlay on top of the stack.</summary>
    public OverlaySession Push(FrameworkElement root, OverlayOptions options)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(options);

        var session = new OverlaySession(this, root, options, _sessions.Count);
        _sessions.Add(session);
        AttachHooksIfNeeded();

        if (options.TrapFocus)
        {
            session.RestoreFocusTarget = Keyboard.FocusedElement;
            EngageFocusTrap(root);
        }

        return session;
    }

    /// <summary>Removes a session from the stack, reindexes the remainder, and (if requested) restores focus.</summary>
    internal void Remove(OverlaySession session)
    {
        var index = _sessions.IndexOf(session);
        if (index < 0)
        {
            return;
        }

        _sessions.RemoveAt(index);
        for (var i = index; i < _sessions.Count; i++)
        {
            _sessions[i].StackIndex = i;
        }

        if (session.RestoreFocusTarget is not null)
        {
            var focusInside = IsFocusWithin(session.Root);
            if (OverlayDismissPolicy.ShouldRestoreFocus(session.Options.RestoreFocus, focusInside))
            {
                session.RestoreFocusTarget.Focus();
            }
        }

        if (_sessions.Count == 0)
        {
            DetachHooks();
        }
    }

    /// <summary>
    /// Hooks PreviewKeyDown/PreviewMouseDown directly on a popup-tree root so Escape and
    /// outside-press routing keep working inside its own HwndSource. Called by
    /// <see cref="OverlaySession.RegisterInputRoot"/>; not meant to be called directly.
    /// </summary>
    internal void AttachInputRootHooks(FrameworkElement root)
    {
        root.PreviewKeyDown += OnInputRootPreviewKeyDown;
        root.PreviewMouseDown += OnInputRootPreviewMouseDown;
    }

    /// <summary>Reverses <see cref="AttachInputRootHooks"/>; called when a session closes.</summary>
    internal void DetachInputRootHooks(FrameworkElement root)
    {
        root.PreviewKeyDown -= OnInputRootPreviewKeyDown;
        root.PreviewMouseDown -= OnInputRootPreviewMouseDown;
    }

    private void AttachHooksIfNeeded()
    {
        if (_hooksAttached)
        {
            return;
        }

        _window.PreviewKeyDown += OnWindowPreviewKeyDown;
        _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        _hooksAttached = true;
    }

    private void DetachHooks()
    {
        if (!_hooksAttached)
        {
            return;
        }

        _window.PreviewKeyDown -= OnWindowPreviewKeyDown;
        _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
        _hooksAttached = false;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e) => HandleEscapeKey(e);

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e) => HandleOutsidePress(e);

    // Registered directly on each session's input roots (see AttachInputRootHooks) so Escape and
    // outside-press routing also run inside a Popup's own HwndSource, which the Window-level hooks
    // above never see key/mouse events from.
    private void OnInputRootPreviewKeyDown(object sender, KeyEventArgs e) => HandleEscapeKey(e);

    private void OnInputRootPreviewMouseDown(object sender, MouseButtonEventArgs e) => HandleOutsidePress(e);

    private void HandleEscapeKey(KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        var target = OverlayDismissPolicy.FindEscapeTarget(_sessions);
        if (target is null)
        {
            return;
        }

        if (target.RequestClose(OverlayCloseReason.EscapeKey))
        {
            e.Handled = true;
        }
    }

    private void HandleOutsidePress(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject pressTarget)
        {
            return;
        }

        var target = OverlayDismissPolicy.FindOutsidePressTarget(
            _sessions,
            session => IsPressInsideSession(pressTarget, session));

        target?.RequestClose(OverlayCloseReason.OutsidePress);
    }

    // A session's "root" for inside/outside purposes is its main Root plus any input roots
    // registered via RegisterInputRoot (e.g. a Tooltip/Popover popup living in its own HwndSource).
    private static bool IsPressInsideSession(DependencyObject pressTarget, OverlaySession session)
    {
        if (IsDescendant(pressTarget, session.Root))
        {
            return true;
        }

        foreach (var inputRoot in session.InputRoots)
        {
            if (IsDescendant(pressTarget, inputRoot))
            {
                return true;
            }
        }

        return false;
    }

    private static void EngageFocusTrap(FrameworkElement root)
    {
        KeyboardNavigation.SetTabNavigation(root, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(root, KeyboardNavigationMode.Cycle);

        var target = FindFirstFocusableDescendant(root) ?? root;
        target.Focus();
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

    private static bool IsFocusWithin(FrameworkElement root)
    {
        if (Keyboard.FocusedElement is not DependencyObject focused)
        {
            return false;
        }

        return IsDescendant(focused, root);
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
