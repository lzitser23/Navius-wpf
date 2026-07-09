using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.ToggleGroup;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ToggleGroupTests
{
    static ToggleGroupTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo OnKeyDownMethod =
        typeof(NaviusToggleGroupItem).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo OnPreviewKeyDownMethod =
        typeof(NaviusToggleGroup).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    private static readonly PresentationSource TestSource =
        new HwndSource(0, 0, 0, 0, 0, "NaviusToggleGroupTests", IntPtr.Zero);

    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static KeyEventArgs MakeKeyArgs(Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        return args;
    }

    private static void SimulateItemKeyDown(NaviusToggleGroupItem item, Key key) =>
        OnKeyDownMethod.Invoke(item, new object[] { MakeKeyArgs(key) });

    private static void SimulateGroupKeyDown(NaviusToggleGroup group, Key key) =>
        OnPreviewKeyDownMethod.Invoke(group, new object[] { group, MakeKeyArgs(key) });

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/ToggleGroup.xaml"),
        });

        return scope;
    }

    private static (NaviusToggleGroup Group, NaviusToggleGroupItem A, NaviusToggleGroupItem B, NaviusToggleGroupItem C) CreateGroup(
        bool multiple = false, bool loop = true, string? dir = null, bool rovingFocus = true)
    {
        var a = new NaviusToggleGroupItem { Value = "a", Content = "A" };
        var b = new NaviusToggleGroupItem { Value = "b", Content = "B" };
        var c = new NaviusToggleGroupItem { Value = "c", Content = "C" };
        var group = new NaviusToggleGroup
        {
            Multiple = multiple,
            Loop = loop,
            Dir = dir,
            RovingFocus = rovingFocus,
            Content = new StackPanel { Children = { a, b, c } },
        };

        return (group, a, b, c);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var (group, a, _, _) = CreateGroup();
        group.Resources = scope;
        a.Resources = scope;

        Assert.True(group.ApplyTemplate());
        Assert.True(a.ApplyTemplate());
    }

    [StaFact]
    public void DefaultState_NothingPressed()
    {
        var (group, _, _, _) = CreateGroup();

        Assert.Empty(group.Value);
    }

    [StaFact]
    public void SingleMode_Click_PressesItem_AndRaisesValueChanged()
    {
        var (group, a, _, _) = CreateGroup();
        var raised = 0;
        group.ValueChanged += (_, _) => raised++;

        SimulateClick(a);

        Assert.Single(group.Value);
        Assert.Contains("a", group.Value);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void SingleMode_ClickingPressedItemAgain_ClearsSelection()
    {
        var (group, a, _, _) = CreateGroup();
        SimulateClick(a);

        SimulateClick(a);

        Assert.Empty(group.Value);
        Assert.False(a.IsChecked);
    }

    [StaFact]
    public void SingleMode_ClickingDifferentItem_ReplacesSelection()
    {
        var (group, a, b, _) = CreateGroup();
        SimulateClick(a);

        SimulateClick(b);

        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Single(group.Value);
        Assert.Contains("b", group.Value);
    }

    [StaFact]
    public void MultipleMode_EachItemTogglesIndependently()
    {
        var (group, a, b, _) = CreateGroup(multiple: true);

        SimulateClick(a);
        SimulateClick(b);

        Assert.True(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Equal(2, group.Value.Count);

        SimulateClick(a);

        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Single(group.Value);
    }

    [StaFact]
    public void Space_TogglesFocusedItem()
    {
        var (group, a, _, _) = CreateGroup();

        SimulateItemKeyDown(a, Key.Space);

        Assert.True(a.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void Enter_TogglesFocusedItem()
    {
        var (group, a, _, _) = CreateGroup();

        SimulateItemKeyDown(a, Key.Enter);

        Assert.True(a.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void Space_AutoRepeat_DoesNotFlapPressedState()
    {
        var (group, a, _, _) = CreateGroup();

        // First (non-repeat) Space press toggles on.
        SimulateItemKeyDown(a, Key.Space);
        Assert.True(a.IsChecked);

        // Auto-repeated Space key-downs from holding the key must be ignored: a native web
        // button fires Space once on key-up, so a held Space never flaps the pressed state.
        var repeatArgs = MakeKeyArgs(Key.Space);
        typeof(KeyEventArgs)
            .GetMethod("SetRepeat", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(repeatArgs, new object[] { true });
        OnKeyDownMethod.Invoke(a, new object[] { repeatArgs });

        Assert.True(a.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void Disabled_CascadesFromGroup_ToEveryItem()
    {
        var (group, a, b, c) = CreateGroup();

        group.IsEnabled = false;

        Assert.False(a.IsEnabled);
        Assert.False(b.IsEnabled);
        Assert.False(c.IsEnabled);
    }

    [StaFact]
    public void RovingTabStop_DefaultsToFirstEnabledItem_ThenFollowsPressedItem()
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
    public void RovingFocusFalse_EveryEnabledItemIsTabStop()
    {
        var (group, a, b, c) = CreateGroup(rovingFocus: false);

        Assert.True(a.IsTabStop);
        Assert.True(b.IsTabStop);
        Assert.True(c.IsTabStop);
    }

    [StaFact]
    public void ArrowRight_MovesFocus_ButDoesNotToggle()
    {
        var (group, a, b, _) = CreateGroup();
        FocusManager.SetFocusedElement(group, a);

        SimulateGroupKeyDown(group, Key.Right);

        Assert.Same(b, FocusManager.GetFocusedElement(group));
        Assert.False(b.IsChecked);
        Assert.Empty(group.Value);
    }

    [StaFact]
    public void ArrowRight_WrapsAtEnd_WhenLooping()
    {
        var (group, a, _, c) = CreateGroup(loop: true);
        FocusManager.SetFocusedElement(group, c);

        SimulateGroupKeyDown(group, Key.Right);

        Assert.Same(a, FocusManager.GetFocusedElement(group));
    }

    [StaFact]
    public void ArrowRight_ClampsAtEnd_WhenLoopIsFalse()
    {
        var (group, _, _, c) = CreateGroup(loop: false);
        FocusManager.SetFocusedElement(group, c);

        SimulateGroupKeyDown(group, Key.Right);

        Assert.Same(c, FocusManager.GetFocusedElement(group));
    }

    [StaFact]
    public void ArrowRight_IsMirroredUnderRtl()
    {
        var (group, a, b, _) = CreateGroup(dir: "rtl");
        FocusManager.SetFocusedElement(group, b);

        SimulateGroupKeyDown(group, Key.Right);

        Assert.Same(a, FocusManager.GetFocusedElement(group));
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLastItem()
    {
        var (group, a, b, c) = CreateGroup();
        FocusManager.SetFocusedElement(group, b);

        SimulateGroupKeyDown(group, Key.End);
        Assert.Same(c, FocusManager.GetFocusedElement(group));

        SimulateGroupKeyDown(group, Key.Home);
        Assert.Same(a, FocusManager.GetFocusedElement(group));
    }

    [StaFact]
    public void AutomationPeer_RootReportsGroup()
    {
        var (group, _, _, _) = CreateGroup();

        var peer = group.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(group, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Group, peer!.GetAutomationControlType());
    }
}
