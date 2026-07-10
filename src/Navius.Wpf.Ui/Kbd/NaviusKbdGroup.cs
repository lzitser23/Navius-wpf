using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Kbd;

/// <summary>
/// Lays out a chord of NaviusKbd items horizontally with a small gap (e.g. Ctrl + Shift + P).
/// An ItemsControl rather than a fixed two/three-slot control, so any number of keys works.
/// </summary>
public class NaviusKbdGroup : ItemsControl
{
    static NaviusKbdGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusKbdGroup), new FrameworkPropertyMetadata(typeof(NaviusKbdGroup)));
    }
}
