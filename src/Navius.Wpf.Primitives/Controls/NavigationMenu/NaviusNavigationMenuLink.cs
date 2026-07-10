using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier A-ish: derives from the native <see cref="Button"/> for native focus/click/automation
/// (the web contract's native anchor "a" gives the same for free). Usable as a top-level item
/// (in place of a Trigger+Content pair) or inside a Content panel.
/// </summary>
public class NaviusNavigationMenuLink : Button
{
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(
        nameof(Href), typeof(string), typeof(NaviusNavigationMenuLink),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(
        nameof(Active), typeof(bool), typeof(NaviusNavigationMenuLink),
        new PropertyMetadata(false, OnActiveChanged));

    static NaviusNavigationMenuLink()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuLink),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuLink)));
    }

    /// <summary>Link target. Navigation itself is left to the consumer's Select/Click handler.</summary>
    public string? Href
    {
        get => (string?)GetValue(HrefProperty);
        set => SetValue(HrefProperty, value);
    }

    /// <summary>Marks the current page: sets AutomationProperties.ItemStatus="page" and a data-active-equivalent style trigger.</summary>
    public bool Active
    {
        get => (bool)GetValue(ActiveProperty);
        set => SetValue(ActiveProperty, value);
    }

    public event EventHandler<NaviusNavigationMenuSelectEventArgs>? Select;

    private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        AutomationProperties.SetItemStatus((UIElement)d, (bool)e.NewValue ? "page" : string.Empty);

    /// <summary>
    /// Raises the cancelable Select event before the native click, same PreventDefault-skips-base
    /// pattern as the Menubar family's items.
    /// </summary>
    protected override void OnClick()
    {
        var args = new NaviusNavigationMenuSelectEventArgs();
        Select?.Invoke(this, args);

        if (args.Cancel)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            return;
        }

        base.OnClick();
    }
}
