using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Maps NaviusToastRoot's role="status" (Low priority, polite) / role="alert" (High priority,
/// assertive) to UIA's LiveSetting -- the fallback path for OS/AT combinations where
/// <see cref="NaviusToast"/>'s RaiseNotificationEvent call isn't supported (see its doc comment).
/// </summary>
public class NaviusToastAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusToastAutomationPeer(NaviusToast owner) : base(owner)
    {
    }

    private NaviusToast Toast => (NaviusToast)Owner;

    protected override string GetClassNameCore() => nameof(NaviusToast);

    // No closer 1:1 UIA control type for a transient status/alert region; Group (like WinUI's
    // InfoBar peer) paired with GetLiveSettingCore is the conventional pairing.
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override AutomationLiveSetting GetLiveSettingCore() =>
        Toast.Priority == ToastPriority.High ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return Toast.Title ?? Toast.Description ?? string.Empty;
    }
}
