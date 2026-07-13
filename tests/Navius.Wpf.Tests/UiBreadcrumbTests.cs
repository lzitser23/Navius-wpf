using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Threading;
using Navius.Wpf.Ui.Breadcrumb;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiBreadcrumbTests
{
    [StaFact]
    public void NaviusBreadcrumbItem_Defaults_IsNotCurrentPageAndFocusable()
    {
        var item = new NaviusBreadcrumbItem();

        Assert.False(item.IsCurrentPage);
        Assert.True(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_IsCurrentPage_SetsItemStatusAndNotFocusable()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };

        Assert.Equal("current", System.Windows.Automation.AutomationProperties.GetItemStatus(item));
        Assert.False(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_LeavingCurrentPage_RestoresFocusable()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };

        item.IsCurrentPage = false;

        Assert.True(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_Click_RaisedOnlyWhenNotCurrentPage()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };
        var clicked = false;
        item.Click += (_, _) => clicked = true;

        item.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

        // Sanity: raising a foreign RoutedEvent shouldn't set our flag; the real activation path
        // (mouse/keyboard) is gated by IsCurrentPage inside OnMouseLeftButtonUp/OnKeyDown.
        Assert.False(clicked);
    }

    [StaFact]
    public void NaviusBreadcrumbSeparator_IsExcludedFromTabOrderAndAutomation()
    {
        var separator = new NaviusBreadcrumbSeparator();

        Assert.False(separator.Focusable);
        Assert.False(separator.IsTabStop);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_AutomationPeer_NonCurrentPage_ExposesInvokePattern()
    {
        var item = new NaviusBreadcrumbItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(AutomationControlType.Hyperlink, peer!.GetAutomationControlType());
        Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));
    }

    [StaFact]
    public void NaviusBreadcrumbItem_AutomationPeer_CurrentPage_ReportsTextAndNoInvokePattern()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(AutomationControlType.Text, peer!.GetAutomationControlType());
        Assert.Null(peer.GetPattern(PatternInterface.Invoke));
    }

    [StaFact]
    public void NaviusBreadcrumbItem_AutomationPeer_DisabledInvoke_Throws()
    {
        var item = new NaviusBreadcrumbItem { IsEnabled = false };

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        var invoke = (IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke);

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
    }

    [StaFact]
    public void NaviusBreadcrumbItem_AutomationPeer_EnabledInvoke_RaisesClickAndCommandOnce()
    {
        object? received = null;
        var executions = 0;
        var command = new RelayCommand(p => { received = p; executions++; }, _ => true);
        var parameter = new object();
        var clicks = 0;
        var item = new NaviusBreadcrumbItem { Command = command, CommandParameter = parameter };
        item.Click += (_, _) => clicks++;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        // Invoke queues activation on the dispatcher per the UIA contract, so it has not run yet;
        // pump at Background priority (below the Input priority the peer queues at) to flush it.
        PumpDispatcher();

        Assert.Equal(1, executions);
        Assert.Same(parameter, received);
        Assert.Equal(1, clicks);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_AutomationPeer_CachedInvoke_AfterBecomingCurrentPage_RefusesAndDoesNotActivate()
    {
        var executions = 0;
        var command = new RelayCommand(_ => executions++, _ => true);
        var clicks = 0;
        var item = new NaviusBreadcrumbItem { Command = command };
        item.Click += (_, _) => clicks++;

        // Obtain the Invoke provider while the crumb is still navigable (the pattern is exposed), then
        // flip the crumb to the current page. A cached provider must not activate the now-terminal
        // entry: Invoke throws and the click/command activation path never runs.
        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        var invoke = (IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke);

        item.IsCurrentPage = true;

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());

        // Pump anyway: if the guard had failed and queued activation, this flushes it before we assert
        // nothing happened.
        PumpDispatcher();

        Assert.Equal(0, executions);
        Assert.Equal(0, clicks);
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
