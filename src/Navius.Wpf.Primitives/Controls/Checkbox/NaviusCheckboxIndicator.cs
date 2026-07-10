using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Checkbox;

/// <summary>
/// Composable indicator part (contract's NaviusCheckboxIndicator). Native WPF CheckBox
/// already renders its own glyph internally, so this is a lightweight ContentControl
/// wired into NaviusCheckbox's default template (see Themes/Checkbox.xaml) via a
/// TemplateBinding of IsChecked; it mirrors the contract's "mounted only when checked
/// or indeterminate (or KeepMounted)" rule by toggling its own Visibility.
/// </summary>
public class NaviusCheckboxIndicator : ContentControl
{
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool?),
        typeof(NaviusCheckboxIndicator),
        new PropertyMetadata(false, OnMountednessChanged));

    public static readonly DependencyProperty KeepMountedProperty = DependencyProperty.Register(
        nameof(KeepMounted),
        typeof(bool),
        typeof(NaviusCheckboxIndicator),
        new PropertyMetadata(false, OnMountednessChanged));

    static NaviusCheckboxIndicator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCheckboxIndicator),
            new FrameworkPropertyMetadata(typeof(NaviusCheckboxIndicator)));
    }

    public NaviusCheckboxIndicator()
    {
        UpdateVisibility();
    }

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool KeepMounted
    {
        get => (bool)GetValue(KeepMountedProperty);
        set => SetValue(KeepMountedProperty, value);
    }

    private static void OnMountednessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCheckboxIndicator)d).UpdateVisibility();

    private void UpdateVisibility() =>
        Visibility = IsChecked != false || KeepMounted ? Visibility.Visible : Visibility.Collapsed;
}
