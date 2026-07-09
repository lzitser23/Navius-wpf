using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// One tree node's container, deriving TreeViewItem for free virtualization (VirtualizingStackPanel
/// with recycling, wired in Themes/Tree.xaml) and UIA (native TreeViewItemAutomationPeer already
/// maps to ExpandCollapsePattern, a reasonable analogue of aria-expanded -- see docs/parity/tree.md
/// "WPF strategy"). Selection painting is driven entirely by the bound NaviusTreeNode.IsSelected
/// (see that class's doc comment for why), not native TreeViewItem.IsSelected: OnCreateAutomationPeer
/// swaps in NaviusTreeItemAutomationPeer so SelectionItemPattern reflects that same state instead of
/// the always-false native one, and this class forwards the bound node's IsSelected changes to the
/// realized peer so a UIA client observes live selection updates. It only otherwise forwards
/// header-click activation up to the owning NaviusTree (the expander glyph's own ToggleButton
/// handles expand/collapse clicks itself and marks the event handled before it would reach here).
/// </summary>
public class NaviusTreeItem : TreeViewItem
{
    static NaviusTreeItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTreeItem), new FrameworkPropertyMetadata(typeof(NaviusTreeItem)));
    }

    public NaviusTreeItem()
    {
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>The bound data node; equals DataContext when generated from a hierarchical ItemsSource (the only supported mode).</summary>
    public NaviusTreeNode? Node => DataContext as NaviusTreeNode;

    protected override DependencyObject GetContainerForItemOverride() => new NaviusTreeItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusTreeItem;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusTreeItemAutomationPeer(this);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NaviusTreeNode oldNode)
        {
            oldNode.PropertyChanged -= OnNodePropertyChanged;
        }

        if (e.NewValue is NaviusTreeNode newNode)
        {
            newNode.PropertyChanged += OnNodePropertyChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NaviusTreeNode.IsSelected))
        {
            return;
        }

        if (UIElementAutomationPeer.FromElement(this) is NaviusTreeItemAutomationPeer peer)
        {
            peer.RaiseSelectionEvents(Node?.IsSelected ?? false);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (Node is not { } node)
        {
            return;
        }

        if (TreeVisualHelpers.FindAncestor<NaviusTree>(this) is { } tree)
        {
            tree.HandleItemClicked(node, this, Keyboard.Modifiers);
        }
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);

        if (Node is { } node && TreeVisualHelpers.FindAncestor<NaviusTree>(this) is { } tree)
        {
            tree.HandleItemFocused(node);
        }
    }
}
