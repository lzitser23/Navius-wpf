using System.Windows;
using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView.WPF;
using Navius.Wpf.Charts.Theming;

namespace Navius.Wpf.Charts;

/// <summary>
/// Thin, token-themed wrapper over LiveCharts2 (see docs/adr/0004-chart-library.md) covering
/// the Navius line/bar/area/pie chart surface. Consumers set <see cref="Kind"/>,
/// <see cref="Series"/> and, for the cartesian kinds, <see cref="Categories"/>; the series
/// palette is derived from the current Navius tokens, not chosen by the consumer. For anything
/// this wrapper doesn't cover, <see cref="EscapeHatch"/> exposes the underlying LiveCharts
/// control (a <see cref="CartesianChart"/> or <see cref="PieChart"/>).
/// </summary>
public sealed class NaviusChart : Grid
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(NaviusChartKind), typeof(NaviusChart),
        new PropertyMetadata(NaviusChartKind.Line, OnChartInvalidated));

    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(IReadOnlyList<NaviusChartSeries>), typeof(NaviusChart),
        new PropertyMetadata(Array.Empty<NaviusChartSeries>(), OnChartInvalidated));

    public static readonly DependencyProperty CategoriesProperty = DependencyProperty.Register(
        nameof(Categories), typeof(IReadOnlyList<string>), typeof(NaviusChart),
        new PropertyMetadata(Array.Empty<string>(), OnChartInvalidated));

    private static readonly DependencyPropertyKey EscapeHatchPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(EscapeHatch), typeof(FrameworkElement), typeof(NaviusChart), new PropertyMetadata(null));

    public static readonly DependencyProperty EscapeHatchProperty = EscapeHatchPropertyKey.DependencyProperty;

    public NaviusChart()
    {
        Loaded += (_, _) => Rebuild();
        // Re-theme automatically on token swaps; unhook while unloaded so charts
        // do not leak through the static event.
        Loaded += (_, _) => Navius.Wpf.Primitives.Theming.ThemeManager.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => Navius.Wpf.Primitives.Theming.ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, Navius.Wpf.Primitives.Theming.NaviusTheme theme) => Rebuild();

    /// <summary>Which shape to render. Changing this rebuilds the underlying chart control.</summary>
    public NaviusChartKind Kind
    {
        get => (NaviusChartKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>The series to plot. Colours come from the current Navius tokens, not from this model.</summary>
    public IReadOnlyList<NaviusChartSeries> Series
    {
        get => (IReadOnlyList<NaviusChartSeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    /// <summary>X-axis category labels for the line/bar/area kinds. Ignored for <see cref="NaviusChartKind.Pie"/>.</summary>
    public IReadOnlyList<string> Categories
    {
        get => (IReadOnlyList<string>)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    /// <summary>The underlying LiveCharts control, for anything this wrapper does not expose.</summary>
    public FrameworkElement? EscapeHatch => (FrameworkElement?)GetValue(EscapeHatchProperty);

    /// <summary>
    /// Re-resolves the Navius tokens from this control's resource scope and rebuilds the chart
    /// with the refreshed palette. Theme changes raised by <c>ThemeManager</c> are handled
    /// automatically while the control is loaded; call this only after changing scoped resources
    /// directly or when an immediate manual refresh is useful.
    /// </summary>
    public void RefreshTheme() => Rebuild();

    private static void OnChartInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((NaviusChart)d).Rebuild();

    private void Rebuild()
    {
        var theme = NaviusChartTheme.Resolve(this);
        var series = Series;

        FrameworkElement chart = Kind == NaviusChartKind.Pie
            ? new PieChart { Series = NaviusChartSeriesMapper.BuildPieSeries(series, theme) }
            : new CartesianChart
            {
                Series = NaviusChartSeriesMapper.BuildCartesianSeries(Kind, series, theme),
                XAxes = [NaviusChartSeriesMapper.BuildCategoryAxis(Categories, theme)],
                YAxes = [NaviusChartSeriesMapper.BuildValueAxis(theme)],
            };

        Children.Clear();
        Children.Add(chart);
        SetValue(EscapeHatchPropertyKey, chart);
    }
}
