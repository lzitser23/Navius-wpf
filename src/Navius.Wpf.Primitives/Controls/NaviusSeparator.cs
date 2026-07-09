using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B: a small lookless Control, since WPF's stock <see cref="Separator"/> has no
/// standalone Orientation of its own (it visually adapts to a hosting ItemsControl instead).
/// Orientation is a strongly-typed enum here, which structurally rules out the invalid
/// "diagonal"-style tokens the parity contract's isValidOrientation guard exists to catch.
/// </summary>
public class NaviusSeparator : Control
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(NaviusSeparator),
        new FrameworkPropertyMetadata(Orientation.Horizontal));

    public static readonly DependencyProperty DecorativeProperty = DependencyProperty.Register(
        nameof(Decorative), typeof(bool), typeof(NaviusSeparator),
        new FrameworkPropertyMetadata(false));

    static NaviusSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSeparator),
            new FrameworkPropertyMetadata(typeof(NaviusSeparator)));
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>When true, the separator is purely visual and removed from the accessibility tree.</summary>
    public bool Decorative
    {
        get => (bool)GetValue(DecorativeProperty);
        set => SetValue(DecorativeProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSeparatorAutomationPeer(this);
}
