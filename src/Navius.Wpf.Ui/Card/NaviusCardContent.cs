using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>The body block of a NaviusCard, padded to align with the header/footer.</summary>
public class NaviusCardContent : ContentControl
{
    static NaviusCardContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCardContent), new FrameworkPropertyMetadata(typeof(NaviusCardContent)));
    }
}
