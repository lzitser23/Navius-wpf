using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ToggleTests
{
    static ToggleTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch, so this reaches
    /// ToggleButton.OnClick -> OnToggle just like a real mouse/keyboard activation would),
    /// without depending on a live visual tree or real input routing.
    /// </summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static NaviusToggle CreateThemedToggle()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Toggle.xaml"),
        });

        return new NaviusToggle { Resources = scope };
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var toggle = CreateThemedToggle();

        Assert.True(toggle.ApplyTemplate());
    }

    [StaFact]
    public void DefaultState_IsUnpressed()
    {
        var toggle = new NaviusToggle();

        Assert.False(toggle.IsChecked);
        Assert.False(toggle.IsThreeState);
    }

    [StaFact]
    public void Click_TogglesPressedState()
    {
        var toggle = new NaviusToggle();

        SimulateClick(toggle);
        Assert.True(toggle.IsChecked);

        SimulateClick(toggle);
        Assert.False(toggle.IsChecked);
    }

    [StaFact]
    public void Disabled_CascadesFromAncestor()
    {
        var toggle = new NaviusToggle();

        // Disabled maps onto the inherited IsEnabled (no custom NaviusToggle logic);
        // WPF's own IsEnabled property-value inheritance is what blocks a descendant
        // from real mouse/keyboard input once an ancestor is disabled.
        _ = new StackPanel { IsEnabled = false, Children = { toggle } };

        Assert.False(toggle.IsEnabled);
    }

    [StaFact]
    public void AutomationPeer_IsToggleButtonAutomationPeer_WithTogglePattern()
    {
        var toggle = new NaviusToggle();

        var peer = new ToggleButtonAutomationPeer(toggle);

        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.IsAssignableFrom<IToggleProvider>(peer);
    }
}
