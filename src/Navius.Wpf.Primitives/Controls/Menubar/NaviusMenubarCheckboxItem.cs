using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/> with <see cref="MenuItem.IsCheckable"/>
/// set, which also gives the contract's "checkable items don't close the menu on click" rule for
/// free (native WPF behavior). Native <see cref="MenuItem.IsChecked"/> is a plain bool with no
/// indeterminate concept, so tri-state is reimplemented via the own nullable <see cref="Checked"/>
/// DP (mirrored one-way into native IsChecked, mapping indeterminate to false, so the built-in
/// MenuItemAutomationPeer's IToggleProvider still reports a binary approximation); the visible
/// glyph itself is driven by <see cref="Checked"/> directly in Themes/Menubar.xaml (same
/// {x:Null}-trigger technique as Themes/Checkbox.xaml).
///
/// This repo's WPF port does not replicate the web contract's controlled-vs-uncontrolled
/// (CheckedChanged.HasDelegate) distinction: Checked is always a two-way bindable DP and
/// CheckedChanged always raises on toggle, matching the canonical strategy called out as an
/// open question in the parity doc.
/// </summary>
public class NaviusMenubarCheckboxItem : MenuItem
{
    public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register(
        nameof(Checked), typeof(bool?), typeof(NaviusMenubarCheckboxItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCheckedChanged));

    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue), typeof(string), typeof(NaviusMenubarCheckboxItem),
        new PropertyMetadata(null));

    static NaviusMenubarCheckboxItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarCheckboxItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarCheckboxItem)));
    }

    public NaviusMenubarCheckboxItem()
    {
        IsCheckable = true;
    }

    /// <summary>
    /// Controlled tri-state; null = indeterminate. Deliberately hides (via <c>new</c>) the base
    /// MenuItem.Checked routed event: same CLR name, unrelated member kind (this is a DP-backed
    /// property, not an event), and the contract's Checked/CheckedChanged pair is the more useful
    /// public surface here.
    /// </summary>
    public new bool? Checked
    {
        get => (bool?)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    public event EventHandler<bool?>? CheckedChanged;

    public event EventHandler<NaviusMenubarSelectEventArgs>? Select;

    private static void OnCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NaviusMenubarCheckboxItem)d;
        var value = (bool?)e.NewValue;
        item.IsChecked = value == true;

        // Raise from the DP-change callback (not just from OnClick) so the paired event fires on
        // every Checked change, programmatic or click-driven, matching the canonical
        // "always raises on change, controlled or not" strategy and the sibling
        // NaviusMenuCheckboxItem's behavior.
        item.CheckedChanged?.Invoke(item, value);
    }

    /// <summary>
    /// Never calls base.OnClick(): native MenuItem's own IsCheckable click handling
    /// unconditionally toggles IsChecked too, which would double-toggle against the tri-state
    /// logic here. The Click routed event is still raised manually so external Click handlers
    /// keep working; a bound Command would not run (checkable items are OnSelect-driven in the
    /// contract, not Command-driven, so this is an accepted delta).
    /// </summary>
    protected override void OnClick()
    {
        // Indeterminate/false -> true; true -> false. The Checked setter's DP-change callback
        // raises CheckedChanged; OnClick does not raise it again (avoids a double-raise per click).
        Checked = Checked != true;

        var args = new NaviusMenubarSelectEventArgs();
        Select?.Invoke(this, args);

        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }
}
