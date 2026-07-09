using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Navius.Wpf.Charts.Theming;
using SkiaSharp;

namespace Navius.Wpf.Charts;

/// <summary>
/// Maps <see cref="NaviusChartSeries"/> onto LiveCharts2 series/axes, applying the
/// token-derived palette (see docs/adr/0004-chart-library.md). Factored out of
/// <see cref="NaviusChart"/> so the mapping is unit-testable without a live chart control.
/// </summary>
internal static class NaviusChartSeriesMapper
{
    public static List<ISeries> BuildCartesianSeries(
        NaviusChartKind kind, IReadOnlyList<NaviusChartSeries> series, NaviusChartTheme theme)
    {
        var palette = theme.Palette(Math.Max(series.Count, 1));
        var destructive = ToSkColor(theme.Destructive);
        var result = new List<ISeries>(series.Count);

        for (var i = 0; i < series.Count; i++)
        {
            var s = series[i];
            var color = ToSkColor(palette[i]);
            var name = s.Label ?? s.Key;

            switch (kind)
            {
                case NaviusChartKind.Bar:
                    result.AddRange(BuildBarSeries(name, s.Values, color, destructive));
                    break;
                case NaviusChartKind.Area:
                    result.Add(new LineSeries<double>
                    {
                        Name = name,
                        Values = s.Values,
                        Fill = new SolidColorPaint(ToSkColor(NaviusChartPalette.WithOpacity(palette[i], 0.25))),
                        Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                        GeometrySize = 0,
                    });
                    break;
                default:
                    result.Add(new LineSeries<double>
                    {
                        Name = name,
                        Values = s.Values,
                        Fill = null,
                        Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                        GeometrySize = 6,
                    });
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// One bar series, or - when it holds negative values - two: a positive-only series in
    /// the series colour and a negative-only series in <paramref name="destructive"/>, each
    /// with the other half masked by a null gap so a category shows exactly one coloured bar.
    /// </summary>
    private static IEnumerable<ISeries> BuildBarSeries(string name, IReadOnlyList<double> values, SKColor color, SKColor destructive)
    {
        var hasNegative = false;
        foreach (var v in values)
        {
            if (NaviusChartPalette.IsNegativeEmphasis(NaviusChartKind.Bar, v)) { hasNegative = true; break; }
        }

        if (!hasNegative)
        {
            yield return new ColumnSeries<double>
            {
                Name = name,
                Values = values,
                Fill = new SolidColorPaint(color),
            };
            yield break;
        }

        var positive = new double?[values.Count];
        var negative = new double?[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] < 0) negative[i] = values[i];
            else positive[i] = values[i];
        }

        yield return new ColumnSeries<double?>
        {
            Name = name,
            Values = positive,
            Fill = new SolidColorPaint(color),
        };
        yield return new ColumnSeries<double?>
        {
            Name = name,
            Values = negative,
            Fill = new SolidColorPaint(destructive),
        };
    }

    public static ISeries[] BuildPieSeries(IReadOnlyList<NaviusChartSeries> series, NaviusChartTheme theme)
    {
        var palette = theme.Palette(Math.Max(series.Count, 1));
        var result = new ISeries[series.Count];

        for (var i = 0; i < series.Count; i++)
        {
            var s = series[i];
            var color = ToSkColor(palette[i]);
            result[i] = new PieSeries<double>
            {
                Name = s.Label ?? s.Key,
                Values = [s.Values.Count > 0 ? s.Values[0] : 0],
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(ToSkColor(theme.Background)) { StrokeThickness = 1 },
            };
        }

        return result;
    }

    public static Axis BuildCategoryAxis(IReadOnlyList<string> categories, NaviusChartTheme theme) => new()
    {
        Labels = categories.ToList(),
        LabelsPaint = new SolidColorPaint(ToSkColor(theme.MutedForeground)),
        SeparatorsPaint = null,
    };

    public static Axis BuildValueAxis(NaviusChartTheme theme) => new()
    {
        LabelsPaint = new SolidColorPaint(ToSkColor(theme.MutedForeground)),
        SeparatorsPaint = new SolidColorPaint(ToSkColor(theme.Border)) { StrokeThickness = 1 },
    };

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);
}
