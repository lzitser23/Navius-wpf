using System;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// App-wide shared timing state for <see cref="NaviusTooltip"/>, mirroring the web contract's
/// TooltipProviderContext (DelayDuration/SkipDelayDuration) as a static, WPF-<c>ToolTipService</c>-like
/// service rather than a cascading provider component, since WPF has no equivalent to a Blazor
/// cascading-parameter tree. There is deliberately no "provider scope": all NaviusTooltip instances
/// in the process share one delay and one skip-delay grace window, same as ToolTipService.
/// </summary>
public static class NaviusTooltipService
{
    /// <summary>Default hover-intent delay (ms) before a tooltip opens. Web default: 700.</summary>
    public static int DelayDuration { get; set; } = 700;

    /// <summary>
    /// Grace window (ms) after any tooltip closes during which the next tooltip to open skips
    /// its delay entirely. Web default: 300.
    /// </summary>
    public static int SkipDelayDuration { get; set; } = 300;

    private static DateTime? _lastClosedUtc;

    /// <summary>True if a tooltip closed recently enough that the next one should open instantly.</summary>
    public static bool ShouldSkipDelay() =>
        _lastClosedUtc is { } closedAt && (DateTime.UtcNow - closedAt).TotalMilliseconds <= SkipDelayDuration;

    /// <summary>Records that a tooltip just closed, starting (or restarting) the skip-delay window.</summary>
    public static void NotifyClosed() => _lastClosedUtc = DateTime.UtcNow;

    /// <summary>Test/reset hook: clears the skip-delay window.</summary>
    public static void Reset() => _lastClosedUtc = null;
}
