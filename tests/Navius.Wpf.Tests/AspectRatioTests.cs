using System.Windows;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class AspectRatioTests
{
    static AspectRatioTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    // --- NaviusAspectRatioMath: pure measure math ---

    [Fact]
    public void Math_EffectiveRatio_PassesThroughPositiveValues()
    {
        Assert.Equal(2.0, NaviusAspectRatioMath.EffectiveRatio(2.0));
    }

    [Fact]
    public void Math_EffectiveRatio_FallsBackToOneForZeroOrNegative()
    {
        Assert.Equal(1.0, NaviusAspectRatioMath.EffectiveRatio(0));
        Assert.Equal(1.0, NaviusAspectRatioMath.EffectiveRatio(-3));
    }

    [Fact]
    public void Math_ComputeDesiredSize_WidthDrivesHeight()
    {
        var size = NaviusAspectRatioMath.ComputeDesiredSize(new Size(200, double.PositiveInfinity), ratio: 2.0);

        Assert.Equal(200, size.Width);
        Assert.Equal(100, size.Height);
    }

    [Fact]
    public void Math_ComputeDesiredSize_SquareRatioIsEqualWidthHeight()
    {
        var size = NaviusAspectRatioMath.ComputeDesiredSize(new Size(150, double.PositiveInfinity), ratio: 1.0);

        Assert.Equal(150, size.Width);
        Assert.Equal(150, size.Height);
    }

    [Fact]
    public void Math_ComputeDesiredSize_FallsBackToHeightWhenWidthInfinite()
    {
        var size = NaviusAspectRatioMath.ComputeDesiredSize(new Size(double.PositiveInfinity, 50), ratio: 2.0);

        Assert.Equal(100, size.Width);
        Assert.Equal(50, size.Height);
    }

    [Fact]
    public void Math_ComputeDesiredSize_CollapsesToZeroWhenBothInfinite()
    {
        var size = NaviusAspectRatioMath.ComputeDesiredSize(
            new Size(double.PositiveInfinity, double.PositiveInfinity), ratio: 1.0);

        Assert.Equal(0, size.Width);
        Assert.Equal(0, size.Height);
    }

    [Fact]
    public void Math_ComputeDesiredSize_NonPositiveRatioFallsBackToSquare()
    {
        var size = NaviusAspectRatioMath.ComputeDesiredSize(new Size(80, double.PositiveInfinity), ratio: 0);

        Assert.Equal(80, size.Width);
        Assert.Equal(80, size.Height);
    }

    // --- NaviusAspectRatio: control-level defaults ---

    [StaFact]
    public void DefaultState_RatioIsOne()
    {
        var control = new NaviusAspectRatio();

        Assert.Equal(1.0, control.Ratio);
        Assert.Equal(1.0, control.EffectiveRatio);
    }

    [StaFact]
    public void EffectiveRatio_FallsBackWhenRatioIsNonPositive()
    {
        var control = new NaviusAspectRatio { Ratio = -1 };

        Assert.Equal(1.0, control.EffectiveRatio);
    }

    [StaFact]
    public void EffectiveRatio_ReflectsSetRatio()
    {
        var control = new NaviusAspectRatio { Ratio = 16.0 / 9 };

        Assert.Equal(16.0 / 9, control.EffectiveRatio);
    }

    [StaFact]
    public void MeasureOverride_ComputesHeightFromWidthAndRatio()
    {
        var control = new NaviusAspectRatio { Ratio = 2.0 };

        control.Measure(new Size(400, double.PositiveInfinity));

        Assert.Equal(400, control.DesiredSize.Width);
        Assert.Equal(200, control.DesiredSize.Height);
    }
}
