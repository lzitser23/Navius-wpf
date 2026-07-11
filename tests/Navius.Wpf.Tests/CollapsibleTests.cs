using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Collapsible;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class CollapsibleTests
{
    static CollapsibleTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Collapsible.xaml"),
        });

        return scope;
    }

    private static (NaviusCollapsible Root, NaviusCollapsibleTrigger Trigger, NaviusCollapsiblePanel Panel) CreateCollapsible()
    {
        var trigger = new NaviusCollapsibleTrigger { Content = "Toggle" };
        var panel = new NaviusCollapsiblePanel { Content = "Body" };
        var root = new NaviusCollapsible
        {
            Content = new StackPanel { Children = { trigger, panel } },
        };

        return (root, trigger, panel);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var (root, trigger, panel) = CreateCollapsible();
        root.Resources = scope;
        trigger.Resources = scope;
        panel.Resources = scope;

        Assert.True(root.ApplyTemplate());
        Assert.True(trigger.ApplyTemplate());
        Assert.True(panel.ApplyTemplate());
    }

    [StaFact]
    public void DefaultState_IsClosed()
    {
        var (root, trigger, panel) = CreateCollapsible();

        Assert.False(root.Open);
        Assert.False(trigger.IsPanelOpen);
        Assert.False(panel.IsOpen);
    }

    [StaFact]
    public void Click_OpensAndRaisesEvent_ThenTogglesClosedAgain()
    {
        var (root, trigger, _) = CreateCollapsible();
        var raised = 0;
        root.OpenChanged += (_, _) => raised++;

        SimulateClick(trigger);

        Assert.True(root.Open);
        Assert.True(trigger.IsPanelOpen);
        Assert.Equal(1, raised);

        SimulateClick(trigger);

        Assert.False(root.Open);
        Assert.False(trigger.IsPanelOpen);
        Assert.Equal(2, raised);
    }

    [StaFact]
    public void Panel_IsOpen_TracksRootState()
    {
        var (root, _, panel) = CreateCollapsible();

        root.Open = true;
        Assert.True(panel.IsOpen);

        root.Open = false;
        Assert.False(panel.IsOpen);
    }

    [StaFact]
    public void Disabled_CascadesFromRoot_BlocksToggle()
    {
        var (root, trigger, _) = CreateCollapsible();

        root.IsEnabled = false;

        Assert.False(trigger.IsEnabled);
    }

    [StaFact]
    public void KeepMounted_DefaultsToFalse()
    {
        var panel = new NaviusCollapsiblePanel();

        Assert.False(panel.KeepMounted);
    }

    [StaFact]
    public void TriggerAutomationPeer_ReportsButtonWithInvokeAndExpandCollapsePatterns()
    {
        var (root, trigger, _) = CreateCollapsible();

        var peer = (AutomationPeer)trigger.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(trigger, null)!;
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());

        var invoke = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
        var expandCollapse = peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
        Assert.NotNull(invoke);
        Assert.NotNull(expandCollapse);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse!.ExpandCollapseState);

        invoke!.Invoke();
        // Invoke queues activation on the dispatcher per the UIA contract, so pump before asserting.
        PumpDispatcher();

        Assert.True(root.Open);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
    }

    [StaFact]
    public void DisabledTriggerAutomationPeer_InvokeAndExpand_ThrowAndDoNotToggle()
    {
        // Regression (DEFECT 2): a disabled collapsible trigger must not be operable through UIA.
        var (root, trigger, _) = CreateCollapsible();
        root.IsEnabled = false;
        Assert.False(trigger.IsEnabled);

        var peer = (AutomationPeer)trigger.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(trigger, null)!;

        Assert.False(peer.IsEnabled());

        var invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!;
        var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
        Assert.Throws<ElementNotEnabledException>(() => expandCollapse.Expand());

        Assert.False(root.Open);
        Assert.False(trigger.IsPanelOpen);
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
        var trigger = new NaviusCollapsibleTrigger { Command = command };
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
        var trigger = new NaviusCollapsibleTrigger();
        trigger.Click += (_, _) => clicks++;

        var invoke = (IInvokeProvider)PeerFor(trigger).GetPattern(PatternInterface.Invoke)!;
        invoke.Invoke();

        Assert.Equal(0, clicks);

        PumpDispatcher();

        Assert.Equal(1, clicks);
    }

    [StaFact]
    public void TriggerAutomationPeer_Expand_IsAsync_OpensPanelAfterPump()
    {
        var (root, trigger, _) = CreateCollapsible();
        var expandCollapse = (IExpandCollapseProvider)PeerFor(trigger).GetPattern(PatternInterface.ExpandCollapse)!;

        expandCollapse.Expand();

        Assert.False(root.Open);

        PumpDispatcher();

        Assert.True(root.Open);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
    }

    private static AutomationPeer PeerFor(NaviusCollapsibleTrigger trigger) =>
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
