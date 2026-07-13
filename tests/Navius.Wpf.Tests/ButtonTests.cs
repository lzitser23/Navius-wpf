using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ButtonTests
{
    static ButtonTests()
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
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    [StaFact]
    public void DefaultState_NotDisabled()
    {
        var button = new NaviusButton();

        Assert.False(button.Disabled);
        Assert.False(button.FocusableWhenDisabled);
        Assert.False(button.IsSoftDisabled);
        Assert.True(button.IsEnabled);
        Assert.Equal(NaviusButtonVariant.Default, button.Variant);
        Assert.Equal(NaviusButtonSize.Default, button.Size);
    }

    [StaFact]
    public void VariantAndSize_ApplyTokenBackedStyle()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Button.xaml"),
        });
        var button = new NaviusButton
        {
            Resources = scope,
            Variant = NaviusButtonVariant.Secondary,
            Size = NaviusButtonSize.Small,
        };

        Assert.True(button.ApplyTemplate());
        Assert.Equal((Color)ColorConverter.ConvertFromString("#E3E3E2"), Assert.IsType<SolidColorBrush>(button.Background).Color);
        Assert.Equal(new Thickness(10, 5, 10, 5), button.Padding);
        Assert.Equal(12, button.FontSize);
    }

    [StaFact]
    public void IconSize_IsSquareAndUnpadded()
    {
        var button = new NaviusButton { Size = NaviusButtonSize.Icon };
        button.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Button.xaml"),
        });

        Assert.True(button.ApplyTemplate());
        Assert.Equal(36, button.Width);
        Assert.Equal(36, button.Height);
        Assert.Equal(new Thickness(0), button.Padding);
    }

    [StaFact]
    public void Disabled_WithoutFocusableWhenDisabled_SetsNativeIsEnabledFalse()
    {
        var button = new NaviusButton { Disabled = true };

        Assert.False(button.IsEnabled);
        Assert.False(button.IsSoftDisabled);
    }

    [StaFact]
    public void Disabled_WithFocusableWhenDisabled_StaysNativelyEnabled()
    {
        var button = new NaviusButton { FocusableWhenDisabled = true, Disabled = true };

        Assert.True(button.IsEnabled);
        Assert.True(button.IsSoftDisabled);
    }

    [StaFact]
    public void Disabled_ReenablingClearsHardDisabled()
    {
        var button = new NaviusButton { Disabled = true };

        button.Disabled = false;

        Assert.True(button.IsEnabled);
        Assert.False(button.IsSoftDisabled);
    }

    [StaFact]
    public void SoftDisabled_SuppressesOnClick()
    {
        // OnClick is the single funnel ButtonBase uses for mouse and keyboard activation
        // (both feed through the protected virtual OnClick before raising Click).
        var button = new NaviusButton { FocusableWhenDisabled = true, Disabled = true };
        var clicked = false;
        button.Click += (_, _) => clicked = true;

        InvokeOnClick(button);

        Assert.False(clicked);
    }

    [StaFact]
    public void NotSoftDisabled_OnClickRaisesClick()
    {
        var button = new NaviusButton();
        var clicked = false;
        button.Click += (_, _) => clicked = true;

        InvokeOnClick(button);

        Assert.True(clicked);
    }

    [StaFact]
    public void AutomationPeer_IsButtonAutomationPeer_WithInvokePattern()
    {
        var button = new NaviusButton();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as NaviusButtonAutomationPeer;

        Assert.NotNull(peer);
        Assert.IsAssignableFrom<IInvokeProvider>(peer);
    }

    [StaFact]
    public void AutomationPeer_ReportsDisabledWhenSoftDisabled()
    {
        var button = new NaviusButton { FocusableWhenDisabled = true, Disabled = true };
        var peer = new NaviusButtonAutomationPeer(button);

        Assert.False(peer.IsEnabled());
    }

    [StaFact]
    public void AutomationPeer_ReportsEnabledWhenNotDisabled()
    {
        var button = new NaviusButton();
        var peer = new NaviusButtonAutomationPeer(button);

        Assert.True(peer.IsEnabled());
    }

    [StaFact]
    public void AutomationPeer_InvokeThrowsElementNotEnabledWhenSoftDisabled()
    {
        // Mirrors aria-disabled: UIA clients (screen readers) can still find/focus a soft-disabled
        // button but WPF's stock IInvokeProvider.Invoke() refuses to activate a peer reporting
        // IsEnabled()==false, throwing rather than silently invoking.
        var button = new NaviusButton { FocusableWhenDisabled = true, Disabled = true };
        var peer = new NaviusButtonAutomationPeer(button);
        var invoke = (IInvokeProvider)peer;

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
    }

    private static void InvokeOnClick(NaviusButton button)
    {
        var method = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(button, null);
    }
}
