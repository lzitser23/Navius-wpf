using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: presentational chevron slot inside a Trigger. Mirrors its owning item's open state
/// via the read-only <see cref="IsOpen"/> DP (a template trigger in Themes/NavigationMenu.xaml
/// rotates the default glyph); AutomationProperties.AccessibilityView is not set to Raw here
/// since WPF has no direct aria-hidden equivalent worth reimplementing for a purely decorative glyph.
/// </summary>
public class NaviusNavigationMenuIcon : ContentControl
{
    private static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOpen), typeof(bool), typeof(NaviusNavigationMenuIcon),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

    static NaviusNavigationMenuIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuIcon),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuIcon)));
    }

    public NaviusNavigationMenuIcon()
    {
        Focusable = false;
        Loaded += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
    }

    public bool IsOpen => (bool)GetValue(IsOpenProperty);

    private NavigationMenuHostBase? _host;

    private void Subscribe()
    {
        _host = NavigationMenuHostBase.GetHost(this);
        if (_host is null)
        {
            return;
        }

        _host.ValueChanged += OnHostValueChanged;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (_host is not null)
        {
            _host.ValueChanged -= OnHostValueChanged;
            _host = null;
        }
    }

    private void OnHostValueChanged(object? sender, string? value) => Refresh();

    private void Refresh()
    {
        var owner = NaviusNavigationMenuItem.GetOwner(this);
        var isOpen = _host is not null && owner is not null
            && string.Equals(_host.Value, owner.Value, StringComparison.Ordinal);
        SetValue(IsOpenPropertyKey, isOpen);
    }
}
