using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
}
