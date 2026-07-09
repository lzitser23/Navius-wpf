using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Alert;

/// <summary>An alert's title. Inherits Foreground from the ancestor NaviusAlert, so it flips color with Variant automatically.</summary>
public class NaviusAlertTitle : ContentControl
{
    static NaviusAlertTitle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusAlertTitle), new FrameworkPropertyMetadata(typeof(NaviusAlertTitle)));
    }
}
