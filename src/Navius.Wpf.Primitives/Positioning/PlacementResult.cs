using System.Windows;

namespace Navius.Wpf.Primitives.Positioning;

/// <summary>
/// Result of <see cref="PlacementMath.Place"/>.
/// </summary>
/// <param name="Origin">Top-left of the popup, in the same coordinate space as the inputs.</param>
/// <param name="EffectiveSide">The side actually used, after flip resolution.</param>
/// <param name="Align">The alignment used (unchanged from the request; flip/shift never alter it).</param>
/// <param name="ArrowOffset">
/// When arrow computation is enabled, the point (local to the popup's top-left) where an
/// arrow glyph of the requested size should be drawn so its center targets the anchor's
/// center, clamped to stay within the popup's facing edge. Null when arrow computation is disabled.
/// </param>
public readonly record struct PlacementResult(
    Point Origin,
    PlacementSide EffectiveSide,
    PlacementAlign Align,
    Point? ArrowOffset);
