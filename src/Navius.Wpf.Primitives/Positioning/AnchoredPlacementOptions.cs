namespace Navius.Wpf.Primitives.Positioning;

/// <summary>
/// Input parameters for <see cref="PlacementMath.Place"/>: the requested side and
/// alignment of a popup relative to its anchor, plus offsets and collision-handling
/// toggles. Mirrors the Side/Align/SideOffset/AlignOffset/Flip/AvoidCollisions
/// vocabulary used by the Popover/Overlays parity docs.
/// </summary>
public sealed class AnchoredPlacementOptions
{
    public PlacementSide Side { get; set; } = PlacementSide.Bottom;

    public PlacementAlign Align { get; set; } = PlacementAlign.Center;

    /// <summary>Distance, along the placement axis, between the anchor and the popup.</summary>
    public double SideOffset { get; set; }

    /// <summary>Distance, along the alignment axis, added after alignment is resolved.</summary>
    public double AlignOffset { get; set; }

    /// <summary>When the popup does not fit on <see cref="Side"/>, flip to the opposite side if it has more room.</summary>
    public bool FlipEnabled { get; set; } = true;

    /// <summary>Shift the popup along the alignment axis by the minimal amount needed to stay inside the work area.</summary>
    public bool ShiftEnabled { get; set; } = true;

    /// <summary>Arrow glyph size in device-independent pixels; 0 disables arrow-offset computation.</summary>
    public double ArrowSize { get; set; }
}
