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
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
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
}
