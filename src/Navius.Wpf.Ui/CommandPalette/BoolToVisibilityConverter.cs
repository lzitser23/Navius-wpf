using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Navius.Wpf.Ui.CommandPalette;

/// <summary>Plain bool-to-Visibility, exposed as a static instance so Themes/CommandPalette.xaml can reference it via x:Static instead of declaring a resource.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Shows the target (the search placeholder) only while the bound string is null/empty.</summary>
public sealed class EmptyStringToVisibilityConverter : IValueConverter
{
    public static readonly EmptyStringToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
