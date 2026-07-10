using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Item;

/// <summary>The title line of a NaviusItem's NaviusItemContent.</summary>
public class NaviusItemTitle : ContentControl
{
    static NaviusItemTitle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusItemTitle), new FrameworkPropertyMetadata(typeof(NaviusItemTitle)));
    }
}
