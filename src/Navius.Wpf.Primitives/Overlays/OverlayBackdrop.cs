using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// Tier A: lightweight token-driven scrim (translucent Navius.Foreground fill). Consumers place
/// this under a modal overlay's content themselves; the overlay stack does not create, own, or
/// reference this control. See Themes/OverlayBackdrop.xaml for the default style.
/// </summary>
public class OverlayBackdrop : Control
{
    static OverlayBackdrop()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(OverlayBackdrop),
            new FrameworkPropertyMetadata(typeof(OverlayBackdrop)));
    }
}
