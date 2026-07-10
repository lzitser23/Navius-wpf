using System.Windows;
using LiveChartsCore.SkiaSharpView.WPF;
using Navius.Wpf.Charts;

namespace Navius.Wpf.Charts.Tests;

/// <summary>
/// Smoke-tests the control itself (property changes rebuild, <c>RefreshTheme()</c> works, the
/// escape hatch tracks the current chart kind), on top of the pure mapping coverage in
/// <see cref="NaviusChartSeriesMapperTests"/>.
/// </summary>
public class NaviusChartTests
{
    static NaviusChartTests()
    {
        // pack://application URIs (LiveCharts' own default styles) only resolve once an
        // Application exists in the process; other Navius.Wpf test suites do the same.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static NaviusChart NewChart() => new()
    {
        Series =
        [
            new NaviusChartSeries { Key = "desktop", Label = "Desktop", Values = [186, 305, 237] },
            new NaviusChartSeries { Key = "mobile", Label = "Mobile", Values = [80, 200, 120] },
        ],
        Categories = ["Jan", "Feb", "Mar"],
    };

    [StaFact]
    public void Setting_kind_rebuilds_the_inner_chart_and_updates_the_escape_hatch()
    {
        var chart = NewChart();
        chart.Kind = NaviusChartKind.Line;

        Assert.IsType<CartesianChart>(chart.EscapeHatch);
        Assert.Single(chart.Children);

        chart.Kind = NaviusChartKind.Pie;

        Assert.IsType<PieChart>(chart.EscapeHatch);
        Assert.Single(chart.Children);
    }

    [StaFact]
    public void RefreshTheme_rebuilds_without_throwing_and_keeps_a_single_child()
    {
        var chart = NewChart();
        chart.Kind = NaviusChartKind.Bar;

        chart.RefreshTheme();
        chart.RefreshTheme();

        Assert.Single(chart.Children);
        Assert.IsType<CartesianChart>(chart.EscapeHatch);
    }

    [StaFact]
    public void Changing_series_rebuilds_the_chart()
    {
        var chart = NewChart();
        chart.Kind = NaviusChartKind.Area;
        var firstHatch = chart.EscapeHatch;

        chart.Series = [new NaviusChartSeries { Key = "only", Values = [1, 2, 3] }];

        Assert.NotSame(firstHatch, chart.EscapeHatch);
        Assert.Single(chart.Children);
    }
}
