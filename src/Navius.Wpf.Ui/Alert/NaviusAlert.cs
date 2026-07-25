using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Alert;

/// <summary>Default | Warning | Destructive.</summary>
public enum NaviusAlertVariant
{
    Default,
    Warning,
    Destructive,
}

/// <summary>
/// A callout for user attention. Compositional: nest NaviusAlertTitle/NaviusAlertDescription
/// (and any icon) inside a StackPanel as the single Content, mirroring the web contract's
/// child-content model. Warning and Destructive map to dedicated semantic theme tokens.
/// </summary>
public class NaviusAlert : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusAlertVariant), typeof(NaviusAlert),
        new FrameworkPropertyMetadata(NaviusAlertVariant.Default));

    static NaviusAlert()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusAlert), new FrameworkPropertyMetadata(typeof(NaviusAlert)));
    }

    public NaviusAlertVariant Variant
    {
        get => (NaviusAlertVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusAlertAutomationPeer(this);
}

/// <summary>
/// A plain ContentControl ships no automation peer, so the callout reached UIA with no control
/// type and no live-region politeness -- a screen reader had no signal to announce it. This peer
/// reports Group (the same status/alert-region pairing WPF's InfoBar-style peers use, mirroring
/// <see cref="Navius.Wpf.Primitives.Controls.Toast.NaviusToastAutomationPeer"/>) and maps Variant
/// to a UIA LiveSetting: the urgent Warning/Destructive variants announce Assertive, the
/// informational Default variant announces Polite.
/// </summary>
internal sealed class NaviusAlertAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAlertAutomationPeer(NaviusAlert owner) : base(owner)
    {
    }

    private NaviusAlert Alert => (NaviusAlert)Owner;

    protected override string GetClassNameCore() => nameof(NaviusAlert);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override AutomationLiveSetting GetLiveSettingCore() =>
        Alert.Variant == NaviusAlertVariant.Default
            ? AutomationLiveSetting.Polite
            : AutomationLiveSetting.Assertive;
}
