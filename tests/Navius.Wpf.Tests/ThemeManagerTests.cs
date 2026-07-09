using System.Linq;
using System.Windows;
using System.Windows.Media;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ThemeManagerTests
{
    static ThemeManagerTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class won the race; Application.Current is now set.
            }
        }
    }

    [StaFact]
    public void Apply_LoadsLightTokens()
    {
        var scope = new ResourceDictionary();

        ThemeManager.Apply(NaviusTheme.Light, scope);

        var background = Assert.IsType<SolidColorBrush>(scope["Navius.Background"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#F4F3F0"), background.Color);
    }

    [StaFact]
    public void Apply_SwapsInsteadOfStacking()
    {
        var scope = new ResourceDictionary();

        ThemeManager.Apply(NaviusTheme.Light, scope);
        ThemeManager.Apply(NaviusTheme.Dark, scope);

        Assert.Single(scope.MergedDictionaries);
        var background = Assert.IsType<SolidColorBrush>(scope["Navius.Background"]);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#161513"), background.Color);
    }

    // --- High contrast ---

    [StaFact]
    public void TokenDictionary_HighContrast_CoversSameKeysAsLight()
    {
        var light = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tokens.Light.xaml", UriKind.Absolute),
        };
        var highContrast = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tokens.HighContrast.xaml", UriKind.Absolute),
        };

        var lightKeys = light.Keys.Cast<object>().ToHashSet();
        var highContrastKeys = highContrast.Keys.Cast<object>().ToHashSet();

        Assert.Equal(lightKeys, highContrastKeys);
    }

    [StaFact]
    public void Apply_LoadsHighContrastTokens()
    {
        var scope = new ResourceDictionary();

        ThemeManager.Apply(NaviusTheme.HighContrast, scope);

        var marker = Assert.IsType<string>(scope["Navius.Tokens.Theme"]);
        Assert.Equal("HighContrast", marker);

        // Every brush is DynamicResource-bound to a SystemColors *ColorKey (see ADR-0007), so it
        // resolves to whatever the current OS system color is, not a literal hex like Light/Dark.
        var background = Assert.IsType<SolidColorBrush>(scope["Navius.Background"]);
        Assert.Equal(SystemColors.WindowColor, background.Color);
        var primary = Assert.IsType<SolidColorBrush>(scope["Navius.Primary"]);
        Assert.Equal(SystemColors.HighlightColor, primary.Color);
    }

    private static void ResetHighContrastSync()
    {
        // Clears any leftover _systemHighContrastActive state from a prior test before establishing
        // a known starting theme (tests run sequentially -- see AssemblyInfo's
        // DisableTestParallelization -- but ThemeManager's statics still persist across tests).
        ThemeManager.SyncSystemHighContrastState(false);
        ThemeManager.Apply(NaviusTheme.Light);
    }

    [StaFact]
    public void SyncSystemHighContrastState_AppliesHighContrast_WhenEnabled()
    {
        ResetHighContrastSync();

        ThemeManager.SyncSystemHighContrastState(true);

        Assert.Equal(NaviusTheme.HighContrast, ThemeManager.Current);
    }

    [StaFact]
    public void SyncSystemHighContrastState_RestoresPreviousTheme_WhenDisabled()
    {
        ResetHighContrastSync();
        ThemeManager.Apply(NaviusTheme.Dark);

        ThemeManager.SyncSystemHighContrastState(true);
        ThemeManager.SyncSystemHighContrastState(false);

        Assert.Equal(NaviusTheme.Dark, ThemeManager.Current);
    }

    [StaFact]
    public void SyncSystemHighContrastState_NoOp_WhenNotActiveAndDisabled()
    {
        ResetHighContrastSync();
        ThemeManager.Apply(NaviusTheme.Dark);

        ThemeManager.SyncSystemHighContrastState(false);

        Assert.Equal(NaviusTheme.Dark, ThemeManager.Current);
    }

    [StaFact]
    public void SyncSystemHighContrastState_FiresThemeChanged_ForEnterAndRestore()
    {
        ResetHighContrastSync();
        ThemeManager.Apply(NaviusTheme.Dark);
        var observed = new List<NaviusTheme>();
        void Handler(object? _, NaviusTheme t) => observed.Add(t);
        ThemeManager.ThemeChanged += Handler;

        try
        {
            ThemeManager.SyncSystemHighContrastState(true);
            ThemeManager.SyncSystemHighContrastState(false);
        }
        finally
        {
            ThemeManager.ThemeChanged -= Handler;
        }

        Assert.Equal(new[] { NaviusTheme.HighContrast, NaviusTheme.Dark }, observed);
    }
}
