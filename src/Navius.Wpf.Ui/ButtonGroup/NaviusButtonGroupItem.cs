using System.Windows;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Ui.ButtonGroup;

/// <summary>
/// A single segment of a <see cref="NaviusButtonGroup"/>. Derives from ButtonBase directly (not
/// Navius.Wpf.Primitives' NaviusButton) because it needs its own bindable CornerRadius, which the
/// primitive's template hardcodes to the full token radius; see Themes/ButtonGroup.xaml for how
/// <see cref="NaviusButtonGroup.IsFirstItemProperty"/>/<see cref="NaviusButtonGroup.IsLastItemProperty"/>
/// mask that radius down to just the group's outer edge on each item.
/// </summary>
public class NaviusButtonGroupItem : ButtonBase
{
    static NaviusButtonGroupItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusButtonGroupItem),
            new FrameworkPropertyMetadata(typeof(NaviusButtonGroupItem)));
    }
}
