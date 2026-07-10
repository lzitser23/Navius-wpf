using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Supplementary field text. Styling for the field's discrete state (invalid/disabled/...)
/// is done in Themes/Field.xaml via a RelativeSource binding to the ancestor NaviusField's
/// read-only state DPs, rather than duplicating those DPs onto every part the way the web
/// contract's MergeState duplicates data-* attributes onto every FieldPart.
/// </summary>
public class NaviusFieldDescription : ContentControl
{
    static NaviusFieldDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldDescription), new FrameworkPropertyMetadata(typeof(NaviusFieldDescription)));
    }
}
