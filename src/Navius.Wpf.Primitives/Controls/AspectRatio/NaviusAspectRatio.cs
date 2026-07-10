using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: a ContentControl enforcing a fixed width:height ratio via MeasureOverride, the
/// behavioral port of the contract's outer/inner-div padding-bottom percentage hack (see
/// docs/parity/aspect-ratio.md "WPF strategy"). There is no CSS cascade to replicate in WPF, so
/// the single child simply fills the computed box via the default ContentPresenter arrange
/// rather than needing a separate inner element. The width-vs-height-driven measure math itself
/// lives in NaviusAspectRatioMath so it is unit-testable without a live Control.
/// </summary>
public class NaviusAspectRatio : ContentControl
{
    public static readonly DependencyProperty RatioProperty = DependencyProperty.Register(
        nameof(Ratio), typeof(double), typeof(NaviusAspectRatio), new PropertyMetadata(1.0));

    static NaviusAspectRatio()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAspectRatio), new FrameworkPropertyMetadata(typeof(NaviusAspectRatio)));
    }

    /// <summary>Desired width:height ratio. EffectiveRatio falls back to 1.0 when Ratio &lt;= 0.</summary>
    public double Ratio
    {
        get => (double)GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }

    public double EffectiveRatio => NaviusAspectRatioMath.EffectiveRatio(Ratio);

    protected override Size MeasureOverride(Size constraint)
    {
        var desired = NaviusAspectRatioMath.ComputeDesiredSize(constraint, Ratio);
        base.MeasureOverride(desired);
        return desired;
    }
}
