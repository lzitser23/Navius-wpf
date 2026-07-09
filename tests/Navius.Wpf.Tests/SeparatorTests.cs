using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class SeparatorTests
{
    static SeparatorTests()
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

    [StaFact]
    public void Orientation_DefaultsToHorizontal()
    {
        var separator = new NaviusSeparator();

        Assert.Equal(Orientation.Horizontal, separator.Orientation);
    }

    [StaFact]
    public void Decorative_DefaultsToFalse()
    {
        var separator = new NaviusSeparator();

        Assert.False(separator.Decorative);
    }

    [StaFact]
    public void AutomationPeer_ReportsSeparatorControlType()
    {
        var separator = new NaviusSeparator();
        var peer = new NaviusSeparatorAutomationPeer(separator);

        Assert.Equal(
            System.Windows.Automation.Peers.AutomationControlType.Separator,
            peer.GetAutomationControlType());
    }

    // IsControlElementCore/IsContentElementCore combine our decorative decision with the base
    // implementation, which depends on UIElement.IsVisible (false for any element outside a real,
    // shown window - true even for native controls, verified independently). The decorative
    // decision itself is tested directly via the pure static helper instead of the instance peer,
    // so this is deterministic without needing a real visual tree.

    [StaFact]
    public void IsAccessibilityTreeMember_TrueWhenNotDecorative()
    {
        Assert.True(NaviusSeparatorAutomationPeer.IsAccessibilityTreeMember(decorative: false));
    }

    [StaFact]
    public void IsAccessibilityTreeMember_FalseWhenDecorative()
    {
        Assert.False(NaviusSeparatorAutomationPeer.IsAccessibilityTreeMember(decorative: true));
    }

    [StaFact]
    public void AutomationPeer_Decorative_RemovedFromAccessibilityTree()
    {
        var separator = new NaviusSeparator { Decorative = true };
        var peer = new NaviusSeparatorAutomationPeer(separator);

        // Decorative short-circuits before the base (IsVisible-dependent) check runs, so this
        // holds regardless of visual-tree membership.
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    }

    [StaFact]
    public void AutomationPeer_Orientation_MatchesHorizontal()
    {
        var separator = new NaviusSeparator { Orientation = Orientation.Horizontal };
        var peer = new NaviusSeparatorAutomationPeer(separator);

        Assert.Equal(
            System.Windows.Automation.Peers.AutomationOrientation.Horizontal,
            peer.GetOrientation());
    }

    [StaFact]
    public void AutomationPeer_Orientation_MatchesVertical()
    {
        var separator = new NaviusSeparator { Orientation = Orientation.Vertical };
        var peer = new NaviusSeparatorAutomationPeer(separator);

        Assert.Equal(
            System.Windows.Automation.Peers.AutomationOrientation.Vertical,
            peer.GetOrientation());
    }

    [StaFact]
    public void Template_AppliesFromDictionaryLoadedViaPackUri()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Separator.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var separator = new NaviusSeparator();
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            separator.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusSeparator));
            separator.ApplyTemplate();

            Assert.NotNull(separator.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }
}
