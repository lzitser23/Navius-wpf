using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Threading;
using Navius.Wpf.Ui.Sidebar;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiSidebarTests
{
    [Theory]
    [InlineData(-1, 5, Key.Down, 0)]
    [InlineData(0, 5, Key.Down, 1)]
    [InlineData(4, 5, Key.Down, 4)] // no wrap past the last item
    [InlineData(-1, 5, Key.Up, 4)]
    [InlineData(2, 5, Key.Up, 1)]
    [InlineData(0, 5, Key.Up, 0)] // no wrap past the first item
    [InlineData(3, 5, Key.Home, 0)]
    [InlineData(1, 5, Key.End, 4)]
    public void MoveFocus_ComputesExpectedIndex(int current, int count, Key key, int expected)
    {
        Assert.Equal(expected, SidebarNavigation.MoveFocus(current, count, key));
    }

    [Fact]
    public void MoveFocus_NoItems_ReturnsNegativeOne()
    {
        Assert.Equal(-1, SidebarNavigation.MoveFocus(-1, 0, Key.Down));
    }

    [Fact]
    public void MoveFocus_UnhandledKey_ReturnsNegativeOne()
    {
        Assert.Equal(-1, SidebarNavigation.MoveFocus(0, 5, Key.Space));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ToggleCollapsed_FlipsState(bool current, bool expected)
    {
        Assert.Equal(expected, SidebarNavigation.ToggleCollapsed(current));
    }

    [StaFact]
    public void NaviusSidebar_Defaults_AreExpanded()
    {
        var sidebar = new NaviusSidebar();

        Assert.False(sidebar.IsCollapsed);
        Assert.Equal(240d, sidebar.ExpandedWidth);
        Assert.Equal(64d, sidebar.CollapsedWidth);
    }

    [StaFact]
    public void NaviusSidebar_ToggleCollapsedCommand_FlipsIsCollapsed()
    {
        var sidebar = new NaviusSidebar();

        NaviusSidebar.ToggleCollapsedCommand.Execute(null, sidebar);
        Assert.True(sidebar.IsCollapsed);

        NaviusSidebar.ToggleCollapsedCommand.Execute(null, sidebar);
        Assert.False(sidebar.IsCollapsed);
    }

    [StaFact]
    public void NaviusSidebarItem_IsActive_SetsItemStatusAutomationProperty()
    {
        var item = new NaviusSidebarItem { IsActive = true };

        Assert.Equal("current", System.Windows.Automation.AutomationProperties.GetItemStatus(item));

        item.IsActive = false;

        Assert.Equal(string.Empty, System.Windows.Automation.AutomationProperties.GetItemStatus(item));
    }

    [StaFact]
    public void NaviusSidebarItem_AutomationPeer_ReportsButtonControlType()
    {
        var item = new NaviusSidebarItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Button, peer!.GetAutomationControlType());
    }

    [StaFact]
    public void NaviusSidebarItem_AutomationPeer_ExposesInvokePattern()
    {
        var item = new NaviusSidebarItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.IsAssignableFrom<IInvokeProvider>(peer!.GetPattern(PatternInterface.Invoke));
    }

    [StaFact]
    public void NaviusSidebarItem_AutomationPeer_DisabledInvoke_Throws()
    {
        var item = new NaviusSidebarItem { IsEnabled = false };

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        var invoke = (IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke);

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
    }

    [StaFact]
    public void NaviusSidebarItem_AutomationPeer_EnabledInvoke_RaisesClick()
    {
        var item = new NaviusSidebarItem();
        var clicked = false;
        item.Click += (_, _) => clicked = true;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        // Invoke queues activation on the dispatcher per the UIA contract, so it has not run yet;
        // pump at Background priority (below the Input priority the peer queues at) to flush it.
        PumpDispatcher();

        Assert.True(clicked);
    }

    [StaFact]
    public void NaviusSidebarItem_AutomationPeer_EnabledInvoke_ExecutesBoundCommandWithParameter()
    {
        object? received = null;
        var executions = 0;
        var command = new RelayCommand(p => { received = p; executions++; }, _ => true);
        var parameter = new object();
        var clicks = 0;
        var item = new NaviusSidebarItem { Command = command, CommandParameter = parameter };
        item.Click += (_, _) => clicks++;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        // Invoke is queued on the dispatcher (UIA contract); pump before asserting the command ran.
        PumpDispatcher();

        Assert.Equal(1, executions);
        Assert.Same(parameter, received);
        Assert.Equal(1, clicks);
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);
    }
}
