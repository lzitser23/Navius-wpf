using System.Windows;
using System.Windows.Controls;

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
