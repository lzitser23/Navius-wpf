using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>A NaviusCard's title, styled with weight/size hierarchy (no color).</summary>
public class NaviusCardTitle : ContentControl
{
    static NaviusCardTitle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCardTitle), new FrameworkPropertyMetadata(typeof(NaviusCardTitle)));
    }
}
