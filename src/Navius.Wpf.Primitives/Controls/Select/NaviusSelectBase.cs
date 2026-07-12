using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.Select;

/// <summary>
/// Tier B lookless listbox-select control (contract's NaviusSelect + Trigger + Value + Popup,
/// collapsed onto one templated ItemsControl). Derives from <see cref="ItemsControl"/> rather
/// than ComboBox because ComboBox's built-in keyboard/selection internals fight the contract's
/// exact semantics (open-on-ArrowUp lands on the LAST option, Loop default false, and multi-select
/// toggle-and-stay-open which ComboBox has no native equivalent for): a fully custom control is
/// more deterministic than fighting ComboBoxItem's selection plumbing. See docs/parity/select.md
/// "WPF implementation notes" for the full decision record and the resulting AutomationPeer gap
/// (no free ComboBoxAutomationPeer).
///
/// This is the non-generic base so a single theme <c>Style TargetType</c> can target it: WPF
/// resolves DefaultStyleKey per closed generic type, so every closed <see cref="NaviusSelect{T}"/>
/// points its DefaultStyleKey at this base and shares one style. All template-bound state lives
/// here; the generic subclass only adds TItem-typed convenience wrappers over the object-typed
/// storage below.
///
/// Keyboard is owned entirely by <see cref="HandlePreviewKeyDown"/> (attached in the constructor):
/// focus stays on PART_Trigger, items are non-focusable and merely highlighted, so keys keep
/// tunnelling through this control. Escape-close is handled there directly; outside-press-close is
/// delegated to <see cref="OverlayStack"/> (both PART_Trigger and PART_PopupContent are registered
/// as input roots so a press on either counts as "inside" and does not trigger an outside-close).
/// </summary>
[TemplatePart(Name = PartTrigger, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
public abstract class NaviusSelectBase : ItemsControl
{
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusSelectBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty MultipleProperty = DependencyProperty.Register(
        nameof(Multiple), typeof(bool), typeof(NaviusSelectBase),
        new PropertyMetadata(false, OnSelectionModeChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusSelectBase),
        new PropertyMetadata(null, OnPlaceholderChanged));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop), typeof(bool), typeof(NaviusSelectBase),
        new PropertyMetadata(false));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusSelectBase),
        new PropertyMetadata(PlacementSide.Bottom));

    // Select overrides the base Positioner's DefaultAlign to Start (vs. the shared "center").
    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusSelectBase),
        new PropertyMetadata(PlacementAlign.Start));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusSelectBase),
        new PropertyMetadata(6d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusSelectBase),
        new PropertyMetadata(0d));

    // Simple marker properties for API parity (contract's Name/Required); no native-form wiring
    // (NaviusBubbleInput / hidden input mirrors are dropped per this repo's precedent).
    public static new readonly DependencyProperty NameProperty = DependencyProperty.Register(
        nameof(Name), typeof(string), typeof(NaviusSelectBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusSelectBase),
        new PropertyMetadata(false));

    /// <summary>Single-select storage (contract's Value); the generic subclass wraps it as TItem.</summary>
    public static readonly DependencyProperty RawValueProperty = DependencyProperty.Register(
        nameof(RawValue), typeof(object), typeof(NaviusSelectBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

    /// <summary>Multi-select storage (contract's Values); the generic subclass wraps it as IReadOnlyList&lt;TItem&gt;.</summary>
    public static readonly DependencyProperty RawValuesProperty = DependencyProperty.Register(
        nameof(RawValues), typeof(IReadOnlyList<object>), typeof(NaviusSelectBase),
        new FrameworkPropertyMetadata(Array.Empty<object>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

    // Real DPs registered under the names "Value"/"Values" -- RawValue/RawValues are registered
    // under the DP names "RawValue"/"RawValues", so a `Value="{Binding ...}"` XAML attribute has no
    // DependencyProperty to resolve to and WPF throws at load. These mirror RawValue/RawValues
    // (kept in sync below, both directions guarded against re-entry) purely so XAML/TypeDescriptor
    // lookups for "Value"/"Values" succeed; RawValue/RawValues remain the storage CommitItem writes.
    // Registered once here (owner NaviusSelectBase) rather than per closed generic so every
    // subclass -- the non-generic NaviusSelect and every NaviusSelect&lt;TItem&gt; instantiation --
    // picks them up via the inherited DependencyProperty name lookup.
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        "Value", typeof(object), typeof(NaviusSelectBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged));

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        "Values", typeof(IReadOnlyList<object>), typeof(NaviusSelectBase),
        new FrameworkPropertyMetadata(Array.Empty<object>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuesPropertyChanged));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(NaviusSelectBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasSelectionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasSelection), typeof(bool), typeof(NaviusSelectBase),
        new PropertyMetadata(false));

    public static readonly DependencyProperty HasSelectionProperty = HasSelectionPropertyKey.DependencyProperty;

    /// <summary>Fires on every single-select commit (contract's ValueChanged). Bubbling routed event; the generic subclass also raises a TItem-typed CLR event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusSelectBase));

    /// <summary>Fires on every multi-select toggle (contract's ValuesChanged).</summary>
    public static readonly RoutedEvent ValuesChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValuesChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusSelectBase));

    private ToggleButton? _triggerPart;
    private FrameworkElement? _popupContentPart;
    private OverlaySession? _session;
    private NaviusSelectItem? _highlighted;
    private bool _syncing;
    private bool _syncingValue;
    private bool _syncingValues;

    protected NaviusSelectBase()
    {
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool Multiple
    {
        get => (bool)GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>When true, arrow navigation wraps at the ends; false (the Select default) clamps.</summary>
    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
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

    /// <summary>Marker only (no native-form mirror); kept for API parity.</summary>
    public new string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>Marker only (no native validation wiring); kept for API parity.</summary>
    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public object? RawValue
    {
        get => GetValue(RawValueProperty);
        set => SetValue(RawValueProperty, value);
    }

    public IReadOnlyList<object> RawValues
    {
        get => (IReadOnlyList<object>)GetValue(RawValuesProperty);
        set => SetValue(RawValuesProperty, value);
    }

    /// <summary>XAML-bindable mirror of <see cref="RawValue"/>; see <see cref="ValueProperty"/>. The generic subclass hides this with a TItem-typed wrapper.</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>XAML-bindable mirror of <see cref="RawValues"/>; see <see cref="ValueProperty"/>. The generic subclass hides this with an IReadOnlyList&lt;TItem&gt;-typed wrapper.</summary>
    public IReadOnlyList<object> Values
    {
        get => (IReadOnlyList<object>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value ?? Array.Empty<object>());
    }

    /// <summary>Resolved trigger label: the selected item's text, joined for multi-select, or the placeholder.</summary>
    public string? DisplayText => (string?)GetValue(DisplayTextProperty);

    /// <summary>True when something is selected (contract's HasSelection; drives the data-placeholder styling).</summary>
    public bool HasSelection => (bool)GetValue(HasSelectionProperty);

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

    protected override DependencyObject GetContainerForItemOverride() => new NaviusSelectItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusSelectItem;

    protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        StampOwners();
        SyncSelectionStates();
        UpdateDisplay();
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is NaviusSelectItem container)
        {
            container.OwnerSelect = this;
            if (item is not NaviusSelectItem)
            {
                container.Value = item;
                container.TextValue = ResolveDisplayText(item);
            }

            container.IsSelectedValue = Multiple
                ? RawValues.Any(value => Equals(value, container.Value))
                : Equals(RawValue, container.Value);
            UpdateDisplay();
        }
    }

    // Stamps the owner back-reference on every current container so activation/hover reach this
    // control directly, independent of container generation or routed-event bubbling.
    private void StampOwners()
    {
        foreach (var item in GetItems())
        {
            item.OwnerSelect = this;
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _triggerPart = GetTemplateChild(PartTrigger) as ToggleButton;
        _popupContentPart = GetTemplateChild(PartPopupContent) as FrameworkElement;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSelectAutomationPeer(this);

    /// <summary>Raised after each single-select commit; the generic subclass overrides to also fire its typed event.</summary>
    protected virtual void OnValueCommitted() => RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));

    /// <summary>Raised after each multi-select toggle; the generic subclass overrides to also fire its typed event.</summary>
    protected virtual void OnValuesCommitted() => RaiseEvent(new RoutedEventArgs(ValuesChangedEvent, this));

    /// <summary>Sets the roving highlight to <paramref name="item"/> (used by mouse hover and the keyboard handler).</summary>
    internal void HighlightItem(NaviusSelectItem? item)
    {
        if (ReferenceEquals(_highlighted, item))
        {
            return;
        }

        if (_highlighted is not null)
        {
            _highlighted.IsHighlightedValue = false;
        }

        _highlighted = item;

        if (_highlighted is not null)
        {
            _highlighted.IsHighlightedValue = true;
        }
    }

    private List<NaviusSelectItem> GetItems()
    {
        var result = new List<NaviusSelectItem>(Items.Count);
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index] as NaviusSelectItem
                ?? ItemContainerGenerator.ContainerFromIndex(index) as NaviusSelectItem;
            if (item is not null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private string ResolveDisplayText(object item)
    {
        if (string.IsNullOrWhiteSpace(DisplayMemberPath))
        {
            return item.ToString() ?? string.Empty;
        }

        // Dotted property paths ("Owner.Name") per WPF's DisplayMemberPath convention; indexers
        // and attached properties are not supported (see docs/parity/select.md).
        object? current = item;
        foreach (var segment in DisplayMemberPath.Split('.'))
        {
            current = current?.GetType().GetProperty(segment)?.GetValue(current);
        }

        return current?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Re-resolves the label of every data-bound container (declared NaviusSelectItems own their
    /// TextValue) and the trigger label, so a DisplayMemberPath change re-renders live.
    /// </summary>
    protected override void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
    {
        base.OnDisplayMemberPathChanged(oldDisplayMemberPath, newDisplayMemberPath);
        for (var index = 0; index < Items.Count; index++)
        {
            if (Items[index] is not NaviusSelectItem
                && ItemContainerGenerator.ContainerFromIndex(index) is NaviusSelectItem container)
            {
                container.TextValue = ResolveDisplayText(Items[index]!);
            }
        }

        UpdateDisplay();
    }

    private List<NaviusSelectItem> GetNavigableItems() =>
        GetItems().Where(i => i.IsNavigable).ToList();

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusSelectBase)d;
        if ((bool)e.NewValue)
        {
            control.OpenCore();
        }
        else
        {
            control.CloseCore();
        }
    }

    private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusSelectBase)d;
        control.SyncSelectionStates();
        control.UpdateDisplay();
    }

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusSelectBase)d).UpdateDisplay();

    private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusSelectBase)d;
        control.SyncMirroredProperty(e.Property, e.NewValue);

        if (control._syncing)
        {
            return;
        }

        control.SyncSelectionStates();
        control.UpdateDisplay();
    }

    // Pushes a RawValue/RawValues change into its Value/Values mirror (see ValueProperty). Runs
    // unconditionally -- unlike the _syncing-guarded block above -- so CommitItem's direct
    // RawValue/RawValues writes (which set _syncing to suppress a redundant SyncSelectionStates
    // call) still propagate out to a two-way bound Value/Values source.
    private void SyncMirroredProperty(DependencyProperty changed, object? newValue)
    {
        if (changed == RawValueProperty && !_syncingValue)
        {
            _syncingValue = true;
            SetValue(ValueProperty, newValue);
            _syncingValue = false;
        }
        else if (changed == RawValuesProperty && !_syncingValues)
        {
            _syncingValues = true;
            SetValue(ValuesProperty, newValue ?? Array.Empty<object>());
            _syncingValues = false;
        }
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusSelectBase)d;
        if (control._syncingValue)
        {
            return;
        }

        control._syncingValue = true;
        control.RawValue = e.NewValue;
        control._syncingValue = false;
    }

    private static void OnValuesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusSelectBase)d;
        if (control._syncingValues)
        {
            return;
        }

        control._syncingValues = true;
        control.RawValues = (IReadOnlyList<object>)e.NewValue ?? Array.Empty<object>();
        control._syncingValues = false;
    }

    /// <summary>
    /// Called by <see cref="NaviusSelectItem.RaiseSelectEvent"/> after the cancelable event has run
    /// (so <paramref name="args"/> reflects any consumer <c>PreventDefault</c>). Commits unless prevented.
    /// </summary>
    internal void OnItemActivated(NaviusSelectItem item, NaviusSelectEventArgs args)
    {
        if (args.IsDefaultPrevented)
        {
            return;
        }

        CommitItem(item);
    }

    private void CommitItem(NaviusSelectItem item)
    {
        if (Multiple)
        {
            _syncing = true;
            RawValues = SelectSelectionEngine.ToggleMultiple(RawValues, item.Value!);
            _syncing = false;
            SyncSelectionStates();
            UpdateDisplay();
            OnValuesCommitted();
            HighlightItem(item);
            return;
        }

        _syncing = true;
        RawValue = item.Value;
        _syncing = false;
        SyncSelectionStates();
        UpdateDisplay();
        OnValueCommitted();
        IsOpen = false;
    }

    private void SyncSelectionStates()
    {
        var values = RawValues;
        foreach (var item in GetItems())
        {
            item.IsSelectedValue = Multiple
                ? values.Any(v => Equals(v, item.Value))
                : Equals(RawValue, item.Value);
        }
    }

    private void UpdateDisplay()
    {
        string? text;
        bool hasSelection;

        if (Multiple)
        {
            var selected = GetItems().Where(i => i.IsSelectedValue).ToList();
            hasSelection = RawValues.Count > 0;
            text = hasSelection
                ? string.Join(", ", RawValues.Select(value =>
                    selected.FirstOrDefault(item => Equals(item.Value, value))?.DisplayText ?? ResolveDisplayText(value)))
                : Placeholder;
        }
        else
        {
            var selected = GetItems().FirstOrDefault(i => Equals(RawValue, i.Value));
            hasSelection = RawValue is not null;
            text = selected?.DisplayText ?? (hasSelection ? ResolveDisplayText(RawValue!) : Placeholder);
        }

        SetValue(DisplayTextPropertyKey, text);
        SetValue(HasSelectionPropertyKey, hasSelection);
    }

    private void OpenCore()
    {
        EngageOverlay();
        HighlightItem(ResolveSelectedOrFirst());
    }

    private void CloseCore()
    {
        HighlightItem(null);
        CloseOverlay();
        _triggerPart?.Focus();
    }

    private NaviusSelectItem? ResolveSelectedOrFirst()
    {
        var navigable = GetNavigableItems();
        if (navigable.Count == 0)
        {
            return null;
        }

        var selected = navigable.FirstOrDefault(i => i.IsSelectedValue);
        return selected ?? navigable[0];
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
            CloseOnEscape = false, // Escape is owned by HandlePreviewKeyDown (single source of truth).
            CloseOnOutsideClick = true,
            TrapFocus = false, // A listbox manages focus with roving highlight, not a focus trap.
            RestoreFocus = false,
        });

        _session.RegisterInputRoot(_popupContentPart);
        if (_triggerPart is not null)
        {
            // Registering the trigger as "inside" stops an outside-press-close from racing the
            // trigger's own toggle when the user clicks the trigger to close (which would otherwise
            // close then immediately reopen).
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
    // unambiguous (same convention as NaviusRadioGroup.HandlePreviewKeyDown).
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
                    OpenWithHighlight(landOnLast: false);
                    e.Handled = true;
                    break;
                case Key.Up:
                    OpenWithHighlight(landOnLast: true);
                    e.Handled = true;
                    break;
            }

            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                MoveHighlight(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveHighlight(-1);
                e.Handled = true;
                break;
            case Key.Home:
                HighlightEdge(first: true);
                e.Handled = true;
                break;
            case Key.End:
                HighlightEdge(first: false);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                _highlighted?.RaiseSelectEvent();
                e.Handled = true;
                break;
            case Key.Escape:
                IsOpen = false;
                e.Handled = true;
                break;
            default:
                if (TryGetTypeaheadChar(e.Key, out var ch))
                {
                    Typeahead(ch);
                    e.Handled = true;
                }

                break;
        }
    }

    private void OpenWithHighlight(bool landOnLast)
    {
        IsOpen = true; // OpenCore highlights the selected-or-first option; override per closed-trigger table.
        var navigable = GetNavigableItems();
        if (navigable.Count == 0)
        {
            return;
        }

        HighlightItem(landOnLast ? navigable[^1] : navigable[0]);
    }

    private void MoveHighlight(int delta)
    {
        var navigable = GetNavigableItems();
        var current = _highlighted is null ? -1 : navigable.IndexOf(_highlighted);
        var next = SelectSelectionEngine.MoveHighlight(current, navigable.Count, delta, Loop);
        if (next >= 0)
        {
            HighlightItem(navigable[next]);
        }
    }

    private void HighlightEdge(bool first)
    {
        var navigable = GetNavigableItems();
        if (navigable.Count == 0)
        {
            return;
        }

        HighlightItem(first ? navigable[0] : navigable[^1]);
    }

    private void Typeahead(char ch)
    {
        var navigable = GetNavigableItems();
        if (navigable.Count == 0)
        {
            return;
        }

        var texts = navigable.Select(i => i.DisplayText).ToList();
        var current = _highlighted is null ? -1 : navigable.IndexOf(_highlighted);
        var match = SelectSelectionEngine.FindTypeaheadMatch(texts, current, ch);
        if (match is int index)
        {
            HighlightItem(navigable[index]);
        }
    }

    private static bool TryGetTypeaheadChar(Key key, out char ch)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            ch = (char)('a' + (key - Key.A));
            return true;
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            ch = (char)('0' + (key - Key.D0));
            return true;
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            ch = (char)('0' + (key - Key.NumPad0));
            return true;
        }

        ch = '\0';
        return false;
    }
}

/// <summary>
/// Peer reporting <see cref="AutomationControlType.ComboBox"/> for the Select root (items
/// report <see cref="AutomationControlType.ListItem"/> via NaviusSelectItemAutomationPeer). Because
/// this control is not ComboBox-derived, the free ComboBoxAutomationPeer is unavailable, so the
/// two patterns a reader actually needs are provided here: a read-only ValuePattern surfacing
/// <see cref="NaviusSelectBase.DisplayText"/> (the M3 keyboard-matrix e2e showed the control
/// otherwise exposes NOTHING readable over UIA) and ExpandCollapse over IsOpen.
/// ISelectionProvider remains an open gap noted in the parity doc.
/// </summary>
internal sealed class NaviusSelectAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider
{
    public NaviusSelectAutomationPeer(NaviusSelectBase owner) : base(owner)
    {
    }

    private NaviusSelectBase Select => (NaviusSelectBase)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    protected override string GetClassNameCore() => nameof(NaviusSelectBase);

    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Value or PatternInterface.ExpandCollapse => this,
        _ => base.GetPattern(patternInterface),
    };

    public bool IsReadOnly => true;

    public string Value => Select.DisplayText ?? string.Empty;

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusSelect is read-only over ValuePattern; change selection via keyboard or items.");

    public ExpandCollapseState ExpandCollapseState =>
        Select.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand()
    {
        if (Select.IsEnabled)
        {
            Select.IsOpen = true;
        }
    }

    public void Collapse() => Select.IsOpen = false;
}
