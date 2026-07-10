using System;
using System.Windows;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier A: reuses <see cref="OverlayBackdrop"/> (Overlays/OverlayBackdrop.cs) directly rather
/// than a new scrim implementation, composed via inheritance since it needs its own
/// Visibility-driving IsOpen/KeepMounted DPs bound to the ambient host's open state. Presentational
/// only, open whenever any item is active.
///
/// Re-overrides DefaultStyleKey to point at its own type (Themes/NavigationMenu.xaml has a
/// matching Style BasedOn OverlayBackdrop's): implicit style lookup by DefaultStyleKey does not
/// reliably fall back to a base type's style for a subclass that never registers its own
/// FrameworkPropertyMetadata, so simply inheriting OverlayBackdrop's override does not resolve.
/// </summary>
public class NaviusNavigationMenuBackdrop : OverlayBackdrop
{
    static NaviusNavigationMenuBackdrop()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuBackdrop),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuBackdrop)));
    }

    public static readonly DependencyProperty KeepMountedProperty = DependencyProperty.Register(
        nameof(KeepMounted), typeof(bool), typeof(NaviusNavigationMenuBackdrop),
        new PropertyMetadata(false, OnMountednessChanged));

    private static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOpen), typeof(bool), typeof(NaviusNavigationMenuBackdrop),
        new PropertyMetadata(false, OnMountednessChanged));

    public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

    private NavigationMenuHostBase? _host;

    public NaviusNavigationMenuBackdrop()
    {
        Loaded += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
        UpdateVisibility();
    }

    /// <summary>Keep the backdrop mounted while closed.</summary>
    public bool KeepMounted
    {
        get => (bool)GetValue(KeepMountedProperty);
        set => SetValue(KeepMountedProperty, value);
    }

    public bool IsOpen => (bool)GetValue(IsOpenProperty);

    private static void OnMountednessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusNavigationMenuBackdrop)d).UpdateVisibility();

    private void UpdateVisibility() =>
        Visibility = IsOpen || KeepMounted ? Visibility.Visible : Visibility.Collapsed;

    private void Subscribe()
    {
        _host = NavigationMenuHostBase.GetHost(this);
        if (_host is null)
        {
            return;
        }

        _host.ValueChanged += OnHostValueChanged;
        SetValue(IsOpenPropertyKey, _host.Open);
    }

    private void Unsubscribe()
    {
        if (_host is not null)
        {
            _host.ValueChanged -= OnHostValueChanged;
            _host = null;
        }
    }

    private void OnHostValueChanged(object? sender, string? value) => SetValue(IsOpenPropertyKey, value is not null);
}
