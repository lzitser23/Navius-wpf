using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Checkbox;

/// <summary>
/// Tier A: derives from the native CheckBox. WPF's CheckBox already models tri-state via
/// IsThreeState + nullable IsChecked, which maps directly onto the parity contract's
/// CheckedState (bool?, null = indeterminate); the contract's separate boolean Checked
/// parameter collapses onto the same IsChecked property (open question in the contract
/// resolved in favor of WPF's single nullable property). role="checkbox" + aria-checked
/// map onto CheckBoxAutomationPeer, which already reports ToggleState.Indeterminate for
/// null IsChecked.
///
/// ReadOnly and Required have no native WPF CheckBox equivalent and are added as new
/// dependency properties. Disabled maps onto the inherited IsEnabled (no new property).
/// GroupValue/IsSelectAll are new properties that give a checkbox an identity within a
/// NaviusCheckboxGroup (they replace the contract's dual-purpose "Name"/"Parent"
/// parameters; "Name" could not be reused because FrameworkElement.Name is a reserved
/// CLR member tied to x:Name/NameScope, and "Parent" collides with the read-only
/// DependencyObject/Visual "Parent" member).
/// </summary>
public class NaviusCheckbox : System.Windows.Controls.CheckBox
{
    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly),
        typeof(bool),
        typeof(NaviusCheckbox),
        new PropertyMetadata(false));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required),
        typeof(bool),
        typeof(NaviusCheckbox),
        new PropertyMetadata(false));

    /// <summary>Identity used by an ancestor NaviusCheckboxGroup to track this checkbox's checked-ness.</summary>
    public static readonly DependencyProperty GroupValueProperty = DependencyProperty.Register(
        nameof(GroupValue),
        typeof(string),
        typeof(NaviusCheckbox),
        new PropertyMetadata(null));

    /// <summary>Marks this checkbox as the group's roll-up "select all" checkbox (contract's Parent).</summary>
    public static readonly DependencyProperty IsSelectAllProperty = DependencyProperty.Register(
        nameof(IsSelectAll),
        typeof(bool),
        typeof(NaviusCheckbox),
        new PropertyMetadata(false));

    static NaviusCheckbox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCheckbox),
            new FrameworkPropertyMetadata(typeof(NaviusCheckbox)));
    }

    public NaviusCheckbox()
    {
        IsThreeState = true;
    }

    /// <summary>Focusable but the value cannot be changed (contract's ReadOnly).</summary>
    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public string? GroupValue
    {
        get => (string?)GetValue(GroupValueProperty);
        set => SetValue(GroupValueProperty, value);
    }

    public bool IsSelectAll
    {
        get => (bool)GetValue(IsSelectAllProperty);
        set => SetValue(IsSelectAllProperty, value);
    }

    /// <summary>
    /// Replaces WPF's native three-value click cycle (unchecked -> checked -> indeterminate
    /// -> unchecked) with the contract's rule: a user click/Space always lands on checked
    /// when starting from indeterminate or unchecked, and on unchecked when starting from
    /// checked. Indeterminate is reachable only by setting IsChecked = null programmatically
    /// (e.g. NaviusCheckboxGroup's roll-up), never via direct user interaction.
    /// </summary>
    protected override void OnToggle()
    {
        if (ReadOnly)
        {
            // Stays focusable (no base.OnToggle call, IsEnabled untouched) but the value
            // is not allowed to change.
            return;
        }

        IsChecked = IsChecked != true;
    }
}
