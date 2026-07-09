using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// The WPF port's hierarchical data source node, mirroring the web contract's framework-free
/// TreeNode&lt;TValue&gt; (see docs/parity/tree.md's "Open questions": "TreeNode&lt;TValue&gt; ...
/// has no framework dependency and should port unchanged as the WPF port's hierarchical data
/// source type"). Non-generic here (Value: object) because NaviusTree itself is non-generic,
/// matching how the web's TreeContext boxes values to object so its parts stay non-generic.
///
/// IsExpanded and IsSelected live on the DATA node (not on the TreeViewItem container) and raise
/// INotifyPropertyChanged so ItemContainerStyle/ControlTemplate bindings stay correct across
/// container recycling: virtualization means a node's TreeViewItem container is created and
/// destroyed as it scrolls in and out of view, but the node itself is a stable object for the
/// tree's lifetime, so state that must survive virtualization has to live here, not on the
/// container.
/// </summary>
public sealed class NaviusTreeNode : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    public NaviusTreeNode(object value, string label, IReadOnlyList<NaviusTreeNode>? children = null, bool disabled = false)
    {
        Value = value;
        Label = label;
        Disabled = disabled;
        Children = children;

        if (children is not null)
        {
            foreach (var child in children)
            {
                child.Parent = this;
            }
        }
    }

    /// <summary>The node's unique identity (used for selection/expansion state), boxed like the web's TValue.</summary>
    public object Value { get; }

    /// <summary>Display / typeahead-match label. Overridable per-node like the contract's TextValue.</summary>
    public string Label { get; set; }

    /// <summary>Child nodes; null or empty means a leaf (never gets an expand affordance).</summary>
    public IReadOnlyList<NaviusTreeNode>? Children { get; }

    /// <summary>Cannot be selected, skipped by keyboard navigation, typeahead and select-all.</summary>
    public bool Disabled { get; set; }

    /// <summary>Set automatically from the constructor's children list; null for root nodes.</summary>
    public NaviusTreeNode? Parent { get; internal set; }

    /// <summary>Whether this node has at least one child (drives the expand affordance / aria-expanded presence).</summary>
    public bool HasChildren => Children is { Count: > 0 };

    /// <summary>Two-way bound from NaviusTreeItem's container Style so expansion survives virtualization recycling.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Selection membership, driven entirely by NaviusTree (both Single and Multiple modes -- see
    /// TreeSelectionState) rather than native TreeViewItem.IsSelected/TreeView.SelectedItem, because
    /// native WPF TreeView enforces single-selection internally (setting IsSelected=true on a second
    /// item silently unselects the first via TreeView.ChangeSelection), which cannot represent the
    /// contract's multi-select set. Painted via a DataTrigger in Themes/Tree.xaml. See this family's
    /// "WPF implementation notes" in docs/parity/tree.md for the accessibility tradeoff this implies.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
