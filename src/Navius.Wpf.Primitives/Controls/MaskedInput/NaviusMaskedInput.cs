using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.MaskedInput;

/// <summary>
/// Tier B, TextBox-derived: caret-stable masked text input. There is no native WPF masking
/// concept, but the control needs no popup/composite template, so it derives directly from
/// <see cref="TextBox"/> (like <c>NaviusInput</c>) rather than introducing a lookless multi-part
/// template. <c>Value</c> IS this control's <c>Text</c> (the masked display string); the only
/// state this class adds beyond a plain TextBox is <see cref="UnmaskedValue"/> and the mask
/// pipeline itself.
///
/// The web source wires exactly one DOM event (<c>@oninput</c>) and funnels typing, Backspace/
/// Delete, and paste through the same <see cref="MaskEngine.Format"/> call; this port mirrors that
/// by overriding the single <see cref="OnTextChanged"/> hook rather than adding key handlers, so
/// the same uniform pipeline runs regardless of how the text changed. WPF's synchronous
/// <c>CaretIndex</c>/<c>Select</c> let the reformat + caret reposition happen inside that one
/// handler; the web version's async <c>OnAfterRenderAsync</c> pending-caret-reapply dance has no
/// WPF equivalent needed (see docs/parity/masked-input.md WPF implementation notes).
/// </summary>
public class NaviusMaskedInput : TextBox
{
    public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(
        nameof(Mask), typeof(string), typeof(NaviusMaskedInput),
        new PropertyMetadata(string.Empty, OnMaskChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusMaskedInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
        nameof(DefaultValue), typeof(string), typeof(NaviusMaskedInput), new PropertyMetadata(null));

    public static readonly DependencyProperty PlaceholderCharProperty = DependencyProperty.Register(
        nameof(PlaceholderChar), typeof(char?), typeof(NaviusMaskedInput),
        new PropertyMetadata(null, OnPipelineInputChanged));

    public static readonly DependencyProperty LazyProperty = DependencyProperty.Register(
        nameof(Lazy), typeof(bool), typeof(NaviusMaskedInput),
        new PropertyMetadata(true, OnPipelineInputChanged));

    /// <summary>Accepted for parity with the web contract; not yet wired to insertion (matches the source's own stub).</summary>
    public static readonly DependencyProperty OverwriteProperty = DependencyProperty.Register(
        nameof(Overwrite), typeof(bool), typeof(NaviusMaskedInput), new PropertyMetadata(false));

    public static readonly DependencyProperty PreprocessorsProperty = DependencyProperty.Register(
        nameof(Preprocessors), typeof(IReadOnlyList<Func<ElementState, ElementState>>), typeof(NaviusMaskedInput),
        new PropertyMetadata(null));

    public static readonly DependencyProperty PostprocessorsProperty = DependencyProperty.Register(
        nameof(Postprocessors), typeof(IReadOnlyList<Func<ElementState, ElementState>>), typeof(NaviusMaskedInput),
        new PropertyMetadata(null));

    private static readonly DependencyPropertyKey UnmaskedValuePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(UnmaskedValue), typeof(string), typeof(NaviusMaskedInput), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty UnmaskedValueProperty = UnmaskedValuePropertyKey.DependencyProperty;

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid), typeof(bool), typeof(NaviusMaskedInput), new PropertyMetadata(false));

    private IReadOnlyList<MaskToken> _tokens = Array.Empty<MaskToken>();
    private bool _suppressTextChanged;
    private bool _syncingValue;
    private bool _defaultValueApplied;

    static NaviusMaskedInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMaskedInput), new FrameworkPropertyMetadata(typeof(NaviusMaskedInput)));
    }

    public NaviusMaskedInput()
    {
        _tokens = MaskEngine.Parse(Mask);
        Loaded += OnLoaded;
    }

    /// <summary>Mask pattern: <c>0</c>=digit, <c>A</c>=letter, <c>*</c>=alnum, any other char is a fixed literal.</summary>
    public string Mask
    {
        get => (string)GetValue(MaskProperty);
        set => SetValue(MaskProperty, value);
    }

    /// <summary>Controlled masked value; IS this control's <c>Text</c>. Use a normal Binding.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Uncontrolled initial value, applied once on <c>Loaded</c> when no <see cref="Value"/> is set.</summary>
    public string? DefaultValue
    {
        get => (string?)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <summary>Char shown for empty slots (e.g. <c>_</c>); null renders nothing (lazy skeleton).</summary>
    public char? PlaceholderChar
    {
        get => (char?)GetValue(PlaceholderCharProperty);
        set => SetValue(PlaceholderCharProperty, value);
    }

    /// <summary>true: trailing fixed tokens appear only once reached; false: eager (always emitted).</summary>
    public bool Lazy
    {
        get => (bool)GetValue(LazyProperty);
        set => SetValue(LazyProperty, value);
    }

    /// <summary>Reserved for type-over (replace) mode; accepted for parity, not yet wired to insertion.</summary>
    public bool Overwrite
    {
        get => (bool)GetValue(OverwriteProperty);
        set => SetValue(OverwriteProperty, value);
    }

    /// <summary>Ordered pure transforms run on the proposed state before masking.</summary>
    public IReadOnlyList<Func<ElementState, ElementState>>? Preprocessors
    {
        get => (IReadOnlyList<Func<ElementState, ElementState>>?)GetValue(PreprocessorsProperty);
        set => SetValue(PreprocessorsProperty, value);
    }

    /// <summary>Ordered pure transforms run on the masked state after masking (postfix, clamp, ...).</summary>
    public IReadOnlyList<Func<ElementState, ElementState>>? Postprocessors
    {
        get => (IReadOnlyList<Func<ElementState, ElementState>>?)GetValue(PostprocessorsProperty);
        set => SetValue(PostprocessorsProperty, value);
    }

    /// <summary>The raw editable characters (placeholder-filled, no literals) behind the current masked <see cref="Value"/>.</summary>
    public string UnmaskedValue => (string)GetValue(UnmaskedValueProperty);

    /// <summary>Presentational only (skin hook, e.g. destructive border); no validation logic lives here.</summary>
    public bool Invalid
    {
        get => (bool)GetValue(InvalidProperty);
        set => SetValue(InvalidProperty, value);
    }

    /// <summary>Emits the raw editable characters (placeholder-filled, no literals) after every reformat.</summary>
    public event EventHandler<string>? UnmaskedValueChanged;

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (_suppressTextChanged)
        {
            return;
        }

        RunPipeline();
    }

    private void RunPipeline()
    {
        // The proposed caret is the selection END, collapsed: after a keystroke the selection is
        // already collapsed there, and a programmatic SelectedText insertion leaves the inserted
        // text selected with End right after it. The web bridge assumed single-cursor (collapsed)
        // selection semantics (see the parity doc's open question); this port resolves it the same
        // way, so the pipeline never preserves a range across a reformat.
        var caret = SelectionStart + SelectionLength;
        var proposed = new ElementState(Text, caret, caret);
        foreach (var pre in Preprocessors ?? Array.Empty<Func<ElementState, ElementState>>())
        {
            proposed = pre(proposed);
        }

        var (masked, caretStart, caretEnd, unmasked) = MaskEngine.Format(
            _tokens, proposed.Value, proposed.SelectionStart, proposed.SelectionEnd, Lazy, PlaceholderChar);

        var result = new ElementState(masked, caretStart, caretEnd);
        foreach (var post in Postprocessors ?? Array.Empty<Func<ElementState, ElementState>>())
        {
            result = post(result);
        }

        _suppressTextChanged = true;
        try
        {
            Text = result.Value;
            var start = Math.Clamp(result.SelectionStart, 0, Text.Length);
            var end = Math.Clamp(result.SelectionEnd, start, Text.Length);
            Select(start, end - start);
        }
        finally
        {
            _suppressTextChanged = false;
        }

        SetValue(UnmaskedValuePropertyKey, unmasked);
        UnmaskedValueChanged?.Invoke(this, unmasked);

        _syncingValue = true;
        try
        {
            Value = result.Value;
        }
        finally
        {
            _syncingValue = false;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_defaultValueApplied)
        {
            return;
        }

        _defaultValueApplied = true;

        // DefaultValue only seeds the control on first load and only when no controlled Value was
        // supplied, mirroring the web source's "DefaultValue only used on first init" precedence.
        var initial = Value ?? DefaultValue;
        if (!string.IsNullOrEmpty(initial))
        {
            Text = initial; // flows through OnTextChanged -> RunPipeline, normalizing through the mask.
        }
    }

    private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusMaskedInput)d;
        input._tokens = MaskEngine.Parse((string)e.NewValue ?? string.Empty);
        input.RunPipeline();
    }

    private static void OnPipelineInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMaskedInput)d).RunPipeline();

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusMaskedInput)d;
        if (input._syncingValue)
        {
            return;
        }

        // External (binding-driven) value change: push it through Text so it re-masks consistently.
        input.Text = (string?)e.NewValue ?? string.Empty;
    }
}
