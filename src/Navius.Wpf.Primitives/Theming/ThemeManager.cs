using System.ComponentModel;
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

    private static bool _systemHighContrastSyncEnabled;
    private static bool _systemHighContrastActive;
    private static NaviusTheme _themeBeforeSystemHighContrast = NaviusTheme.Light;

    public static NaviusTheme Current { get; private set; } = NaviusTheme.Light;

    /// <summary>Raised after a token dictionary swap; consumers that resolve token VALUES
    /// in code (rather than via DynamicResource) re-resolve on this signal.</summary>
    public static event EventHandler<NaviusTheme>? ThemeChanged;

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
        ThemeChanged?.Invoke(null, theme);
    }

    private static Uri TokenUri(NaviusTheme theme) => new(
        $"pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tokens.{theme}.xaml",
        UriKind.Absolute);

    /// <summary>
    /// Opt-in: applies <see cref="NaviusTheme.HighContrast"/> application-wide whenever the OS's
    /// SystemParameters.HighContrast is on, tracking it live via SystemParameters.StaticPropertyChanged,
    /// and restores whatever theme was active immediately before switching once it turns back off.
    /// Idempotent (a second call is a no-op) and windows/desktop-only -- the web port never needed
    /// this because browsers own high-contrast rendering themselves.
    /// </summary>
    public static void EnableSystemHighContrastSync()
    {
        if (_systemHighContrastSyncEnabled)
        {
            return;
        }

        _systemHighContrastSyncEnabled = true;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        SyncSystemHighContrastState(SystemParameters.HighContrast);
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast))
        {
            SyncSystemHighContrastState(SystemParameters.HighContrast);
        }
    }

    /// <summary>
    /// The sync decision behind <see cref="EnableSystemHighContrastSync"/>, factored out and given
    /// the OS flag as a parameter (rather than reading SystemParameters.HighContrast itself) so it
    /// is directly unit-testable without flipping the real OS high-contrast setting. Public rather
    /// than the more natural <c>internal</c> because this assembly has no InternalsVisibleTo -- the
    /// same tradeoff NaviusTree.HandleKey()/NaviusRating.HandleKey() make elsewhere in this codebase.
    /// Applies/restores through the normal <see cref="Apply(NaviusTheme)"/> path, so
    /// <see cref="ThemeChanged"/> still fires for these transitions.
    /// </summary>
    public static void SyncSystemHighContrastState(bool systemHighContrastEnabled) =>
        SyncSystemHighContrastState(systemHighContrastEnabled, null);

    /// <summary>Scope overload: tests pass an isolated dictionary so the sync logic can be
    /// exercised without mutating Application.Current.Resources (global test-state hygiene).</summary>
    public static void SyncSystemHighContrastState(bool systemHighContrastEnabled, ResourceDictionary? scope)
    {
        if (systemHighContrastEnabled)
        {
            if (!_systemHighContrastActive)
            {
                _themeBeforeSystemHighContrast = Current;
            }

            _systemHighContrastActive = true;
            if (Current != NaviusTheme.HighContrast)
            {
                ApplyToScopeOrApp(NaviusTheme.HighContrast, scope);
            }
        }
        else if (_systemHighContrastActive)
        {
            _systemHighContrastActive = false;
            ApplyToScopeOrApp(_themeBeforeSystemHighContrast, scope);
        }
    }

    private static void ApplyToScopeOrApp(NaviusTheme theme, ResourceDictionary? scope)
    {
        if (scope is null)
        {
            Apply(theme);
        }
        else
        {
            Apply(theme, scope);
        }
    }
}
