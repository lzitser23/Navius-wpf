using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier A: derives from the native MenuItem with IsCheckable = true. WPF's MenuItem has no
/// built-in radio/GroupName concept the way RadioButton does, so single-checked enforcement
/// within a group is implemented here directly: GroupName is a plain dependency property
/// (not WPF's attached RadioButton.GroupName, which this class deliberately avoids reusing -
/// same reasoning as NaviusRadioGroupItem, to keep one source of truth for "which item is
/// checked"), and a click walks the item's own owning ItemsControl (the immediate submenu or
/// root popup - exactly the scope the contract's NaviusMenuRadioGroup would otherwise wrap)
/// via ItemsControl.ItemsControlFromItemContainer, unchecking siblings that share GroupName.
///
/// The contract's separate NaviusMenuRadioGroup wrapper part collapses into this attached
/// GroupName property: native MenuItem/ContextMenu item collections expect flat item
/// containers for arrow-key roving to work, so a transparent, non-MenuItem "group" wrapper
/// would break native keyboard navigation between its children and the rest of the menu.
/// </summary>
public class NaviusMenuRadioItem : NaviusMenuItemBase
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusMenuRadioItem),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
        nameof(GroupName),
        typeof(string),
        typeof(NaviusMenuRadioItem),
        new PropertyMetadata(string.Empty));

    static NaviusMenuRadioItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuRadioItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenuRadioItem)));
    }

    public NaviusMenuRadioItem()
    {
        IsCheckable = true;
    }

    /// <summary>This item's value within its radio group.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Items sharing a GroupName under the same owning menu/submenu are mutually exclusive.</summary>
    public string GroupName
    {
        get => (string)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    protected override void OnClick()
    {
        // Space/click selects; there is no deselect (matches RadioItem semantics elsewhere
        // in this library), so re-clicking the already-checked item skips the no-op toggle.
        if (IsChecked != true)
        {
            IsChecked = true;
            EnforceSingleSelection();
        }

        var args = RaiseSelect();

        if (!args.IsDefaultPrevented)
        {
            CloseOwningMenu(this);
        }
    }

    private void EnforceSingleSelection()
    {
        if (ItemsControl.ItemsControlFromItemContainer(this) is not ItemsControl parent)
        {
            return;
        }

        foreach (var sibling in parent.Items.OfType<NaviusMenuRadioItem>())
        {
            if (!ReferenceEquals(sibling, this) && string.Equals(sibling.GroupName, GroupName, System.StringComparison.Ordinal))
            {
                sibling.IsChecked = false;
            }
        }
    }
}
