namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: the native <see cref="System.Windows.Controls.Separator"/> already renders and
/// behaves correctly inside a Menu's Items (role="separator" equivalent via
/// SeparatorAutomationPeer, and MenuItem/Menu already special-case it as "its own container"
/// alongside MenuItem). This subclass exists only so the family has its own strongly-named
/// type and default style key for XAML/token consistency with the rest of Controls/Menubar.
/// </summary>
public class NaviusMenubarSeparator : System.Windows.Controls.Separator
{
    static NaviusMenubarSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarSeparator),
            new System.Windows.FrameworkPropertyMetadata(typeof(NaviusMenubarSeparator)));
    }
}
