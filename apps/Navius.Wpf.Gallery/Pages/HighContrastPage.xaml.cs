using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusTheme.HighContrast: applying it and restoring Light, plus a few controls
/// painted from the mapped tokens. Self-contained, no navigation wiring (see this family's HARD
/// RULES in the task brief). See ADR-0007 for the token mapping and system-sync design.
/// </summary>
public partial class HighContrastPage : UserControl
{
    public HighContrastPage()
    {
        InitializeComponent();

        UpdateCurrentThemeSummary();
        ThemeManager.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private void OnApplyHighContrastClick(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(NaviusTheme.HighContrast);

    private void OnRestoreLightClick(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(NaviusTheme.Light);

    private void OnThemeChanged(object? sender, NaviusTheme theme) => UpdateCurrentThemeSummary();

    private void UpdateCurrentThemeSummary() => CurrentThemeSummary.Text = $"Current theme: {ThemeManager.Current}";
}
