using System.Globalization;
using System.Windows.Data;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>Binds NaviusTreeNode.Disabled (true = cannot be selected/navigated) to the container's native IsEnabled (true = enabled), the inverse.</summary>
public sealed class BoolInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value!;
}
