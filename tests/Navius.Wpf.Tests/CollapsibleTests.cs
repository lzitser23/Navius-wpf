using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        Assert.True(root.Open);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
    }
}
