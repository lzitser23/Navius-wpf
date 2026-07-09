using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Item;

/// <summary>Default | Outline | Muted.</summary>
public enum NaviusItemVariant
{
    Default,
    Outline,
    Muted,
}

/// <summary>Default | Small.</summary>
public enum NaviusItemSize
{
    Default,
    Small,
}

/// <summary>
/// A list row: leading media, a title+description block, and trailing content, laid out by
/// nesting NaviusItemMedia / NaviusItemContent (with NaviusItemTitle + NaviusItemDescription
/// inside) / trailing content in a horizontal StackPanel as the single Content.
/// </summary>
public class NaviusItem : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusItemVariant), typeof(NaviusItem),
        new FrameworkPropertyMetadata(NaviusItemVariant.Default));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(NaviusItemSize), typeof(NaviusItem),
        new FrameworkPropertyMetadata(NaviusItemSize.Default));

    static NaviusItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusItem), new FrameworkPropertyMetadata(typeof(NaviusItem)));
    }

    public NaviusItemVariant Variant
    {
        get => (NaviusItemVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public NaviusItemSize Size
    {
        get => (NaviusItemSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
}
