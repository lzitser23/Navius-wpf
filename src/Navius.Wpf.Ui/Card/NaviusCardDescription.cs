using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>A NaviusCard's supplementary text, muted.</summary>
public class NaviusCardDescription : ContentControl
{
    static NaviusCardDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCardDescription), new FrameworkPropertyMetadata(typeof(NaviusCardDescription)));
    }
}
