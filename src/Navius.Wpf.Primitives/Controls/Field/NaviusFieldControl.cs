using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Hosts the field-aware input. The ancestor NaviusField fills a default NaviusInput in when
/// Content is left null, and registers whatever ends up in Content (default or a
/// consumer-supplied custom control -- TextBox, ComboBox, PasswordBox, ...) as the field's
/// control. This replaces the web contract's ControlProps-cascading/@attributes-splat
/// pattern (Blazor-specific) with WPF's ordinary Content model, per field.md's own open
/// question that the splat pattern "has no direct WPF analog."
/// </summary>
public class NaviusFieldControl : ContentControl
{
    static NaviusFieldControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldControl), new FrameworkPropertyMetadata(typeof(NaviusFieldControl)));
    }
}
