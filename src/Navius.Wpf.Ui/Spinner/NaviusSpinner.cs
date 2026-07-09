using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Navius.Wpf.Ui.Internal;

namespace Navius.Wpf.Ui.Spinner;

/// <summary>
/// A rotating-arc loading indicator (indeterminate only; for a determinate progress bar use
/// Navius.Wpf.Primitives' NaviusProgress instead). ShouldAnimate is read once at construction
/// from ReducedMotion.AnimationsEnabled, same guard as NaviusSkeleton: when the OS "reduce
/// animations" preference is on, the arc renders statically instead of spinning forever.
/// </summary>
public class NaviusSpinner : Control
{
    private static readonly DependencyPropertyKey ShouldAnimatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShouldAnimate), typeof(bool), typeof(NaviusSpinner), new PropertyMetadata(true));

    public static readonly DependencyProperty ShouldAnimateProperty = ShouldAnimatePropertyKey.DependencyProperty;

    static NaviusSpinner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusSpinner), new FrameworkPropertyMetadata(typeof(NaviusSpinner)));
    }

    public NaviusSpinner()
    {
        SetValue(ShouldAnimatePropertyKey, ReducedMotion.AnimationsEnabled);
        AutomationProperties.SetName(this, "Loading");
    }

    /// <summary>True when the rotation should run, snapshotted from ReducedMotion.AnimationsEnabled at construction.</summary>
    public bool ShouldAnimate => (bool)GetValue(ShouldAnimateProperty);
}
