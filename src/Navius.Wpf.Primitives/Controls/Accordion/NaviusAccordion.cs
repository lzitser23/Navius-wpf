using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Accordion;

/// <summary>
/// Tier B (lookless custom control), following the same "root owns state, discovers
/// Item/Trigger/Panel descendants via the logical tree" shape as NaviusRadioGroup and
/// NaviusCollapsible. WPF has no first-class multi-expand accordion primitive, so
/// <see cref="Type"/> ("single" vs "multiple") is handled entirely by this root rather
/// than composing per-item Expanders: single mode owns one <see cref="Value"/> string,
/// multiple mode owns a <see cref="Values"/> list, mirroring the contract's own two
/// controlled-state shapes.
///
/// Disabled is not reimplemented as its own property: each Item's native IsEnabled is
/// reused directly. WPF's IsEnabled does not automatically cascade through a
/// ContentControl's logical Content the way it does through a Panel's Children, so
/// SyncDescendants explicitly pushes each Item's IsEnabled down onto its Trigger (and
/// re-subscribes to IsEnabledChanged so later toggles keep propagating), matching
/// "Disabled cascades to every item." Loop is not exposed either: unlike NaviusRadioGroup/NaviusToggleGroup, the
/// contract's own accordion keyboard table always wraps ("Move focus to the previous
/// enabled trigger, wrapping"), with no non-looping variant, so arrow navigation here
/// always wraps unconditionally.
/// </summary>
public class NaviusAccordion : ContentControl
{
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type),
        typeof(string),
        typeof(NaviusAccordion),
        new PropertyMetadata("single", OnOpenStateChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusAccordion),
        new PropertyMetadata(null, OnOpenStateChanged));

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values),
        typeof(IReadOnlyList<string>),
        typeof(NaviusAccordion),
        new PropertyMetadata(Array.Empty<string>(), OnOpenStateChanged));

    public static readonly DependencyProperty CollapsibleProperty = DependencyProperty.Register(
        nameof(Collapsible),
        typeof(bool),
        typeof(NaviusAccordion),
        new PropertyMetadata(false));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(string),
        typeof(NaviusAccordion),
        new PropertyMetadata("vertical"));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir),
        typeof(string),
        typeof(NaviusAccordion),
        new PropertyMetadata(null));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusAccordion));

    public static readonly RoutedEvent ValuesChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValuesChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusAccordion));

    static NaviusAccordion()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccordion),
            new FrameworkPropertyMetadata(typeof(NaviusAccordion)));
    }

    public NaviusAccordion()
    {
        Focusable = false;
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnDescendantClick));
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    /// <summary>"single" or "multiple".</summary>
    public string Type
    {
        get => (string)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>Controlled open value for Type="single".</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Controlled open values for Type="multiple".</summary>
    public IReadOnlyList<string> Values
    {
        get => (IReadOnlyList<string>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Single mode only: when false, the sole open item cannot close itself.</summary>
    public bool Collapsible
    {
        get => (bool)GetValue(CollapsibleProperty);
        set => SetValue(CollapsibleProperty, value);
    }

    /// <summary>"vertical" or "horizontal"; drives arrow-key direction.</summary>
    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Reading direction; falls back to FlowDirection when unset.</summary>
    public string? Dir
    {
        get => (string?)GetValue(DirProperty);
        set => SetValue(DirProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public event RoutedEventHandler ValuesChanged
    {
        add => AddHandler(ValuesChangedEvent, value);
        remove => RemoveHandler(ValuesChangedEvent, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        SyncDescendants();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusAccordionAutomationPeer(this);

    private static void OnOpenStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAccordion)d).SyncDescendants();

    private void OnDescendantClick(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled || e.OriginalSource is not NaviusAccordionTrigger trigger || !trigger.IsEnabled)
        {
            return;
        }

        var item = LogicalTreeWalker.Ancestor<NaviusAccordionItem>(trigger);
        if (item is null || !item.IsEnabled)
        {
            return;
        }

        ToggleItem(item.Value);
    }

    private void ToggleItem(string value)
    {
        if (IsMultiple)
        {
            var next = Values.ToList();
            if (!next.Remove(value))
            {
                next.Add(value);
            }

            Values = next;
            RaiseEvent(new RoutedEventArgs(ValuesChangedEvent, this));
        }
        else
        {
            if (string.Equals(Value, value, StringComparison.Ordinal))
            {
                if (Collapsible)
                {
                    Value = null;
                }
                // else: the sole open item cannot close itself.
            }
            else
            {
                Value = value;
            }

            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }
    }

    private bool IsMultiple => string.Equals(Type, "multiple", StringComparison.OrdinalIgnoreCase);

    private void SyncDescendants()
    {
        var items = LogicalTreeWalker.Descendants<NaviusAccordionItem>(this).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            item.Index = i;

            var isOpen = IsMultiple ? Values.Contains(item.Value) : string.Equals(Value, item.Value, StringComparison.Ordinal);

            item.IsEnabledChanged -= OnItemIsEnabledChanged;
            item.IsEnabledChanged += OnItemIsEnabledChanged;

            foreach (var trigger in LogicalTreeWalker.Descendants<NaviusAccordionTrigger>(item))
            {
                trigger.IsPanelOpen = isOpen;
                trigger.IsEnabled = item.IsEnabled;
            }

            foreach (var panel in LogicalTreeWalker.Descendants<NaviusAccordionPanel>(item))
            {
                panel.IsOpen = isOpen;
            }
        }
    }

    private void OnItemIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var item = (NaviusAccordionItem)sender;
        foreach (var trigger in LogicalTreeWalker.Descendants<NaviusAccordionTrigger>(item))
        {
            trigger.IsEnabled = item.IsEnabled;
        }
    }

    // Named distinctly from UIElement.OnPreviewKeyDown so reflection-based test lookups by
    // name stay unambiguous (matches the NaviusRadioGroup convention).
    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var triggers = LogicalTreeWalker.Descendants<NaviusAccordionTrigger>(this).Where(t => t.IsEnabled).ToList();
        if (triggers.Count == 0)
        {
            return;
        }

        var isVertical = !string.Equals(Orientation, "horizontal", StringComparison.OrdinalIgnoreCase);
        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);

        NaviusAccordionTrigger? target = e.Key switch
        {
            Key.Down when isVertical => Move(triggers, 1),
            Key.Up when isVertical => Move(triggers, -1),
            Key.Right when !isVertical => Move(triggers, isRtl ? -1 : 1),
            Key.Left when !isVertical => Move(triggers, isRtl ? 1 : -1),
            Key.Home => triggers.FirstOrDefault(),
            Key.End => triggers.LastOrDefault(),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        // FocusManager.SetFocusedElement both moves real keyboard focus (when connected to
        // a live PresentationSource) and records logical focus for this FocusScope, which
        // is what makes roving focus observable/testable without a live window.
        FocusManager.SetFocusedElement(this, target);
        e.Handled = true;
    }

    /// <summary>Always wraps: the contract's own keyboard table has no non-looping accordion variant.</summary>
    private NaviusAccordionTrigger Move(List<NaviusAccordionTrigger> triggers, int delta)
    {
        var focused = FocusManager.GetFocusedElement(this) as NaviusAccordionTrigger;
        var currentIndex = focused is null ? -1 : triggers.IndexOf(focused);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var index = ((currentIndex + delta) % triggers.Count + triggers.Count) % triggers.Count;
        return triggers[index];
    }
}

internal sealed class NaviusAccordionAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAccordionAutomationPeer(NaviusAccordion owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusAccordion);
}
