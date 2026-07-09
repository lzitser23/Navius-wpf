using System.Windows;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls.RadioGroup;

/// <summary>
/// Tier A for the item itself: derives from the native RadioButton. RadioButton's default
/// AutomationPeer (RadioButtonAutomationPeer) already implements ISelectionItemProvider and
/// reports ControlType.RadioButton, matching the contract's role="radio"/aria-checked
/// without a custom peer. RadioButton.OnToggle already only ever sets IsChecked to true
/// (never unchecks on its own click), which matches the contract's "Space selects, there is
/// no deselect" rule for free; the only addition needed is a ReadOnly guard.
///
/// GroupName is deliberately left unset: selection is fully owned by the ancestor
/// NaviusRadioGroup (routed-event bubbling + explicit keyboard handling), not WPF's native
/// GroupName-based mutual exclusion, to keep controlled/uncontrolled parity exact and avoid
/// two competing sources of truth for "which item is checked."
/// </summary>
public class NaviusRadioGroupItem : System.Windows.Controls.RadioButton
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusRadioGroupItem),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly),
        typeof(bool),
        typeof(NaviusRadioGroupItem),
        new PropertyMetadata(false));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required),
        typeof(bool),
        typeof(NaviusRadioGroupItem),
        new PropertyMetadata(false));

    static NaviusRadioGroupItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusRadioGroupItem),
            new FrameworkPropertyMetadata(typeof(NaviusRadioGroupItem)));
    }

    /// <summary>The item's value; equality against the group's Value determines checked state.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Focusable but selection cannot change (contract's ReadOnly).</summary>
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

    protected override void OnToggle()
    {
        if (ReadOnly)
        {
            return;
        }

        base.OnToggle();
    }
}
