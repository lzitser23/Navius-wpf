# ADR-0004: Chart library for Navius.Wpf.Charts

Status: accepted

## Context

The locked v1 chart decision is a thin, token-themed wrapper over a single existing OSS WPF
chart library, with the dependency confined to its own project
(`src/Navius.Wpf.Charts/Navius.Wpf.Charts.csproj`). The wrapper must cover line/bar/area/pie,
match the shape of the web chart surface (`Zits.Ui`'s `ZitsChart.cs` / `ZitsLineChart.razor` /
`ZitsPieChart.razor`: a `Key -> ChartSeries { Label, Color }` config plus row data, each series
colour driven by a design token), and re-colour live when
`Navius.Wpf.Primitives.Theming.ThemeManager` swaps the token dictionary (`Apply(theme, scope)`
removes and re-adds the `Navius.Tokens.Theme`-marked `ResourceDictionary`). Reading
`ThemeManager.cs`: it exposes no theme-changed event or signal, only `Current` and `Apply(...)`,
so any re-themed consumer has to re-resolve tokens and re-apply them itself after a swap.

Two candidates were spiked: `LiveChartsCore.SkiaSharpView.WPF` (LiveCharts2) and `ScottPlot.WPF`.

- **License.** Both MIT.
- **NuGet health/recency.** Both actively published: LiveChartsCore.SkiaSharpView.WPF 2.0.5
  (2026-06-18), ScottPlot.WPF 5.1.59 (2026-06-22).
- **.NET 8/10 WPF support.** Both ship `net8.0-windows` builds and work under this repo's
  `net8.0-windows;net10.0-windows` `TargetFrameworks`.
- **Runtime brush/color theming.** LiveCharts2 series (`LineSeries<T>`, `ColumnSeries<T>`,
  `PieSeries<T>`) expose settable `Fill`/`Stroke` `SolidColorPaint` properties, plus a
  per-point `OnPointMeasured(Action<ChartPoint>)` hook for conditional point colour (used here
  for negative-value bar emphasis); reassigning these and letting the control redraw is a
  direct, documented re-theme path with no extra machinery. ScottPlot's colouring centers on a
  `Palette` assigned once when a plottable is added (`myPlot.Add.Palette = ...`); re-theming an
  already-built plot means walking every plottable's `Color` by hand and calling `Refresh()`,
  which is a less direct fit for "swap the token dictionary, every series re-colors."
- **API fit for the line/bar/area/pie surface.** LiveCharts2 has first-class `LineSeries`,
  `ColumnSeries` and `PieSeries` types that map close to 1:1 onto line/bar/pie (area is a
  `LineSeries` with `Fill` set and `GeometrySize = 0`, the same trick the web `ZitsLineChart`
  effectively achieves with an SVG fill). ScottPlot is oriented at scientific/engineering
  plotting (large-dataset scatter/signal, log axes, statistical plots); reaching a flat,
  hairline-grid, dashboard-style look needs more custom styling code than LiveCharts2's
  paint-based series model.

## Decision

Use `LiveChartsCore.SkiaSharpView.WPF` (LiveCharts2) as the chart engine behind
`Navius.Wpf.Charts`. The dependency is a `PackageReference` in
`src/Navius.Wpf.Charts/Navius.Wpf.Charts.csproj` only; no other project references it.

## Consequences

- Re-theming stays push-based: `NaviusChart.RefreshTheme()` re-resolves the Navius tokens from
  the control's resource scope and rebuilds series/axis paints. Consumers must call it after
  `ThemeManager.Apply(...)` themselves (the control also re-resolves on `Loaded`) until/unless
  `ThemeManager` grows a theme-changed signal, at which point `NaviusChart` should subscribe to
  it instead.
- Negative-value bar emphasis (`Navius.Destructive`) relies on LiveCharts2's per-point
  `OnPointMeasured` callback, a LiveCharts2-specific mechanism; porting to a different chart
  engine later would need an equivalent per-point styling hook.
- A future consumer that needs ScottPlot-style large-dataset scientific plotting can add a
  second thin wrapper project without touching `Navius.Wpf.Charts` or this decision.

## Update 2026-07-11: registry policy for Charts

The copy-paste registry (`registry/registry.json`, driven by `navius-wpf registry-sync` and
`navius-wpf add`) is deliberately scoped to Navius-owned source whose complete compile closure can
be vendored into a consumer project. Under that scope:

- `Navius.Wpf.Charts` is not a registry item. It is a third-party-engine adapter (its rendering is
  LiveCharts2, a `PackageReference`); it is distributed as an optional package, not as vendorable
  source, and its compile closure cannot be satisfied by copying files alone.
- No `registry:chart` item type will be added.
- The current registry and CLI are designed for self-contained source vendoring, not for modifying
  a consumer's package references. Revisit only if the CLI later gains generic
  `packageDependencies` installation support, at which point a Charts registry item that pulls in
  the LiveCharts2 package reference could be reconsidered.
