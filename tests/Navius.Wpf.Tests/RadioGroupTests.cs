using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.RadioGroup;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class RadioGroupTests : IDisposable
{
    static RadioGroupTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo OnPreviewKeyDownMethod =
        typeof(NaviusRadioGroup).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never
    // shown, style = 0 means no WS_VISIBLE bit) is the lightest real one available headlessly.
    // Lazily created (not a static field initializer) and disposed per test instance -- it must
    // not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusRadioGroupTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch reaches
    /// RadioButton.OnClick -> NaviusRadioGroupItem.OnToggle, just like a real click).
    /// </summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    /// <summary>Drives the group's private PreviewKeyDown handler directly with a real KeyEventArgs.</summary>
    private void SimulateKey(NaviusRadioGroup group, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        OnPreviewKeyDownMethod.Invoke(group, new object[] { group, args });
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/RadioGroup.xaml"),
        });

        return scope;
    }

    private static (NaviusRadioGroup Group, NaviusRadioGroupItem A, NaviusRadioGroupItem B, NaviusRadioGroupItem C) CreateGroup(bool loop = true, string? dir = null)
    {
        var a = new NaviusRadioGroupItem { Value = "a" };
        var b = new NaviusRadioGroupItem { Value = "b" };
        var c = new NaviusRadioGroupItem { Value = "c" };
        var group = new NaviusRadioGroup
        {
            Loop = loop,
            Dir = dir,
            Content = new StackPanel { Children = { a, b, c } },
        };

        return (group, a, b, c);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var group = new NaviusRadioGroup { Resources = scope };
        var item = new NaviusRadioGroupItem { Resources = scope };
        var indicator = new NaviusRadioGroupIndicator { Resources = scope };

        Assert.True(group.ApplyTemplate());
        Assert.True(item.ApplyTemplate());
        Assert.True(indicator.ApplyTemplate());
    }

    [StaFact]
    public void DefaultTemplate_UsesSeparateContentAndIndicatorForegrounds()
    {
        var scope = CreateThemedScope();
        var item = new NaviusRadioGroupItem { Content = "Label", IsChecked = true, Resources = scope };

        Assert.True(item.ApplyTemplate());

        var panel = Assert.IsType<StackPanel>(System.Windows.Media.VisualTreeHelper.GetChild(item, 0));
        var circle = Assert.IsType<Border>(panel.Children[0]);
        var indicator = Assert.IsType<NaviusRadioGroupIndicator>(circle.Child);
        Assert.Equal(item.FindResource("Navius.Foreground"), item.Foreground);
        Assert.Equal(item.FindResource("Navius.PrimaryForeground"), indicator.Foreground);
    }

    [StaFact]
    public void Click_SelectsItem_UpdatesValueAndRaisesEvent()
    {
        var (group, a, _, _) = CreateGroup();
        var raised = 0;
        group.ValueChanged += (_, _) => raised++;

        SimulateClick(a);

        Assert.Equal("a", group.Value);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void Click_EnforcesSingleSelectionInvariant()
    {
        var (group, a, b, c) = CreateGroup();

        SimulateClick(a);
        SimulateClick(b);

        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.False(c.IsChecked);
        Assert.Equal("b", group.Value);
    }

    [StaFact]
    public void ReadOnly_BlocksClickButStaysFocusable()
    {
        var (group, a, _, _) = CreateGroup();
        a.ReadOnly = true;

        SimulateClick(a);

        Assert.False(a.IsChecked);
        Assert.Null(group.Value);
        Assert.True(a.Focusable);
    }

    [StaFact]
    public void Disabled_CascadesFromAncestor()
    {
        var item = new NaviusRadioGroupItem { Value = "a" };

        // Disabled maps onto the inherited IsEnabled (no custom NaviusRadioGroup/-Item
        // logic, beyond ReadOnly); WPF's own IsEnabled property-value inheritance is what
        // cascades from an ancestor (e.g. the group) to every item for free.
        _ = new StackPanel { IsEnabled = false, Children = { item } };

        Assert.False(item.IsEnabled);
    }

    [StaFact]
    public void RovingTabStop_DefaultsToFirstEnabledItem_ThenFollowsSelection()
    {
        var (group, a, b, c) = CreateGroup();

        Assert.True(a.IsTabStop);
        Assert.False(b.IsTabStop);
        Assert.False(c.IsTabStop);

        SimulateClick(b);

        Assert.False(a.IsTabStop);
        Assert.True(b.IsTabStop);
        Assert.False(c.IsTabStop);
    }

    [StaFact]
    public void ArrowDown_MovesSelectionToNextItem_AndWrapsWhenLooping()
    {
        var (group, a, b, c) = CreateGroup(loop: true);
        SimulateClick(a);

        SimulateKey(group, Key.Down);
        Assert.Equal("b", group.Value);

        SimulateKey(group, Key.Down);
        Assert.Equal("c", group.Value);

        SimulateKey(group, Key.Down);
        Assert.Equal("a", group.Value);
    }

    [StaFact]
    public void ArrowDown_ClampsAtEnd_WhenLoopIsFalse()
    {
        var (group, a, b, c) = CreateGroup(loop: false);
        SimulateClick(c);

        SimulateKey(group, Key.Down);

        Assert.Equal("c", group.Value);
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLastEnabledItem()
    {
        var (group, a, b, c) = CreateGroup();
        SimulateClick(b);

        SimulateKey(group, Key.End);
        Assert.Equal("c", group.Value);

        SimulateKey(group, Key.Home);
        Assert.Equal("a", group.Value);
    }

    [StaFact]
    public void ArrowRight_IsMirroredUnderRtl()
    {
        var (group, a, b, _) = CreateGroup(dir: "rtl");
        SimulateClick(b);

        SimulateKey(group, Key.Right);

        Assert.Equal("a", group.Value);
    }

    [StaFact]
    public void AutomationPeer_RootReportsGroup_ItemReportsRadioButtonWithSelectionItemPattern()
    {
        var (group, a, _, _) = CreateGroup();

        var rootPeer = group.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(group, null) as AutomationPeer;
        Assert.NotNull(rootPeer);
        Assert.Equal(AutomationControlType.Group, rootPeer!.GetAutomationControlType());

        var itemPeer = new RadioButtonAutomationPeer(a);
        Assert.Equal(AutomationControlType.RadioButton, itemPeer.GetAutomationControlType());
        Assert.IsAssignableFrom<ISelectionItemProvider>(itemPeer);
    }
}
