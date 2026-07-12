using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Navius.Wpf.Ui.Badge;

/// <summary>Default | Secondary | Outline | Destructive.</summary>
public enum NaviusBadgeVariant
{
    Default,
    Secondary,
    Outline,
    Destructive,
}

/// <summary>A small pill-shaped status/count label.</summary>
public class NaviusBadge : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusBadgeVariant), typeof(NaviusBadge),
        new FrameworkPropertyMetadata(NaviusBadgeVariant.Default));

    static NaviusBadge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusBadge), new FrameworkPropertyMetadata(typeof(NaviusBadge)));
    }

    public NaviusBadgeVariant Variant
    {
        get => (NaviusBadgeVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}

/// <summary>
/// Halves a bound ActualHeight into a capsule CornerRadius, so the badge's rounded ends track its
/// rendered size instead of a fixed pixel constant. Exactly half the height keeps the horizontal
/// sides straight (WPF only normalizes a radius that EXCEEDS half a dimension into an ellipse).
/// </summary>
public sealed class NaviusPillRadiusConverter : IValueConverter
{
    public static NaviusPillRadiusConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double height && height > 0 ? new CornerRadius(height / 2) : new CornerRadius(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
