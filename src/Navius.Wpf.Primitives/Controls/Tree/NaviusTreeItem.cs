using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// One tree node's container, deriving TreeViewItem for free virtualization (VirtualizingStackPanel
/// with recycling, wired in Themes/Tree.xaml) and UIA (native TreeViewItemAutomationPeer already
/// maps to ExpandCollapsePattern, a reasonable analogue of aria-expanded -- see docs/parity/tree.md
/// "WPF strategy"). Selection painting is driven entirely by the bound NaviusTreeNode.IsSelected
/// (see that class's doc comment for why), not native TreeViewItem.IsSelected, so this class adds
/// no selection-related members of its own; it only forwards header-click activation up to the
/// owning NaviusTree (the expander glyph's own ToggleButton handles expand/collapse clicks itself
/// and marks the event handled before it would reach here).
/// </summary>
public class NaviusTreeItem : TreeViewItem
{
    static NaviusTreeItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTreeItem), new FrameworkPropertyMetadata(typeof(NaviusTreeItem)));
    }

    /// <summary>The bound data node; equals DataContext when generated from a hierarchical ItemsSource (the only supported mode).</summary>
    public NaviusTreeNode? Node => DataContext as NaviusTreeNode;

    protected override DependencyObject GetContainerForItemOverride() => new NaviusTreeItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusTreeItem;

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
