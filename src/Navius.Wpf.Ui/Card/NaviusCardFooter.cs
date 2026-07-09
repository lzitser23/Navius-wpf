using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>The bottom block of a NaviusCard, typically holding a row of actions.</summary>
public class NaviusCardFooter : ContentControl
{
    static NaviusCardFooter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCardFooter), new FrameworkPropertyMetadata(typeof(NaviusCardFooter)));
    }
}
