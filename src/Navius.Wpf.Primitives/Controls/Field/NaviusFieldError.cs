using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Surfaces the field's live validation errors: reads Validation.Errors off the field's
/// registered control (plus any ExternalErrors) via NaviusField.GetErrors(), per the locked
/// plan's "FieldError surfaces Validation.Errors of the associated control's binding" rather
/// than the web contract's own ValidityState-key matching. Match narrows to a specific error
/// message rather than an HTML5 validity key, since WPF's INotifyDataErrorInfo/Binding
/// validation model has no equivalent key set (see field.md Open Questions). Only shown while
/// the field is revealed-invalid (or ForceMatch is set), mirroring the web's reveal-gated
/// display; AutomationProperties.LiveSetting=Assertive is the WPF analog of role="alert".
///
/// The ancestor NaviusField pushes fresh state via UpdateFromField (called once ChildContent
/// is assigned, and again on every validity-affecting change) instead of this control pulling
/// its ancestor on Loaded, since Loaded never fires for elements outside a live Window (true
/// of every headless unit test in this suite).
/// </summary>
public class NaviusFieldError : ContentControl
{
    public static readonly DependencyProperty MatchProperty = DependencyProperty.Register(
        nameof(Match),
        typeof(string),
        typeof(NaviusFieldError),
        new PropertyMetadata(null, OnRecomputeTrigger));

    public static readonly DependencyProperty ForceMatchProperty = DependencyProperty.Register(
        nameof(ForceMatch),
        typeof(bool),
        typeof(NaviusFieldError),
        new PropertyMetadata(false, OnRecomputeTrigger));

    private NaviusField? _field;

    static NaviusFieldError()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldError), new FrameworkPropertyMetadata(typeof(NaviusFieldError)));
    }

    public NaviusFieldError()
    {
        Visibility = Visibility.Collapsed;
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
    }

    public string? Match
    {
        get => (string?)GetValue(MatchProperty);
        set => SetValue(MatchProperty, value);
    }

    public bool ForceMatch
    {
        get => (bool)GetValue(ForceMatchProperty);
        set => SetValue(ForceMatchProperty, value);
    }

    /// <summary>Called by the owning NaviusField whenever its validity-affecting state changes.</summary>
    internal void UpdateFromField(NaviusField? field)
    {
        _field = field;
        Recompute();
    }

    private static void OnRecomputeTrigger(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusFieldError)d).Recompute();

    private void Recompute()
    {
        var errors = _field?.GetErrors() ?? Array.Empty<string>();
        var matches = Match is null || errors.Contains(Match);
        var show = ForceMatch || ((_field?.IsFieldInvalid ?? false) && matches);

        Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && Content is null)
        {
            Content = string.Join(" ", errors);
        }
    }
}
