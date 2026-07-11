using System.Windows.Automation.Peers;
using Navius.Wpf.Ui.Spinner;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiSpinnerTests
{
    [StaFact]
    public void NaviusSpinner_AutomationPeer_ReportsProgressBarControlType()
    {
        var spinner = new NaviusSpinner();

        var peer = UIElementAutomationPeer.CreatePeerForElement(spinner);

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.ProgressBar, peer!.GetAutomationControlType());
    }

    [StaFact]
    public void NaviusSpinner_AutomationPeer_SurfacesLoadingName()
    {
        var spinner = new NaviusSpinner();

        var peer = UIElementAutomationPeer.CreatePeerForElement(spinner);

        Assert.Equal("Loading", peer!.GetName());
    }
}
