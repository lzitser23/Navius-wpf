using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>Default | Outline | Secondary | Destructive | Muted.</summary>
public enum NaviusTimelineDotVariant
{
    Default,
    Outline,
    Secondary,
    Destructive,
    Muted,
}

/// <summary>The node marker on a NaviusTimeline's rail.</summary>
public class NaviusTimelineDot : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusTimelineDotVariant), typeof(NaviusTimelineDot),
        new FrameworkPropertyMetadata(NaviusTimelineDotVariant.Default));

    static NaviusTimelineDot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineDot), new FrameworkPropertyMetadata(typeof(NaviusTimelineDot)));
    }

    public NaviusTimelineDotVariant Variant
    {
        get => (NaviusTimelineDotVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
