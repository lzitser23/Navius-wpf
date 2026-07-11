using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using Navius.Wpf.Ui.ButtonGroup;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiButtonGroupTests
{
    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_ReportsButtonControlType()
    {
        var item = new NaviusButtonGroupItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Button, peer!.GetAutomationControlType());
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_ExposesInvokePattern()
    {
        var item = new NaviusButtonGroupItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.IsAssignableFrom<IInvokeProvider>(peer!.GetPattern(PatternInterface.Invoke));
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_DisabledInvoke_Throws()
    {
        var item = new NaviusButtonGroupItem { IsEnabled = false };

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        var invoke = (IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke);

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_EnabledInvoke_RaisesClick()
    {
        var item = new NaviusButtonGroupItem();
        var clicked = false;
        item.Click += (_, _) => clicked = true;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        Assert.True(clicked);
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_EnabledInvoke_ExecutesBoundCommandWithParameter()
    {
        object? received = null;
        var executions = 0;
        var command = new RelayCommand(p => { received = p; executions++; }, _ => true);
        var parameter = new object();
        var clicks = 0;
        var item = new NaviusButtonGroupItem { Command = command, CommandParameter = parameter };
        item.Click += (_, _) => clicks++;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        Assert.Equal(1, executions);
        Assert.Same(parameter, received);
        Assert.Equal(1, clicks);
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);
    }
}
