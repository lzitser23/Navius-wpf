using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Empty;

/// <summary>The title of a NaviusEmpty state, e.g. "No results".</summary>
public class NaviusEmptyTitle : ContentControl
{
    static NaviusEmptyTitle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusEmptyTitle), new FrameworkPropertyMetadata(typeof(NaviusEmptyTitle)));
    }
}
