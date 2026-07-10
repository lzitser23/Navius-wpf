using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.Tabs;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class TabsTests : IDisposable
{
    static TabsTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnPreviewKeyDownMethod =
        typeof(NaviusTabs).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusTabsTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    private void SimulateKey(NaviusTabs tabs, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        OnPreviewKeyDownMethod.Invoke(tabs, new object[] { tabs, args });
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tabs.xaml"),
        });

        return scope;
    }

    private static (NaviusTabs Root, NaviusTabItem A, NaviusTabItem B, NaviusTabItem C) CreateTabs(
        string activationMode = "automatic", bool loop = true, string? dir = null, string orientation = "horizontal")
    {
        var a = new NaviusTabItem { Value = "a", Header = "A", Content = "A body" };
        var b = new NaviusTabItem { Value = "b", Header = "B", Content = "B body" };
        var c = new NaviusTabItem { Value = "c", Header = "C", Content = "C body" };
        var root = new NaviusTabs { ActivationMode = activationMode, Loop = loop, Dir = dir, Orientation = orientation };
        root.Items.Add(a);
        root.Items.Add(b);
        root.Items.Add(c);

        return (root, a, b, c);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var (root, a, _, _) = CreateTabs();
        root.Resources = scope;
        a.Resources = scope;

        Assert.True(root.ApplyTemplate());
        Assert.True(a.ApplyTemplate());
    }

    [StaFact]
    public void SettingValue_SelectsMatchingItem()
    {
        var (root, _, b, _) = CreateTabs();

        root.Value = "b";

        Assert.True(b.IsSelected);
    }

    [StaFact]
    public void Selecting_SyncsValue_AndRaisesValueChanged()
    {
        var (root, _, b, _) = CreateTabs();
        var raised = 0;
        root.ValueChanged += (_, _) => raised++;

        b.IsSelected = true;

        Assert.Equal("b", root.Value);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void ActivationDirection_ComputesRightThenLeft_Horizontal()
    {
        var (root, a, b, _) = CreateTabs();
        a.IsSelected = true;

        b.IsSelected = true;
        Assert.Equal("right", root.ActivationDirection);

        a.IsSelected = true;
        Assert.Equal("left", root.ActivationDirection);
    }

    [StaFact]
    public void ActivationDirection_ComputesDownThenUp_Vertical()
    {
        var (root, a, b, _) = CreateTabs(orientation: "vertical");
        a.IsSelected = true;

        b.IsSelected = true;
        Assert.Equal("down", root.ActivationDirection);

        a.IsSelected = true;
        Assert.Equal("up", root.ActivationDirection);
    }

    [StaFact]
    public void AutomaticMode_ArrowRight_MovesFocusAndSelects()
    {
        var (root, a, b, _) = CreateTabs();
        a.IsSelected = true;
        FocusManager.SetFocusedElement(root, a);

        SimulateKey(root, Key.Right);

        Assert.Same(b, FocusManager.GetFocusedElement(root));
        Assert.True(b.IsSelected);
        Assert.Equal("b", root.Value);
    }

    [StaFact]
    public void ManualMode_ArrowRight_MovesFocusOnly_DoesNotSelect()
    {
        var (root, a, b, _) = CreateTabs(activationMode: "manual");
        a.IsSelected = true;
        FocusManager.SetFocusedElement(root, a);

        SimulateKey(root, Key.Right);

        Assert.Same(b, FocusManager.GetFocusedElement(root));
        Assert.False(b.IsSelected);
        Assert.Equal("a", root.Value);
    }

    [StaFact]
    public void ManualMode_EnterOnFocusedTab_Selects()
    {
        var (root, a, b, _) = CreateTabs(activationMode: "manual");
        a.IsSelected = true;
        FocusManager.SetFocusedElement(root, a);
        SimulateKey(root, Key.Right);

        SimulateKey(root, Key.Enter);

        Assert.True(b.IsSelected);
        Assert.Equal("b", root.Value);
    }

    [StaFact]
    public void ManualMode_SpaceOnFocusedTab_Selects()
    {
        var (root, a, b, _) = CreateTabs(activationMode: "manual");
        a.IsSelected = true;
        FocusManager.SetFocusedElement(root, a);
        SimulateKey(root, Key.Right);

        SimulateKey(root, Key.Space);

        Assert.True(b.IsSelected);
        Assert.Equal("b", root.Value);
    }

    [StaFact]
    public void ArrowRight_WrapsAtEnd_WhenLooping()
    {
        var (root, _, _, c) = CreateTabs(loop: true);
        c.IsSelected = true;
        FocusManager.SetFocusedElement(root, c);

        SimulateKey(root, Key.Right);

        Assert.Equal("a", root.Value);
    }

    [StaFact]
    public void ArrowRight_ClampsAtEnd_WhenLoopIsFalse()
    {
        var (root, _, _, c) = CreateTabs(loop: false);
        c.IsSelected = true;
        FocusManager.SetFocusedElement(root, c);

        SimulateKey(root, Key.Right);

        Assert.Equal("c", root.Value);
    }

    [StaFact]
    public void ArrowRight_IsMirroredUnderRtl()
    {
        var (root, a, b, _) = CreateTabs(dir: "rtl");
        b.IsSelected = true;
        FocusManager.SetFocusedElement(root, b);

        SimulateKey(root, Key.Right);

        Assert.Equal("a", root.Value);
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLastTab()
    {
        var (root, a, b, c) = CreateTabs();
        b.IsSelected = true;
        FocusManager.SetFocusedElement(root, b);

        SimulateKey(root, Key.End);
        Assert.Equal("c", root.Value);

        SimulateKey(root, Key.Home);
        Assert.Equal("a", root.Value);
    }

    [StaFact]
    public void DisabledTab_IsSkippedByArrowNavigation()
    {
        var (root, a, b, c) = CreateTabs();
        b.IsEnabled = false;
        a.IsSelected = true;
        FocusManager.SetFocusedElement(root, a);

        SimulateKey(root, Key.Right);

        Assert.Equal("c", root.Value);
    }
}
