using System.Windows;
using System.Windows.Media;

namespace Navius.Wpf.Charts.Theming;

/// <summary>
/// The current Navius token colours (see Navius.Wpf.Primitives <c>Themes/Tokens.*.xaml</c>),
/// resolved from a control's resource scope for use by <see cref="NaviusChart"/>.
/// <c>Navius.Wpf.Primitives.Theming.ThemeManager</c> has no theme-changed event today - it only
/// swaps the token <c>ResourceDictionary</c> in <c>Apply(theme, scope)</c> - so callers must
/// re-resolve after a swap; <see cref="NaviusChart.RefreshTheme"/> does that.
/// </summary>
public sealed class NaviusChartTheme
{
    public required Color Ink { get; init; }
    public required Color MutedForeground { get; init; }
    public required Color Muted { get; init; }
    public required Color Destructive { get; init; }
    public required Color Border { get; init; }
    public required Color Background { get; init; }

    /// <summary>Resolves every token this theme needs from <paramref name="scope"/>'s DynamicResource lookup.</summary>
    public static NaviusChartTheme Resolve(FrameworkElement scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new NaviusChartTheme
        {
            Ink = ResolveColor(scope, "Navius.Primary", Colors.Black),
            MutedForeground = ResolveColor(scope, "Navius.MutedForeground", Colors.Gray),
            Muted = ResolveColor(scope, "Navius.Muted", Colors.LightGray),
            Destructive = ResolveColor(scope, "Navius.Destructive", Colors.Red),
            Border = ResolveColor(scope, "Navius.Border", Colors.Gray),
            Background = ResolveColor(scope, "Navius.Background", Colors.White),
        };
    }

    /// <summary>The derived series palette for <paramref name="count"/> series (see <see cref="NaviusChartPalette"/>).</summary>
    public IReadOnlyList<Color> Palette(int count) => NaviusChartPalette.Derive(Ink, MutedForeground, count);

    private static Color ResolveColor(FrameworkElement scope, string resourceKey, Color fallback)
        => scope.TryFindResource(resourceKey) is SolidColorBrush brush ? brush.Color : fallback;
}
