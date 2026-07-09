using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Alert;

/// <summary>
/// An alert's supplementary text. Kept a constant MutedForeground regardless of Variant
/// (a deliberate simplification from the web contract, which flips description color with
/// severity too): a muted secondary line reads clearly against either the default or
/// destructive title color without adding a second Destructive-text-on-Destructive-title
/// legibility question.
/// </summary>
public class NaviusAlertDescription : ContentControl
{
    static NaviusAlertDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusAlertDescription), new FrameworkPropertyMetadata(typeof(NaviusAlertDescription)));
    }
}
