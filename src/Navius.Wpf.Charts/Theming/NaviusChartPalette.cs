using System.Windows.Media;

namespace Navius.Wpf.Charts.Theming;

/// <summary>
/// Pure derivation of a chart series colour palette from already-resolved Navius token
/// colours. No WPF resource lookup here: <see cref="NaviusChartTheme"/> resolves the tokens
/// and calls into this class, which keeps the derivation itself unit-testable.
/// </summary>
public static class NaviusChartPalette
{
    /// <summary>Opacity applied to <c>mutedForeground</c> for series after the first, cycling if needed.</summary>
    private static readonly double[] MutedOpacitySteps = [1.0, 0.7, 0.5, 0.35, 0.25];

    /// <summary>
    /// Derives <paramref name="count"/> series colours: the ink <paramref name="primary"/>
    /// leads (series 0), followed by <paramref name="mutedForeground"/> at decreasing opacity.
    /// </summary>
    public static IReadOnlyList<Color> Derive(Color primary, Color mutedForeground, int count)
    {
        if (count <= 0) return [];

        var palette = new Color[count];
        palette[0] = primary;
        for (var i = 1; i < count; i++)
        {
            var opacity = MutedOpacitySteps[(i - 1) % MutedOpacitySteps.Length];
            palette[i] = WithOpacity(mutedForeground, opacity);
        }

        return palette;
    }

    /// <summary>Returns <paramref name="color"/> with its alpha channel set from <paramref name="opacity"/> (0..1).</summary>
    public static Color WithOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>
    /// True when a plotted value should be emphasised with the destructive colour instead of
    /// its series colour: negative bars/columns, per the locked chart decision.
    /// </summary>
    public static bool IsNegativeEmphasis(NaviusChartKind kind, double value)
        => kind == NaviusChartKind.Bar && value < 0;
}
