using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Tier A for the button itself (derives from the native ToggleButton), Tier B for the explicit
/// key handling. IToolbarItem registers it with the ancestor NaviusToolbar's single shared
/// roving-focus scan (see NaviusToolbar's remarks); Disabled/GroupContext.Disabled registers it
/// with the ancestor NaviusToolbarToggleGroup's pressed-state ownership instead -- two separate
/// ancestor relationships, matching the contract's "registers with both ToolbarContext (for
/// roving) and ToolbarToggleGroupContext (for pressed state)".
///
/// The Space/Enter OnKeyDown override is copied from NaviusToggleGroupItem verbatim (see that
/// class's remarks for the full 2026-07-09 audit rationale: the shipped web version's Space was
/// found dead on toolbar/toggle-group items despite the contract's own keyboard table saying
/// Space and Enter should both activate). Reusing the exact fix here, rather than re-deriving it,
/// is the explicit residual/follow-up instruction in docs/parity/toolbar.md's M6 audit section.
/// </summary>
public class NaviusToolbarToggleItem : ToggleButton, IToolbarItem
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusToolbarToggleItem),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(NaviusToolbarToggleItem),
        new PropertyMetadata(false, OnDisabledChanged));

    static NaviusToolbarToggleItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToolbarToggleItem),
            new FrameworkPropertyMetadata(typeof(NaviusToolbarToggleItem)));
    }

    public NaviusToolbarToggleItem()
    {
        // Contract's item is strictly two-state (pressed: bool), like NaviusToggle/NaviusToggleGroupItem.
        IsThreeState = false;
    }

    /// <summary>Identifies this item in the group's pressed-value set.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// This item's own disabled flag. Effective IsEnabled is Disabled || the ancestor
    /// NaviusToolbarToggleGroup's own Disabled, recomputed here and whenever the group's Disabled
    /// changes (see NaviusToolbarToggleGroup.PushDisabledToItems).
    /// </summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsEnabled && e.Key is Key.Space or Key.Enter)
        {
            // Held Space must not flap the pressed state: a native web button fires Space once on
            // key-up, ignoring auto-repeat. Enter auto-repeat is native on both platforms and
            // stays allowed.
            if (!(e.Key == Key.Space && e.IsRepeat))
            {
                OnClick();
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    internal void UpdateEffectiveDisabled()
    {
        var groupDisabled = LogicalTreeWalker.Ancestor<NaviusToolbarToggleGroup>(this)?.Disabled ?? false;
        IsEnabled = !(Disabled || groupDisabled);
    }

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToolbarToggleItem)d).UpdateEffectiveDisabled();
}
