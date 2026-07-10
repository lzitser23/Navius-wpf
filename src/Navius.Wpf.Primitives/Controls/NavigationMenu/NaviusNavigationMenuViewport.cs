using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Stub: only meaningful in the shared/morphing-viewport layout, which this M2 port does not
/// implement (see <see cref="NaviusNavigationMenu.UseSharedViewport"/>). Present for API-surface
/// parity only; in per-item popup mode (the only mode this port supports) it renders nothing and
/// no Content panel teleports into it.
/// </summary>
public class NaviusNavigationMenuViewport : ContentControl
{
    public static readonly DependencyProperty ForceMountProperty = DependencyProperty.Register(
        nameof(ForceMount), typeof(bool), typeof(NaviusNavigationMenuViewport),
        new PropertyMetadata(false));

    static NaviusNavigationMenuViewport()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuViewport),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuViewport)));
    }

    /// <summary>Accepted for API parity; has no effect since shared-viewport mode is not implemented.</summary>
    public bool ForceMount
    {
        get => (bool)GetValue(ForceMountProperty);
        set => SetValue(ForceMountProperty, value);
    }
}
