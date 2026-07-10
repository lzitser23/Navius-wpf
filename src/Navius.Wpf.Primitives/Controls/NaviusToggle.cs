using System.Windows;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native ToggleButton. ToggleButton.IsChecked maps directly
/// onto the parity contract's Pressed (both plain bool, IsThreeState left false), and
/// ToggleButtonAutomationPeer already exposes UIA TogglePattern, matching the contract's
/// aria-pressed semantics. Checked/Unchecked/Click cover PressedChanged; no custom logic
/// is needed beyond the token-driven template (see Themes/Toggle.xaml).
/// </summary>
public class NaviusToggle : ToggleButton
{
    static NaviusToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToggle),
            new FrameworkPropertyMetadata(typeof(NaviusToggle)));
    }

    public NaviusToggle()
    {
        // Contract's Toggle is strictly two-state (Pressed: bool), unlike Checkbox's
        // tri-state CheckedState; keep the native three-state cycle disabled.
        IsThreeState = false;
    }
}
