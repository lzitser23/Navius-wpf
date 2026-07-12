using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Alert;

/// <summary>Default | Warning | Destructive.</summary>
public enum NaviusAlertVariant
{
    Default,
    Warning,
    Destructive,
}

/// <summary>
/// A callout for user attention. Compositional: nest NaviusAlertTitle/NaviusAlertDescription
/// (and any icon) inside a StackPanel as the single Content, mirroring the web contract's
/// child-content model. Warning and Destructive map to dedicated semantic theme tokens.
/// </summary>
public class NaviusAlert : ContentControl
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusAlertVariant), typeof(NaviusAlert),
        new FrameworkPropertyMetadata(NaviusAlertVariant.Default));

    static NaviusAlert()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusAlert), new FrameworkPropertyMetadata(typeof(NaviusAlert)));
    }

    public NaviusAlertVariant Variant
    {
        get => (NaviusAlertVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
