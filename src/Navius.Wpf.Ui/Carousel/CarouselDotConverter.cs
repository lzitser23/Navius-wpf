using System;
using System.Globalization;
using System.Windows.Data;

namespace Navius.Wpf.Ui.Carousel;

/// <summary>Compares a dot's AlternationIndex to the carousel's SelectedIndex to decide whether it renders as the active dot.</summary>
public sealed class CarouselDotConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is [int alternationIndex, int selectedIndex])
        {
            return alternationIndex == selectedIndex;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Formats a zero-based slide index as a one-based accessible action name.</summary>
public sealed class CarouselSlideNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int index ? $"Slide {index + 1}" : "Slide";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
