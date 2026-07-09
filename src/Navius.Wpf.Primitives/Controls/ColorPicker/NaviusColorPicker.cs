using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B (custom lookless control): the contract's eight parts (Root, Area, HueSlider,
/// AlphaSlider, Field, Swatches, SwatchItem, Swatch) are folded into one Control that owns the
/// authoritative HSVA model directly as dependency properties (Hue/Saturation/Brightness/Alpha)
/// and drives named template parts -- the same "one control owns its parts centrally" shape
/// NaviusRating and NaviusAvatar use elsewhere in this port (see docs/parity/color-picker.md
/// "WPF strategy": "a lookless Control ... with a dependency-property HSVA model and named
/// template parts").
///
/// Pointer interaction uses WPF's native Thumb/DragDelta (mouse-capture) machinery instead of the
/// contract's JS PointerTracker2D. The Area's saturation/brightness axes are exposed as two
/// separate DPs (mirroring the contract's two role="slider" thumbs: a visible saturation thumb
/// plus a screen-reader-only brightness thumb) but this port renders one visible 2D thumb with
/// one AutomationPeer rather than two separate hidden/visible peers -- see the doc's own open
/// question on this point, resolved here in favor of a single 2D thumb for a simpler WPF surface.
///
/// The Swatches list reuses native ListBox keyboard navigation (roving selection, Home/End,
/// arrows) rather than reimplementing the contract's bespoke roving-tabindex coordinator,
/// resolving another of the doc's open questions.
///
/// Deviations: the contract's hidden-bubble-input form participation itself has no WPF analog and
/// is dropped; the surviving "Name" parameter is exposed as FieldName (not Name) since
/// FrameworkElement.Name is already a DependencyProperty with XAML-special x:Name/FindName
/// meaning that a same-named DP here would shadow. ReadOnly and Disabled are both honored but
/// Disabled maps to native IsEnabled rather than a separate property.
/// </summary>
[TemplatePart(Name = PartArea, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartAreaThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PartHueTrack, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHueThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PartAlphaTrack, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartAlphaThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PartHexField, Type = typeof(TextBox))]
[TemplatePart(Name = PartSwatches, Type = typeof(ListBox))]
public class NaviusColorPicker : Control
{
    private const string PartArea = "PART_Area";
    private const string PartAreaThumb = "PART_AreaThumb";
    private const string PartHueTrack = "PART_HueTrack";
    private const string PartHueThumb = "PART_HueThumb";
    private const string PartAlphaTrack = "PART_AlphaTrack";
    private const string PartAlphaThumb = "PART_AlphaThumb";
    private const string PartHexField = "PART_HexField";
    private const string PartSwatches = "PART_Swatches";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusColorPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
        nameof(DefaultValue), typeof(string), typeof(NaviusColorPicker), new PropertyMetadata(null));

    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(NaviusColorPicker), new PropertyMetadata("hex", OnModelDependentChanged));

    public static readonly DependencyProperty AlphaEnabledProperty = DependencyProperty.Register(
        nameof(AlphaEnabled), typeof(bool), typeof(NaviusColorPicker), new PropertyMetadata(false));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusColorPicker), new PropertyMetadata(false));

    // Named FieldName, not Name: FrameworkElement.Name is already a DependencyProperty with
    // XAML-special meaning (x:Name/FindName resolution) that a same-named DP on this subclass
    // would shadow and break, so this parity-only property uses a distinct name.
    public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register(
        nameof(FieldName), typeof(string), typeof(NaviusColorPicker), new PropertyMetadata(null));

    public static readonly DependencyProperty ColorsProperty = DependencyProperty.Register(
        nameof(Colors), typeof(IReadOnlyList<string>), typeof(NaviusColorPicker), new PropertyMetadata(null));

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(NaviusColorPicker),
        new PropertyMetadata(0.0, OnModelDependentChanged, CoerceHue));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation), typeof(double), typeof(NaviusColorPicker),
        new PropertyMetadata(0.0, OnModelDependentChanged, CoerceUnit));

    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness), typeof(double), typeof(NaviusColorPicker),
        new PropertyMetadata(1.0, OnModelDependentChanged, CoerceUnit));

    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        nameof(Alpha), typeof(double), typeof(NaviusColorPicker),
        new PropertyMetadata(1.0, OnModelDependentChanged, CoerceUnit));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<string?>), typeof(NaviusColorPicker));

    private FrameworkElement? _area;
    private Thumb? _areaThumb;
    private FrameworkElement? _hueTrack;
    private Thumb? _hueThumb;
    private FrameworkElement? _alphaTrack;
    private Thumb? _alphaThumb;
    private TextBox? _hexField;
    private ListBox? _swatches;
    private bool _isApplyingValueToModel;
    private bool _isSyncingValueFromModel;

    static NaviusColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusColorPicker), new FrameworkPropertyMetadata(typeof(NaviusColorPicker)));
    }

    public NaviusColorPicker()
    {
        Loaded += OnLoaded;
    }

    /// <summary>Controlled color string; pairs with ValueChanged for @bind-Value parity.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Uncontrolled initial color string, applied once if Value is unset when the control loads.</summary>
    public string? DefaultValue
    {
        get => (string?)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <summary>Output format for Value: hex|rgb|rgba|hsl|hsla.</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public bool AlphaEnabled
    {
        get => (bool)GetValue(AlphaEnabledProperty);
        set => SetValue(AlphaEnabledProperty, value);
    }

    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    /// <summary>API-parity only (mirrors the contract's Name): the hidden bubble-input form field name has no WPF equivalent.</summary>
    public string? FieldName
    {
        get => (string?)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public IReadOnlyList<string>? Colors
    {
        get => (IReadOnlyList<string>?)GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    /// <summary>Degrees, [0, 360).</summary>
    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Fraction, [0, 1].</summary>
    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>HSV's "V" (brightness). Fraction, [0, 1].</summary>
    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    /// <summary>Fraction, [0, 1]. Only reflected in Value when AlphaEnabled.</summary>
    public double Alpha
    {
        get => (double)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    /// <summary>The current color as hex, regardless of Format -- mirrors ColorPickerContext.HexValue.</summary>
    public string HexValue => ColorMath.Format(Hue, Saturation, Brightness, Alpha, "hex");

    public event RoutedPropertyChangedEventHandler<string?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        DetachParts();

        base.OnApplyTemplate();

        _area = GetTemplateChild(PartArea) as FrameworkElement;
        _areaThumb = GetTemplateChild(PartAreaThumb) as Thumb;
        _hueTrack = GetTemplateChild(PartHueTrack) as FrameworkElement;
        _hueThumb = GetTemplateChild(PartHueThumb) as Thumb;
        _alphaTrack = GetTemplateChild(PartAlphaTrack) as FrameworkElement;
        _alphaThumb = GetTemplateChild(PartAlphaThumb) as Thumb;
        _hexField = GetTemplateChild(PartHexField) as TextBox;
        _swatches = GetTemplateChild(PartSwatches) as ListBox;

        if (_areaThumb is not null)
        {
            _areaThumb.DragDelta += OnAreaThumbDragDelta;
            _areaThumb.PreviewKeyDown += OnAreaThumbKeyDown;
        }

        if (_area is not null)
        {
            _area.SizeChanged += OnTrackSizeChanged;
        }

        if (_hueThumb is not null)
        {
            _hueThumb.DragDelta += OnHueThumbDragDelta;
            _hueThumb.PreviewKeyDown += OnHueThumbKeyDown;
        }

        if (_hueTrack is not null)
        {
            _hueTrack.SizeChanged += OnTrackSizeChanged;
        }

        if (_alphaThumb is not null)
        {
            _alphaThumb.DragDelta += OnAlphaThumbDragDelta;
            _alphaThumb.PreviewKeyDown += OnAlphaThumbKeyDown;
        }

        if (_alphaTrack is not null)
        {
            _alphaTrack.SizeChanged += OnTrackSizeChanged;
        }

        if (_hexField is not null)
        {
            _hexField.LostFocus += OnHexFieldLostFocus;
            _hexField.KeyDown += OnHexFieldKeyDown;
        }

        if (_swatches is not null)
        {
            _swatches.SelectionChanged += OnSwatchesSelectionChanged;
        }

        RefreshHexFieldText();
        UpdateThumbPositions();
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => UpdateThumbPositions();

    /// <summary>
    /// Positions the Area/Hue/Alpha thumbs from the HSVA model, the code-behind equivalent of the
    /// contract's AreaThumbLeft/AreaThumbTop/HueThumbLeft/AlphaThumbLeft percentage projections
    /// (docs/parity/color-picker.md "State + data attributes"). WPF's Canvas has no percentage
    /// positioning, so this recomputes pixel offsets from each track's ActualWidth/ActualHeight
    /// whenever the model or the track's size changes.
    /// </summary>
    private void UpdateThumbPositions()
    {
        const double thumbRadius = 7;

        if (_areaThumb is not null && _area is not null && _area.ActualWidth > 0 && _area.ActualHeight > 0)
        {
            Canvas.SetLeft(_areaThumb, Saturation * _area.ActualWidth - thumbRadius);
            Canvas.SetTop(_areaThumb, (1 - Brightness) * _area.ActualHeight - thumbRadius);
        }

        if (_hueThumb is not null && _hueTrack is not null && _hueTrack.ActualWidth > 0)
        {
            Canvas.SetLeft(_hueThumb, Hue / 360 * _hueTrack.ActualWidth - thumbRadius);
        }

        if (_alphaThumb is not null && _alphaTrack is not null && _alphaTrack.ActualWidth > 0)
        {
            Canvas.SetLeft(_alphaThumb, Alpha * _alphaTrack.ActualWidth - thumbRadius);
        }
    }

    private void DetachParts()
    {
        if (_areaThumb is not null)
        {
            _areaThumb.DragDelta -= OnAreaThumbDragDelta;
            _areaThumb.PreviewKeyDown -= OnAreaThumbKeyDown;
        }

        if (_hueThumb is not null)
        {
            _hueThumb.DragDelta -= OnHueThumbDragDelta;
            _hueThumb.PreviewKeyDown -= OnHueThumbKeyDown;
        }

        if (_alphaThumb is not null)
        {
            _alphaThumb.DragDelta -= OnAlphaThumbDragDelta;
            _alphaThumb.PreviewKeyDown -= OnAlphaThumbKeyDown;
        }

        if (_hexField is not null)
        {
            _hexField.LostFocus -= OnHexFieldLostFocus;
            _hexField.KeyDown -= OnHexFieldKeyDown;
        }

        if (_swatches is not null)
        {
            _swatches.SelectionChanged -= OnSwatchesSelectionChanged;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusColorPickerAutomationPeer(this);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Value is null && DefaultValue is not null)
        {
            Value = DefaultValue;
        }
    }

    private static object CoerceHue(DependencyObject d, object baseValue) => ((((double)baseValue) % 360) + 360) % 360;

    private static object CoerceUnit(DependencyObject d, object baseValue) => Math.Clamp((double)baseValue, 0, 1);

    private static void OnModelDependentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (NaviusColorPicker)d;
        picker.SyncValueFromModel();
        picker.UpdateThumbPositions();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (NaviusColorPicker)d;

        // Skip re-parsing the string back into the model when this Value change was itself
        // produced by SyncValueFromModel (the model is already authoritative in that direction);
        // ValueChanged must still fire either way, since it reflects any change to Value, whether
        // the model or an external caller drove it.
        if (!picker._isSyncingValueFromModel)
        {
            picker.ApplyValueString((string?)e.NewValue);
        }

        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<string?>((string?)e.OldValue, (string?)e.NewValue, ValueChangedEvent));
    }

    private void ApplyValueString(string? value)
    {
        if (!ColorMath.TryParse(value, out var h, out var s, out var v, out var a))
        {
            return;
        }

        _isApplyingValueToModel = true;
        try
        {
            SetCurrentValue(HueProperty, h);
            SetCurrentValue(SaturationProperty, s);
            SetCurrentValue(BrightnessProperty, v);
            if (AlphaEnabled)
            {
                SetCurrentValue(AlphaProperty, a);
            }
        }
        finally
        {
            _isApplyingValueToModel = false;
        }

        RefreshHexFieldText();
    }

    private void SyncValueFromModel()
    {
        // Skip reformatting Value while we're still mid-parse in ApplyValueString: Value already
        // holds the caller-supplied string and will be reformatted on the next model-driven change.
        if (_isApplyingValueToModel)
        {
            return;
        }

        var projected = ColorMath.Format(Hue, Saturation, Brightness, Alpha, Format);

        _isSyncingValueFromModel = true;
        try
        {
            SetCurrentValue(ValueProperty, projected);
        }
        finally
        {
            _isSyncingValueFromModel = false;
        }

        RefreshHexFieldText();
    }

    private void OnAreaThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsInteractive() || _area is null || _area.ActualWidth <= 0 || _area.ActualHeight <= 0)
        {
            return;
        }

        Saturation += e.HorizontalChange / _area.ActualWidth;
        Brightness -= e.VerticalChange / _area.ActualHeight;
    }

    private void OnHueThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsInteractive() || _hueTrack is null || _hueTrack.ActualWidth <= 0)
        {
            return;
        }

        Hue += e.HorizontalChange / _hueTrack.ActualWidth * 360;
    }

    private void OnAlphaThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!IsInteractive() || _alphaTrack is null || _alphaTrack.ActualWidth <= 0)
        {
            return;
        }

        Alpha += e.HorizontalChange / _alphaTrack.ActualWidth;
    }

    private void OnAreaThumbKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsInteractive())
        {
            return;
        }

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        switch (e.Key)
        {
            case Key.Right:
                Saturation = ColorPickerSteps.StepSaturation(Saturation, increase: true, shift);
                break;
            case Key.Left:
                Saturation = ColorPickerSteps.StepSaturation(Saturation, increase: false, shift);
                break;
            case Key.Up:
                Brightness = ColorPickerSteps.StepBrightness(Brightness, increase: true, shift);
                break;
            case Key.Down:
                Brightness = ColorPickerSteps.StepBrightness(Brightness, increase: false, shift);
                break;
            case Key.PageUp:
                Brightness = ColorPickerSteps.StepBrightnessLarge(Brightness, increase: true);
                break;
            case Key.PageDown:
                Brightness = ColorPickerSteps.StepBrightnessLarge(Brightness, increase: false);
                break;
            case Key.Home:
                Saturation = 0;
                break;
            case Key.End:
                Saturation = 1;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnHueThumbKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsInteractive())
        {
            return;
        }

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        switch (e.Key)
        {
            case Key.Right:
            case Key.Up:
                Hue = ColorPickerSteps.StepHue(Hue, increase: true, shift);
                break;
            case Key.Left:
            case Key.Down:
                Hue = ColorPickerSteps.StepHue(Hue, increase: false, shift);
                break;
            case Key.PageUp:
                Hue = ColorPickerSteps.StepHueLarge(Hue, increase: true);
                break;
            case Key.PageDown:
                Hue = ColorPickerSteps.StepHueLarge(Hue, increase: false);
                break;
            case Key.Home:
                Hue = 0;
                break;
            case Key.End:
                Hue = 360;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnAlphaThumbKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsInteractive())
        {
            return;
        }

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        switch (e.Key)
        {
            case Key.Right:
            case Key.Up:
                Alpha = ColorPickerSteps.StepAlpha(Alpha, increase: true, shift);
                break;
            case Key.Left:
            case Key.Down:
                Alpha = ColorPickerSteps.StepAlpha(Alpha, increase: false, shift);
                break;
            case Key.PageUp:
                Alpha = ColorPickerSteps.StepAlphaLarge(Alpha, increase: true);
                break;
            case Key.PageDown:
                Alpha = ColorPickerSteps.StepAlphaLarge(Alpha, increase: false);
                break;
            case Key.Home:
                Alpha = 0;
                break;
            case Key.End:
                Alpha = 1;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnHexFieldLostFocus(object sender, RoutedEventArgs e) => CommitHexField();

    private void OnHexFieldKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitHexField();
            e.Handled = true;
        }
    }

    private void CommitHexField()
    {
        if (_hexField is null)
        {
            return;
        }

        if (!IsInteractive() || !ColorMath.TryParse(_hexField.Text, out var h, out var s, out var v, out var a))
        {
            RefreshHexFieldText();
            return;
        }

        Hue = h;
        Saturation = s;
        Brightness = v;
        if (AlphaEnabled)
        {
            Alpha = a;
        }

        RefreshHexFieldText();
    }

    private void RefreshHexFieldText()
    {
        if (_hexField is not null)
        {
            _hexField.Text = HexValue;
        }
    }

    private void OnSwatchesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_swatches?.SelectedItem is not string hex)
        {
            return;
        }

        if (!IsInteractive() || !ColorMath.TryParse(hex, out var h, out var s, out var v, out var a))
        {
            return;
        }

        Hue = h;
        Saturation = s;
        Brightness = v;
        if (AlphaEnabled)
        {
            Alpha = a;
        }
    }

    private bool IsInteractive() => IsEnabled && !ReadOnly;
}
