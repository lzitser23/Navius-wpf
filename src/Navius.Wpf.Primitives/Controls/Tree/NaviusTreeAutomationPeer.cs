using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// Adds ISelectionProvider.CanSelectMultiple / IsSelectionRequired reflecting
/// NaviusTree.SelectionMode on top of the native TreeViewAutomationPeer (native TreeView is always
/// single-select, so its own CanSelectMultiple is hardcoded false -- see docs/parity/tree.md's "WPF
/// implementation notes" for why selection itself is fully custom-tracked rather than routed
/// through native TreeViewItem.IsSelected). GetSelection() is best-effort: it only returns
/// providers for currently realized containers, matching this control's virtualization-first
/// design -- it does not force off-screen selected nodes to realize just to answer a UIA query.
/// </summary>
public class NaviusTreeAutomationPeer : TreeViewAutomationPeer, ISelectionProvider
{
    private readonly NaviusTree _owner;

    public NaviusTreeAutomationPeer(NaviusTree owner) : base(owner)
    {
        _owner = owner;
    }

    bool ISelectionProvider.CanSelectMultiple => _owner.SelectionMode == NaviusTreeSelectionMode.Multiple;

    bool ISelectionProvider.IsSelectionRequired => false;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var result = new List<IRawElementProviderSimple>();
        foreach (var value in _owner.SelectedValues)
        {
            if (_owner.TryGetRealizedPeer(value) is { } peer)
            {
                result.Add(ProviderFromPeer(peer));
            }
        }

        return result.ToArray();
    }
}
