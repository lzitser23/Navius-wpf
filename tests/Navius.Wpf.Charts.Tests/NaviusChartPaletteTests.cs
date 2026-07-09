using System.Windows.Media;
using Navius.Wpf.Charts;
using Navius.Wpf.Charts.Theming;

namespace Navius.Wpf.Charts.Tests;

public class NaviusChartPaletteTests
{
    private static readonly Color Ink = Color.FromRgb(0x17, 0x16, 0x14);
    private static readonly Color MutedForeground = Color.FromRgb(0x73, 0x72, 0x70);

    [Fact]
    public void Derive_returns_empty_for_non_positive_count()
    {
        Assert.Empty(NaviusChartPalette.Derive(Ink, MutedForeground, 0));
        Assert.Empty(NaviusChartPalette.Derive(Ink, MutedForeground, -1));
    }

    [Fact]
    public void Derive_leads_with_the_ink_colour_at_full_opacity()
    {
        var palette = NaviusChartPalette.Derive(Ink, MutedForeground, 3);

        Assert.Equal(Ink, palette[0]);
        Assert.Equal((byte)255, palette[0].A);
    }

    [Fact]
    public void Derive_shades_subsequent_series_from_muted_foreground_with_decreasing_opacity()
    {
        var palette = NaviusChartPalette.Derive(Ink, MutedForeground, 4);

        for (var i = 1; i < palette.Count; i++)
        {
            Assert.Equal(MutedForeground.R, palette[i].R);
            Assert.Equal(MutedForeground.G, palette[i].G);
            Assert.Equal(MutedForeground.B, palette[i].B);
        }

        // Opacity strictly decreases series-over-series after the ink lead.
        for (var i = 2; i < palette.Count; i++)
        {
            Assert.True(palette[i].A < palette[i - 1].A);
        }
    }

    [Fact]
    public void Derive_cycles_the_opacity_ramp_when_there_are_more_series_than_steps()
    {
        var palette = NaviusChartPalette.Derive(Ink, MutedForeground, 7);

        // 5 muted opacity steps; series 1 and series 6 (index 1 and 6) share a step.
        Assert.Equal(palette[1].A, palette[6].A);
    }

    [Fact]
    public void WithOpacity_only_changes_the_alpha_channel()
    {
        var result = NaviusChartPalette.WithOpacity(MutedForeground, 0.5);

        Assert.Equal(MutedForeground.R, result.R);
        Assert.Equal(MutedForeground.G, result.G);
        Assert.Equal(MutedForeground.B, result.B);
        Assert.Equal((byte)128, result.A);
    }

    [Fact]
    public void WithOpacity_clamps_out_of_range_values()
    {
        Assert.Equal((byte)0, NaviusChartPalette.WithOpacity(MutedForeground, -1).A);
        Assert.Equal((byte)255, NaviusChartPalette.WithOpacity(MutedForeground, 2).A);
    }

    [Theory]
    [InlineData(NaviusChartKind.Bar, -1, true)]
    [InlineData(NaviusChartKind.Bar, 0, false)]
    [InlineData(NaviusChartKind.Bar, 1, false)]
    [InlineData(NaviusChartKind.Line, -1, false)]
    [InlineData(NaviusChartKind.Area, -1, false)]
    [InlineData(NaviusChartKind.Pie, -1, false)]
    public void IsNegativeEmphasis_only_applies_to_negative_bar_values(NaviusChartKind kind, double value, bool expected)
        => Assert.Equal(expected, NaviusChartPalette.IsNegativeEmphasis(kind, value));
}
