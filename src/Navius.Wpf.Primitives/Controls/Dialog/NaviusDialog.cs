using System.Windows;
using System.Windows.Automation.Peers;
using Navius.Wpf.Primitives.Controls.OverlaySurface;

namespace Navius.Wpf.Primitives.Controls.Dialog;

/// <summary>
/// Tier B (custom lookless control). WPF port of the web NaviusDialog family, folded into a
/// single ContentControl (see <see cref="NaviusOverlaySurfaceBase"/> remarks for why this
/// codebase collapses the web's Root/Trigger/Portal/Backdrop/Popup/Title/Description/Close parts
/// into one control): declare it anywhere in a window that also hosts a
/// <see cref="OverlaySurface.NaviusOverlayLayer"/>, set <see cref="Title"/>/<see cref="Description"/>
/// for the accessible name/help text, and put the dialog's body (plus any button bound to
/// <see cref="OverlaySurface.NaviusOverlaySurfaceBase.CloseCommand"/>) in Content.
///
/// Modal defaults to true (focus-trapped, backdrop shown, outside interaction blocked); set it to
/// false for a non-modal dialog (no trap, no backdrop, outside content stays interactive), per
/// the web contract's Modal parameter. CloseOnOutsideClick defaults to true and is independently
/// overridable, matching the "outside dismissal optional per contract" note in docs/parity/dialog.md.
/// </summary>
public class NaviusDialog : NaviusOverlaySurfaceBase
{
    public static readonly DependencyProperty ModalProperty = DependencyProperty.Register(
        nameof(Modal),
        typeof(bool),
        typeof(NaviusDialog),
        new PropertyMetadata(true));

    public static readonly DependencyProperty CloseOnOutsideClickProperty = DependencyProperty.Register(
        nameof(CloseOnOutsideClick),
        typeof(bool),
        typeof(NaviusDialog),
        new PropertyMetadata(true));

    static NaviusDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusDialog),
            new FrameworkPropertyMetadata(typeof(NaviusDialog)));
    }

    public bool Modal
    {
        get => (bool)GetValue(ModalProperty);
        set => SetValue(ModalProperty, value);
    }

    public bool CloseOnOutsideClick
    {
        get => (bool)GetValue(CloseOnOutsideClickProperty);
        set => SetValue(CloseOnOutsideClickProperty, value);
    }

    protected override bool ModalEffective => Modal;

    protected override bool CloseOnOutsideClickEffective => CloseOnOutsideClick;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusDialogAutomationPeer(this);
}

/// <summary>
/// role="dialog" has no WPF/UIA equivalent by name; the closest native mapping is reporting
/// ControlType.Window and opting into UIA's dialog-detection via IsDialogCore (available since
/// .NET 6, per docs/parity/dialog.md's WPF strategy section), so assistive tech announces this as
/// a dialog the same way it would a role="dialog" web popup.
/// </summary>
internal sealed class NaviusDialogAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusDialogAutomationPeer(NaviusDialog owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override string GetClassNameCore() => nameof(NaviusDialog);

    protected override bool IsDialogCore() => true;
}
