using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B (custom lookless control): there is no native WPF NumberBox. The contract's 5 parts
/// (Root/Group/Input/Increment/Decrement) fold into one Control with three named template parts
/// (PART_Input, PART_Increment, PART_Decrement); Group has no behavior of its own beyond a
/// role="group" wrapper div in the contract, so it stays a plain Border in
/// Themes/NumberField.xaml rather than a separate CLR type, the same minimalism NaviusSlider and
/// NaviusProgress use for their own non-interactive template parts.
///
/// PART_Increment/PART_Decrement are RepeatButton, so press-and-hold auto-repeat -- tracked as a
/// follow-up in the web source -- comes for free in this port. Step/clamp/format/parse math is
/// factored into the pure, unit-testable NaviusNumberFieldMath. A custom AutomationPeer implements
/// IRangeValueProvider with AutomationControlType.Spinner, mirroring role="spinbutton" +
/// aria-valuenow/min/max.
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
[TemplatePart(Name = PartIncrement, Type = typeof(RepeatButton))]
[TemplatePart(Name = PartDecrement, Type = typeof(RepeatButton))]
public class NaviusNumberField : Control
{
    private const string PartInput = "PART_Input";
    private const string PartIncrement = "PART_Increment";
    private const string PartDecrement = "PART_Decrement";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double?), typeof(NaviusNumberField),
        new FrameworkPropertyMetadata(null, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double?), typeof(NaviusNumberField),
        new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double?), typeof(NaviusNumberField),
        new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(NaviusNumberField),
        new PropertyMetadata(1.0, null, CoerceStep));

    public static readonly DependencyProperty LargeStepProperty = DependencyProperty.Register(
        nameof(LargeStep), typeof(double), typeof(NaviusNumberField), new PropertyMetadata(10.0));

    public static readonly DependencyProperty SmallStepProperty = DependencyProperty.Register(
        nameof(SmallStep), typeof(double), typeof(NaviusNumberField), new PropertyMetadata(0.1));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusNumberField), new PropertyMetadata(false, OnBoundChanged));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusNumberField), new PropertyMetadata(false));

    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(NaviusNumberField),
        new PropertyMetadata(null, OnFormatChanged));

    private static readonly DependencyPropertyKey CanIncrementPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CanIncrement), typeof(bool), typeof(NaviusNumberField), new PropertyMetadata(true));

    public static readonly DependencyProperty CanIncrementProperty = CanIncrementPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CanDecrementPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CanDecrement), typeof(bool), typeof(NaviusNumberField), new PropertyMetadata(true));

    public static readonly DependencyProperty CanDecrementProperty = CanDecrementPropertyKey.DependencyProperty;

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<double?>), typeof(NaviusNumberField));

    private TextBox? _input;
    private RepeatButton? _increment;
    private RepeatButton? _decrement;
    private bool _suppressTextSync;

    static NaviusNumberField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNumberField), new FrameworkPropertyMetadata(typeof(NaviusNumberField)));
        FocusableProperty.OverrideMetadata(typeof(NaviusNumberField), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Controlled value; null renders empty.</summary>
    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double? Minimum
    {
        get => (double?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double? Maximum
    {
        get => (double?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public double LargeStep
    {
        get => (double)GetValue(LargeStepProperty);
        set => SetValue(LargeStepProperty, value);
    }

    public double SmallStep
    {
        get => (double)GetValue(SmallStepProperty);
        set => SetValue(SmallStepProperty, value);
    }

    /// <summary>Focusable but the value cannot be changed (contract's ReadOnly).</summary>
    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    /// <summary>.NET numeric format string, formatted/parsed with InvariantCulture per the contract.</summary>
    public string? Format
    {
        get => (string?)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public bool CanIncrement => (bool)GetValue(CanIncrementProperty);

    public bool CanDecrement => (bool)GetValue(CanDecrementProperty);

    public event RoutedPropertyChangedEventHandler<double?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>The formatted display text for the current Value; empty when null.</summary>
    public string Display => NaviusNumberFieldMath.Format(Value, Format);

    public override void OnApplyTemplate()
    {
        if (_input is not null)
        {
            _input.LostFocus -= OnInputLostFocus;
            _input.KeyDown -= OnInputKeyDown;
        }

        if (_increment is not null)
        {
            _increment.Click -= OnIncrementClick;
        }

        if (_decrement is not null)
        {
            _decrement.Click -= OnDecrementClick;
        }

        base.OnApplyTemplate();

        _input = GetTemplateChild(PartInput) as TextBox;
        _increment = GetTemplateChild(PartIncrement) as RepeatButton;
        _decrement = GetTemplateChild(PartDecrement) as RepeatButton;

        if (_input is not null)
        {
            _input.Text = Display;
            _input.LostFocus += OnInputLostFocus;
            _input.KeyDown += OnInputKeyDown;
        }

        // tabindex="-1": the increment/decrement buttons never receive tab focus, the input is
        // the sole tab-stop.
        if (_increment is not null)
        {
            _increment.Focusable = false;
            _increment.Click += OnIncrementClick;
        }

        if (_decrement is not null)
        {
            _decrement.Focusable = false;
            _decrement.Click += OnDecrementClick;
        }

        UpdateCanStep();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusNumberFieldAutomationPeer(this);

    /// <summary>Steps Value by delta (already the effective step); no-op when Disabled or ReadOnly.</summary>
    public void StepBy(double delta)
    {
        if (!IsEnabled || ReadOnly)
        {
            return;
        }

        Value = NaviusNumberFieldMath.Step(Value, delta, Minimum, Maximum);
    }

    /// <summary>Home/End: jump to the given bound; no-op when the bound is unset, Disabled, or ReadOnly.</summary>
    public void SetToBound(double? bound)
    {
        if (!IsEnabled || ReadOnly || bound is null)
        {
            return;
        }

        Value = bound.Value;
    }

    /// <summary>
    /// Commits the input's current text: parses and clamps on success, reverts to Display on
    /// failure ("snap back on invalid text", matching the contract's SetTextAsync).
    /// </summary>
    public void CommitText(string text)
    {
        if (_suppressTextSync)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Value = null;
            return;
        }

        if (!ReadOnly && IsEnabled && NaviusNumberFieldMath.TryParse(text, out var parsed))
        {
            Value = NaviusNumberFieldMath.Clamp(parsed, Minimum, Maximum);
        }

        SyncInputText();
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e) => CommitText(_input?.Text ?? string.Empty);

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled || ReadOnly)
        {
            return;
        }

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        var effectiveStep = shift ? LargeStep : alt ? SmallStep : Step;

        switch (e.Key)
        {
            case Key.Up:
                StepBy(effectiveStep);
                break;
            case Key.Down:
                StepBy(-effectiveStep);
                break;
            case Key.PageUp:
                StepBy(LargeStep);
                break;
            case Key.PageDown:
                StepBy(-LargeStep);
                break;
            case Key.Home:
                SetToBound(Minimum);
                break;
            case Key.End:
                SetToBound(Maximum);
                break;
            case Key.Enter:
                CommitText(_input?.Text ?? string.Empty);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnIncrementClick(object sender, RoutedEventArgs e) => StepBy(Step);

    private void OnDecrementClick(object sender, RoutedEventArgs e) => StepBy(-Step);

    private void SyncInputText()
    {
        if (_input is null)
        {
            return;
        }

        _suppressTextSync = true;
        try
        {
            _input.Text = Display;
        }
        finally
        {
            _suppressTextSync = false;
        }
    }

    private void UpdateCanStep()
    {
        SetValue(CanIncrementPropertyKey, NaviusNumberFieldMath.CanIncrement(Value, Maximum, !IsEnabled, ReadOnly));
        SetValue(CanDecrementPropertyKey, NaviusNumberFieldMath.CanDecrement(Value, Minimum, !IsEnabled, ReadOnly));
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var field = (NaviusNumberField)d;
        if (baseValue is not double value)
        {
            return baseValue;
        }

        return NaviusNumberFieldMath.Clamp(value, field.Minimum, field.Maximum);
    }

    private static object CoerceStep(DependencyObject d, object baseValue) =>
        NaviusNumberFieldMath.CoerceStep((double)baseValue);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (NaviusNumberField)d;
        field.SyncInputText();
        field.UpdateCanStep();
        field.RaiseEvent(new RoutedPropertyChangedEventArgs<double?>(
            (double?)e.OldValue, (double?)e.NewValue, ValueChangedEvent));
    }

    private static void OnBoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (NaviusNumberField)d;
        if (field.Value is { } value)
        {
            field.Value = NaviusNumberFieldMath.Clamp(value, field.Minimum, field.Maximum);
        }

        field.UpdateCanStep();
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusNumberField)d).SyncInputText();
}
