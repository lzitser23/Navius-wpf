using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Select;

/// <summary>
/// A single option row inside a <see cref="NaviusSelectBase"/> listbox (contract's
/// NaviusSelectItem, role="option"). Tier B: derives from <see cref="ContentControl"/> rather
/// than ListBoxItem/ComboBoxItem, because the owning Select is not a WPF Selector and drives
/// selection itself by walking its containers (mirroring NaviusRadioGroup.SyncCheckedFromValue),
/// so <see cref="IsSelectedValue"/> is a plain owner-set property, not Selector.IsSelected.
///
/// The contract's per-item ChildContent/ItemText/ItemIndicator parts collapse onto this one
/// control: the label is <see cref="TextValue"/> (also the type-ahead/trigger-label text), the
/// selected check glyph is a template trigger keyed on <see cref="IsSelectedValue"/>, and the
/// roving-focus data-highlighted state is the owner-set <see cref="IsHighlightedValue"/> (WPF
/// focus stays on the trigger, so highlight is visual-only rather than a real focus move; see
/// docs/parity/select.md "WPF implementation notes").
///
/// ContentControl-derived so the owner's <see cref="ItemsControl.ItemTemplate"/> (stamped onto
/// <see cref="ContentControl.ContentTemplate"/> by container preparation) renders arbitrary row
/// visuals; without a template the row renders the plain <see cref="DisplayText"/> label, and
/// <see cref="DisplayText"/> always powers the trigger label and type-ahead regardless.
/// </summary>
public class NaviusSelectItem : ContentControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(object), typeof(NaviusSelectItem),
        new PropertyMetadata(null, OnValueOrTextChanged));

    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue), typeof(string), typeof(NaviusSelectItem),
        new PropertyMetadata(null, OnValueOrTextChanged));

    public static readonly DependencyProperty IsSelectedValueProperty = DependencyProperty.Register(
        nameof(IsSelectedValue), typeof(bool), typeof(NaviusSelectItem),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsHighlightedValueProperty = DependencyProperty.Register(
        nameof(IsHighlightedValue), typeof(bool), typeof(NaviusSelectItem),
        new PropertyMetadata(false));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusSelectItem),
        new PropertyMetadata(false));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(NaviusSelectItem),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    /// <summary>Cancelable activation event (contract's OnSelect); the owner commits only if not prevented.</summary>
    public static readonly RoutedEvent SelectEvent = EventManager.RegisterRoutedEvent(
        nameof(Select), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusSelectItem));

    static NaviusSelectItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusSelectItem), new FrameworkPropertyMetadata(typeof(NaviusSelectItem)));
        // Focus stays on the Select trigger; items are visually highlighted, never focused, so
        // key events keep tunneling through the owner's PreviewKeyDown handler.
        FocusableProperty.OverrideMetadata(typeof(NaviusSelectItem), new FrameworkPropertyMetadata(false));
    }

    public NaviusSelectItem()
    {
        UpdateDisplayText();
    }

    /// <summary>The opaque value key this option commits (contract's Value; object-typed, cast by the owner).</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Display/type-ahead text; falls back to <see cref="Value"/>'s string form when null.</summary>
    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    /// <summary>Owner-set selected flag (contract's data-selected/aria-selected); not Selector.IsSelected.</summary>
    public bool IsSelectedValue
    {
        get => (bool)GetValue(IsSelectedValueProperty);
        set => SetValue(IsSelectedValueProperty, value);
    }

    /// <summary>Owner-set roving-highlight flag (contract's data-highlighted).</summary>
    public bool IsHighlightedValue
    {
        get => (bool)GetValue(IsHighlightedValueProperty);
        set => SetValue(IsHighlightedValueProperty, value);
    }

    /// <summary>Skipped by roving highlight and selection (contract's Disabled); combines with inherited IsEnabled.</summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    /// <summary>Resolved label (TextValue, else Value's string form): the template binds to this and the owner reads it for the trigger label and type-ahead.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    public event RoutedEventHandler Select
    {
        add => AddHandler(SelectEvent, value);
        remove => RemoveHandler(SelectEvent, value);
    }

    /// <summary>
    /// Back-reference to the owning Select, stamped by the owner when this item joins its Items.
    /// Used so activation and hover reach the owner directly, without depending on routed-event
    /// bubbling crossing the popup's separate HwndSource or on container generation having run
    /// (which matters for headless unit tests where no template/panel is realized).
    /// </summary>
    internal NaviusSelectBase? OwnerSelect { get; set; }

    /// <summary>True when the item can be highlighted/selected (enabled and not <see cref="Disabled"/>).</summary>
    public bool IsNavigable => IsEnabled && !Disabled;

    /// <summary>
    /// Raises the cancelable <see cref="Select"/> event (consumers may <c>PreventDefault</c>), then
    /// hands the final args to the owner, which commits only when not prevented. Returns the args.
    /// Called by both the mouse-activation path here and the owner's keyboard-activation path.
    /// </summary>
    public NaviusSelectEventArgs RaiseSelectEvent()
    {
        var args = new NaviusSelectEventArgs(SelectEvent, this);
        RaiseEvent(args);
        OwnerSelect?.OnItemActivated(this, args);
        return args;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsNavigable)
        {
            return;
        }

        RaiseSelectEvent();
        e.Handled = true;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (IsNavigable)
        {
            OwnerSelect?.HighlightItem(this);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSelectItemAutomationPeer(this);

    private static void OnValueOrTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusSelectItem)d).UpdateDisplayText();

    private void UpdateDisplayText()
    {
        var text = TextValue ?? Value?.ToString() ?? string.Empty;
        SetValue(DisplayTextPropertyKey, text);
    }
}

internal sealed class NaviusSelectItemAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSelectItemAutomationPeer(NaviusSelectItem owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override string GetClassNameCore() => nameof(NaviusSelectItem);

    protected override string GetNameCore()
    {
        var owner = (NaviusSelectItem)Owner;
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? owner.DisplayText : name;
    }
}
