using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Navius.Wpf.Ui.ButtonGroup;

/// <summary>
/// Builds a rounded-rect clip geometry from a container's live size and a token CornerRadius read
/// off a sibling element (so the radius itself still comes from a normal DynamicResource-fed
/// DependencyProperty binding, not from the converter). Used to mask the group's square item
/// segments down to one continuous rounded silhouette.
/// </summary>
public sealed class RoundedClipConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [double width, double height, CornerRadius radius] || width <= 0 || height <= 0)
        {
            return null;
        }

        return new RectangleGeometry(new Rect(0, 0, width, height), radius.TopLeft, radius.TopLeft);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
