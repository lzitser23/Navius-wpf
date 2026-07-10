using System.Windows.Media;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Navius.Wpf.Charts;
using Navius.Wpf.Charts.Theming;

namespace Navius.Wpf.Charts.Tests;

/// <summary>
/// Covers the internal series-mapping logic <c>NaviusChart</c> delegates to, plus the
/// re-theme reapplication path: <c>NaviusChart.RefreshTheme()</c> just re-resolves the tokens
/// (see <see cref="NaviusChartTheme"/>) and re-runs this same mapping, so calling the mapper
/// twice with two different themes exercises exactly what a runtime theme swap does.
/// </summary>
public class NaviusChartSeriesMapperTests
{
    private static readonly NaviusChartTheme LightTheme = new()
    {
        Ink = Color.FromRgb(0x17, 0x16, 0x14),
        MutedForeground = Color.FromRgb(0x73, 0x72, 0x70),
        Muted = Color.FromRgb(0xF1, 0xF1, 0xF0),
        Destructive = Color.FromRgb(0xE7, 0x00, 0x0B),
        Border = Color.FromRgb(0xE6, 0xE4, 0xDE),
        Background = Color.FromRgb(0xF4, 0xF3, 0xF0),
    };

    private static readonly NaviusChartTheme DarkTheme = new()
    {
        Ink = Color.FromRgb(0xF4, 0xF3, 0xF0),
        MutedForeground = Color.FromRgb(0xA1, 0xA1, 0x9E),
        Muted = Color.FromRgb(0x27, 0x27, 0x25),
        Destructive = Color.FromRgb(0xFF, 0x63, 0x67),
        Border = Color.FromRgb(0x2E, 0x2D, 0x2A),
        Background = Color.FromRgb(0x17, 0x16, 0x14),
    };

    private static NaviusChartSeries[] TwoSeries() =>
    [
        new NaviusChartSeries { Key = "desktop", Label = "Desktop", Values = [186, 305, 237] },
        new NaviusChartSeries { Key = "mobile", Label = "Mobile", Values = [80, 200, 120] },
    ];

    [Fact]
    public void BuildCartesianSeries_maps_one_series_per_input_for_line_and_area()
    {
        foreach (var kind in new[] { NaviusChartKind.Line, NaviusChartKind.Area })
        {
            var built = NaviusChartSeriesMapper.BuildCartesianSeries(kind, TwoSeries(), LightTheme);

            Assert.Equal(2, built.Count);
            Assert.All(built, s => Assert.IsAssignableFrom<LineSeries<double>>(s));
            Assert.Equal("Desktop", built[0].Name);
            Assert.Equal("Mobile", built[1].Name);
        }
    }

    [Fact]
    public void BuildCartesianSeries_first_series_uses_the_ink_colour()
    {
        var built = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Line, TwoSeries(), LightTheme);

        var stroke = Assert.IsType<SolidColorPaint>(((LineSeries<double>)built[0]).Stroke);
        Assert.Equal(LightTheme.Ink.R, stroke.Color.Red);
        Assert.Equal(LightTheme.Ink.G, stroke.Color.Green);
        Assert.Equal(LightTheme.Ink.B, stroke.Color.Blue);
    }

    [Fact]
    public void BuildCartesianSeries_area_kind_sets_a_fill_and_flat_geometry()
    {
        var built = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Area, TwoSeries(), LightTheme);

        var area = Assert.IsType<LineSeries<double>>(built[0]);
        Assert.NotNull(area.Fill);
        Assert.Equal(0, area.GeometrySize);
    }

    [Fact]
    public void BuildCartesianSeries_bar_kind_without_negative_values_is_one_series()
    {
        var built = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Bar, TwoSeries(), LightTheme);

        Assert.Equal(2, built.Count);
        Assert.All(built, s => Assert.IsAssignableFrom<ColumnSeries<double>>(s));
    }

    [Fact]
    public void BuildCartesianSeries_bar_kind_with_negative_values_splits_into_a_destructive_series()
    {
        var series = new[] { new NaviusChartSeries { Key = "net", Label = "Net", Values = [12, -5, 8, -3] } };

        var built = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Bar, series, LightTheme);

        Assert.Equal(2, built.Count);
        var positive = Assert.IsType<ColumnSeries<double?>>(built[0]);
        var negative = Assert.IsType<ColumnSeries<double?>>(built[1]);

        Assert.Equal(new double?[] { 12, null, 8, null }, positive.Values);
        Assert.Equal(new double?[] { null, -5, null, -3 }, negative.Values);

        var negativeFill = Assert.IsType<SolidColorPaint>(negative.Fill);
        Assert.Equal(LightTheme.Destructive.R, negativeFill.Color.Red);
        Assert.Equal(LightTheme.Destructive.G, negativeFill.Color.Green);
        Assert.Equal(LightTheme.Destructive.B, negativeFill.Color.Blue);
    }

    [Fact]
    public void BuildPieSeries_maps_one_slice_per_series_using_its_first_value()
    {
        var series = new[]
        {
            new NaviusChartSeries { Key = "chrome", Label = "Chrome", Values = [275] },
            new NaviusChartSeries { Key = "safari", Label = "Safari", Values = [200] },
        };

        var built = NaviusChartSeriesMapper.BuildPieSeries(series, LightTheme);

        Assert.Equal(2, built.Length);
        var first = Assert.IsType<PieSeries<double>>(built[0]);
        Assert.Equal("Chrome", first.Name);
        Assert.Equal([275d], first.Values);
    }

    [Fact]
    public void BuildCategoryAxis_carries_the_category_labels()
    {
        var axis = NaviusChartSeriesMapper.BuildCategoryAxis(["Jan", "Feb", "Mar"], LightTheme);

        Assert.Equal(["Jan", "Feb", "Mar"], axis.Labels);
    }

    [Fact]
    public void Rebuilding_with_a_different_theme_reapplies_new_token_colours()
    {
        // This is what NaviusChart.RefreshTheme() does under the hood: re-resolve the tokens,
        // then re-run the same series mapping. Two different resolved themes must produce two
        // differently-coloured builds.
        var lightBuilt = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Line, TwoSeries(), LightTheme);
        var darkBuilt = NaviusChartSeriesMapper.BuildCartesianSeries(NaviusChartKind.Line, TwoSeries(), DarkTheme);

        var lightStroke = Assert.IsType<SolidColorPaint>(((LineSeries<double>)lightBuilt[0]).Stroke);
        var darkStroke = Assert.IsType<SolidColorPaint>(((LineSeries<double>)darkBuilt[0]).Stroke);

        Assert.NotEqual(lightStroke.Color, darkStroke.Color);
        Assert.Equal(DarkTheme.Ink.R, darkStroke.Color.Red);
        Assert.Equal(DarkTheme.Ink.G, darkStroke.Color.Green);
        Assert.Equal(DarkTheme.Ink.B, darkStroke.Color.Blue);
    }
}
