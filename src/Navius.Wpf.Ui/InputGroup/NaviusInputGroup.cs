using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.InputGroup;

/// <summary>
/// A text field with optional leading/trailing adornment slots (icon, unit label, inline button)
/// sharing a single hairline border with the input, rather than each slot drawing its own box.
/// Own lookless Control (not a decorated TextBox) so the border/focus-ring chrome lives in one
/// place around all three regions; the real <see cref="TextBox"/> is a template part (PART_Input)
/// so callers can still reach it (e.g. to set validation adorners) via <see cref="GetTemplateChild"/>.
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
public class NaviusInputGroup : Control
{
    private const string PartInput = "PART_Input";

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(NaviusInputGroup),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusInputGroup), new PropertyMetadata(null));

    public static readonly DependencyProperty LeadingContentProperty = DependencyProperty.Register(
        nameof(LeadingContent), typeof(object), typeof(NaviusInputGroup), new PropertyMetadata(null));

    public static readonly DependencyProperty TrailingContentProperty = DependencyProperty.Register(
        nameof(TrailingContent), typeof(object), typeof(NaviusInputGroup), new PropertyMetadata(null));

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NaviusInputGroup), new PropertyMetadata(false));

    private TextBox? _inputPart;

    static NaviusInputGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusInputGroup),
            new FrameworkPropertyMetadata(typeof(NaviusInputGroup)));
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Content shown before the input (e.g. a search icon or a fixed unit like "$").</summary>
    public object? LeadingContent
    {
        get => GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    /// <summary>Content shown after the input (e.g. a clear button or a unit like "kg").</summary>
    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Focuses the underlying text input, once the template has applied.</summary>
    public void FocusInput() => _inputPart?.Focus();

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _inputPart = GetTemplateChild(PartInput) as TextBox;
    }
}
