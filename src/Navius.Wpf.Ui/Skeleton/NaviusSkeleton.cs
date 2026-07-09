using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Ui.Internal;

namespace Navius.Wpf.Ui.Skeleton;

/// <summary>
/// A loading placeholder shaped like the content it stands in for (size it with Width/Height).
/// Pulses opacity slowly to read as "in progress" without implying determinate value, unlike
/// NaviusProgress. ShouldAnimate is read once at construction from ReducedMotion.AnimationsEnabled
/// (SystemParameters.ClientAreaAnimation, testable via ReducedMotion.SetTestOverride) so the
/// shimmer never runs when the OS "reduce animations" preference is on.
/// </summary>
public class NaviusSkeleton : Control
{
    private static readonly DependencyPropertyKey ShouldAnimatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShouldAnimate), typeof(bool), typeof(NaviusSkeleton), new PropertyMetadata(true));

    public static readonly DependencyProperty ShouldAnimateProperty = ShouldAnimatePropertyKey.DependencyProperty;

    static NaviusSkeleton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusSkeleton), new FrameworkPropertyMetadata(typeof(NaviusSkeleton)));
    }

    public NaviusSkeleton()
    {
        SetValue(ShouldAnimatePropertyKey, ReducedMotion.AnimationsEnabled);
    }

    /// <summary>True when the shimmer pulse should run, snapshotted from ReducedMotion.AnimationsEnabled at construction.</summary>
    public bool ShouldAnimate => (bool)GetValue(ShouldAnimateProperty);
}
