namespace Navius.Wpf.Charts;

/// <summary>
/// One series of a <see cref="NaviusChart"/>: a stable <see cref="Key"/> (mirrors the web
/// <c>ChartConfig</c> key), an optional display <see cref="Label"/>, and its values. For
/// line/bar/area kinds, <see cref="Values"/> holds one point per <see cref="NaviusChart.Categories"/>
/// entry; for <see cref="NaviusChartKind.Pie"/>, only the first value is used as the slice
/// magnitude. Consumers do not pick a colour: <see cref="NaviusChart"/> derives it from the
/// current Navius tokens (see <c>Theming/NaviusChartTheme.cs</c>).
/// </summary>
public sealed class NaviusChartSeries
{
    public required string Key { get; init; }

    public string? Label { get; init; }

    public IReadOnlyList<double> Values { get; init; } = Array.Empty<double>();
}
