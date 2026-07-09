using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class AccessibleIconTests
{
    static AccessibleIconTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
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

    [StaFact]
    public void DefaultState_LabelIsNull()
    {
        var icon = new NaviusAccessibleIcon();

        Assert.Null(icon.Label);
    }

    [StaFact]
    public void ContentChanged_AppliesLabelToDependencyObjectContent()
    {
        var content = new TextBlock();
        var icon = new NaviusAccessibleIcon { Label = "Delete", Content = content };

        Assert.Equal("Delete", AutomationProperties.GetName(content));
    }

    [StaFact]
    public void LabelChanged_ReappliesToExistingContent()
    {
        var content = new TextBlock();
        var icon = new NaviusAccessibleIcon { Content = content, Label = "Close" };

        icon.Label = "Dismiss";

        Assert.Equal("Dismiss", AutomationProperties.GetName(content));
    }

    [StaFact]
    public void ContentChanged_NoOpWhenContentIsNotDependencyObject()
    {
        // Should not throw for a plain CLR object content.
        var icon = new NaviusAccessibleIcon { Label = "Star", Content = "plain text" };

        Assert.Equal("plain text", icon.Content);
    }

    [StaFact]
    public void AutomationPeer_ReportsImageControlTypeAndLabelAsName()
    {
        var icon = new NaviusAccessibleIcon { Label = "Warning" };
        var peer = new NaviusAccessibleIconAutomationPeer(icon);

        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.Equal("Warning", peer.GetName());
    }

    [StaFact]
    public void AutomationPeer_HiddenFromUiaTreeWhenLabelIsNull()
    {
        var icon = new NaviusAccessibleIcon();
        var peer = new NaviusAccessibleIconAutomationPeer(icon);

        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    }

    [StaFact]
    public void AutomationPeer_VisibleInUiaTreeWhenLabelIsSet()
    {
        var icon = new NaviusAccessibleIcon { Label = "Info" };
        var peer = new NaviusAccessibleIconAutomationPeer(icon);

        Assert.True(peer.IsControlElement());
        Assert.True(peer.IsContentElement());
    }
}
