using System;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
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

    // Hosts the toggle in a real (never-shown) HwndSource so keyboard focus and the mouse-capture
    // that ButtonBase's Space handler performs actually work, then raises the real KeyDown/KeyUp
    // routed events. This exercises the native ButtonBase key path (not the OnClick shortcut), so
    // these are the true regressions for the contract's "Space / Enter native activation" claim --
    // the "Space is dead" bug class the M6 batch re-checks everywhere.
    private static NaviusToggle CreateFocusedToggle(out HwndSource source)
    {
        var toggle = new NaviusToggle();
        source = new HwndSource(new HwndSourceParameters("NaviusToggleKeyTests", 100, 100)) { RootVisual = toggle };
        toggle.Focus();
        Keyboard.Focus(toggle);
        return toggle;
    }

    private static void RaiseKey(UIElement target, Key key, RoutedEvent routedEvent, PresentationSource source) =>
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = routedEvent });

    [StaFact]
    public void SpaceKey_ActivatesToggle()
    {
        var toggle = CreateFocusedToggle(out var source);
        using var _ = source;

        // ButtonBase (ClickMode.Release) presses on KeyDown and clicks on KeyUp.
        RaiseKey(toggle, Key.Space, Keyboard.KeyDownEvent, source);
        RaiseKey(toggle, Key.Space, Keyboard.KeyUpEvent, source);

        Assert.True(toggle.IsChecked);
    }

    [StaFact]
    public void EnterKey_ActivatesToggle()
    {
        var toggle = CreateFocusedToggle(out var source);
        using var _ = source;

        RaiseKey(toggle, Key.Enter, Keyboard.KeyDownEvent, source);

        Assert.True(toggle.IsChecked);
    }

    [StaFact]
    public void NonActivationKey_DoesNotToggle()
    {
        var toggle = CreateFocusedToggle(out var source);
        using var _ = source;

        RaiseKey(toggle, Key.A, Keyboard.KeyDownEvent, source);
        RaiseKey(toggle, Key.A, Keyboard.KeyUpEvent, source);

        Assert.False(toggle.IsChecked);
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
