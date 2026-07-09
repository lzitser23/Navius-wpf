using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native ToggleButton, the same approach as NaviusToggle. IsChecked
/// maps directly onto the contract's Checked/CheckedChanged (both plain bool, IsThreeState left
/// false), and the inherited ToggleButtonAutomationPeer already exposes UIA TogglePattern, which
/// is the closest native mapping to the contract's role="switch"/aria-checked (see
/// docs/parity/switch.md "WPF strategy"); no custom AutomationPeer is added.
///
/// WPF has no native ReadOnly concept on ToggleButton, so OnToggle is overridden to no-op while
/// ReadOnly (the control stays focusable, not disabled) -- the identical pattern already used by
/// NaviusCheckbox and NaviusRadioGroupItem in this codebase.
///
/// PART_Thumb is a template part only (see Themes/Switch.xaml); it has no dedicated CLR type,
/// resolving the contract's open question in favor of a mandatory template part styled directly
/// off the same IsChecked/IsEnabled triggers as the track, rather than a separate
/// cascaded-context component.
/// </summary>
[TemplatePart(Name = PartThumb, Type = typeof(FrameworkElement))]
public class NaviusSwitch : ToggleButton
{
    private const string PartThumb = "PART_Thumb";

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusSwitch), new PropertyMetadata(false));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusSwitch), new PropertyMetadata(false));

    static NaviusSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSwitch),
            new FrameworkPropertyMetadata(typeof(NaviusSwitch)));
    }

    public NaviusSwitch()
    {
        IsThreeState = false;
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

    protected override void OnToggle()
    {
        if (ReadOnly)
        {
            // Stays focusable (no base.OnToggle call, IsEnabled untouched) but the value is not
            // allowed to change.
            return;
        }

        base.OnToggle();
    }
}
