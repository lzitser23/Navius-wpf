using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/>. Collapses the contract's
/// NaviusMenubarMenu (context-only) + NaviusMenubarTrigger (the button) + NaviusMenubarPortal +
/// NaviusMenubarPositioner + NaviusMenubarPopup (the floating surface) into a single control:
/// a top-level WPF MenuItem's Header already renders as the clickable trigger and its Items
/// already render as a floating, positioned, dismissable submenu popup. See parity notes.
/// </summary>
public class NaviusMenubarMenu : MenuItem
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusMenubarMenu),
        new PropertyMetadata(null));

    static NaviusMenubarMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarMenu),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarMenu)));
    }

    /// <summary>Identity of this menu; required (throws if null/empty once initialized).</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        if (string.IsNullOrEmpty(Value))
        {
            throw new InvalidOperationException(
                $"{nameof(NaviusMenubarMenu)}.{nameof(Value)} is required and must be non-empty.");
        }
    }
}
