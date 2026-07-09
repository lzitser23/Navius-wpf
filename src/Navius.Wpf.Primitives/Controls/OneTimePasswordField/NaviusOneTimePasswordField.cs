using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.OneTimePasswordField;

/// <summary>
/// Tier B (custom lookless control). No native WPF OTP control exists. This is a lookless
/// Control that owns an internal collection of NaviusOneTimePasswordFieldInput cells built
/// directly in code (rather than an ItemsControl + generator, matching the
/// RadioGroup/CheckboxGroup precedent already established in this codebase) and drives them
/// from a pure OneTimePasswordBuffer, per the WPF strategy note. All advance/backspace/
/// delete/arrow/paste logic is re-implemented manually via PreviewKeyDown/PreviewTextInput/
/// DataObject.Pasting since WPF has no equivalent to HTML autocomplete="one-time-code" or
/// oninput-length-based paste detection.
///
/// Contract delta (resolves one-time-password-field.md's open question about cell 0's
/// maxlength=Length quirk): WPF exposes paste via the separate DataObject.Pasting event,
/// entirely decoupled from typed-character PreviewTextInput, so every cell uses a uniform
/// MaxLength=1 -- the web's cell-0-only-length hack (needed only to detect paste through a
/// single oninput length heuristic) has no reason to exist in the port.
///
/// Contract delta (resolves the AutoSubmit open question): AutoSubmit's web behavior submits
/// the closest HTML form via JS interop, which has no WPF equivalent. Enter always raises the
/// bubbling SubmitRequested event (WPF's own analog of "closest enclosing form", since a
/// containing NaviusForm or any ancestor can subscribe to a bubbling routed event), and
/// AutoSubmit gates a separate AutoSubmitted event fired once every cell fills, rather than
/// invoking anything directly.
/// </summary>
public class NaviusOneTimePasswordField : Control
{
    public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
        nameof(Length), typeof(int), typeof(NaviusOneTimePasswordField), new PropertyMetadata(6, OnLengthChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusOneTimePasswordField), new PropertyMetadata(null, OnValueChanged));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusOneTimePasswordField), new PropertyMetadata(false, OnDisabledChanged));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusOneTimePasswordField), new PropertyMetadata(false, OnReadOnlyChanged));

    public static readonly DependencyProperty ValidationTypeProperty = DependencyProperty.Register(
        nameof(ValidationType), typeof(string), typeof(NaviusOneTimePasswordField), new PropertyMetadata("numeric"));

    public static readonly DependencyProperty SanitizeValueProperty = DependencyProperty.Register(
        nameof(SanitizeValue), typeof(Func<string, string>), typeof(NaviusOneTimePasswordField), new PropertyMetadata(null));

    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type), typeof(string), typeof(NaviusOneTimePasswordField), new PropertyMetadata("text", OnTypeChanged));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(string), typeof(NaviusOneTimePasswordField), new PropertyMetadata("vertical"));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusOneTimePasswordField), new PropertyMetadata(null));

    public static readonly DependencyProperty AutoFocusProperty = DependencyProperty.Register(
        nameof(AutoFocus), typeof(bool), typeof(NaviusOneTimePasswordField), new PropertyMetadata(false));

    public static readonly DependencyProperty AutoSubmitProperty = DependencyProperty.Register(
        nameof(AutoSubmit), typeof(bool), typeof(NaviusOneTimePasswordField), new PropertyMetadata(false));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        "ValueChanged", RoutingStrategy.Bubble, typeof(EventHandler<OtpRoutedEventArgs>), typeof(NaviusOneTimePasswordField));

    public static readonly RoutedEvent CompleteEvent = EventManager.RegisterRoutedEvent(
        "Complete", RoutingStrategy.Bubble, typeof(EventHandler<OtpRoutedEventArgs>), typeof(NaviusOneTimePasswordField));

    public static readonly RoutedEvent AutoSubmittedEvent = EventManager.RegisterRoutedEvent(
        "AutoSubmitted", RoutingStrategy.Bubble, typeof(EventHandler<OtpRoutedEventArgs>), typeof(NaviusOneTimePasswordField));

    public static readonly RoutedEvent SubmitRequestedEvent = EventManager.RegisterRoutedEvent(
        "SubmitRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusOneTimePasswordField));

    private readonly List<NaviusOneTimePasswordFieldInput> _cells = new();
    private Panel? _cellsHost;
    private char?[] _buffer = Array.Empty<char?>();
    private bool _isSyncingValue;

    static NaviusOneTimePasswordField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusOneTimePasswordField),
            new FrameworkPropertyMetadata(typeof(NaviusOneTimePasswordField)));
    }

    public NaviusOneTimePasswordField()
    {
        _buffer = new char?[Length];
        IsEnabled = !Disabled;
    }

    public event EventHandler<OtpRoutedEventArgs> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public event EventHandler<OtpRoutedEventArgs> Complete
    {
        add => AddHandler(CompleteEvent, value);
        remove => RemoveHandler(CompleteEvent, value);
    }

    public event EventHandler<OtpRoutedEventArgs> AutoSubmitted
    {
        add => AddHandler(AutoSubmittedEvent, value);
        remove => RemoveHandler(AutoSubmittedEvent, value);
    }

    /// <summary>WPF's analog of the web's "submit the closest enclosing form"; a containing NaviusForm or consumer subscribes.</summary>
    public event RoutedEventHandler SubmitRequested
    {
        add => AddHandler(SubmitRequestedEvent, value);
        remove => RemoveHandler(SubmitRequestedEvent, value);
    }

    public int Length
    {
        get => (int)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    public string ValidationType
    {
        get => (string)GetValue(ValidationTypeProperty);
        set => SetValue(ValidationTypeProperty, value);
    }

    public Func<string, string>? SanitizeValue
    {
        get => (Func<string, string>?)GetValue(SanitizeValueProperty);
        set => SetValue(SanitizeValueProperty, value);
    }

    public string Type
    {
        get => (string)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool AutoFocus
    {
        get => (bool)GetValue(AutoFocusProperty);
        set => SetValue(AutoFocusProperty, value);
    }

    public bool AutoSubmit
    {
        get => (bool)GetValue(AutoSubmitProperty);
        set => SetValue(AutoSubmitProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _cellsHost = GetTemplateChild("PART_Cells") as Panel;
        RebuildCells();

        if (AutoFocus && _cells.Count > 0)
        {
            _cells[0].Focus();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusOneTimePasswordFieldAutomationPeer(this);

    private static void OnLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var otp = (NaviusOneTimePasswordField)d;
        otp._buffer = OneTimePasswordBuffer.FromValue(otp.Value, (int)e.NewValue);
        otp.RebuildCells();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var otp = (NaviusOneTimePasswordField)d;
        if (otp._isSyncingValue)
        {
            return;
        }

        otp._buffer = OneTimePasswordBuffer.FromValue((string?)e.NewValue, otp.Length);
        otp.RefreshCellsFromBuffer();
    }

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusOneTimePasswordField)d).IsEnabled = !(bool)e.NewValue;

    private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var otp = (NaviusOneTimePasswordField)d;
        foreach (var cell in otp._cells)
        {
            cell.IsReadOnly = otp.ReadOnly;
        }
    }

    private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var otp = (NaviusOneTimePasswordField)d;
        var masked = string.Equals(otp.Type, "password", StringComparison.Ordinal);
        foreach (var cell in otp._cells)
        {
            cell.IsMasked = masked;
        }
    }

    private void RebuildCells()
    {
        if (_cellsHost is null)
        {
            return;
        }

        _cellsHost.Children.Clear();
        _cells.Clear();

        var masked = string.Equals(Type, "password", StringComparison.Ordinal);

        for (var i = 0; i < Length; i++)
        {
            var cell = new NaviusOneTimePasswordFieldInput
            {
                Index = i,
                IsReadOnly = ReadOnly,
                IsMasked = masked,
                ToolTip = Placeholder,
            };
            AutomationProperties.SetName(cell, $"Character {i + 1} of {Length}");

            cell.PreviewKeyDown += OnCellPreviewKeyDown;
            cell.PreviewTextInput += OnCellPreviewTextInput;
            DataObject.AddPastingHandler(cell, OnCellPaste);

            _cells.Add(cell);
            _cellsHost.Children.Add(cell);
        }

        RefreshCellsFromBuffer();
    }

    private void RefreshCellsFromBuffer()
    {
        for (var i = 0; i < _cells.Count && i < _buffer.Length; i++)
        {
            _cells[i].Char = _buffer[i];
        }
    }

    private void OnCellPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var index = ((NaviusOneTimePasswordFieldInput)sender).Index;
        var isVertical = !string.Equals(Orientation, "horizontal", StringComparison.Ordinal);

        switch (e.Key)
        {
            case Key.Back when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                if (!ReadOnly)
                {
                    ApplyBufferChange(OneTimePasswordBuffer.ClearAll(Length));
                }

                e.Handled = true;
                break;

            case Key.Back:
                if (!ReadOnly)
                {
                    ApplyBufferChange(OneTimePasswordBuffer.Backspace(_buffer, index));
                }

                e.Handled = true;
                break;

            case Key.Delete:
                if (!ReadOnly)
                {
                    ApplyBufferChange(OneTimePasswordBuffer.Delete(_buffer, index));
                }

                e.Handled = true;
                break;

            case Key.Up when isVertical:
                FocusCell(Math.Max(index - 1, 0));
                e.Handled = true;
                break;

            case Key.Down when isVertical:
                FocusCell(Math.Min(index + 1, Length - 1));
                e.Handled = true;
                break;

            case Key.Left when !isVertical:
                FocusCell(Math.Max(index - 1, 0));
                e.Handled = true;
                break;

            case Key.Right when !isVertical:
                FocusCell(Math.Min(index + 1, Length - 1));
                e.Handled = true;
                break;

            case Key.Home:
                FocusCell(0);
                e.Handled = true;
                break;

            case Key.End:
                FocusCell(Length - 1);
                e.Handled = true;
                break;

            case Key.Enter:
                RaiseEvent(new RoutedEventArgs(SubmitRequestedEvent, this));
                e.Handled = true;
                break;
        }
    }

    private void OnCellPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = true;

        if (Disabled || ReadOnly || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        // "last char of raw input wins"
        var ch = e.Text[^1];
        if (!OneTimePasswordBuffer.IsAllowedChar(ch, ValidationType))
        {
            return;
        }

        if (SanitizeValue is not null)
        {
            var sanitized = SanitizeValue(ch.ToString());
            if (sanitized.Length == 0)
            {
                return;
            }

            ch = sanitized[0];
        }

        var index = ((NaviusOneTimePasswordFieldInput)sender).Index;
        ApplyBufferChange(OneTimePasswordBuffer.SetChar(_buffer, index, ch));
    }

    private void OnCellPaste(object sender, DataObjectPastingEventArgs e)
    {
        e.CancelCommand();

        if (Disabled || ReadOnly || !e.SourceDataObject.GetDataPresent(DataFormats.Text))
        {
            return;
        }

        var raw = (string)e.SourceDataObject.GetData(DataFormats.Text);
        var filtered = new string(raw.Where(c => OneTimePasswordBuffer.IsAllowedChar(c, ValidationType)).ToArray());
        if (SanitizeValue is not null)
        {
            filtered = SanitizeValue(filtered);
        }

        if (filtered.Length == 0)
        {
            return;
        }

        ApplyBufferChange(OneTimePasswordBuffer.Paste(filtered, Length));
    }

    private void ApplyBufferChange((char?[] Buffer, int FocusIndex) result)
    {
        var wasComplete = OneTimePasswordBuffer.IsComplete(_buffer);
        _buffer = result.Buffer;
        RefreshCellsFromBuffer();

        var value = OneTimePasswordBuffer.ToValue(_buffer);
        _isSyncingValue = true;
        try
        {
            SetCurrentValue(ValueProperty, value);
        }
        finally
        {
            _isSyncingValue = false;
        }

        RaiseEvent(new OtpRoutedEventArgs(ValueChangedEvent, this, value));
        FocusCell(result.FocusIndex);

        var isComplete = OneTimePasswordBuffer.IsComplete(_buffer);
        if (isComplete && !wasComplete)
        {
            RaiseEvent(new OtpRoutedEventArgs(CompleteEvent, this, value));
            if (AutoSubmit)
            {
                RaiseEvent(new OtpRoutedEventArgs(AutoSubmittedEvent, this, value));
            }
        }
    }

    private void FocusCell(int index)
    {
        if (index >= 0 && index < _cells.Count)
        {
            _cells[index].Focus();
        }
    }
}

internal sealed class NaviusOneTimePasswordFieldAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusOneTimePasswordFieldAutomationPeer(NaviusOneTimePasswordField owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusOneTimePasswordField);
}
