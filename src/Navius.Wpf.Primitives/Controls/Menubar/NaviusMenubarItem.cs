using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/>. Disabled maps directly onto the
/// inherited IsEnabled (no separate DP, unlike the web contract) since WPF already gives that
/// for free. TextValue is kept for API parity but has no native effect: WPF's own typeahead
/// already matches against Header text.
/// </summary>
public class NaviusMenubarItem : MenuItem
{
    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue), typeof(string), typeof(NaviusMenubarItem),
        new PropertyMetadata(null));

    static NaviusMenubarItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarItem)));
    }

    public event EventHandler<NaviusMenubarSelectEventArgs>? Select;

    /// <summary>Overrides typeahead match text. Not wired to WPF's native typeahead (see remarks).</summary>
    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    /// <summary>
    /// Raises the cancelable Select event before the native click. When
    /// <see cref="NaviusMenubarSelectEventArgs.PreventDefault"/> is called, the base MenuItem
    /// click handling (which both invokes any bound Command and closes the ancestor submenu chain) is skipped
    /// entirely; only the plain Click routed event still fires. Command-bound items should avoid
    /// PreventDefault, or invoke their Command manually from the Select handler.
    /// </summary>
    protected override void OnClick()
    {
        var args = new NaviusMenubarSelectEventArgs();
        Select?.Invoke(this, args);

        if (args.Cancel)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            return;
        }

        base.OnClick();
    }
}
