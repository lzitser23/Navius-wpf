using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Maps <see cref="NaviusSeparator"/> to role="separator" semantics: AutomationControlType.Separator
/// with an orientation-aware GetOrientation, and removal from the accessibility tree (both control
/// and content) when <see cref="NaviusSeparator.Decorative"/> is true, mirroring the web contract's
/// "plain div, no role" decorative mode.
/// </summary>
public class NaviusSeparatorAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSeparatorAutomationPeer(NaviusSeparator owner) : base(owner)
    {
    }

    private NaviusSeparator Separator => (NaviusSeparator)Owner;

    protected override string GetClassNameCore() => nameof(NaviusSeparator);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Separator;

    protected override AutomationOrientation GetOrientationCore() =>
        Separator.Orientation == Orientation.Vertical
            ? AutomationOrientation.Vertical
            : AutomationOrientation.Horizontal;

    protected override bool IsControlElementCore() =>
        IsAccessibilityTreeMember(Separator.Decorative) && base.IsControlElementCore();

    protected override bool IsContentElementCore() =>
        IsAccessibilityTreeMember(Separator.Decorative) && base.IsContentElementCore();

    /// <summary>
    /// Pure decorative-vs-not decision, split out so it is unit testable without a real, shown
    /// window: the base Core methods this combines with (IsControlElementCore/IsContentElementCore)
    /// depend on UIElement.IsVisible, which is false for any element outside a live visual tree.
    /// </summary>
    public static bool IsAccessibilityTreeMember(bool decorative) => !decorative;
}
