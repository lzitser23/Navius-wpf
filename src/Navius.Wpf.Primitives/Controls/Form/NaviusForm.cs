using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Field;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Form;

/// <summary>
/// Tier B (custom lookless control). No native WPF control models a &lt;form&gt;'s
/// submit/validation/error orchestration, so this is a scope root that discovers every
/// descendant NaviusField by walking its own logical tree (LogicalTreeWalker) on each submit
/// attempt, rather than porting the web contract's FormContext Register/Unregister registry
/// -- WPF's tree-walk is a direct, always-correct substitute for that bookkeeping. Field
/// validity itself comes from NaviusField's own Validation.Error-bubble-backed
/// IsFieldInvalid, i.e. WPF's native Binding-validation pipeline, per the locked plan
/// ("integrate WPF's INotifyDataErrorInfo + Validation.* rather than reinventing the web's
/// constraint-validation reader").
///
/// PreventDefault has no meaning here (there is no browser-level default navigation to
/// prevent) and is dropped rather than ported as a no-op parameter.
/// </summary>
public class NaviusForm : ContentControl
{
    public static readonly DependencyProperty ErrorsProperty = DependencyProperty.Register(
        nameof(Errors),
        typeof(IReadOnlyDictionary<string, string[]>),
        typeof(NaviusForm),
        new PropertyMetadata(null, OnErrorsChanged));

    public static readonly RoutedEvent SubmitEvent = EventManager.RegisterRoutedEvent(
        nameof(Submitted),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusForm));

    public static readonly RoutedEvent ClearErrorsRequestedRoutedEvent = EventManager.RegisterRoutedEvent(
        "ClearErrorsRequested",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusForm));

    private static readonly DependencyPropertyKey SubmitCommandPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SubmitCommand),
        typeof(ICommand),
        typeof(NaviusForm),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SubmitCommandProperty = SubmitCommandPropertyKey.DependencyProperty;

    static NaviusForm()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusForm), new FrameworkPropertyMetadata(typeof(NaviusForm)));
    }

    public NaviusForm()
    {
        SetValue(SubmitCommandPropertyKey, new RelayCommand(HandleSubmit));
    }

    /// <summary>Fires from HandleSubmit only when every descendant NaviusField is valid after reveal.</summary>
    public event RoutedEventHandler Submitted
    {
        add => AddHandler(SubmitEvent, value);
        remove => RemoveHandler(SubmitEvent, value);
    }

    /// <summary>Fires before revalidating on submit, and again on Reset.</summary>
    public event RoutedEventHandler ClearErrorsRequested
    {
        add => AddHandler(ClearErrorsRequestedRoutedEvent, value);
        remove => RemoveHandler(ClearErrorsRequestedRoutedEvent, value);
    }

    public IReadOnlyDictionary<string, string[]>? Errors
    {
        get => (IReadOnlyDictionary<string, string[]>?)GetValue(ErrorsProperty);
        set => SetValue(ErrorsProperty, value);
    }

    /// <summary>Bindable from a consumer-supplied Button's Command, or used automatically by NaviusFormSubmit.</summary>
    public ICommand SubmitCommand => (ICommand)GetValue(SubmitCommandProperty);

    public void Reset()
    {
        RaiseEvent(new RoutedEventArgs(ClearErrorsRequestedRoutedEvent, this));
        foreach (var field in Fields())
        {
            field.ExternalErrors = null;
        }
    }

    private static void OnErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusForm)d).ApplyErrors((IReadOnlyDictionary<string, string[]>?)e.NewValue);

    private void HandleSubmit()
    {
        // ClearServerErrorsAsync + OnClearErrors, before revalidating.
        foreach (var field in Fields())
        {
            field.ExternalErrors = null;
        }

        RaiseEvent(new RoutedEventArgs(ClearErrorsRequestedRoutedEvent, this));

        ApplyErrors(Errors);

        var fields = Fields().ToList();
        foreach (var field in fields)
        {
            field.Reveal();
        }

        var firstInvalid = fields.FirstOrDefault(f => f.IsFieldInvalid);
        if (firstInvalid is null)
        {
            RaiseEvent(new RoutedEventArgs(SubmitEvent, this));
        }
        else
        {
            firstInvalid.FocusRegisteredControl();
        }
    }

    private void ApplyErrors(IReadOnlyDictionary<string, string[]>? errors)
    {
        foreach (var field in Fields())
        {
            field.ExternalErrors = errors is not null && errors.TryGetValue(field.FieldName, out var messages)
                ? messages
                : null;
        }
    }

    private IEnumerable<NaviusField> Fields() => LogicalTreeWalker.Descendants<NaviusField>(this);

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
