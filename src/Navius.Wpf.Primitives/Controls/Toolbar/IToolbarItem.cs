namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Marker for the Control types NaviusToolbar's roving-focus scan picks up: NaviusToolbarButton,
/// NaviusToolbarLink, and NaviusToolbarToggleItem (but not NaviusSeparator or
/// NaviusToolbarToggleGroup itself). Mirrors the web contract's shared
/// `[data-navius-toolbar-item]` selector -- a single flat marker across otherwise-unrelated
/// control types (Button, Button-derived link, ToggleButton) is exactly what lets one roving
/// scan treat them "indiscriminately" per docs/parity/toolbar.md's keyboard table, including
/// items nested inside a NaviusToolbarToggleGroup (LogicalTreeWalker.Descendants recurses through
/// that nesting for free).
/// </summary>
public interface IToolbarItem
{
}
