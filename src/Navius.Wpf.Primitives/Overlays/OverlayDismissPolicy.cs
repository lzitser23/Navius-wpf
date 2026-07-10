using System;
using System.Collections.Generic;

namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// Pure routing decisions for <see cref="OverlayStack"/>'s window-level Escape and outside-press
/// hooks. Factored out of OverlayStack so the "which session reacts" policy is testable without
/// simulating real key/mouse events or requiring InternalsVisibleTo.
/// </summary>
public static class OverlayDismissPolicy
{
    /// <summary>
    /// Scans the stack from topmost to bottommost and returns the first session with
    /// CloseOnEscape enabled, skipping (not closing) sessions stacked above it that opted out.
    /// Returns null if no session in the stack has CloseOnEscape enabled.
    /// </summary>
    public static OverlaySession? FindEscapeTarget(IReadOnlyList<OverlaySession> stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        for (var i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i].Options.CloseOnEscape)
            {
                return stack[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Scans the stack from topmost to bottommost for the first session with CloseOnOutsideClick
    /// enabled. If the press falls inside that session's root OR inside the root of any session
    /// stacked at or above it (per <paramref name="isPressInsideRoot"/>), the press is treated as
    /// "inside" and nothing closes (returns null). Otherwise returns that candidate session.
    /// </summary>
    public static OverlaySession? FindOutsidePressTarget(
        IReadOnlyList<OverlaySession> stack,
        Func<OverlaySession, bool> isPressInsideRoot)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(isPressInsideRoot);

        for (var i = stack.Count - 1; i >= 0; i--)
        {
            if (!stack[i].Options.CloseOnOutsideClick)
            {
                continue;
            }

            for (var j = stack.Count - 1; j >= i; j--)
            {
                if (isPressInsideRoot(stack[j]))
                {
                    return null;
                }
            }

            return stack[i];
        }

        return null;
    }

    /// <summary>
    /// True only if RestoreFocus was requested and focus is still somewhere inside the overlay's
    /// subtree at close time (guards against stealing focus the user already moved elsewhere).
    /// </summary>
    public static bool ShouldRestoreFocus(bool restoreFocusOption, bool focusIsWithinOverlaySubtree)
        => restoreFocusOption && focusIsWithinOverlaySubtree;
}
