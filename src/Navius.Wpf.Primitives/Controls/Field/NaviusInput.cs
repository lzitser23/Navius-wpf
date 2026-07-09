using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Tier A: the field-aware native input. The web contract's Value/DefaultValue
/// controlled/uncontrolled pair and its "on every native oninput" ValueChanged collapse
/// directly onto WPF's own TextBox.Text dependency property, which already supports
/// two-way Binding with UpdateSourceTrigger=PropertyChanged -- reinventing a parallel
/// Value/DefaultValue pair here would just duplicate what Text already does natively.
/// The ancestor NaviusField registers this control (WireDescendants), so this class stays a
/// thin, directly themable TextBox with no wiring of its own.
/// </summary>
public class NaviusInput : TextBox
{
    static NaviusInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusInput), new FrameworkPropertyMetadata(typeof(NaviusInput)));
    }
}
