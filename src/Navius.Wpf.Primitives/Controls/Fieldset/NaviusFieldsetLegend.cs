using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Fieldset;

/// <summary>
/// Free-floating legend content, deliberately not a native WPF GroupBox header so it can be
/// positioned anywhere inside the fieldset's ChildContent (matching the web contract's
/// "rendered as a div, not a legend, for positioning freedom"). Its disabled visual comes
/// for free from IsEnabled inheritance: it never needs its own Disabled property because it
/// always sits inside a NaviusFieldset's visual tree, which already cascades IsEnabled.
/// </summary>
public class NaviusFieldsetLegend : ContentControl
{
    static NaviusFieldsetLegend()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusFieldsetLegend),
            new FrameworkPropertyMetadata(typeof(NaviusFieldsetLegend)));
    }
}
