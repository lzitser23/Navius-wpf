using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Card;

/// <summary>
/// Card root: a hairline-bordered surface (no shadow, per Navius one-ink discipline).
/// Compositional, matching the web contract's div-based parts: nest NaviusCardHeader,
/// NaviusCardContent and NaviusCardFooter directly, or use the root alone for a plain panel.
/// </summary>
public class NaviusCard : ContentControl
{
    static NaviusCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusCard), new FrameworkPropertyMetadata(typeof(NaviusCard)));
    }
}
