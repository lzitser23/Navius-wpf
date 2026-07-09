using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Item;

/// <summary>Default | Icon | Image.</summary>
public enum NaviusItemMediaVariant
{
    Default,
    Icon,
    Image,
}

/// <summary>The leading slot (icon / image / avatar) of a NaviusItem.</summary>
public class NaviusItemMedia : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusItemMediaVariant), typeof(NaviusItemMedia),
        new FrameworkPropertyMetadata(NaviusItemMediaVariant.Default));

    static NaviusItemMedia()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusItemMedia), new FrameworkPropertyMetadata(typeof(NaviusItemMedia)));
    }

    public NaviusItemMediaVariant Variant
    {
        get => (NaviusItemMediaVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
