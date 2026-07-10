using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Item;

/// <summary>
/// The vertical title+description column of a NaviusItem, between the leading media and
/// trailing content. A StackPanel subclass (fixed to Vertical) rather than a ContentControl, so
/// NaviusItemTitle and NaviusItemDescription can be nested directly as siblings.
/// </summary>
public class NaviusItemContent : StackPanel
{
    static NaviusItemContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusItemContent), new FrameworkPropertyMetadata(typeof(NaviusItemContent)));
    }

    public NaviusItemContent()
    {
        Orientation = Orientation.Vertical;
    }
}
