using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: one menu item ("li"). Cascades itself to its Trigger/Icon/Content descendants via an
/// inherited attached property (<see cref="OwnerProperty"/>), the same role Blazor's
/// CascadingValue plays for the item's Value in the web contract.
/// </summary>
public class NaviusNavigationMenuItem : ContentControl
{
    public static readonly DependencyProperty OwnerProperty = DependencyProperty.RegisterAttached(
        "Owner", typeof(NaviusNavigationMenuItem), typeof(NaviusNavigationMenuItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    public static NaviusNavigationMenuItem? GetOwner(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (NaviusNavigationMenuItem?)element.GetValue(OwnerProperty);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusNavigationMenuItem),
        new PropertyMetadata(string.Empty));

    static NaviusNavigationMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuItem),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuItem)));
    }

    public NaviusNavigationMenuItem()
    {
        SetValue(OwnerProperty, this);
    }

    /// <summary>Matched against the ambient host's open Value.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
