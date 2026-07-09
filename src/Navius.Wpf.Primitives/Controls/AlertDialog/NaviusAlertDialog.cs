using System.Windows;
using System.Windows.Automation.Peers;
using Navius.Wpf.Primitives.Controls.OverlaySurface;

namespace Navius.Wpf.Primitives.Controls.AlertDialog;

/// <summary>
/// Tier B (custom lookless control). WPF port of the web NaviusAlertDialog family. Always modal
/// (no Modal DP: the web contract never allows AlertDialog to be non-modal) and never dismissed
/// by an outside press (<see cref="CloseOnOutsideClickEffective"/> is hard-coded false, matching
/// NaviusAlertDialogPopup.CloseOnOutside => false in the source). Escape still closes it.
///
/// Per the APG recommendation the web contract follows, initial focus lands on the
/// least-destructive action rather than the panel or its first focusable child: mark that button
/// with the attached <see cref="IsCancelButtonProperty"/> (mirrors the source's
/// InitialFocusSelector => "[data-navius-alert-dialog-cancel]"), e.g.
/// &lt;primitives:NaviusButton alertDialog:NaviusAlertDialog.IsCancelButton="True" .../&gt;.
/// </summary>
public class NaviusAlertDialog : NaviusOverlaySurfaceBase
{
    public static readonly DependencyProperty IsCancelButtonProperty = DependencyProperty.RegisterAttached(
        "IsCancelButton",
        typeof(bool),
        typeof(NaviusAlertDialog),
        new PropertyMetadata(false));

    static NaviusAlertDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAlertDialog),
            new FrameworkPropertyMetadata(typeof(NaviusAlertDialog)));
    }

    public static void SetIsCancelButton(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsCancelButtonProperty, value);
    }

    public static bool GetIsCancelButton(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsCancelButtonProperty);
    }

    protected override bool ModalEffective => true;

    protected override bool CloseOnOutsideClickEffective => false;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusAlertDialogAutomationPeer(this);

    protected override FrameworkElement? ResolveInitialFocusElement() =>
        AlertDialogFocus.FindCancelElement(this);
}

/// <summary>
/// Pure logical-tree search for the element marked as the Cancel action, factored out of
/// <see cref="NaviusAlertDialog"/> so the "which element gets initial focus" decision is testable.
/// Walks the logical tree (not visual), matching NaviusRadioGroup/NaviusCheckboxGroup's own
/// descendant search: descendants set via Content are discoverable immediately, without
/// requiring a layout pass, ControlTemplate application, or a live PresentationSource.
/// </summary>
public static class AlertDialogFocus
{
    public static FrameworkElement? FindCancelElement(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is FrameworkElement { IsEnabled: true } candidate &&
                NaviusAlertDialog.GetIsCancelButton(candidate))
            {
                return candidate;
            }

            var nested = FindCancelElement(childObj);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}

internal sealed class NaviusAlertDialogAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAlertDialogAutomationPeer(NaviusAlertDialog owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override string GetClassNameCore() => nameof(NaviusAlertDialog);

    protected override string GetLocalizedControlTypeCore() => "alert dialog";

    protected override bool IsDialogCore() => true;
}
