using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class SwitchTests
{
    static SwitchTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch, so this reaches
    /// ToggleButton.OnClick -> OnToggle -> NaviusSwitch.OnToggle just like a real mouse/keyboard
    /// activation would), without depending on a live visual tree or real input routing.
    /// </summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    /// <summary>
    /// Hosts the switch in a real (never-shown) HwndSource so routed key events carry the switch
    /// as OriginalSource. That matters: native ButtonBase's Enter path is gated on
    /// e.OriginalSource == this (plus KeyboardNavigation.AcceptsReturn, which ButtonBase defaults
    /// to true), so directly invoking OnKeyDown with a fabricated KeyEventArgs never exercises it
    /// and falsely reads as "Enter is dead". Both Space (press on key-down, click on key-up) and
    /// Enter (click on key-down) are native ButtonBase behavior; NaviusSwitch adds no key handling.
    /// </summary>
    private static NaviusSwitch CreateHostedSwitch(out HwndSource source)
    {
        var toggle = new NaviusSwitch();
        source = new HwndSource(new HwndSourceParameters("NaviusSwitchKeyTests", 100, 100)) { RootVisual = toggle };
        toggle.Focus();
        return toggle;
    }

    private static void RaiseKey(UIElement target, Key key, RoutedEvent routedEvent, PresentationSource source) =>
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = routedEvent });

    [StaFact]
    public void DefaultState_IsUncheckedAndTwoState()
    {
        var toggle = new NaviusSwitch();

        Assert.False(toggle.IsChecked);
        Assert.False(toggle.IsThreeState);
        Assert.False(toggle.ReadOnly);
        Assert.False(toggle.Required);
    }

    [StaFact]
    public void Click_TogglesCheckedState()
    {
        var toggle = new NaviusSwitch();

        SimulateClick(toggle);
        Assert.True(toggle.IsChecked);

        SimulateClick(toggle);
        Assert.False(toggle.IsChecked);
    }

    [StaFact]
    public void SpaceKey_TogglesCheckedState()
    {
        var toggle = CreateHostedSwitch(out var source);

        // ButtonBase (ClickMode.Release) presses on KeyDown and clicks on KeyUp.
        RaiseKey(toggle, Key.Space, Keyboard.KeyDownEvent, source);
        RaiseKey(toggle, Key.Space, Keyboard.KeyUpEvent, source);

        Assert.True(toggle.IsChecked);
    }

    [StaFact]
    public void EnterKey_TogglesCheckedState()
    {
        var toggle = CreateHostedSwitch(out var source);

        RaiseKey(toggle, Key.Enter, Keyboard.KeyDownEvent, source);
        Assert.True(toggle.IsChecked);

        RaiseKey(toggle, Key.Enter, Keyboard.KeyDownEvent, source);
        Assert.False(toggle.IsChecked);
    }

    [StaFact]
    public void EnterKey_BlockedWhenReadOnly()
    {
        var toggle = CreateHostedSwitch(out var source);
        toggle.ReadOnly = true;

        RaiseKey(toggle, Key.Enter, Keyboard.KeyDownEvent, source);

        Assert.False(toggle.IsChecked);
    }

    [StaFact]
    public void ReadOnly_BlocksClickToggle()
    {
        var toggle = new NaviusSwitch { ReadOnly = true };

        SimulateClick(toggle);

        Assert.False(toggle.IsChecked);
    }

    [StaFact]
    public void ReadOnly_StaysFocusable()
    {
        var toggle = new NaviusSwitch { ReadOnly = true };

        Assert.True(toggle.Focusable);
        Assert.True(toggle.IsEnabled);
    }

    [StaFact]
    public void ReadOnly_DoesNotBlockProgrammaticChange()
    {
        var toggle = new NaviusSwitch { ReadOnly = true };

        toggle.IsChecked = true;

        Assert.True(toggle.IsChecked);
    }

    [StaFact]
    public void Disabled_CascadesFromAncestor()
    {
        var toggle = new NaviusSwitch();

        // Disabled maps onto the inherited IsEnabled (no custom NaviusSwitch logic); WPF's own
        // IsEnabled property-value inheritance is what blocks a descendant from real
        // mouse/keyboard input once an ancestor is disabled.
        _ = new StackPanel { IsEnabled = false, Children = { toggle } };

        Assert.False(toggle.IsEnabled);
    }

    [StaFact]
    public void AutomationPeer_IsToggleButtonAutomationPeer_WithTogglePattern()
    {
        var toggle = new NaviusSwitch();

        var peer = new ToggleButtonAutomationPeer(toggle);

        Assert.IsAssignableFrom<IToggleProvider>(peer);
    }

    [StaFact]
    public void Template_AppliesAndExposesThumbPart()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Switch.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var toggle = new NaviusSwitch();
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            toggle.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusSwitch));
            toggle.ApplyTemplate();

            Assert.NotNull(toggle.Template);
            Assert.NotNull(FindByName(toggle, "PART_Thumb"));
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    private static FrameworkElement? FindByName(DependencyObject root, string name)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement { } element && element.Name == name)
            {
                return element;
            }

            var descendant = FindByName(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
