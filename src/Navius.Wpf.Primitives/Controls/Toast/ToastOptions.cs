using System;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Per-toast configuration passed to <see cref="ToastManager.Add"/> and
/// <see cref="ToastManager.Update"/>. Mirrors the union of NaviusToastRoot's Toast-driven
/// parameters (Title/Description/Type/Priority/Timeout/Action) from the web contract.
/// </summary>
public sealed record ToastOptions
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public ToastType Type { get; init; } = ToastType.Default;

    /// <summary>Low -> role="status" (polite); High -> role="alert" (assertive).</summary>
    public ToastPriority Priority { get; init; } = ToastPriority.Low;

    /// <summary>
    /// Auto-dismiss window. Null uses <see cref="ToastManager.DefaultDuration"/>; TimeSpan.Zero
    /// (or any non-positive value) is sticky (no auto-dismiss), matching the contract's
    /// Timeout=0 convention.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    public ToastActionSpec? Action { get; init; }
}
