using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>Top padding block of a NaviusCard; nest NaviusCardTitle/NaviusCardDescription inside a StackPanel.</summary>
public class NaviusCardHeader : ContentControl
{
    static NaviusCardHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCardHeader), new FrameworkPropertyMetadata(typeof(NaviusCardHeader)));
    }
}
