using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Toolbar;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ToolbarTests : IDisposable
{
    static ToolbarTests()
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

    private static readonly MethodInfo OnPreviewKeyDownMethod =
        typeof(NaviusToolbar).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on (see the
    // HwndSource-disposal fix that added this class's IDisposable.Dispose()).
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusToolbarTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static KeyEventArgs MakeKeyArgs(Key key, PresentationSource source)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, source, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        return args;
    }

    private void SimulateToolbarKeyDown(NaviusToolbar toolbar, Key key) =>
        OnPreviewKeyDownMethod.Invoke(toolbar, new object[] { toolbar, MakeKeyArgs(key, TestSource) });

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Toolbar.xaml"),
        });

        return scope;
    }

    private static (NaviusToolbar Toolbar, NaviusToolbarButton A, NaviusToolbarButton B, NaviusToolbarButton C) CreateToolbar(
        bool loop = true, string? dir = null, string orientation = "horizontal")
    {
        var a = new NaviusToolbarButton { Content = "A" };
        var b = new NaviusToolbarButton { Content = "B" };
        var c = new NaviusToolbarButton { Content = "C" };
        var toolbar = new NaviusToolbar
        {
            Loop = loop,
            Dir = dir,
            Orientation = orientation,
            Content = new StackPanel { Children = { a, b, c } },
        };

        return (toolbar, a, b, c);
    }

    /// <summary>
    /// Hosts the item in a real (never-shown) HwndSource so routed key events carry the item as
    /// OriginalSource. Native ButtonBase's Enter path is gated on e.OriginalSource == this (plus
    /// KeyboardNavigation.AcceptsReturn, which ButtonBase defaults to true), so directly invoking
    /// OnKeyDown with a fabricated KeyEventArgs never exercises that gate and would falsely read
    /// as "Enter is dead" -- the same HwndSource-hosted pattern SwitchTests uses post-M6.
    /// Callers whose <paramref name="element"/> IS the intended focus/key target (e.g. a bare
    /// toolbar button) should follow up with <see cref="FocusAndPump"/> to deterministically wait
    /// for real focus before raising a synthetic key; callers hosting a non-focusable container
    /// (e.g. a toggle group, where a nested item is the real target) rely on this method's plain
    /// Focus() call being a harmless no-op instead.
    /// </summary>
    private static T CreateHosted<T>(T element, out HwndSource source) where T : UIElement
    {
        source = new HwndSource(new HwndSourceParameters("NaviusToolbarKeyTests", 100, 100)) { RootVisual = element };
        element.Focus();
        return element;
    }

    /// <summary>
    /// Focuses <paramref name="element"/> and pumps the dispatcher at Background priority before
    /// asserting the focus actually landed. Focus() on a freshly-created HwndSource does not
    /// always take effect synchronously; without pumping and verifying here, a synthetic
    /// RaiseKey call immediately afterward can race the real focus change and be silently
    /// dropped, producing an intermittent failure a small percentage of runs.
    /// </summary>
    private static void FocusAndPump(UIElement element)
    {
        element.Focus();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        Assert.Same(element, Keyboard.FocusedElement);
    }

    private static void RaiseKey(UIElement target, Key key, RoutedEvent routedEvent, PresentationSource source) =>
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = routedEvent });

    // --- Template application ---

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var (toolbar, a, _, _) = CreateToolbar();
        toolbar.Resources = scope;
        a.Resources = scope;
        var link = new NaviusToolbarLink { Resources = scope };
        var group = new NaviusToolbarToggleGroup { Resources = scope };
        var toggleItem = new NaviusToolbarToggleItem { Resources = scope };

        Assert.True(toolbar.ApplyTemplate());
        Assert.True(a.ApplyTemplate());
        Assert.True(link.ApplyTemplate());
        Assert.True(group.ApplyTemplate());
        Assert.True(toggleItem.ApplyTemplate());
    }

    [StaFact]
    public void ContentAlignment_ExplicitLeft_ForwardsToContentPresenter()
    {
        // Regression: NaviusToolbarButton, NaviusToolbarLink and NaviusToolbarToggleItem all
        // hardcoded Center on their ContentPresenter, ignoring HorizontalContentAlignment. Covers
        // NaviusToolbarButton as the representative case -- the other two share the identical
        // template shape (Border > ContentPresenter, Margin={TemplateBinding Padding}).
        var content = new Border { Width = 20, Height = 10 };
        var button = new NaviusToolbarButton
        {
            Content = content,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Width = 200,
            Height = 40,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Resources = CreateThemedScope(),
        };

        button.ApplyTemplate();
        button.Measure(new Size(200, 40));
        button.Arrange(new Rect(0, 0, 200, 40));

        var offset = content.TranslatePoint(new Point(0, 0), button);
        Assert.Equal(0, offset.X, 3);
    }

    // --- Roving tab stop ---

    [StaFact]
    public void RovingTabStop_DefaultsToFirstEnabledItem()
    {
        var (_, a, b, c) = CreateToolbar();

        Assert.True(a.IsTabStop);
        Assert.False(b.IsTabStop);
        Assert.False(c.IsTabStop);
    }

    [StaFact]
    public void RovingTabStop_SkipsDisabledFirstItem()
    {
        var a = new NaviusToolbarButton { Content = "A", Disabled = true };
        var b = new NaviusToolbarButton { Content = "B" };
        var toolbar = new NaviusToolbar { Content = new StackPanel { Children = { a, b } } };

        Assert.False(a.IsTabStop);
        Assert.True(b.IsTabStop);
    }

    [StaFact]
    public void ArrowRight_MovesFocus_AcrossMixedItemTypes()
    {
        var a = new NaviusToolbarButton { Content = "A" };
        var link = new NaviusToolbarLink { Content = "Link" };
        var toolbar = new NaviusToolbar { Content = new StackPanel { Children = { a, link } } };
        FocusManager.SetFocusedElement(toolbar, a);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.Same(link, FocusManager.GetFocusedElement(toolbar));
        Assert.True(link.IsTabStop);
        Assert.False(a.IsTabStop);
    }

    [StaFact]
    public void ArrowRight_WrapsAtEnd_WhenLooping()
    {
        var (toolbar, a, _, c) = CreateToolbar(loop: true);
        FocusManager.SetFocusedElement(toolbar, c);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.Same(a, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void ArrowRight_ClampsAtEnd_WhenLoopIsFalse()
    {
        var (toolbar, _, _, c) = CreateToolbar(loop: false);
        FocusManager.SetFocusedElement(toolbar, c);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.Same(c, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void ArrowRight_IsMirroredUnderRtl()
    {
        var (toolbar, a, b, _) = CreateToolbar(dir: "rtl");
        FocusManager.SetFocusedElement(toolbar, b);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.Same(a, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void ArrowKeys_AreIgnored_WhenOrientationIsVertical()
    {
        var (toolbar, a, b, _) = CreateToolbar(orientation: "vertical");
        FocusManager.SetFocusedElement(toolbar, a);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.NotSame(b, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void ArrowDownUp_MoveFocus_WhenOrientationIsVertical()
    {
        var (toolbar, a, b, c) = CreateToolbar(orientation: "vertical");
        FocusManager.SetFocusedElement(toolbar, a);

        SimulateToolbarKeyDown(toolbar, Key.Down);
        Assert.Same(b, FocusManager.GetFocusedElement(toolbar));

        SimulateToolbarKeyDown(toolbar, Key.Down);
        Assert.Same(c, FocusManager.GetFocusedElement(toolbar));

        SimulateToolbarKeyDown(toolbar, Key.Up);
        Assert.Same(b, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLastItem()
    {
        var (toolbar, a, b, c) = CreateToolbar();
        FocusManager.SetFocusedElement(toolbar, b);

        SimulateToolbarKeyDown(toolbar, Key.End);
        Assert.Same(c, FocusManager.GetFocusedElement(toolbar));

        SimulateToolbarKeyDown(toolbar, Key.Home);
        Assert.Same(a, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void RovingFocus_SkipsDisabledItems()
    {
        var a = new NaviusToolbarButton { Content = "A" };
        var b = new NaviusToolbarButton { Content = "B", Disabled = true };
        var c = new NaviusToolbarButton { Content = "C" };
        var toolbar = new NaviusToolbar { Content = new StackPanel { Children = { a, b, c } } };
        FocusManager.SetFocusedElement(toolbar, a);

        SimulateToolbarKeyDown(toolbar, Key.Right);

        Assert.Same(c, FocusManager.GetFocusedElement(toolbar));
    }

    // --- Toggle items share the toolbar's single roving domain ---

    [StaFact]
    public void RovingFocus_ReachesToggleItems_NestedInsideToolbarToggleGroup()
    {
        var a = new NaviusToolbarButton { Content = "A" };
        var t1 = new NaviusToolbarToggleItem { Value = "bold", Content = "B" };
        var t2 = new NaviusToolbarToggleItem { Value = "italic", Content = "I" };
        var group = new NaviusToolbarToggleGroup { Content = new StackPanel { Children = { t1, t2 } } };
        var toolbar = new NaviusToolbar { Content = new StackPanel { Children = { a, group } } };
        FocusManager.SetFocusedElement(toolbar, a);

        SimulateToolbarKeyDown(toolbar, Key.Right);
        Assert.Same(t1, FocusManager.GetFocusedElement(toolbar));

        SimulateToolbarKeyDown(toolbar, Key.Right);
        Assert.Same(t2, FocusManager.GetFocusedElement(toolbar));
    }

    [StaFact]
    public void ToolbarToggleGroup_DoesNotOwnItsOwnRovingTabStops()
    {
        // The group root itself never becomes an IToolbarItem/tab stop; only its items do,
        // and only via the ancestor NaviusToolbar's scan (asserted by the nested-focus test
        // above). This just confirms the group root stays non-focusable.
        var group = new NaviusToolbarToggleGroup();

        Assert.False(group.Focusable);
    }

    // --- Click activation ---

    [StaFact]
    public void Click_ActivatesToolbarButton()
    {
        var clicked = 0;
        var button = new NaviusToolbarButton { Content = "A" };
        button.Click += (_, _) => clicked++;

        SimulateClick(button);

        Assert.Equal(1, clicked);
    }

    // --- Toggle group pressed-set semantics (ported ComputeNext) ---

    [StaFact]
    public void SingleType_Click_PressesItem_AndRaisesValueChanged()
    {
        var a = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = new StackPanel { Children = { a } } };
        var raised = 0;
        group.ValueChanged += (_, _) => raised++;

        SimulateClick(a);

        Assert.Single(group.Value);
        Assert.Contains("a", group.Value);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void SingleType_ClickingPressedItemAgain_ClearsSelection()
    {
        var a = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = new StackPanel { Children = { a } } };
        SimulateClick(a);

        SimulateClick(a);

        Assert.Empty(group.Value);
        Assert.False(a.IsChecked);
    }

    [StaFact]
    public void SingleType_ClickingDifferentItem_ReplacesSelection()
    {
        var a = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var b = new NaviusToolbarToggleItem { Value = "b", Content = "B" };
        var group = new NaviusToolbarToggleGroup { Content = new StackPanel { Children = { a, b } } };
        SimulateClick(a);

        SimulateClick(b);

        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Single(group.Value);
        Assert.Contains("b", group.Value);
    }

    [StaFact]
    public void MultipleType_EachItemTogglesIndependently()
    {
        var a = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var b = new NaviusToolbarToggleItem { Value = "b", Content = "B" };
        var group = new NaviusToolbarToggleGroup
        {
            Type = "multiple",
            Content = new StackPanel { Children = { a, b } },
        };

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
    public void GroupDisabled_CascadesToEveryItem_CombinedWithItsOwnDisabled()
    {
        var a = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var b = new NaviusToolbarToggleItem { Value = "b", Content = "B", Disabled = true };
        var group = new NaviusToolbarToggleGroup { Content = new StackPanel { Children = { a, b } } };

        Assert.True(a.IsEnabled);
        Assert.False(b.IsEnabled);

        group.Disabled = true;

        Assert.False(a.IsEnabled);
        Assert.False(b.IsEnabled);

        group.Disabled = false;

        // b stays disabled: its own Disabled flag is independent of the group's.
        Assert.True(a.IsEnabled);
        Assert.False(b.IsEnabled);
    }

    // --- Space/Enter activation on NaviusToolbarButton (real routed key events; the M6 a11y regression this guards against) ---

    [StaFact]
    public void SpaceKey_ActivatesToolbarButton()
    {
        var clicked = 0;
        var button = CreateHosted(new NaviusToolbarButton { Content = "A" }, out var source);
        using var _ = source;
        FocusAndPump(button);
        button.Click += (_, _) => clicked++;

        // ButtonBase (ClickMode.Release) presses on KeyDown and clicks on KeyUp -- but the native
        // Space path also reads live thread input state (see InputState): OnKeyUp suppresses the
        // click while the thread sees the left mouse button down, and OnKeyDown ignores Space
        // while Alt reads held or the mouse is captured. Neutralize the thread state each
        // attempt and retry briefly (releasing any capture a failed KeyUp left behind) to cover
        // a real gesture arriving mid-attempt via the capture ButtonBase takes.
        for (var attempt = 0; attempt < 20 && clicked == 0; attempt++)
        {
            if (attempt > 0) Thread.Sleep(100);
            InputState.Neutralize();
            RaiseKey(button, Key.Space, Keyboard.KeyDownEvent, source);
            RaiseKey(button, Key.Space, Keyboard.KeyUpEvent, source);
            if (button.IsMouseCaptured) button.ReleaseMouseCapture();
        }

        Assert.Equal(1, clicked);
    }

    [StaFact]
    public void EnterKey_ActivatesToolbarButton()
    {
        var clicked = 0;
        var button = CreateHosted(new NaviusToolbarButton { Content = "A" }, out var source);
        using var _ = source;
        FocusAndPump(button);
        button.Click += (_, _) => clicked++;

        RaiseKey(button, Key.Enter, Keyboard.KeyDownEvent, source);

        Assert.Equal(1, clicked);
    }

    // --- Space/Enter activation on NaviusToolbarToggleItem, including the IsRepeat guard ---

    private static readonly MethodInfo OnKeyDownMethod =
        typeof(NaviusToolbarToggleItem).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private void SimulateItemKeyDown(NaviusToolbarToggleItem item, Key key, bool isRepeat = false)
    {
        var args = MakeKeyArgs(key, TestSource);
        if (isRepeat)
        {
            typeof(KeyEventArgs)
                .GetMethod("SetRepeat", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(args, new object[] { true });
        }

        OnKeyDownMethod.Invoke(item, new object[] { args });
    }

    [StaFact]
    public void SpaceKey_TogglesToolbarToggleItem_ViaRealRoutedKeyEvent()
    {
        var item = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = item };
        CreateHosted(group, out var source);
        using var _ = source;
        FocusAndPump(item);

        RaiseKey(item, Key.Space, Keyboard.KeyDownEvent, source);
        RaiseKey(item, Key.Space, Keyboard.KeyUpEvent, source);

        Assert.True(item.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void EnterKey_TogglesToolbarToggleItem_ViaRealRoutedKeyEvent()
    {
        var item = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = item };
        CreateHosted(group, out var source);
        using var _ = source;
        FocusAndPump(item);

        RaiseKey(item, Key.Enter, Keyboard.KeyDownEvent, source);

        Assert.True(item.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void Space_AutoRepeat_DoesNotFlapPressedState()
    {
        var item = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = item };

        // First (non-repeat) Space press toggles on.
        SimulateItemKeyDown(item, Key.Space);
        Assert.True(item.IsChecked);

        // Auto-repeated Space key-downs from holding the key must be ignored: a native web
        // button fires Space once on key-up, so a held Space never flaps the pressed state.
        SimulateItemKeyDown(item, Key.Space, isRepeat: true);

        Assert.True(item.IsChecked);
        Assert.Contains("a", group.Value);
    }

    [StaFact]
    public void Enter_AutoRepeat_IsAllowed_LikeNativeWebButton()
    {
        var item = new NaviusToolbarToggleItem { Value = "a", Content = "A" };
        var group = new NaviusToolbarToggleGroup { Content = item };

        SimulateItemKeyDown(item, Key.Enter);
        Assert.True(item.IsChecked);

        SimulateItemKeyDown(item, Key.Enter, isRepeat: true);
        Assert.False(item.IsChecked);
    }

    // --- AutomationPeer ---

    [StaFact]
    public void AutomationPeer_RootReportsToolBar_WithOrientation()
    {
        var (toolbar, _, _, _) = CreateToolbar(orientation: "vertical");

        var peer = toolbar.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(toolbar, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.ToolBar, peer!.GetAutomationControlType());
        Assert.Equal(AutomationOrientation.Vertical, ((NaviusToolbarAutomationPeer)peer).GetOrientation());
    }

    [StaFact]
    public void AutomationPeer_RootDefaultsToHorizontalOrientation()
    {
        var (toolbar, _, _, _) = CreateToolbar();

        var peer = new NaviusToolbarAutomationPeer(toolbar);

        Assert.Equal(AutomationOrientation.Horizontal, peer.GetOrientation());
    }

    [StaFact]
    public void AutomationPeer_ToggleGroupReportsGroup()
    {
        var group = new NaviusToolbarToggleGroup();

        var peer = group.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(group, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Group, peer!.GetAutomationControlType());
    }
}
