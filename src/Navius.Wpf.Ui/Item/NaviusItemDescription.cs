using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Item;

/// <summary>The muted description line of a NaviusItem's NaviusItemContent.</summary>
public class NaviusItemDescription : ContentControl
{
    static NaviusItemDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusItemDescription), new FrameworkPropertyMetadata(typeof(NaviusItemDescription)));
    }
}
