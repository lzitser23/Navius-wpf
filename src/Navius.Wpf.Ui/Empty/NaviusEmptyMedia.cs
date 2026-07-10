using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Empty;

/// <summary>Default | Icon. Icon renders a muted rounded tile sized for a glyph; Default is a bare, unstyled slot.</summary>
public enum NaviusEmptyMediaVariant
{
    Default,
    Icon,
}

/// <summary>The icon/illustration slot of a NaviusEmpty.</summary>
public class NaviusEmptyMedia : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusEmptyMediaVariant), typeof(NaviusEmptyMedia),
        new FrameworkPropertyMetadata(NaviusEmptyMediaVariant.Default));

    static NaviusEmptyMedia()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusEmptyMedia), new FrameworkPropertyMetadata(typeof(NaviusEmptyMedia)));
    }

    public NaviusEmptyMediaVariant Variant
    {
        get => (NaviusEmptyMediaVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
