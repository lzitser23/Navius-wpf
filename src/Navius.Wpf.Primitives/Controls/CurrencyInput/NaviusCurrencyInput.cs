using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.CurrencyInput;

/// <summary>
/// Tier A, TextBox-derived: a caret-stable currency text field whose internal truth is a
/// <c>decimal?</c> (never a string). The web version's JS <c>MaskedSelection</c> bridge (atomic
/// read of value + caret, write-back in <c>OnAfterRenderAsync</c>) collapses into a single
/// synchronous <see cref="OnTextChanged"/> handler here: parse digits via
/// <see cref="CurrencyEngine"/>, reformat from <see cref="NumberFormatInfo"/> parts, and re-land
/// the caret by digit count (<see cref="CurrencyEngine.CountDigitsBefore"/> /
/// <see cref="CurrencyEngine.CaretForDigits"/>) so typing mid-string never jumps the cursor after
/// regrouping. On blur the value is clamped to <see cref="Minimum"/>/<see cref="Maximum"/> and the
/// fraction is padded to <see cref="MinFractionDigits"/>.
///
/// No custom AutomationPeer: TextBoxAutomationPeer already provides ControlType.Edit +
/// ValuePattern, matching the web source's bare native input (no custom ARIA).
/// </summary>
public class NaviusCurrencyInput : TextBox
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(decimal?), typeof(NaviusCurrencyInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
        nameof(DefaultValue), typeof(decimal?), typeof(NaviusCurrencyInput), new PropertyMetadata(null));

    public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
        nameof(Culture), typeof(CultureInfo), typeof(NaviusCurrencyInput),
        new PropertyMetadata(null, OnFormatSettingsChanged));

    public static readonly DependencyProperty CurrencyProperty = DependencyProperty.Register(
        nameof(Currency), typeof(string), typeof(NaviusCurrencyInput),
        new PropertyMetadata(null, OnFormatSettingsChanged));

    public static readonly DependencyProperty MinFractionDigitsProperty = DependencyProperty.Register(
        nameof(MinFractionDigits), typeof(int?), typeof(NaviusCurrencyInput),
        new PropertyMetadata(null, OnFormatSettingsChanged));

    public static readonly DependencyProperty MaxFractionDigitsProperty = DependencyProperty.Register(
        nameof(MaxFractionDigits), typeof(int?), typeof(NaviusCurrencyInput),
        new PropertyMetadata(null, OnFormatSettingsChanged));

    public static readonly DependencyProperty AllowNegativeProperty = DependencyProperty.Register(
        nameof(AllowNegative), typeof(bool), typeof(NaviusCurrencyInput), new PropertyMetadata(false));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(decimal?), typeof(NaviusCurrencyInput), new PropertyMetadata(null));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(decimal?), typeof(NaviusCurrencyInput), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowSymbolProperty = DependencyProperty.Register(
        nameof(ShowSymbol), typeof(bool), typeof(NaviusCurrencyInput),
        new PropertyMetadata(true, OnFormatSettingsChanged));

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid), typeof(bool), typeof(NaviusCurrencyInput), new PropertyMetadata(false));

    private static readonly DependencyPropertyKey IsNegativePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsNegative), typeof(bool), typeof(NaviusCurrencyInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsNegativeProperty = IsNegativePropertyKey.DependencyProperty;

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<decimal?>), typeof(NaviusCurrencyInput));

    private bool _suppressTextChanged;
    private bool _syncingValue;
    private bool _defaultValueApplied;

    static NaviusCurrencyInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCurrencyInput), new FrameworkPropertyMetadata(typeof(NaviusCurrencyInput)));
    }

    public NaviusCurrencyInput()
    {
        Loaded += OnLoaded;
    }

    /// <summary>Controlled value (the decimal truth; never a string).</summary>
    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Uncontrolled initial value, applied once on <c>Loaded</c> when no <see cref="Value"/> is set.</summary>
    public decimal? DefaultValue
    {
        get => (decimal?)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <summary>Drives symbol, grouping and decimals. Defaults to <see cref="CultureInfo.CurrentCulture"/> when unset.</summary>
    public CultureInfo? Culture
    {
        get => (CultureInfo?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    /// <summary>ISO 4217 code; overrides the culture's default currency symbol via <see cref="CurrencyEngine.SymbolFor"/>.</summary>
    public string? Currency
    {
        get => (string?)GetValue(CurrencyProperty);
        set => SetValue(CurrencyProperty, value);
    }

    /// <summary>Defaults to the culture's currency decimal digits; used for blur padding.</summary>
    public int? MinFractionDigits
    {
        get => (int?)GetValue(MinFractionDigitsProperty);
        set => SetValue(MinFractionDigitsProperty, value);
    }

    /// <summary>Defaults to the culture's currency decimal digits; caps digits typeable in the fraction.</summary>
    public int? MaxFractionDigits
    {
        get => (int?)GetValue(MaxFractionDigitsProperty);
        set => SetValue(MaxFractionDigitsProperty, value);
    }

    /// <summary>Enables recognizing/typing the culture's negative sign.</summary>
    public bool AllowNegative
    {
        get => (bool)GetValue(AllowNegativeProperty);
        set => SetValue(AllowNegativeProperty, value);
    }

    /// <summary>Clamped on blur (the contract's <c>Min</c>).</summary>
    public decimal? Minimum
    {
        get => (decimal?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Clamped on blur (the contract's <c>Max</c>).</summary>
    public decimal? Maximum
    {
        get => (decimal?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Render the currency symbol in the displayed value.</summary>
    public bool ShowSymbol
    {
        get => (bool)GetValue(ShowSymbolProperty);
        set => SetValue(ShowSymbolProperty, value);
    }

    /// <summary>Presentational only (skin hook); no validation logic lives here (the contract's <c>data-invalid</c>).</summary>
    public bool Invalid
    {
        get => (bool)GetValue(InvalidProperty);
        set => SetValue(InvalidProperty, value);
    }

    /// <summary>True when the current value is negative (the contract's <c>data-negative</c> skin hook).</summary>
    public bool IsNegative => (bool)GetValue(IsNegativeProperty);

    public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>The resolved NumberFormatInfo: the culture's currency format, symbol overridden by <see cref="Currency"/>.</summary>
    public NumberFormatInfo ResolveFormat()
    {
        var nfi = (NumberFormatInfo)(Culture ?? CultureInfo.CurrentCulture).NumberFormat.Clone();
        if (!string.IsNullOrEmpty(Currency))
        {
            nfi.CurrencySymbol = CurrencyEngine.SymbolFor(Currency);
        }

        return nfi;
    }

    private int ResolvedMinFrac(NumberFormatInfo nfi) => MinFractionDigits ?? nfi.CurrencyDecimalDigits;

    private int ResolvedMaxFrac(NumberFormatInfo nfi) =>
        Math.Max(MaxFractionDigits ?? nfi.CurrencyDecimalDigits, ResolvedMinFrac(nfi));

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (_suppressTextChanged)
        {
            return;
        }

        RunEditingPipeline();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        CommitValue();
    }

    /// <summary>
    /// The live (per-edit) reformat: parse the proposed text, derive the decimal truth, regroup the
    /// display, and re-land the caret after the same digit count it sat behind before the reformat.
    /// </summary>
    private void RunEditingPipeline()
    {
        var nfi = ResolveFormat();
        var maxFrac = ResolvedMaxFrac(nfi);

        var proposed = Text;
        // The caret anchor is the selection END: after a keystroke the selection is collapsed
        // there, and a programmatic SelectedText insertion leaves the inserted text selected with
        // End right after it. This mirrors the web bridge's single-cursor (collapsed) assumption.
        var caret = SelectionStart + SelectionLength;
        var digitsBefore = CurrencyEngine.CountDigitsBefore(proposed, caret);

        var (negative, intDigits, fracDigits, hadSep) = CurrencyEngine.Parse(proposed, nfi, AllowNegative, maxFrac);
        var value = CurrencyEngine.ToDecimal(negative, intDigits, fracDigits);
        var formatted = CurrencyEngine.FormatEditing(negative, intDigits, fracDigits, hadSep, nfi, ShowSymbol);

        _suppressTextChanged = true;
        try
        {
            Text = formatted;
            CaretIndex = Math.Clamp(CurrencyEngine.CaretForDigits(formatted, digitsBefore), 0, formatted.Length);
        }
        finally
        {
            _suppressTextChanged = false;
        }

        SetValueInternal(value);
    }

    /// <summary>Blur commit: clamp to [Minimum, Maximum], pad the fraction to MinFractionDigits, regroup.</summary>
    public void CommitValue()
    {
        var nfi = ResolveFormat();

        var value = Value;
        if (value is decimal v)
        {
            if (Minimum is decimal min && v < min)
            {
                v = min;
            }

            if (Maximum is decimal max && v > max)
            {
                v = max;
            }

            if (v != value)
            {
                SetValueInternal(v);
            }

            SetDisplay(CurrencyEngine.FormatCommitted(v, nfi, ResolvedMinFrac(nfi), ResolvedMaxFrac(nfi), ShowSymbol));
        }
        else
        {
            SetDisplay(string.Empty);
        }
    }

    private void SetDisplay(string text)
    {
        _suppressTextChanged = true;
        try
        {
            Text = text;
        }
        finally
        {
            _suppressTextChanged = false;
        }
    }

    private void SetValueInternal(decimal? value)
    {
        var old = Value;
        if (old == value)
        {
            SetValue(IsNegativePropertyKey, value is < 0);
            return;
        }

        _syncingValue = true;
        try
        {
            Value = value;
        }
        finally
        {
            _syncingValue = false;
        }

        SetValue(IsNegativePropertyKey, value is < 0);
        RaiseEvent(new RoutedPropertyChangedEventArgs<decimal?>(old, value, ValueChangedEvent));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_defaultValueApplied)
        {
            return;
        }

        _defaultValueApplied = true;

        if (Value is null && DefaultValue is decimal initial)
        {
            SetValueInternal(initial);
        }

        CommitValue();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusCurrencyInput)d;
        input.SetValue(IsNegativePropertyKey, (decimal?)e.NewValue is < 0);
        if (input._syncingValue)
        {
            return;
        }

        // External (binding-driven) value change: render it committed (not mid-edit).
        var nfi = input.ResolveFormat();
        input.SetDisplay((decimal?)e.NewValue is decimal v
            ? CurrencyEngine.FormatCommitted(v, nfi, input.ResolvedMinFrac(nfi), input.ResolvedMaxFrac(nfi), input.ShowSymbol)
            : string.Empty);
        input.RaiseEvent(new RoutedPropertyChangedEventArgs<decimal?>(
            (decimal?)e.OldValue, (decimal?)e.NewValue, ValueChangedEvent));
    }

    private static void OnFormatSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusCurrencyInput)d;
        if (input.IsKeyboardFocusWithin)
        {
            input.RunEditingPipeline();
        }
        else
        {
            input.CommitValue();
        }
    }
}
