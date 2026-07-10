using System.Windows;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: a nested submenu root placed inside a Content panel. Renders as a plain
/// <see cref="System.Windows.Controls.ContentControl"/> (not re-using NaviusNavigationMenu's
/// root chrome, matching the contract's "NOT &lt;nav&gt;" distinction) but reuses the exact same
/// List/Item/Trigger/Content parts recursively: it establishes its own
/// <see cref="NavigationMenuHostBase.HostProperty"/> scope via DP-inheritance shadowing, so
/// descendants automatically bind to this Sub instead of the ancestor root/Sub.
/// </summary>
public class NaviusNavigationMenuSub : NavigationMenuHostBase
{
    static NaviusNavigationMenuSub()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuSub),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuSub)));
    }

    public NaviusNavigationMenuSub()
    {
        // Sub menus default to vertical (root defaults horizontal).
        Orientation = "vertical";
    }
}
