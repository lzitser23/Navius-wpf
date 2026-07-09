using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native Button (inheriting its AutomationPeer and
/// keyboard behavior) and supplies a token-driven default template.
/// </summary>
public class NaviusButton : Button
{
    static NaviusButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusButton),
            new FrameworkPropertyMetadata(typeof(NaviusButton)));
    }
}
