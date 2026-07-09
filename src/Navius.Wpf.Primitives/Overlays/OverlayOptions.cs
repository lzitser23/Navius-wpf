namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// Per-overlay behavior flags passed to <see cref="OverlayStack.Push"/>.
/// Mirrors the web's OverlayPopupBase protected members (CloseOnEscape,
/// CloseOnOutside, TrapFocus) plus a WPF-specific RestoreFocus toggle.
/// </summary>
public sealed record OverlayOptions
{
    /// <summary>When true, the overlay is exposed as modal on its session (see <see cref="OverlaySession"/>).</summary>
    public bool Modal { get; init; }

    /// <summary>When true (default), Escape can close this overlay. Web default: true.</summary>
    public bool CloseOnEscape { get; init; } = true;

    /// <summary>When true, a press outside the overlay root can close it. Web default: true (Alert Dialog overrides to false).</summary>
    public bool CloseOnOutsideClick { get; init; }

    /// <summary>When true, Tab/Shift+Tab cycle within the overlay root and focus moves in on push.</summary>
    public bool TrapFocus { get; init; }

    /// <summary>When true (default), focus returns to the element focused before push, if still inside the overlay subtree at close time.</summary>
    public bool RestoreFocus { get; init; } = true;
}
