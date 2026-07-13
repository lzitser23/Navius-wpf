using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// Re-implements ISelectionItemProvider on top of the base TreeViewItemAutomationPeer, which
/// otherwise reflects native TreeViewItem.IsSelected -- always false here, since selection is
/// fully custom-tracked on NaviusTreeNode.IsSelected instead of native TreeView selection (see
/// docs/parity/tree.md's "WPF implementation notes" for why). Re-declaring the interface here
/// gives instances of this (more derived) type their own interface map entry for
/// ISelectionItemProvider, which wins over TreeViewItemAutomationPeer's own explicit
/// implementation for any client that queries the pattern -- IsSelected reads the bound
/// NaviusTreeNode's real selection flag, and AddToSelection/RemoveFromSelection/Select route
/// through NaviusTree's own selection API so a UIA client (e.g. a screen reader) can both observe
/// and drive this control's actual multi-select state.
/// </summary>
public class NaviusTreeItemAutomationPeer : TreeViewItemAutomationPeer, ISelectionItemProvider
{
    private readonly NaviusTreeItem _owner;

    public NaviusTreeItemAutomationPeer(NaviusTreeItem owner) : base(owner)
    {
        _owner = owner;
    }

    bool ISelectionItemProvider.IsSelected => _owner.Node?.IsSelected ?? false;

    IRawElementProviderSimple? ISelectionItemProvider.SelectionContainer
    {
        get
        {
            if (TreeVisualHelpers.FindAncestor<NaviusTree>(_owner) is not { } tree)
            {
                return null;
            }

            var peer = UIElementAutomationPeer.FromElement(tree) ?? UIElementAutomationPeer.CreatePeerForElement(tree);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    void ISelectionItemProvider.AddToSelection()
    {
        ThrowIfDisabled();
        if (_owner.Node is { } node && TreeVisualHelpers.FindAncestor<NaviusTree>(_owner) is { } tree)
        {
            tree.AddNodeToSelection(node);
        }
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        ThrowIfDisabled();
        if (_owner.Node is { } node && TreeVisualHelpers.FindAncestor<NaviusTree>(_owner) is { } tree)
        {
            tree.RemoveNodeFromSelection(node);
        }
    }

    void ISelectionItemProvider.Select()
    {
        ThrowIfDisabled();
        if (_owner.Node is { } node && TreeVisualHelpers.FindAncestor<NaviusTree>(_owner) is { } tree)
        {
            tree.SelectNodeExclusive(node);
        }
    }

    private void ThrowIfDisabled()
    {
        if (!_owner.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }
    }

    /// <summary>
    /// Raised by NaviusTreeItem when the bound node's IsSelected changes, so a UIA client sees a
    /// live SelectionItemPattern update for state this control tracks itself rather than via
    /// native TreeViewItem.IsSelected. Public (rather than the more natural <c>internal</c>) so it
    /// is directly unit-testable; this assembly has no InternalsVisibleTo, the same tradeoff
    /// NaviusTree.HandleKey()/NaviusRating.HandleKey() make elsewhere in this codebase.
    /// </summary>
    public void RaiseSelectionEvents(bool isSelected)
    {
        RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, !isSelected, isSelected);
        RaiseAutomationEvent(isSelected
            ? AutomationEvents.SelectionItemPatternOnElementSelected
            : AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
    }
}
