using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// Best-effort container realization for a virtualized NaviusTree: keyboard navigation (Home/End,
/// arrow moves, Ctrl+Shift+Home/End range-select) needs a live NaviusTreeItem to call Focus()/
/// BringIntoView() on even for a node far outside the current viewport, but VirtualizingStackPanel
/// only creates containers for what's on screen (plus a small recycling buffer) by design -- that
/// is the whole point of keeping virtualization on for the 10k-node gate. This walks the node's
/// ancestor chain root-to-leaf, and at each level, if the child isn't realized yet, asks that
/// level's VirtualizingPanel to bring it into view (via the panel's protected BringIndexIntoView,
/// reached through reflection since ItemsControl/VirtualizingPanel expose no public equivalent for
/// an arbitrary index) before re-checking. This is the standard, widely-used workaround for
/// programmatic navigation in a virtualized WPF ItemsControl.
/// </summary>
internal static class TreeContainerLocator
{
    private static readonly MethodInfo? BringIndexIntoViewMethod =
        typeof(VirtualizingPanel).GetMethod("BringIndexIntoView", BindingFlags.NonPublic | BindingFlags.Instance);

    public static NaviusTreeItem? Locate(ItemsControl root, NaviusTreeNode node)
    {
        var chain = new List<NaviusTreeNode>();
        for (var n = node; n is not null; n = n.Parent)
        {
            chain.Add(n);
        }

        chain.Reverse();

        ItemsControl current = root;
        NaviusTreeItem? container = null;

        foreach (var n in chain)
        {
            container = RealizeChild(current, n);
            if (container is null)
            {
                return null;
            }

            current = container;
        }

        return container;
    }

    private static NaviusTreeItem? RealizeChild(ItemsControl parent, NaviusTreeNode node)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(node) is NaviusTreeItem existing)
        {
            return existing;
        }

        var index = IndexOf(parent, node);
        if (index < 0)
        {
            return null;
        }

        parent.UpdateLayout();
        BringIndexIntoView(parent, index);
        parent.UpdateLayout();

        return parent.ItemContainerGenerator.ContainerFromItem(node) as NaviusTreeItem
            ?? parent.ItemContainerGenerator.ContainerFromIndex(index) as NaviusTreeItem;
    }

    private static int IndexOf(ItemsControl parent, NaviusTreeNode node)
    {
        var i = 0;
        foreach (var item in parent.Items)
        {
            if (ReferenceEquals(item, node))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    private static void BringIndexIntoView(ItemsControl parent, int index)
    {
        if (BringIndexIntoViewMethod is null)
        {
            return;
        }

        if (FindItemsHostPanel(parent) is VirtualizingPanel panel)
        {
            BringIndexIntoViewMethod.Invoke(panel, new object[] { index });
        }
    }

    private static VirtualizingPanel? FindItemsHostPanel(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is VirtualizingPanel { IsItemsHost: true } vp)
            {
                return vp;
            }

            var found = FindItemsHostPanel(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
