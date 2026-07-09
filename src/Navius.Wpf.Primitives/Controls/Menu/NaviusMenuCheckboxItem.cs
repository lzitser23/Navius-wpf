using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier A: derives from the native MenuItem with IsCheckable = true. Native MenuItem.IsChecked
/// is a plain bool (MenuItem has no IsThreeState the way CheckBox does), so the contract's
/// tri-state `Checked` (bool?) is a new dependency property layered on top: null drives a
/// separate IsIndeterminate flag (consumed by the ControlTemplate to draw a dash glyph)
/// while keeping native IsChecked false, and a click always resolves indeterminate/false to
/// true, true to false - matching NaviusCheckbox's own click-resolution rule for consistency
/// across the two families.
///
/// UIA note: MenuItemAutomationPeer already implements IToggleProvider when IsCheckable is
/// true (reporting ControlType.MenuItem, not a separate CheckBox control type - which is
/// actually how real Windows apps expose checkable menu items). Its ToggleState has no
/// Indeterminate case since native IsChecked is a plain bool, so a null Checked value is
/// reported as Off at the automation layer. Per the parity doc's own open question,
/// indeterminate is reachable only programmatically (no user gesture produces it), so this
/// gap is left undocumented-in-code and just noted here rather than given a custom peer.
/// </summary>
public class NaviusMenuCheckboxItem : NaviusMenuItemBase
{
    public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register(
        nameof(Checked),
        typeof(bool?),
        typeof(NaviusMenuCheckboxItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCheckedChanged));

    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
        nameof(IsIndeterminate),
        typeof(bool),
        typeof(NaviusMenuCheckboxItem),
        new PropertyMetadata(false));

    public static readonly RoutedEvent CheckedChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(CheckedChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusMenuCheckboxItem));

    static NaviusMenuCheckboxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuCheckboxItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenuCheckboxItem)));
    }

    public NaviusMenuCheckboxItem()
    {
        IsCheckable = true;
    }

    /// <summary>
    /// Controlled tri-state (true / false / null = indeterminate). Intentionally hides
    /// MenuItem's own `Checked` routed event (same tradeoff NaviusMenubarCheckboxItem makes
    /// elsewhere in this port); use the CheckedChanged event on this class instead.
    /// </summary>
    public new bool? Checked
    {
        get => (bool?)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public event RoutedEventHandler CheckedChanged
    {
        add => AddHandler(CheckedChangedEvent, value);
        remove => RemoveHandler(CheckedChangedEvent, value);
    }

    protected override void OnClick()
    {
        // Toggles regardless of what OnSelect decides below: the contract fires OnSelect
        // "after the checked state toggles", so the flip always happens first.
        Checked = Checked != true;

        var args = RaiseSelect();

        if (!args.IsDefaultPrevented)
        {
            CloseOwningMenu(this);
        }
    }

    private static void OnCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NaviusMenuCheckboxItem)d;
        item.IsIndeterminate = e.NewValue is null;
        item.IsChecked = (bool?)e.NewValue == true;
        item.RaiseEvent(new RoutedEventArgs(CheckedChangedEvent, item));
    }
}
