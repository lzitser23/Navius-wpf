using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>
/// A plain Button (not ToggleButton), deliberately matching the web contract's omission of
/// aria-pressed/data-state on the toggle (documented there as avoiding double-announcing
/// state to screen readers): a ButtonAutomationPeer has no IsChecked/pressed-state exposure,
/// so choosing Button over ToggleButton achieves the same "no pressed state" outcome for
/// free rather than needing to suppress ToggleButtonAutomationPeer's TogglePattern.
/// AutomationProperties.Name flips between "Show password"/"Hide password" (pushed by the
/// ancestor NaviusPasswordToggleField via UpdateAccessibleName). The contract's aria-controls
/// (an id reference) is dropped rather than ported: this WPF runtime's AutomationPeer has no
/// overridable GetControllerForCore (only the lower-level, non-virtual
/// GetControllerForProviderArray), so there is no supported extensibility point to wire a
/// ControllerFor relationship from a custom peer.
///
/// Click looks up its ancestor field on demand (VisualAncestorWalker), not cached from
/// Loaded: the lookup itself only needs the logical tree, which is wired synchronously as
/// soon as the element is placed in its parent's Content, so it works whether or not Loaded
/// ever fires (Loaded never fires for elements outside a live Window).
/// </summary>
public class NaviusPasswordToggleFieldToggle : Button
{
    static NaviusPasswordToggleFieldToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPasswordToggleFieldToggle),
            new FrameworkPropertyMetadata(typeof(NaviusPasswordToggleFieldToggle)));
    }

    public NaviusPasswordToggleFieldToggle()
    {
        Click += OnClick;
    }

    /// <summary>Called by the ancestor NaviusPasswordToggleField whenever Visible changes.</summary>
    internal void UpdateAccessibleName(bool visible) =>
        AutomationProperties.SetName(this, visible ? "Hide password" : "Show password");

    private void OnClick(object sender, RoutedEventArgs e) =>
        VisualAncestorWalker.FindAncestor<NaviusPasswordToggleField>(this)?.ToggleVisible();
}
