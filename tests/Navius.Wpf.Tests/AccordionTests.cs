using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Accordion;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class AccordionTests : IDisposable
{
    static AccordionTests()
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
        typeof(NaviusAccordion).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusAccordionTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private void SimulateKey(NaviusAccordion root, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        OnPreviewKeyDownMethod.Invoke(root, new object[] { root, args });
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Accordion.xaml"),
        });

        return scope;
    }

    private sealed record Section(
        NaviusAccordionItem Item,
        NaviusAccordionHeader Header,
        NaviusAccordionTrigger Trigger,
        NaviusAccordionPanel Panel);

    private static Section MakeSection(string value)
    {
        var trigger = new NaviusAccordionTrigger { Content = value };
        var header = new NaviusAccordionHeader { Content = trigger };
        var panel = new NaviusAccordionPanel { Content = $"{value} body" };
        var item = new NaviusAccordionItem
        {
            Value = value,
            Content = new StackPanel { Children = { header, panel } },
        };

        return new Section(item, header, trigger, panel);
    }

    private static (NaviusAccordion Root, Section A, Section B, Section C) CreateAccordion(
        string type = "single", bool collapsible = false)
    {
        var a = MakeSection("a");
        var b = MakeSection("b");
        var c = MakeSection("c");
        var root = new NaviusAccordion
        {
            Type = type,
            Collapsible = collapsible,
            Content = new StackPanel { Children = { a.Item, b.Item, c.Item } },
        };

        return (root, a, b, c);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var (root, a, _, _) = CreateAccordion();
        root.Resources = scope;
        a.Item.Resources = scope;
        a.Header.Resources = scope;
        a.Trigger.Resources = scope;
        a.Panel.Resources = scope;

        Assert.True(root.ApplyTemplate());
        Assert.True(a.Item.ApplyTemplate());
        Assert.True(a.Header.ApplyTemplate());
        Assert.True(a.Trigger.ApplyTemplate());
        Assert.True(a.Panel.ApplyTemplate());
    }

    [StaFact]
    public void DefaultState_NothingOpen()
    {
        var (root, a, b, c) = CreateAccordion();

        Assert.Null(root.Value);
        Assert.False(a.Trigger.IsPanelOpen);
        Assert.False(b.Trigger.IsPanelOpen);
        Assert.False(c.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void SingleMode_ClickOpensItem_AndRaisesValueChanged()
    {
        var (root, a, _, _) = CreateAccordion();
        var raised = 0;
        root.ValueChanged += (_, _) => raised++;

        SimulateClick(a.Trigger);

        Assert.Equal("a", root.Value);
        Assert.True(a.Trigger.IsPanelOpen);
        Assert.True(a.Panel.IsOpen);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void SingleMode_ClickingAnotherItem_ClosesThePrevious()
    {
        var (root, a, b, _) = CreateAccordion();
        SimulateClick(a.Trigger);

        SimulateClick(b.Trigger);

        Assert.Equal("b", root.Value);
        Assert.False(a.Trigger.IsPanelOpen);
        Assert.True(b.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void SingleMode_NotCollapsible_ClickingOpenItemAgain_StaysOpen()
    {
        var (root, a, _, _) = CreateAccordion(collapsible: false);
        SimulateClick(a.Trigger);

        SimulateClick(a.Trigger);

        Assert.Equal("a", root.Value);
        Assert.True(a.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void SingleMode_Collapsible_ClickingOpenItemAgain_Closes()
    {
        var (root, a, _, _) = CreateAccordion(collapsible: true);
        SimulateClick(a.Trigger);

        SimulateClick(a.Trigger);

        Assert.Null(root.Value);
        Assert.False(a.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void MultipleMode_BothItemsCanBeOpenSimultaneously()
    {
        var (root, a, b, _) = CreateAccordion(type: "multiple");
        var raised = 0;
        root.ValuesChanged += (_, _) => raised++;

        SimulateClick(a.Trigger);
        SimulateClick(b.Trigger);

        Assert.Equal(2, root.Values.Count);
        Assert.Contains("a", root.Values);
        Assert.Contains("b", root.Values);
        Assert.True(a.Trigger.IsPanelOpen);
        Assert.True(b.Trigger.IsPanelOpen);
        Assert.Equal(2, raised);

        SimulateClick(a.Trigger);

        Assert.Single(root.Values);
        Assert.Contains("b", root.Values);
        Assert.False(a.Trigger.IsPanelOpen);
        Assert.True(b.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void Disabled_CascadesFromItem_BlocksToggle()
    {
        var (root, a, _, _) = CreateAccordion();

        a.Item.IsEnabled = false;

        Assert.False(a.Trigger.IsEnabled);
    }

    [StaFact]
    public void Header_DefaultLevel_IsThree_AndPublishesAutomationHeadingLevel()
    {
        var header = new NaviusAccordionHeader();

        Assert.Equal(3, header.Level);
        Assert.Equal(AutomationHeadingLevel.Level3, AutomationProperties.GetHeadingLevel(header));

        header.Level = 1;
        Assert.Equal(AutomationHeadingLevel.Level1, AutomationProperties.GetHeadingLevel(header));
    }

    [StaFact]
    public void ArrowDown_MovesFocusToNextTrigger_AndWraps()
    {
        var (root, a, b, c) = CreateAccordion();
        FocusManager.SetFocusedElement(root, a.Trigger);

        SimulateKey(root, Key.Down);
        Assert.Same(b.Trigger, FocusManager.GetFocusedElement(root));

        SimulateKey(root, Key.Down);
        Assert.Same(c.Trigger, FocusManager.GetFocusedElement(root));

        SimulateKey(root, Key.Down);
        Assert.Same(a.Trigger, FocusManager.GetFocusedElement(root));
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLastTrigger()
    {
        var (root, a, _, c) = CreateAccordion();
        FocusManager.SetFocusedElement(root, a.Trigger);

        SimulateKey(root, Key.End);
        Assert.Same(c.Trigger, FocusManager.GetFocusedElement(root));

        SimulateKey(root, Key.Home);
        Assert.Same(a.Trigger, FocusManager.GetFocusedElement(root));
    }

    [StaFact]
    public void ArrowKeys_NeverChangeOpenState()
    {
        var (root, a, b, _) = CreateAccordion();
        SimulateClick(a.Trigger);
        FocusManager.SetFocusedElement(root, a.Trigger);

        SimulateKey(root, Key.Down);

        Assert.Equal("a", root.Value);
        Assert.False(b.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void RootAutomationPeer_ReportsGroup()
    {
        var (root, _, _, _) = CreateAccordion();

        var peer = (AutomationPeer)root.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(root, null)!;

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
    }

    [StaFact]
    public void TriggerAutomationPeer_ReportsButtonWithInvokeAndExpandCollapsePatterns()
    {
        var (root, a, _, _) = CreateAccordion();

        var peer = (AutomationPeer)a.Trigger.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(a.Trigger, null)!;

        var invoke = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
        var expandCollapse = peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
        Assert.NotNull(invoke);
        Assert.NotNull(expandCollapse);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse!.ExpandCollapseState);

        invoke!.Invoke();
        // Invoke queues activation on the dispatcher per the UIA contract, so pump before asserting.
        PumpDispatcher();

        Assert.Equal("a", root.Value);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
    }

    [StaFact]
    public void DisabledTriggerAutomationPeer_InvokeAndExpand_ThrowAndDoNotToggle()
    {
        // Regression (DEFECT 2): a disabled trigger must not be operable through UIA. The
        // Invoke and ExpandCollapse providers previously raised Click regardless of IsEnabled,
        // so a screen reader could expand a disabled accordion section.
        var (root, a, _, _) = CreateAccordion();
        a.Item.IsEnabled = false;
        Assert.False(a.Trigger.IsEnabled);

        var peer = (AutomationPeer)a.Trigger.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(a.Trigger, null)!;

        Assert.False(peer.IsEnabled());

        var invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!;
        var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
        Assert.Throws<ElementNotEnabledException>(() => expandCollapse.Expand());

        Assert.Null(root.Value);
        Assert.False(a.Trigger.IsPanelOpen);
    }

    [StaFact]
    public void TriggerAutomationPeer_Invoke_ExecutesBoundCommandOnceAndRaisesClickOnce()
    {
        // Regression: the peer previously raised ClickEvent with a bare RaiseEvent, which fires Click
        // but skips ButtonBase.OnClick, so a bound Command never executed from UIA. Routing through
        // OnClick executes the command exactly once and raises Click exactly once.
        var executions = 0;
        var clicks = 0;
        var command = new RelayCommand(_ => executions++);
        var trigger = new NaviusAccordionTrigger { Command = command };
        trigger.Click += (_, _) => clicks++;

        var invoke = (IInvokeProvider)PeerFor(trigger).GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();
        PumpDispatcher();

        Assert.Equal(1, executions);
        Assert.Equal(1, clicks);
    }

    [StaFact]
    public void TriggerAutomationPeer_Invoke_IsAsync_DoesNotActivateBeforePump()
    {
        // Regression: Invoke previously ran inline, violating the UIA contract that it return
        // immediately. The activation must be queued on the dispatcher, so nothing happens until
        // the queue is pumped. (Before the fix, this assert-without-pump would already see the click.)
        var clicks = 0;
        var trigger = new NaviusAccordionTrigger();
        trigger.Click += (_, _) => clicks++;

        var invoke = (IInvokeProvider)PeerFor(trigger).GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();

        Assert.Equal(0, clicks);

        PumpDispatcher();

        Assert.Equal(1, clicks);
    }

    [StaFact]
    public void TriggerAutomationPeer_ExpandAndCollapse_AreBlockingAndExecuteCommand()
    {
        var (root, a, _, _) = CreateAccordion(collapsible: true);
        var clicks = 0;
        var commandExecutions = 0;
        a.Trigger.Command = new RelayCommand(_ => commandExecutions++);
        a.Trigger.Click += (_, _) => clicks++;
        var expandCollapse = (IExpandCollapseProvider)PeerFor(a.Trigger).GetPattern(PatternInterface.ExpandCollapse)!;

        expandCollapse.Expand();

        Assert.Equal("a", root.Value);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
        Assert.Equal(1, clicks);
        Assert.Equal(1, commandExecutions);

        expandCollapse.Collapse();

        Assert.Null(root.Value);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
        Assert.Equal(2, clicks);
        Assert.Equal(2, commandExecutions);
    }

    [StaFact]
    public void TriggerAutomationPeer_RepeatedExpandAndCollapse_AreIdempotent()
    {
        var (root, a, _, _) = CreateAccordion(collapsible: true);
        var clicks = 0;
        a.Trigger.Click += (_, _) => clicks++;
        var expandCollapse = (IExpandCollapseProvider)PeerFor(a.Trigger).GetPattern(PatternInterface.ExpandCollapse)!;

        expandCollapse.Expand();
        expandCollapse.Expand();

        Assert.Equal("a", root.Value);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
        Assert.Equal(1, clicks);

        expandCollapse.Collapse();
        expandCollapse.Collapse();

        Assert.Null(root.Value);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
        Assert.Equal(2, clicks);
    }

    private static AutomationPeer PeerFor(NaviusAccordionTrigger trigger) =>
        (AutomationPeer)trigger.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(trigger, null)!;

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
