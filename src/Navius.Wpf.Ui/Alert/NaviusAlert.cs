using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Alert;

/// <summary>Default | Destructive. The only two severities with dedicated brand tokens; see docs comment on NaviusAlert.Variant.</summary>
public enum NaviusAlertVariant
{
    Default,
    Destructive,
}

/// <summary>
/// A callout for user attention. Compositional: nest NaviusAlertTitle/NaviusAlertDescription
/// (and any icon) inside a StackPanel as the single Content, mirroring the web contract's
/// child-content model. Only Default/Destructive variants ship: the token set has no
/// Warning/Success/Info brushes, and inventing untokenized colors would break the one-ink
/// discipline, so severity is limited to what the palette actually supports.
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
