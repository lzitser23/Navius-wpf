using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Empty;

/// <summary>The supplementary text of a NaviusEmpty state, e.g. "Try a different search."</summary>
public class NaviusEmptyDescription : ContentControl
{
    static NaviusEmptyDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusEmptyDescription), new FrameworkPropertyMetadata(typeof(NaviusEmptyDescription)));
    }
}
