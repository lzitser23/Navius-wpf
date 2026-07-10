using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Drawer;

/// <summary>
/// Pure geometry for the Drawer's slide-in/out translate animation, factored out of
/// <see cref="NaviusDrawer"/> so the per-side offscreen-offset mapping is unit-testable without a
/// live visual tree or Storyboard.
/// </summary>
public static class DrawerGeometry
{
    /// <summary>
    /// The (x, y) translation that places a panel of <paramref name="panelSize"/> fully offscreen
    /// past the edge given by <paramref name="side"/>. Animating a TranslateTransform from this
    /// vector to (0, 0) is the enter transition; the reverse is the exit transition.
    /// </summary>
    public static Vector GetOffscreenOffset(NaviusDrawerSide side, Size panelSize) => side switch
    {
        NaviusDrawerSide.Left => new Vector(-panelSize.Width, 0),
        NaviusDrawerSide.Right => new Vector(panelSize.Width, 0),
        NaviusDrawerSide.Top => new Vector(0, -panelSize.Height),
        NaviusDrawerSide.Bottom => new Vector(0, panelSize.Height),
        _ => new Vector(0, panelSize.Height),
    };
}
