using System;
using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A delta: unlike the web contract (an ItemsControl wrapper that nests its
/// NaviusMenubarRadioItem children), this is a non-visual coordinator (plain DependencyObject,
/// not part of the logical/visual tree). Nesting an arbitrary ItemsControl inside a native
/// MenuItem's Items would make WPF auto-generate a wrapper MenuItem around it (MenuItem/Menu
/// only treat MenuItem/Separator as "their own container"), which risks breaking the nested
/// items' native Role/keyboard-navigation assignment in ways that can't be verified without an
/// interactive runtime. Declare one instance (e.g. as a resource) and point each
/// NaviusMenubarRadioItem's <see cref="NaviusMenubarRadioItem.Group"/> at it instead:
/// <code>
/// &lt;menubar:NaviusMenubarRadioGroup x:Key="ViewGroup" Value="list" /&gt;
/// ...
/// &lt;menubar:NaviusMenubarRadioItem Value="list" Group="{StaticResource ViewGroup}" /&gt;
/// </code>
/// See docs/parity/menubar.md "WPF implementation notes" for the full rationale.
/// </summary>
public class NaviusMenubarRadioGroup : DependencyObject
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusMenubarRadioGroup),
        new PropertyMetadata(null, OnValueChanged));

    /// <summary>Selected value shared across every RadioItem pointed at this group.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Raised whenever <see cref="Value"/> changes, including from a RadioItem selection.</summary>
    public event EventHandler<string?>? ValueChanged;

    /// <summary>Invoked by a member RadioItem when it is selected.</summary>
    internal void Select(string value) => Value = value;

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMenubarRadioGroup)d).ValueChanged?.Invoke(d, (string?)e.NewValue);
}
