using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Shared base for NaviusMenuItem/NaviusMenuCheckboxItem/NaviusMenuRadioItem (Tier A: all
/// derive from the native MenuItem). Factors out the two behaviors the contract asks all
/// three item kinds to have: a TextValue override for typeahead, and a cancelable OnSelect
/// that governs whether activation closes the owning menu.
///
/// TextValue is wired straight onto WPF's own TextSearch.Text attached property (native
/// first-letter typeahead already reads it), resolving the parity doc's open question in
/// favor of reusing the native mechanism instead of reimplementing one.
///
/// Native MenuItem.OnClick bakes "raise Click, run Command, close the open menu chain" into
/// one non-overridable block with no seam to keep the close conditional on a cancelable
/// event, so every concrete subclass here bypasses base.OnClick() for leaf activation and
/// calls CloseOwningMenu() itself only when OnSelect was not prevented. One side effect:
/// MenuItem's own Click routed event no longer fires from these controls; Select replaces it.
/// </summary>
public abstract class NaviusMenuItemBase : System.Windows.Controls.MenuItem
{
    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue),
        typeof(string),
        typeof(NaviusMenuItemBase),
        new PropertyMetadata(null, OnTextValueChanged));

    public static readonly RoutedEvent SelectEvent = EventManager.RegisterRoutedEvent(
        nameof(Select),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusMenuItemBase));

    /// <summary>Overrides the text typeahead matches against (contract's TextValue).</summary>
    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    public event RoutedEventHandler Select
    {
        add => AddHandler(SelectEvent, value);
        remove => RemoveHandler(SelectEvent, value);
    }

    private static void OnTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        TextSearch.SetText(d, e.NewValue as string ?? string.Empty);

    protected NaviusSelectEventArgs RaiseSelect()
    {
        var args = new NaviusSelectEventArgs(SelectEvent, this);
        RaiseEvent(args);
        return args;
    }

    /// <summary>
    /// Walks up the ItemsControl chain (a submenu MenuItem is simultaneously an item
    /// container in its parent and its own children's ItemsControl) to the owning root
    /// ContextMenu and closes it. Closing the root drops every nested submenu Popup with it,
    /// so no per-level bookkeeping is needed.
    /// </summary>
    protected static void CloseOwningMenu(DependencyObject item)
    {
        for (var current = ItemsControl.ItemsControlFromItemContainer(item);
             current is not null;
             current = ItemsControl.ItemsControlFromItemContainer(current))
        {
            if (current is System.Windows.Controls.ContextMenu contextMenu)
            {
                contextMenu.IsOpen = false;
                return;
            }
        }
    }
}
