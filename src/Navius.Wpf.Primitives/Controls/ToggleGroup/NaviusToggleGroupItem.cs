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
/// should activate a button-role widget. Native WPF ButtonBase does in fact wire BOTH keys
/// (Space via its press-on-key-down/click-on-key-up state machine; Enter clicks on key-down
/// because ButtonBase defaults KeyboardNavigation.AcceptsReturn to true, no IsDefault
/// needed). This override still handles both explicitly, for two reasons: it makes the
/// activation deterministic (a single OnClick on key-down, no mouse-capture state machine
/// that can be disturbed by the group's roving-focus PreviewKeyDown handling) and it
/// guarantees neither key can regress into "dead" the way Space did on the web. Space skips
/// key auto-repeat to match a native web button (Space fires once on key-up); Enter is
/// allowed to repeat, which is what a held Enter does on a native web button too.
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
            // Held Space must not flap the pressed state: a native web button fires Space
            // once on key-up, ignoring auto-repeat. Enter auto-repeat is native on both
            // platforms and stays allowed.
            if (!(e.Key == Key.Space && e.IsRepeat))
            {
                OnClick();
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
