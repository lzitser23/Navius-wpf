using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// The WAI-ARIA APG TreeView port (see docs/parity/tree.md). Derives TreeView for free
/// virtualization (VirtualizingStackPanel + Recycling, wired in Themes/Tree.xaml) and native UIA
/// mapping (TreeViewAutomationPeer -> ExpandCollapsePattern), then layers the contract's
/// multi-select (Ctrl/Shift ranges, Ctrl+Shift+Home/End, Shift+Space contiguous span, Ctrl+A
/// select-all), ltr/rtl-aware expand/collapse arrow semantics, the "*" expand-siblings shortcut
/// and a 500ms-reset typeahead buffer on top, since native WPF TreeView is single-select only and
/// has none of those extra shortcuts (see docs/parity/tree.md "WPF strategy" and "Open questions").
///
/// All navigation/selection is driven off the NaviusTreeNode DATA model (RootNodes), not off
/// realized TreeViewItem containers, so it works correctly under virtualization even for nodes
/// that are currently off-screen and have no container at all (see NaviusTreeNode's doc comment
/// and TreeContainerLocator).
/// </summary>
public class NaviusTree : TreeView
{
    private const int TypeaheadResetMs = 500;

    public static readonly DependencyProperty RootNodesProperty = DependencyProperty.Register(
        nameof(RootNodes), typeof(IReadOnlyList<NaviusTreeNode>), typeof(NaviusTree),
        new PropertyMetadata(null, OnRootNodesChanged));

    public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
        nameof(SelectionMode), typeof(NaviusTreeSelectionMode), typeof(NaviusTree),
        new PropertyMetadata(NaviusTreeSelectionMode.Single));

    private readonly Dictionary<object, NaviusTreeNode> _byValue = new();
    private HashSet<object> _selected = new();
    private NaviusTreeNode? _activeNode;
    private object? _anchor;
    private string _typeBuffer = "";
    private DateTime _typeAt = DateTime.MinValue;

    static NaviusTree()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTree), new FrameworkPropertyMetadata(typeof(NaviusTree)));
    }

    /// <summary>The hierarchical data source; when set, ItemsSource is wired automatically (data-driven mode is the only supported mode, see docs/parity/tree.md "Open questions").</summary>
    public IReadOnlyList<NaviusTreeNode>? RootNodes
    {
        get => (IReadOnlyList<NaviusTreeNode>?)GetValue(RootNodesProperty);
        set => SetValue(RootNodesProperty, value);
    }

    /// <summary>"none" | "single" | "multiple" (contract default "single"). The whole-tree Disabled parameter maps to native IsEnabled instead of a redundant wrapper property.</summary>
    public NaviusTreeSelectionMode SelectionMode
    {
        get => (NaviusTreeSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>The full current selection (both Single and Multiple modes route through the same custom set, see NaviusTreeNode.IsSelected).</summary>
    public IReadOnlyList<object> SelectedValues => _selected.ToList();

    /// <summary>
    /// Convenience single-value read (first of SelectedValues, or null); most meaningful in Single
    /// mode. Deliberately hides TreeView.SelectedItem's own SelectedValue (a different, unused
    /// concept here since selection is fully custom-tracked, see NaviusTreeNode.IsSelected).
    /// </summary>
    public new object? SelectedValue => _selected.Count > 0 ? _selected.First() : null;

    /// <summary>The roving-focus/active node's Value, or null before any navigation has occurred. Read-only, mainly for tests.</summary>
    public object? ActiveValue => _activeNode?.Value;

    /// <summary>Fires with the full selection on every change, mirroring the contract's OnSelectionChange / SelectedValuesChanged.</summary>
    public event EventHandler<TreeSelectionChangedEventArgs>? SelectedValuesChanged;

    /// <summary>Programmatically replaces the selection (clamped to a single value automatically in Single mode by the caller's own discipline; use ReplaceSelection for a hard single-select).</summary>
    public void SetSelectedValues(IEnumerable<object> values) => ApplySelection(new HashSet<object>(values));

    protected override DependencyObject GetContainerForItemOverride() => new NaviusTreeItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusTreeItem;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusTreeAutomationPeer(this);

    /// <summary>
    /// Called by NaviusTreeItem when a node is clicked. Public (rather than the more natural
    /// `internal`) so it is directly unit-testable without a live input/hit-test pipeline, the
    /// same tradeoff HandleKey and NaviusRating.HandleKey() make elsewhere in this codebase.
    /// </summary>
    public void HandleItemClicked(NaviusTreeNode node, NaviusTreeItem container, ModifierKeys mods)
    {
        if (!IsEnabled || node.Disabled)
        {
            return;
        }

        _activeNode = node;

        if (SelectionMode == NaviusTreeSelectionMode.Multiple && mods == ModifierKeys.Control)
        {
            ApplySelection(TreeSelectionState.ToggleSelection(_selected, node));
            _anchor = node.Value;
        }
        else if (SelectionMode == NaviusTreeSelectionMode.Multiple && mods == ModifierKeys.Shift)
        {
            var visible = TreeSelectionState.VisibleOrder(RootNodes ?? Array.Empty<NaviusTreeNode>());
            var anchorNode = _anchor is not null && _byValue.TryGetValue(_anchor, out var a) ? a : node;
            ApplySelection(TreeSelectionState.SelectSpan(_selected, visible, anchorNode, node));
        }
        else
        {
            Activate(node);
        }

        container.Focus();
    }

    /// <summary>Keeps the roving/active node in sync with whatever actually received keyboard focus (Tab-in, a click landing on it), mirroring the contract's SetActiveFromFocusAsync.</summary>
    internal void HandleItemFocused(NaviusTreeNode node)
    {
        if (!node.Disabled)
        {
            _activeNode = node;
        }
    }

    /// <summary>Best-effort AutomationPeer lookup for a selected value's currently realized container; used by NaviusTreeAutomationPeer.GetSelection, never forces realization.</summary>
    internal AutomationPeer? TryGetRealizedPeer(object value)
    {
        if (!_byValue.TryGetValue(value, out var node))
        {
            return null;
        }

        var owner = node.Parent is null ? (ItemsControl)this : FindRealizedParent(node.Parent);
        if (owner is null)
        {
            return null;
        }

        if (owner.ItemContainerGenerator.ContainerFromItem(node) is not NaviusTreeItem item)
        {
            return null;
        }

        return UIElementAutomationPeer.FromElement(item) ?? UIElementAutomationPeer.CreatePeerForElement(item);
    }

    private ItemsControl? FindRealizedParent(NaviusTreeNode parent)
    {
        if (parent.Parent is null)
        {
            return ItemContainerGenerator.ContainerFromItem(parent) as ItemsControl;
        }

        var grandParentOwner = FindRealizedParent(parent.Parent);
        return grandParentOwner?.ItemContainerGenerator.ContainerFromItem(parent) as ItemsControl;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (HandleKey(e.Key, Keyboard.Modifiers))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <summary>
    /// Handles one key exactly per the contract's keyboard table (see docs/parity/tree.md
    /// "Keyboard"); returns whether the key was consumed. Public (rather than the more natural
    /// `internal`) so it is directly unit-testable without constructing real KeyEventArgs/a live
    /// input pipeline, the same tradeoff NaviusRating.HandleKey() makes elsewhere in this codebase.
    /// </summary>
    public bool HandleKey(Key key, ModifierKeys mods)
    {
        if (!IsEnabled)
        {
            return false;
        }

        var roots = RootNodes;
        if (roots is null || roots.Count == 0)
        {
            return false;
        }

        var visible = TreeSelectionState.VisibleOrder(roots);
        if (visible.Count == 0)
        {
            return false;
        }

        var current = ResolveActive(visible);
        var isMulti = SelectionMode == NaviusTreeSelectionMode.Multiple;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var expandKey = rtl ? Key.Left : Key.Right;
        var collapseKey = rtl ? Key.Right : Key.Left;

        switch (key)
        {
            case Key.Down:
                if (isMulti && (mods & ModifierKeys.Shift) != 0)
                {
                    MoveAndExtend(current, visible, +1);
                }
                else
                {
                    Move(current, visible, +1);
                }

                return true;

            case Key.Up:
                if (isMulti && (mods & ModifierKeys.Shift) != 0)
                {
                    MoveAndExtend(current, visible, -1);
                }
                else
                {
                    Move(current, visible, -1);
                }

                return true;

            case Key.Home:
                if (isMulti && (mods & ModifierKeys.Control) != 0 && (mods & ModifierKeys.Shift) != 0)
                {
                    SelectRangeToEdge(current, visible, last: false);
                }
                else
                {
                    FocusAt(visible, 0);
                }

                return true;

            case Key.End:
                if (isMulti && (mods & ModifierKeys.Control) != 0 && (mods & ModifierKeys.Shift) != 0)
                {
                    SelectRangeToEdge(current, visible, last: true);
                }
                else
                {
                    FocusAt(visible, visible.Count - 1);
                }

                return true;

            case Key.Enter:
                Activate(current);
                RealizeAndFocus(current);
                return true;

            case Key.Space:
                if (isMulti && (mods & ModifierKeys.Shift) != 0)
                {
                    SelectContiguous(current, visible);
                }
                else
                {
                    SelectFocused(current);
                }

                return true;

            case Key.Multiply:
                ExpandSiblings(current);
                return true;

            case Key.D8 when (mods & ModifierKeys.Shift) != 0 && (mods & ModifierKeys.Control) == 0:
                ExpandSiblings(current);
                return true;

            case Key.A when isMulti && (mods & ModifierKeys.Control) != 0:
                ApplySelection(TreeSelectionState.ToggleSelectAll(_selected, visible));
                return true;
        }

        if (key == expandKey)
        {
            ExpandOrIntoChild(current, visible);
            return true;
        }

        if (key == collapseKey)
        {
            CollapseOrToParent(current);
            return true;
        }

        if ((mods & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
        {
            var ch = KeyToChar(key);
            if (ch is not null)
            {
                Typeahead(ch.Value, current, visible);
                return true;
            }
        }

        return false;
    }

    private NaviusTreeNode ResolveActive(List<NaviusTreeNode> visible)
    {
        if (_activeNode is not null && !_activeNode.Disabled && visible.Contains(_activeNode))
        {
            return _activeNode;
        }

        var selected = visible.FirstOrDefault(n => !n.Disabled && _selected.Contains(n.Value));
        if (selected is not null)
        {
            return selected;
        }

        return visible.FirstOrDefault(n => !n.Disabled) ?? visible[0];
    }

    private void Move(NaviusTreeNode current, List<NaviusTreeNode> visible, int delta)
    {
        var index = visible.IndexOf(current);
        var next = TreeSelectionState.NextEnabledIndex(visible, index, delta);
        if (next >= 0)
        {
            FocusAt(visible, next);
        }
    }

    private void MoveAndExtend(NaviusTreeNode current, List<NaviusTreeNode> visible, int delta)
    {
        var index = visible.IndexOf(current);
        var next = TreeSelectionState.NextEnabledIndex(visible, index, delta);
        if (next < 0)
        {
            return;
        }

        var target = visible[next];
        _activeNode = target;
        ApplySelection(TreeSelectionState.ToggleSelection(_selected, target));
        RealizeAndFocus(target);
    }

    private void FocusAt(List<NaviusTreeNode> visible, int index)
    {
        if (index < 0 || index >= visible.Count)
        {
            return;
        }

        var dir = index <= 0 ? +1 : -1;
        var target = TreeSelectionState.FirstEnabledFrom(visible, index, dir)
            ?? TreeSelectionState.FirstEnabledFrom(visible, index, -dir);
        if (target is null)
        {
            return;
        }

        _activeNode = target;
        RealizeAndFocus(target);
    }

    private void Activate(NaviusTreeNode node)
    {
        if (node.Disabled)
        {
            return;
        }

        _anchor = node.Value;
        if (node.HasChildren)
        {
            node.IsExpanded = !node.IsExpanded;
        }

        if (SelectionMode == NaviusTreeSelectionMode.Single)
        {
            ApplySelection(TreeSelectionState.ReplaceSelection(node));
        }
        else if (SelectionMode == NaviusTreeSelectionMode.Multiple)
        {
            ApplySelection(TreeSelectionState.ToggleSelection(_selected, node));
        }
    }

    private void SelectFocused(NaviusTreeNode node)
    {
        if (SelectionMode == NaviusTreeSelectionMode.None || node.Disabled)
        {
            return;
        }

        _anchor = node.Value;
        ApplySelection(SelectionMode == NaviusTreeSelectionMode.Single
            ? TreeSelectionState.ReplaceSelection(node)
            : TreeSelectionState.ToggleSelection(_selected, node));
    }

    private void SelectContiguous(NaviusTreeNode current, List<NaviusTreeNode> visible)
    {
        var anchorNode = _anchor is not null && _byValue.TryGetValue(_anchor, out var a) && visible.Contains(a) ? a : current;
        ApplySelection(TreeSelectionState.SelectSpan(_selected, visible, anchorNode, current));
    }

    private void SelectRangeToEdge(NaviusTreeNode current, List<NaviusTreeNode> visible, bool last)
    {
        var edgeIndex = last ? visible.Count - 1 : 0;
        var target = TreeSelectionState.FirstEnabledFrom(visible, edgeIndex, last ? -1 : +1);
        if (target is null)
        {
            return;
        }

        ApplySelection(TreeSelectionState.SelectSpan(_selected, visible, current, target));
        _activeNode = target;
        RealizeAndFocus(target);
    }

    private void ExpandOrIntoChild(NaviusTreeNode current, List<NaviusTreeNode> visible)
    {
        if (!current.HasChildren)
        {
            return;
        }

        if (!current.IsExpanded)
        {
            current.IsExpanded = true;
            return;
        }

        var index = visible.IndexOf(current);
        if (index >= 0 && index + 1 < visible.Count && ReferenceEquals(visible[index + 1].Parent, current))
        {
            FocusAt(visible, index + 1);
        }
    }

    private void CollapseOrToParent(NaviusTreeNode current)
    {
        if (current.HasChildren && current.IsExpanded)
        {
            current.IsExpanded = false;
            return;
        }

        if (current.Parent is { Disabled: false } parent)
        {
            _activeNode = parent;
            RealizeAndFocus(parent);
        }
    }

    private void ExpandSiblings(NaviusTreeNode current)
    {
        foreach (var sibling in TreeSelectionState.ExpandableSiblings(current, RootNodes ?? Array.Empty<NaviusTreeNode>()))
        {
            sibling.IsExpanded = true;
        }
    }

    private void Typeahead(char ch, NaviusTreeNode current, List<NaviusTreeNode> visible)
    {
        var now = DateTime.UtcNow;
        if ((now - _typeAt).TotalMilliseconds > TypeaheadResetMs)
        {
            _typeBuffer = "";
        }

        _typeAt = now;
        _typeBuffer += ch;

        var match = TreeSelectionState.Typeahead(_typeBuffer, current, visible);
        if (match is not null)
        {
            _activeNode = match;
            RealizeAndFocus(match);
        }
    }

    private void RealizeAndFocus(NaviusTreeNode node)
    {
        var container = TreeContainerLocator.Locate(this, node);
        container?.Focus();
    }

    private void ApplySelection(HashSet<object> next)
    {
        if (next.SetEquals(_selected))
        {
            return;
        }

        foreach (var value in _selected)
        {
            if (!next.Contains(value) && _byValue.TryGetValue(value, out var removed))
            {
                removed.IsSelected = false;
            }
        }

        foreach (var value in next)
        {
            if (_byValue.TryGetValue(value, out var added))
            {
                added.IsSelected = true;
            }
        }

        _selected = next;
        SelectedValuesChanged?.Invoke(this, new TreeSelectionChangedEventArgs(_selected.ToList()));
    }

    private static void OnRootNodesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tree = (NaviusTree)d;
        tree.ItemsSource = e.NewValue as IReadOnlyList<NaviusTreeNode>;
        tree.RebuildIndex();
    }

    private void RebuildIndex()
    {
        _byValue.Clear();
        _selected.Clear();
        _activeNode = null;
        _anchor = null;

        Walk(RootNodes);

        void Walk(IEnumerable<NaviusTreeNode>? nodes)
        {
            if (nodes is null)
            {
                return;
            }

            foreach (var n in nodes)
            {
                _byValue[n.Value] = n;
                Walk(n.Children);
            }
        }
    }

    private static char? KeyToChar(Key key) => key switch
    {
        >= Key.A and <= Key.Z => (char)('a' + (key - Key.A)),
        >= Key.D0 and <= Key.D9 => (char)('0' + (key - Key.D0)),
        >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (key - Key.NumPad0)),
        _ => null,
    };
}
