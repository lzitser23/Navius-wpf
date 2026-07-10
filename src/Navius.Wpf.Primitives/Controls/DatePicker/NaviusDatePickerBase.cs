using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Calendar;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.DatePicker;

/// <summary>
/// Tier B lookless base shared by <see cref="NaviusDatePicker"/> and
/// <see cref="Controls.DateRangePicker.NaviusDateRangePicker"/>: a trigger button anchoring a
/// <see cref="NaviusAnchoredPopup"/> that hosts a <see cref="NaviusCalendar"/>. Owns the open/close
/// lifecycle (trigger toggle; Enter/Space/ArrowDown open when closed; Escape cancels; outside press
/// dismisses via <see cref="OverlayStack"/> with both trigger and popup content registered as input
/// roots, the Select-family precedent) and the pick detection that both pickers commit through.
///
/// Pick detection deliberately does NOT listen to Calendar.SelectedDatesChanged: the native
/// calendar moves its selection on every arrow key, and the contract wants arrow keys to navigate
/// without committing ("calendar keys navigate, Enter commits"). Instead a pick is (a) a left
/// mouse-up on a CalendarDayButton, or (b) Enter/Space bubbling out of the calendar (read as the
/// calendar's SelectedDate, which its own class handler has just set to the focused day). Both are
/// attached with handledEventsToo because Calendar's class handlers mark those events handled.
/// </summary>
[TemplatePart(Name = PartTrigger, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCalendar, Type = typeof(NaviusCalendar))]
public abstract class NaviusDatePickerBase : Control
{
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";
    private const string PartCalendar = "PART_Calendar";

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusDatePickerBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusDatePickerBase),
        new PropertyMetadata(null, OnPlaceholderChanged));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusDatePickerBase),
        new PropertyMetadata(PlacementSide.Bottom));

    // Same DefaultAlign override to Start as the Select family (a picker popup lines up with the
    // trigger's leading edge; the web contract's DateRangePickerContent default is Align="start").
    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusDatePickerBase),
        new PropertyMetadata(PlacementAlign.Start));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusDatePickerBase),
        new PropertyMetadata(6d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusDatePickerBase),
        new PropertyMetadata(0d));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(NaviusDatePickerBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasSelectionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasSelection), typeof(bool), typeof(NaviusDatePickerBase),
        new PropertyMetadata(false));

    public static readonly DependencyProperty HasSelectionProperty = HasSelectionPropertyKey.DependencyProperty;

    private ToggleButton? _triggerPart;
    private FrameworkElement? _popupContentPart;
    private NaviusCalendar? _calendarPart;
    private OverlaySession? _session;

    protected NaviusDatePickerBase()
    {
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Muted trigger text shown while nothing is selected (web data-placeholder styling).</summary>
    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public PlacementSide Side
    {
        get => (PlacementSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public PlacementAlign Align
    {
        get => (PlacementAlign)GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    public double SideOffset
    {
        get => (double)GetValue(SideOffsetProperty);
        set => SetValue(SideOffsetProperty, value);
    }

    public double AlignOffset
    {
        get => (double)GetValue(AlignOffsetProperty);
        set => SetValue(AlignOffsetProperty, value);
    }

    /// <summary>Resolved trigger label: the formatted value, or the placeholder. This is a plain
    /// read-only display, not an editable segment field: the DateInput family (a separate family
    /// this wave) owns segmented editing; composing the two is a follow-up.</summary>
    public string? DisplayText => (string?)GetValue(DisplayTextProperty);

    /// <summary>True when a value is set (drives the placeholder-muting trigger styling).</summary>
    public bool HasSelection => (bool)GetValue(HasSelectionProperty);

    /// <summary>The templated calendar surface; null until the template is applied.</summary>
    protected NaviusCalendar? CalendarPart => _calendarPart;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_calendarPart is not null)
        {
            _calendarPart.RemoveHandler(MouseUpEvent, (MouseButtonEventHandler)OnCalendarMouseUp);
            _calendarPart.RemoveHandler(KeyDownEvent, (KeyEventHandler)OnCalendarKeyDown);
        }

        if (_popupContentPart is not null)
        {
            _popupContentPart.PreviewKeyDown -= HandlePopupPreviewKeyDown;
        }

        _triggerPart = GetTemplateChild(PartTrigger) as ToggleButton;
        _popupContentPart = GetTemplateChild(PartPopupContent) as FrameworkElement;
        _calendarPart = GetTemplateChild(PartCalendar) as NaviusCalendar;

        if (_calendarPart is not null)
        {
            // handledEventsToo: Calendar's class handlers mark day-click MouseUp and Enter/Space
            // KeyDown handled, and the pick detection below must still see them.
            _calendarPart.AddHandler(MouseUpEvent, (MouseButtonEventHandler)OnCalendarMouseUp, handledEventsToo: true);
            _calendarPart.AddHandler(KeyDownEvent, (KeyEventHandler)OnCalendarKeyDown, handledEventsToo: true);
        }

        if (_popupContentPart is not null)
        {
            // The popup content lives in the Popup's own HwndSource, so key events raised inside it
            // never tunnel through this control's PreviewKeyDown; Escape needs its own hook there.
            _popupContentPart.PreviewKeyDown += HandlePopupPreviewKeyDown;
        }

        UpdateDisplay();
    }

    /// <summary>One committed pick (a day the user clicked or pressed Enter/Space on).</summary>
    protected abstract void OnPickCommitted(DateTime day);

    /// <summary>Called when the popup opens, before the overlay engages: seed the calendar
    /// (selection mode, selection, DisplayDate) and snapshot whatever Escape should revert to.</summary>
    protected abstract void OnOpened();

    /// <summary>Recompute <see cref="DisplayText"/>/<see cref="HasSelection"/> from the value.</summary>
    protected abstract void UpdateDisplay();

    /// <summary>Escape/cancel path; default just closes. The range picker overrides to first
    /// revert both endpoints to the open-time snapshot per the contract.</summary>
    protected virtual void CancelAndClose() => IsOpen = false;

    protected void SetDisplay(string? text, bool hasSelection)
    {
        SetValue(DisplayTextPropertyKey, text);
        SetValue(HasSelectionPropertyKey, hasSelection);
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDatePickerBase)d).UpdateDisplay();

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusDatePickerBase)d;
        if ((bool)e.NewValue)
        {
            control.OpenCore();
        }
        else
        {
            control.CloseCore();
        }
    }

    private void OpenCore()
    {
        // Seed + snapshot first so the open contract also holds headless (unit tests have no
        // Window, so EngageOverlay below no-ops there; same shape as NaviusSelectBase.OpenCore).
        OnOpened();
        EngageOverlay();
        FocusCalendarSoon();
    }

    private void CloseCore()
    {
        CloseOverlay();
        _triggerPart?.Focus();
    }

    private void FocusCalendarSoon()
    {
        if (_calendarPart is null)
        {
            return;
        }

        // Deferred: the Popup's HwndSource does not exist until after the open pass, and focusing
        // the calendar synchronously would land on a not-yet-connected element.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (IsOpen)
            {
                _calendarPart?.Focus();
            }
        }));
    }

    private void EngageOverlay()
    {
        if (_session is not null || _popupContentPart is null)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        var stack = OverlayStack.GetFor(window);
        _session = stack.Push(_popupContentPart, new OverlayOptions
        {
            Modal = false,
            CloseOnEscape = false, // Escape is owned by the handlers here (revert semantics differ per picker).
            CloseOnOutsideClick = true,
            TrapFocus = false, // Focus is moved onto the calendar explicitly above.
            RestoreFocus = false,
        });

        _session.RegisterInputRoot(_popupContentPart);
        if (_triggerPart is not null)
        {
            // Same as Select: a press on the trigger while open must count as "inside" so the
            // outside-press close does not race the trigger's own toggle (close-then-reopen).
            _session.RegisterInputRoot(_triggerPart);
        }

        _session.Closed += OnSessionClosed;
    }

    private void CloseOverlay() => _session?.RequestClose(OverlayCloseReason.Programmatic);

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        IsOpen = false;
    }

    // Named distinctly from UIElement.OnPreviewKeyDown so reflection-based test lookups stay
    // unambiguous (same convention as NaviusSelectBase.HandlePreviewKeyDown).
    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!IsOpen)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                case Key.Down:
                    IsOpen = true;
                    e.Handled = true;
                    break;
            }

            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelAndClose();
            e.Handled = true;
        }
    }

    private void HandlePopupPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelAndClose();
            e.Handled = true;
        }
    }

    private void OnCalendarKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space))
        {
            return;
        }

        // Calendar's own class handler has already moved SelectedDate to the focused day.
        if (_calendarPart?.SelectedDate is DateTime day)
        {
            OnPickCommitted(day.Date);
        }
    }

    private void OnCalendarMouseUp(object sender, MouseButtonEventArgs e)
    {
        // CalendarItem keeps mouse capture after a day click (the classic WPF DatePicker popup
        // quirk that swallows the next click anywhere in the window); release it.
        if (Mouse.Captured is CalendarItem or System.Windows.Controls.Calendar)
        {
            Mouse.Capture(null);
        }

        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (FindDayButton(e.OriginalSource as DependencyObject) is { DataContext: DateTime day })
        {
            OnPickCommitted(day.Date);
        }
    }

    private static CalendarDayButton? FindDayButton(DependencyObject? node)
    {
        var current = node;
        while (current is not null)
        {
            if (current is CalendarDayButton dayButton)
            {
                return dayButton;
            }

            if (current is System.Windows.Controls.Calendar)
            {
                return null;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
