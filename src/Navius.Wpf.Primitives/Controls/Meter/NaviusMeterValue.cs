using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Attachable text part mirroring NaviusProgressValue: renders the formatted value text of a
/// NaviusMeter named via <see cref="Source"/> (WPF's ProgressBar has no content model to nest a
/// value part inside). Defaults to the rounded percentage; <see cref="TextOverride"/> overrides it.
/// </summary>
public class NaviusMeterValue : TextBlock
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(NaviusMeter), typeof(NaviusMeterValue),
        new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty TextOverrideProperty = DependencyProperty.Register(
        nameof(TextOverride), typeof(string), typeof(NaviusMeterValue),
        new PropertyMetadata(null, OnTextOverrideChanged));

    public NaviusMeter? Source
    {
        get => (NaviusMeter?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>When set, overrides the default rounded-percentage text.</summary>
    public string? TextOverride
    {
        get => (string?)GetValue(TextOverrideProperty);
        set => SetValue(TextOverrideProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (NaviusMeterValue)d;

        if (e.OldValue is NaviusMeter oldSource)
        {
            oldSource.StateChanged -= self.OnSourceStateChanged;
        }

        if (e.NewValue is NaviusMeter newSource)
        {
            newSource.StateChanged += self.OnSourceStateChanged;
        }

        self.Refresh();
    }

    private static void OnTextOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMeterValue)d).Refresh();

    private void OnSourceStateChanged(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh() => Text = TextOverride ?? Source?.FormatValueText() ?? string.Empty;
}
