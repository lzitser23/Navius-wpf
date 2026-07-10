using System.Windows;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure measure math backing NaviusAspectRatio (see docs/parity/aspect-ratio.md "WPF strategy").
/// The web contract enforces the ratio via the CSS padding-bottom percentage hack, driven purely
/// by available width; this is the width-driven equivalent for WPF's MeasureOverride, factored
/// out so it is unit-testable without a live Control/Application.
/// </summary>
public static class NaviusAspectRatioMath
{
    /// <summary>EffectiveRatio: falls back to 1.0 for non-positive Ratio, mirroring the contract.</summary>
    public static double EffectiveRatio(double ratio) => ratio > 0 ? ratio : 1.0;

    /// <summary>
    /// Computes the box NaviusAspectRatio should measure/report as its DesiredSize, given the
    /// available size and Ratio. Width drives height (Width/EffectiveRatio) whenever a finite
    /// width is available -- the direct analog of the contract's width:100%;padding-bottom:%
    /// hack. When width is infinite (e.g. hosted in a horizontally-unconstrained panel) but
    /// height is finite, height instead drives width (Height*EffectiveRatio). When both are
    /// infinite there is nothing to constrain against, so the box collapses to zero.
    /// </summary>
    public static Size ComputeDesiredSize(Size availableSize, double ratio)
    {
        var effectiveRatio = EffectiveRatio(ratio);

        if (!double.IsPositiveInfinity(availableSize.Width))
        {
            var width = Math.Max(0, availableSize.Width);
            return new Size(width, width / effectiveRatio);
        }

        if (!double.IsPositiveInfinity(availableSize.Height))
        {
            var height = Math.Max(0, availableSize.Height);
            return new Size(height * effectiveRatio, height);
        }

        return new Size(0, 0);
    }
}
