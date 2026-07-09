using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Tier B lookless control for a single toast. The web contract splits a toast into five parts
/// (NaviusToastRoot/Content/Title/Description/Action/Close); this collapses them into one
/// templated control with named template parts (PART_Close/PART_Action) instead of five separate
/// classes, since none of those parts have independent behavior worth a standalone type in WPF --
/// see Themes/Toast.xaml for the default template. NaviusToastPositioner/NaviusToastArrow are not
/// ported: they are documented upstream as unwired stubs with no engine behind them (contract:
/// "not used by the primary viewport-stacked toast").
///
/// Manager-agnostic by design: <see cref="NaviusToastViewport"/> owns the ToastHandle/ToastManager
/// wiring and only sets these DPs / listens to <see cref="CloseRequested"/>/<see cref="ActionRequested"/>.
/// </summary>
[TemplatePart(Name = PartClose, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartAction, Type = typeof(ButtonBase))]
public class NaviusToast : Control
{
    private const string PartClose = "PART_Close";
    private const string PartAction = "PART_Action";

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(NaviusToast),
        new PropertyMetadata(null, OnAnnounceablePropertyChanged));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(NaviusToast),
        new PropertyMetadata(null, OnAnnounceablePropertyChanged));

    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type), typeof(ToastType), typeof(NaviusToast),
        new PropertyMetadata(ToastType.Default));

    public static readonly DependencyProperty PriorityProperty = DependencyProperty.Register(
        nameof(Priority), typeof(ToastPriority), typeof(NaviusToast),
        new PropertyMetadata(ToastPriority.Low));

    public static readonly DependencyProperty ActionLabelProperty = DependencyProperty.Register(
        nameof(ActionLabel), typeof(string), typeof(NaviusToast),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActionAltTextProperty = DependencyProperty.Register(
        nameof(ActionAltText), typeof(string), typeof(NaviusToast),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
        nameof(ShowCloseButton), typeof(bool), typeof(NaviusToast),
        new PropertyMetadata(true));

    public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(CloseRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusToast));

    public static readonly RoutedEvent ActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ActionRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusToast));

    private ButtonBase? _closeButton;
    private ButtonBase? _actionButton;

    static NaviusToast()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusToast), new FrameworkPropertyMetadata(typeof(NaviusToast)));
        // Contract: tabindex="0", part of the natural tab order.
        FocusableProperty.OverrideMetadata(typeof(NaviusToast), new FrameworkPropertyMetadata(true));
    }

    public NaviusToast()
    {
        Loaded += (_, _) => Announce();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ToastType Type
    {
        get => (ToastType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>Low -> role="status" (polite); High -> role="alert" (assertive).</summary>
    public ToastPriority Priority
    {
        get => (ToastPriority)GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    public string? ActionLabel
    {
        get => (string?)GetValue(ActionLabelProperty);
        set => SetValue(ActionLabelProperty, value);
    }

    public string ActionAltText
    {
        get => (string)GetValue(ActionAltTextProperty);
        set => SetValue(ActionAltTextProperty, value);
    }

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>Raised when Escape is pressed or the close part is clicked; the viewport dismisses the toast.</summary>
    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    /// <summary>Raised when the action part is clicked; the viewport runs the action callback then dismisses.</summary>
    public event RoutedEventHandler ActionRequested
    {
        add => AddHandler(ActionRequestedEvent, value);
        remove => RemoveHandler(ActionRequestedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseClick;
        }

        if (_actionButton is not null)
        {
            _actionButton.Click -= OnActionClick;
        }

        base.OnApplyTemplate();

        _closeButton = GetTemplateChild(PartClose) as ButtonBase;
        if (_closeButton is not null)
        {
            _closeButton.Click += OnCloseClick;
        }

        _actionButton = GetTemplateChild(PartAction) as ButtonBase;
        if (_actionButton is not null)
        {
            _actionButton.Click += OnActionClick;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusToastAutomationPeer(this);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));

    private void OnActionClick(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(ActionRequestedEvent, this));

    private static void OnAnnounceablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToast)d).Announce();

    /// <summary>
    /// Contract calls for role="status"/"alert" with text duplicated into a hidden aria-live
    /// announcer region. WPF's UIA notification event is a more direct, better-supported
    /// mechanism for the same "announce this transient text" need (see docs/parity/toast.md's
    /// WPF strategy), so this raises it instead of reproducing the web's duplicate-announcer-div
    /// pattern. RaiseNotificationEvent requires Windows 10 1709+ and a listening AT; on an older
    /// OS build (or no AT attached) it returns false and does nothing, at which point
    /// <see cref="NaviusToastAutomationPeer.GetLiveSettingCore"/>'s Polite/Assertive LiveSetting
    /// is the fallback a screen reader can still key off.
    /// </summary>
    private void Announce()
    {
        if (!IsLoaded)
        {
            return;
        }

        var text = string.IsNullOrEmpty(Description) ? Title : $"{Title} {Description}";
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        var processing = Priority == ToastPriority.High
            ? AutomationNotificationProcessing.ImportantMostRecent
            : AutomationNotificationProcessing.CurrentThenMostRecent;

        peer?.RaiseNotificationEvent(AutomationNotificationKind.Other, processing, text, Guid.NewGuid().ToString());
    }
}
