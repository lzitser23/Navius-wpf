using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.RadioGroup;

/// <summary>
/// Composable indicator part (contract's NaviusRadioGroupIndicator). Wired into
/// NaviusRadioGroupItem's default template (see Themes/RadioGroup.xaml) via a
/// TemplateBinding of IsChecked; mirrors the contract's "renders only when checked (or
/// always when KeepMounted)" rule by toggling its own Visibility. No WPF built-in
/// equivalent exists for this conditional-mount behavior.
/// </summary>
public class NaviusRadioGroupIndicator : ContentControl
{
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool),
        typeof(NaviusRadioGroupIndicator),
        new PropertyMetadata(false, OnMountednessChanged));

    public static readonly DependencyProperty KeepMountedProperty = DependencyProperty.Register(
        nameof(KeepMounted),
        typeof(bool),
        typeof(NaviusRadioGroupIndicator),
        new PropertyMetadata(false, OnMountednessChanged));

    static NaviusRadioGroupIndicator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusRadioGroupIndicator),
            new FrameworkPropertyMetadata(typeof(NaviusRadioGroupIndicator)));
    }

    public NaviusRadioGroupIndicator()
    {
        UpdateVisibility();
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool KeepMounted
    {
        get => (bool)GetValue(KeepMountedProperty);
        set => SetValue(KeepMountedProperty, value);
    }

    private static void OnMountednessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusRadioGroupIndicator)d).UpdateVisibility();

    private void UpdateVisibility() =>
        Visibility = IsChecked || KeepMounted ? Visibility.Visible : Visibility.Collapsed;
}
