using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.ToggleGroup;

/// <summary>
/// Tier A for the button itself (derives from the native ToggleButton, giving
/// ToggleButtonAutomationPeer / UIA TogglePattern / aria-pressed semantics for free), Tier
/// B for the explicit key handling documented below. Own item type rather than reusing
/// NaviusToggle: this needs a Value identity (for the group's pressed-value set) and,
/// unlike a standalone Toggle, its checked state is entirely driven by the ancestor
/// NaviusToggleGroup (click bubbles up, the group decides single-vs-multiple semantics and
/// writes IsChecked back down), matching the RadioGroup/RadioGroupItem split.
///
/// A11y delta: the 2026-07-09 audit found Space was dead on ToggleGroup items in the
/// shipped web version, even though the contract's own keyboard table says Space/Enter
/// should both activate via native &lt;button&gt; semantics. WAI-ARIA APG agrees both keys
/// should activate a button-role widget. Native WPF ButtonBase only wires Space by default
/// (Enter requires IsDefault, which does not apply to a roving-focus group item), so this
/// overrides OnKeyDown to explicitly handle both Space and Enter via the same OnClick path
/// a mouse click uses, guaranteeing neither can regress into "dead" the way Space did on
/// the web.
/// </summary>
public class NaviusToggleGroupItem : ToggleButton
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusToggleGroupItem),
        new PropertyMetadata(string.Empty));

    static NaviusToggleGroupItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToggleGroupItem),
            new FrameworkPropertyMetadata(typeof(NaviusToggleGroupItem)));
    }

    public NaviusToggleGroupItem()
    {
        // Contract's item is strictly two-state (pressed: bool), like NaviusToggle.
        IsThreeState = false;
    }

    /// <summary>Identifies this item in the group's pressed-value set.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsEnabled && e.Key is Key.Space or Key.Enter)
        {
            OnClick();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
