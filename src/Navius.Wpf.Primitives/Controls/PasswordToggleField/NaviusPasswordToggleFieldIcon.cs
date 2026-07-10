using System.Windows;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>Swaps between VisibleContent and HiddenContent as the field's revealed state changes.</summary>
public class NaviusPasswordToggleFieldIcon : PasswordToggleFieldSlotBase
{
    static NaviusPasswordToggleFieldIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPasswordToggleFieldIcon),
            new FrameworkPropertyMetadata(typeof(NaviusPasswordToggleFieldIcon)));
    }
}
