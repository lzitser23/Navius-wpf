using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Kbd;

/// <summary>A single keyboard key/shortcut glyph, e.g. inside a NaviusKbdGroup for a chord like Ctrl+Shift+P.</summary>
public class NaviusKbd : ContentControl
{
    static NaviusKbd()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusKbd), new FrameworkPropertyMetadata(typeof(NaviusKbd)));
    }
}
