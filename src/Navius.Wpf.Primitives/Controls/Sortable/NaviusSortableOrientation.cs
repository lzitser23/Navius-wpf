namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// Mirrors the contract's SortableOrientation. Only affects pointer-drag midpoint/nearest-cell
/// math; keyboard navigation stays linear (next/prev) for every value, including <see cref="Grid"/>.
/// </summary>
public enum NaviusSortableOrientation
{
    Vertical,
    Horizontal,
    Grid,
}
