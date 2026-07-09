using System.Windows;

namespace Navius.Wpf.Primitives.Theming;

/// <summary>
/// Swaps the Navius token dictionary (Themes/Tokens.*.xaml) in a resource scope at
/// runtime. Controls consume tokens via DynamicResource, so a swap restyles live UI.
/// </summary>
public static class ThemeManager
{
    /// <summary>Key present in every token dictionary; used to find and replace it.</summary>
    internal const string TokenDictionaryMarker = "Navius.Tokens.Theme";

    public static NaviusTheme Current { get; private set; } = NaviusTheme.Light;

    /// <summary>Applies the theme application-wide.</summary>
    public static void Apply(NaviusTheme theme) => Apply(theme, Application.Current.Resources);

    /// <summary>Applies the theme to an explicit resource scope (e.g. a single window, or tests).</summary>
    public static void Apply(NaviusTheme theme, ResourceDictionary scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        for (var i = scope.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (scope.MergedDictionaries[i].Contains(TokenDictionaryMarker))
            {
                scope.MergedDictionaries.RemoveAt(i);
            }
        }

        scope.MergedDictionaries.Add(new ResourceDictionary { Source = TokenUri(theme) });
        Current = theme;
    }

    private static Uri TokenUri(NaviusTheme theme) => new(
        $"pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tokens.{theme}.xaml",
        UriKind.Absolute);
}
