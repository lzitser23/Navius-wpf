using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Lightweight, attachable text part mirroring NaviusProgressValue: renders the formatted value
/// text of a <see cref="NaviusProgress"/> named via <see cref="Source"/> (WPF's ProgressBar has no
/// content model to nest a value part inside, so this is a companion element wired the same way
/// WPF's own <see cref="Label"/> wires its Target, rather than a cascading-context registration).
/// Defaults to the rounded percentage, empty while indeterminate; <see cref="TextOverride"/> overrides it.
/// </summary>
public class NaviusProgressValue : TextBlock
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(NaviusProgress), typeof(NaviusProgressValue),
        new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty TextOverrideProperty = DependencyProperty.Register(
        nameof(TextOverride), typeof(string), typeof(NaviusProgressValue),
        new PropertyMetadata(null, OnTextOverrideChanged));

    public NaviusProgress? Source
    {
        get => (NaviusProgress?)GetValue(SourceProperty);
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
        var self = (NaviusProgressValue)d;

        if (e.OldValue is NaviusProgress oldSource)
        {
            oldSource.StateChanged -= self.OnSourceStateChanged;
        }

        if (e.NewValue is NaviusProgress newSource)
        {
            newSource.StateChanged += self.OnSourceStateChanged;
        }

        self.Refresh();
    }

    private static void OnTextOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusProgressValue)d).Refresh();

    private void OnSourceStateChanged(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh() => Text = TextOverride ?? Source?.FormatValueText() ?? string.Empty;
}
