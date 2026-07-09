using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Tier B (custom lookless control). PARITY OF OUTCOME THROUGH NATIVE MECHANICS: rather than
/// reimplementing the web contract's JS-interop createConstraintValidation bridge and its
/// FieldContext cascading-parameter/id-registry object, this integrates WPF's own
/// Validation.Error routed event (which already bubbles from any descendant Binding
/// validation failure) plus INotifyDataErrorInfo-driven bindings. The web's discrete
/// touched/dirty/filled/focused state attrs become read-only dependency properties computed
/// from real bubbling control events (GotFocus/LostFocus/TextChanged), so styles/templates
/// can trigger on them exactly like the contract's data-* attributes.
///
/// There is no cascading-parameter equivalent in WPF, so instead of descendant parts pulling
/// a cascaded FieldContext, NaviusField pushes: on OnContentChanged (fired synchronously once
/// its whole ChildContent subtree exists, exactly like RadioGroup/CheckboxGroup's own
/// descendant-wiring in this codebase) it walks its logical descendants once to register the
/// control and wire NaviusFieldLabel.Target, and on every validity-affecting change it walks
/// again to push fresh state into every NaviusFieldError. This also sidesteps FrameworkElement
/// .Loaded, which never fires for elements that aren't connected to a real PresentationSource
/// (a live Window) -- true for every headless unit test in this suite. The web's GUID-based id
/// registry (ControlId/DescribedBy) is dropped entirely in favor of direct element references,
/// per field.md's own open question that AutomationProperties.LabeledBy/DescribedBy use
/// element references, not string ids.
///
/// FieldValidity's granular HTML5 ValidityState-style flags (BadInput/PatternMismatch/...)
/// collapse to a single Validation.Errors-backed invalid/valid + message-list model, per
/// field.md's own open question sanctioning that collapse for WPF controls.
/// </summary>
public class NaviusField : ContentControl
{
    // Named FieldName (not Name) to avoid hiding FrameworkElement.Name, which XAML's x:Name
    // and FindName rely on; this is the web contract's form-field Name, an unrelated concept.
    public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register(
        nameof(FieldName),
        typeof(string),
        typeof(NaviusField),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(NaviusField),
        new PropertyMetadata(false, OnDisabledChanged));

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid),
        typeof(bool),
        typeof(NaviusField),
        new PropertyMetadata(false, OnInvalidatingPropertyChanged));

    public static readonly DependencyProperty ServerInvalidProperty = DependencyProperty.Register(
        nameof(ServerInvalid),
        typeof(bool),
        typeof(NaviusField),
        new PropertyMetadata(false, OnInvalidatingPropertyChanged));

    public static readonly DependencyProperty ValidationModeProperty = DependencyProperty.Register(
        nameof(ValidationMode),
        typeof(FieldValidationMode),
        typeof(NaviusField),
        new PropertyMetadata(FieldValidationMode.OnSubmit));

    public static readonly DependencyProperty ExternalErrorsProperty = DependencyProperty.Register(
        nameof(ExternalErrors),
        typeof(IReadOnlyList<string>),
        typeof(NaviusField),
        new PropertyMetadata(null, OnInvalidatingPropertyChanged));

    private static readonly DependencyPropertyKey IsFieldValidPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFieldValid),
        typeof(bool?),
        typeof(NaviusField),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsFieldValidProperty = IsFieldValidPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsFieldInvalidPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFieldInvalid),
        typeof(bool),
        typeof(NaviusField),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsFieldInvalidProperty = IsFieldInvalidPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsDirtyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsDirty), typeof(bool), typeof(NaviusField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsDirtyProperty = IsDirtyPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsTouchedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsTouched), typeof(bool), typeof(NaviusField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsTouchedProperty = IsTouchedPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsFilledPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFilled), typeof(bool), typeof(NaviusField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilledProperty = IsFilledPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsFieldFocusedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFieldFocused), typeof(bool), typeof(NaviusField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsFieldFocusedProperty = IsFieldFocusedPropertyKey.DependencyProperty;

    private int _bindingErrorCount;
    private bool _revealed;

    static NaviusField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusField), new FrameworkPropertyMetadata(typeof(NaviusField)));
    }

    public NaviusField()
    {
        IsEnabled = !Disabled;
        AddHandler(System.Windows.Controls.Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(OnDescendantValidationError));
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnDescendantTextChanged));
        AddHandler(GotFocusEvent, new RoutedEventHandler(OnDescendantGotFocus));
        AddHandler(LostFocusEvent, new RoutedEventHandler(OnDescendantLostFocus));
    }

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    /// <summary>Consumer-controlled "mark invalid" flag; does not auto-clear.</summary>
    public bool Invalid
    {
        get => (bool)GetValue(InvalidProperty);
        set => SetValue(InvalidProperty, value);
    }

    /// <summary>Server-side invalidity; auto-clears on the next descendant TextChanged.</summary>
    public bool ServerInvalid
    {
        get => (bool)GetValue(ServerInvalidProperty);
        set => SetValue(ServerInvalidProperty, value);
    }

    public FieldValidationMode ValidationMode
    {
        get => (FieldValidationMode)GetValue(ValidationModeProperty);
        set => SetValue(ValidationModeProperty, value);
    }

    /// <summary>External error messages injected by name, e.g. from NaviusForm.Errors.</summary>
    public IReadOnlyList<string>? ExternalErrors
    {
        get => (IReadOnlyList<string>?)GetValue(ExternalErrorsProperty);
        set => SetValue(ExternalErrorsProperty, value);
    }

    /// <summary>Tri-state: null until revealed per ValidationMode, matching the web's valid: null.</summary>
    public bool? IsFieldValid
    {
        get => (bool?)GetValue(IsFieldValidProperty);
        private set => SetValue(IsFieldValidPropertyKey, value);
    }

    public bool IsFieldInvalid
    {
        get => (bool)GetValue(IsFieldInvalidProperty);
        private set => SetValue(IsFieldInvalidPropertyKey, value);
    }

    public bool IsDirty
    {
        get => (bool)GetValue(IsDirtyProperty);
        private set => SetValue(IsDirtyPropertyKey, value);
    }

    public bool IsTouched
    {
        get => (bool)GetValue(IsTouchedProperty);
        private set => SetValue(IsTouchedPropertyKey, value);
    }

    public bool IsFilled
    {
        get => (bool)GetValue(IsFilledProperty);
        private set => SetValue(IsFilledPropertyKey, value);
    }

    public bool IsFieldFocused
    {
        get => (bool)GetValue(IsFieldFocusedProperty);
        private set => SetValue(IsFieldFocusedPropertyKey, value);
    }

    /// <summary>The descendant control registered via NaviusFieldControl/NaviusInput, if any.</summary>
    public UIElement? RegisteredControl { get; private set; }

    /// <summary>
    /// Surfaces current validity per ValidationMode. Called by NaviusForm on a submit
    /// attempt (the WPF stand-in for the web's FieldContext.RevealAsync()); a consumer can
    /// also call it directly outside a Form.
    /// </summary>
    public void Reveal()
    {
        _revealed = true;
        RecomputeValidity();
    }

    public void RegisterControl(UIElement control) => RegisteredControl = control;

    public void FocusRegisteredControl() => RegisteredControl?.Focus();

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        WireDescendants();
    }

    /// <summary>
    /// Runs once ChildContent is fully assigned: finds (or default-fills) the field's control
    /// inside a descendant NaviusFieldControl, or a bare descendant NaviusInput when no
    /// NaviusFieldControl wrapper is used, registers it, and points any NaviusFieldLabel at it.
    /// </summary>
    private void WireDescendants()
    {
        var fieldControl = LogicalTreeWalker.Descendants<NaviusFieldControl>(this).FirstOrDefault();
        if (fieldControl is not null && fieldControl.Content is null)
        {
            fieldControl.Content = new NaviusInput();
        }

        var control = fieldControl?.Content as UIElement
            ?? LogicalTreeWalker.Descendants<NaviusInput>(this).FirstOrDefault();

        if (control is not null)
        {
            RegisterControl(control);
        }

        var label = LogicalTreeWalker.Descendants<NaviusFieldLabel>(this).FirstOrDefault();
        if (label is not null)
        {
            label.Target = RegisteredControl as FrameworkElement;
        }

        PushErrorsToDescendants();
    }

    private void PushErrorsToDescendants()
    {
        foreach (var error in LogicalTreeWalker.Descendants<NaviusFieldError>(this))
        {
            error.UpdateFromField(this);
        }
    }

    /// <summary>Combines live Binding-validation messages on the registered control with ExternalErrors.</summary>
    internal IReadOnlyList<string> GetErrors()
    {
        var messages = new List<string>();
        if (RegisteredControl is not null)
        {
            foreach (var error in System.Windows.Controls.Validation.GetErrors(RegisteredControl))
            {
                if (error.ErrorContent is string text)
                {
                    messages.Add(text);
                }
            }
        }

        if (ExternalErrors is { Count: > 0 })
        {
            messages.AddRange(ExternalErrors);
        }

        return messages;
    }

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusField)d).IsEnabled = !(bool)e.NewValue;

    private static void OnInvalidatingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusField)d).RecomputeValidity();

    private void OnDescendantValidationError(object? sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added)
        {
            _bindingErrorCount++;
        }
        else if (_bindingErrorCount > 0)
        {
            _bindingErrorCount--;
        }

        RecomputeValidity();
    }

    private void OnDescendantTextChanged(object sender, TextChangedEventArgs e)
    {
        IsDirty = true;

        // "auto-clears ServerInvalid on the next user edit" (field.md State section).
        if (ServerInvalid)
        {
            ServerInvalid = false;
        }

        if (e.OriginalSource is TextBox textBox)
        {
            IsFilled = !string.IsNullOrEmpty(textBox.Text);
        }

        if (ValidationMode == FieldValidationMode.OnChange)
        {
            Reveal();
        }
        else
        {
            RecomputeValidity();
        }
    }

    private void OnDescendantGotFocus(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, this))
        {
            IsFieldFocused = true;
        }
    }

    private void OnDescendantLostFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
        {
            return;
        }

        IsFieldFocused = false;
        IsTouched = true;

        if (ValidationMode == FieldValidationMode.OnBlur)
        {
            Reveal();
        }
    }

    private void RecomputeValidity()
    {
        var hasError = _bindingErrorCount > 0 || Invalid || ServerInvalid || (ExternalErrors?.Count ?? 0) > 0;
        IsFieldValid = _revealed ? !hasError : null;
        IsFieldInvalid = _revealed && hasError;
        PushErrorsToDescendants();
    }
}
