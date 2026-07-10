namespace Navius.Wpf.Primitives.Controls.Drawer;

/// <summary>
/// The edge a Drawer docks to and slides from. WPF equivalent of the web contract's free-form
/// <c>Side</c> string ("bottom" | "top" | "left" | "right"); an enum here per
/// docs/parity/drawer.md's WPF strategy open question ("needs a proper Dock/enum type").
/// </summary>
public enum NaviusDrawerSide
{
    Left,
    Right,
    Top,
    Bottom,
}
