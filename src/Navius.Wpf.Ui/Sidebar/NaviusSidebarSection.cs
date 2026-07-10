using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Sidebar;

/// <summary>
/// A labeled group of <see cref="NaviusSidebarItem"/> rows. Reuses WPF's own
/// <see cref="HeaderedItemsControl"/> directly (Header = the section label, Items = its rows)
/// rather than a bespoke type, since that is exactly this anatomy.
/// </summary>
public class NaviusSidebarSection : HeaderedItemsControl
{
    static NaviusSidebarSection()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSidebarSection),
            new FrameworkPropertyMetadata(typeof(NaviusSidebarSection)));
    }
}
