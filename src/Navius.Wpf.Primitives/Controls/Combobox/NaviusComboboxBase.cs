using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.Combobox;

/// <summary>
/// Non-generic base for <see cref="NaviusCombobox{TItem}"/>. Holds every dependency property the
/// theme <see cref="System.Windows.Controls.ControlTemplate"/> TemplateBinds, and owns all the
/// WPF plumbing that is independent of the item type: template-part discovery, the single keyboard
/// handler on the input, mouse-driven row selection, the chip-remove command, and the overlay
/// session that drives Escape / outside-press dismissal.
///
/// The type-specific state transitions (filter recompute, value commit, revert-to-label) are
/// declared here as protected virtuals and implemented in the generic subclass, which is the only
/// layer that knows TItem, the typed Filter, and the typed ItemToString.
///
/// Why a non-generic base at all: WPF resolves a control's default style per CLOSED generic type,
/// so a single <c>&lt;Style TargetType&gt;</c> cannot target the open generic. The generic subclass
/// overrides DefaultStyleKeyProperty to point back at THIS base type, so every closed
/// NaviusCombobox&lt;T&gt; instantiation shares one theme style keyed to NaviusComboboxBase.
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartList, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartChips, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartClear, Type = typeof(ButtonBase))]
public abstract class NaviusComboboxBase : Control
{
    private const string PartInput = "PART_Input";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";
    private const string PartList = "PART_List";
    private const string PartChips = "PART_Chips";
    private const string PartClear = "PART_Clear";

    /// <summary>Removes one selected value BY VALUE identity. Bound to each chip's remove button with the chip's value as the parameter, so removal never depends on the chip's displayed index.</summary>
    public static readonly RoutedCommand RemoveChipCommand = new(nameof(RemoveChipCommand), typeof(NaviusComboboxBase));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusComboboxBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty QueryProperty = DependencyProperty.Register(
        nameof(Query), typeof(string), typeof(NaviusComboboxBase),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQueryChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusComboboxBase), new PropertyMetadata(null));

    public static readonly DependencyProperty MultipleProperty = DependencyProperty.Register(
        nameof(Multiple), typeof(bool), typeof(NaviusComboboxBase), new PropertyMetadata(false, OnMultipleChanged));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusComboboxBase), new PropertyMetadata(false));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusComboboxBase), new PropertyMetadata(false));

    public static readonly DependencyProperty HighlightedIndexProperty = DependencyProperty.Register(
        nameof(HighlightedIndex), typeof(int), typeof(NaviusComboboxBase), new PropertyMetadata(-1));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusComboboxBase), new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusComboboxBase), new PropertyMetadata(PlacementAlign.Start));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusComboboxBase), new PropertyMetadata(4d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusComboboxBase), new PropertyMetadata(0d));

    public static readonly DependencyProperty FilteredRowsProperty = DependencyProperty.Register(
        nameof(FilteredRows), typeof(IReadOnlyList<ComboboxRowVm>), typeof(NaviusComboboxBase), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedChipsProperty = DependencyProperty.Register(
        nameof(SelectedChips), typeof(IReadOnlyList<ComboboxChipVm>), typeof(NaviusComboboxBase), new PropertyMetadata(null));

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(NaviusComboboxBase), new PropertyMetadata(null, OnItemTemplateChanged));

    public static readonly DependencyProperty ChipTemplateProperty = DependencyProperty.Register(
        nameof(ChipTemplate), typeof(DataTemplate), typeof(NaviusComboboxBase), new PropertyMetadata(null, OnChipTemplateChanged));

    private static readonly DependencyPropertyKey HasSelectionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasSelection), typeof(bool), typeof(NaviusComboboxBase), new PropertyMetadata(false));

    public static readonly DependencyProperty HasSelectionProperty = HasSelectionPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsEmpty), typeof(bool), typeof(NaviusComboboxBase), new PropertyMetadata(true));

    public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StatusText), typeof(string), typeof(NaviusComboboxBase), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusTextProperty = StatusTextPropertyKey.DependencyProperty;

    private TextBox? _input;
    private ItemsControl? _list;
    private ItemsControl? _chipsList;
    private FrameworkElement? _popupContent;
    private ButtonBase? _clearButton;
    private OverlaySession? _session;
    private bool _suppressQueryCallback;
    private DataTemplate? _defaultRowTemplate;
    private DataTemplate? _defaultChipTemplate;
    private IReadOnlyList<ComboboxRowVm> _rows = Array.Empty<ComboboxRowVm>();

    protected NaviusComboboxBase()
    {
        CommandBindings.Add(new CommandBinding(RemoveChipCommand, OnRemoveChipExecuted));

        // The theme style is keyed to this NON-generic base type, but WPF's implicit style lookup in
        // element/app resources keys off the element's closed generic GetType(), and DefaultStyleKey
        // lookup only searches theme (Generic.xaml) dictionaries, which this family does not merge
        // into (out of scope). A deferred dynamic reference by the base-type key bridges the gap: it
        // resolves against the ambient resource scope (whichever page merged Themes/Combobox.xaml)
        // once the control sits in a tree. A consumer-set Style still wins (it simply replaces this
        // local reference).
        SetResourceReference(StyleProperty, typeof(NaviusComboboxBase));
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Live filter text, tracked separately from the committed value(s).</summary>
    public string Query
    {
        get => (string)GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool Multiple
    {
        get => (bool)GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
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

    /// <summary>The highlighted-row pointer (-1 = none). Virtual focus: this never moves WPF focus.</summary>
    public int HighlightedIndex
    {
        get => (int)GetValue(HighlightedIndexProperty);
        set => SetValue(HighlightedIndexProperty, value);
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

    public IReadOnlyList<ComboboxRowVm>? FilteredRows
    {
        get => (IReadOnlyList<ComboboxRowVm>?)GetValue(FilteredRowsProperty);
        private set => SetValue(FilteredRowsProperty, value);
    }

    public IReadOnlyList<ComboboxChipVm>? SelectedChips
    {
        get => (IReadOnlyList<ComboboxChipVm>?)GetValue(SelectedChipsProperty);
        private set => SetValue(SelectedChipsProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplate? ChipTemplate
    {
        get => (DataTemplate?)GetValue(ChipTemplateProperty);
        set => SetValue(ChipTemplateProperty, value);
    }

    public bool HasSelection => (bool)GetValue(HasSelectionProperty);

    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

    /// <summary>role="status" text ("{n} results"); mirrored to a Polite live region and a UIA notification.</summary>
    public string StatusText => (string)GetValue(StatusTextProperty);

    public override void OnApplyTemplate()
    {
        if (_input is not null)
        {
            _input.PreviewKeyDown -= OnInputPreviewKeyDown;
        }

        if (_list is not null)
        {
            _list.PreviewMouseLeftButtonUp -= OnListMouseUp;
            _list.PreviewMouseMove -= OnListMouseMove;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click -= OnClearClick;
        }

        base.OnApplyTemplate();

        _input = GetTemplateChild(PartInput) as TextBox;
        _list = GetTemplateChild(PartList) as ItemsControl;
        _chipsList = GetTemplateChild(PartChips) as ItemsControl;
        _popupContent = GetTemplateChild(PartPopupContent) as FrameworkElement;
        _clearButton = GetTemplateChild(PartClear) as ButtonBase;

        if (_input is not null)
        {
            _input.PreviewKeyDown += OnInputPreviewKeyDown;
        }

        if (_list is not null)
        {
            _defaultRowTemplate = _list.ItemTemplate;
            _list.PreviewMouseLeftButtonUp += OnListMouseUp;
            _list.PreviewMouseMove += OnListMouseMove;
        }

        if (_chipsList is not null)
        {
            _defaultChipTemplate = _chipsList.ItemTemplate;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click += OnClearClick;
        }

        ApplyItemTemplate();
        ApplyChipTemplate();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusComboboxAutomationPeer(this);

    // ---- Protected surface the generic subclass overrides / calls ----

    /// <summary>Rebuilds <see cref="FilteredRows"/> from the typed items, query, and selection.</summary>
    protected virtual void RecomputeRows()
    {
    }

    /// <summary>Commits a clicked / Enter-highlighted row into the committed value(s).</summary>
    protected virtual void CommitRow(ComboboxRowVm row)
    {
    }

    /// <summary>Removes one committed value by identity (chip remove / Backspace path funnels here).</summary>
    protected virtual void RemoveSelectedValue(object value)
    {
    }

    /// <summary>Clears the whole selection and the filter text.</summary>
    protected virtual void ClearAll()
    {
    }

    /// <summary>Resets the filter text to the committed value's label (single) or empty (multi).</summary>
    protected virtual void RevertQuery()
    {
    }

    /// <summary>Called when the user edits the filter text (not when it is set programmatically).</summary>
    protected virtual void OnUserQueryChanged()
    {
    }

    /// <summary>Called from the generic when the user Backspaces with an empty filter and a selection.</summary>
    protected virtual void RemoveLastSelectedValue()
    {
    }

    /// <summary>Publishes a freshly computed filtered-row list and refreshes the status live region.</summary>
    protected void SetFilteredRows(IReadOnlyList<ComboboxRowVm> rows)
    {
        _rows = rows ?? Array.Empty<ComboboxRowVm>();
        FilteredRows = _rows;
        SetValue(IsEmptyPropertyKey, _rows.Count == 0);

        if (HighlightedIndex >= _rows.Count)
        {
            HighlightedIndex = _rows.Count - 1;
        }

        RefreshHighlightFlags();
        UpdateStatus();
    }

    protected void SetSelectedChips(IReadOnlyList<ComboboxChipVm> chips) => SelectedChips = chips;

    protected void SetHasSelection(bool value) => SetValue(HasSelectionPropertyKey, value);

    /// <summary>Sets <see cref="Query"/> without triggering the user-edit path (used by commit / revert).</summary>
    protected void SetQuerySilently(string value)
    {
        _suppressQueryCallback = true;
        try
        {
            Query = value ?? string.Empty;
        }
        finally
        {
            _suppressQueryCallback = false;
        }
    }

    protected IReadOnlyList<ComboboxRowVm> Rows => _rows;

    /// <summary>Sets the highlighted-row pointer and refreshes row flags (clamped by the caller).</summary>
    protected void SetHighlightedRow(int index) => SetHighlight(index);

    protected void FocusInput() => _input?.Focus();

    // ---- Keyboard: the single handler, mirroring the contract's "all keys live on the input" ----

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Disabled)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                if (!IsOpen)
                {
                    OpenFromKeyboard(highlightLast: false);
                }
                else
                {
                    MoveHighlightBy(1);
                }

                e.Handled = true;
                break;

            case Key.Up:
                if (!IsOpen)
                {
                    OpenFromKeyboard(highlightLast: true);
                }
                else
                {
                    MoveHighlightBy(-1);
                }

                e.Handled = true;
                break;

            case Key.Enter:
                if (IsOpen)
                {
                    CommitHighlighted();
                    e.Handled = true;
                }

                break;

            case Key.Escape:
                if (IsOpen)
                {
                    IsOpen = false;
                    e.Handled = true;
                }

                break;

            case Key.Tab:
                if (IsOpen)
                {
                    // Close + revert, but do NOT mark handled: real Tab focus navigation must still
                    // proceed (mirrors the web contract's note that Tab does not preventDefault).
                    IsOpen = false;
                }

                break;

            case Key.Back:
                if (Multiple && string.IsNullOrEmpty(Query) && HasSelection)
                {
                    RemoveLastSelectedValue();
                    e.Handled = true;
                }

                break;

            case Key.Home:
            case Key.PageUp:
                if (IsOpen)
                {
                    HighlightEdge(first: true);
                    e.Handled = true;
                }

                break;

            case Key.End:
            case Key.PageDown:
                if (IsOpen)
                {
                    HighlightEdge(first: false);
                    e.Handled = true;
                }

                break;
        }
    }

    private void OpenFromKeyboard(bool highlightLast)
    {
        IsOpen = true;
        if (highlightLast && _rows.Count > 0)
        {
            SetHighlight(_rows.Count - 1);
        }
    }

    private void MoveHighlightBy(int delta) =>
        SetHighlight(ComboboxEngine.MoveHighlight(HighlightedIndex, _rows.Count, delta));

    private void HighlightEdge(bool first) =>
        SetHighlight(_rows.Count == 0 ? -1 : first ? 0 : _rows.Count - 1);

    private void CommitHighlighted()
    {
        if (HighlightedIndex >= 0 && HighlightedIndex < _rows.Count)
        {
            CommitRow(_rows[HighlightedIndex]);
        }
    }

    private void SetHighlight(int index)
    {
        HighlightedIndex = index;
        RefreshHighlightFlags();
        ScrollHighlightIntoView();
    }

    private void RefreshHighlightFlags()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            _rows[i].IsHighlighted = i == HighlightedIndex;
        }
    }

    private void ScrollHighlightIntoView()
    {
        if (_list is null || HighlightedIndex < 0 || HighlightedIndex >= _rows.Count)
        {
            return;
        }

        var container = _list.ItemContainerGenerator.ContainerFromIndex(HighlightedIndex) as FrameworkElement;
        container?.BringIntoView();
    }

    // ---- Mouse: click a row to commit, hover to highlight ----

    private void OnListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Disabled || _list is null || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (_list.ContainerFromElement(source) is FrameworkElement { DataContext: ComboboxRowVm row })
        {
            CommitRow(row);
            e.Handled = true;
        }
    }

    private void OnListMouseMove(object sender, MouseEventArgs e)
    {
        if (_list is null || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (_list.ContainerFromElement(source) is FrameworkElement { DataContext: ComboboxRowVm row }
            && row.Index != HighlightedIndex)
        {
            SetHighlight(row.Index);
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        ClearAll();
        FocusInput();
    }

    private void OnRemoveChipExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (Disabled || ReadOnly || e.Parameter is null)
        {
            return;
        }

        RemoveSelectedValue(e.Parameter);
        FocusInput();
    }

    // ---- Open / close lifecycle + overlay session (non-modal, virtual focus: no focus trap) ----

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusComboboxBase)d;
        if ((bool)e.NewValue)
        {
            control.RecomputeRows();
            control.OpenCore();
        }
        else
        {
            control.CloseCore();
        }
    }

    private void OpenCore()
    {
        if (_session is not null)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        var root = _popupContent ?? _list;
        if (root is null)
        {
            return;
        }

        var stack = OverlayStack.GetFor(window);
        _session = stack.Push(root, new OverlayOptions
        {
            Modal = false,
            CloseOnEscape = false, // Escape is handled on the input so it can also revert the query.
            CloseOnOutsideClick = true,
            TrapFocus = false,     // Virtual focus: DOM/real focus never leaves the input.
            RestoreFocus = false,
        });

        _session.RegisterInputRoot(root);
        _session.Closed += OnSessionClosed;
    }

    private void CloseCore()
    {
        RevertQuery();
        HighlightedIndex = -1;
        RefreshHighlightFlags();
        _session?.RequestClose(OverlayCloseReason.Programmatic);
    }

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        if (IsOpen)
        {
            IsOpen = false;
        }
    }

    private void UpdateStatus()
    {
        var text = $"{_rows.Count} result{(_rows.Count == 1 ? string.Empty : "s")}";
        SetValue(StatusTextPropertyKey, text);

        if (!IsLoaded)
        {
            return;
        }

        var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            text,
            "NaviusComboboxStatus");
    }

    private static void OnQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusComboboxBase)d;
        if (control._suppressQueryCallback)
        {
            return;
        }

        control.OnUserQueryChanged();
    }

    private static void OnMultipleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusComboboxBase)d).RecomputeRows();

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusComboboxBase)d).ApplyItemTemplate();

    private static void OnChipTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusComboboxBase)d).ApplyChipTemplate();

    // A custom ItemTemplate/ChipTemplate is swapped in via code rather than an ancestor-bound trigger
    // in the row DataTemplate, because RelativeSource FindAncestor cannot cross the Popup's separate
    // visual tree to reach this control. Null restores the theme's default template.
    private void ApplyItemTemplate()
    {
        if (_list is not null)
        {
            _list.ItemTemplate = ItemTemplate ?? _defaultRowTemplate;
        }
    }

    private void ApplyChipTemplate()
    {
        if (_chipsList is not null)
        {
            _chipsList.ItemTemplate = ChipTemplate ?? _defaultChipTemplate;
        }
    }
}

/// <summary>
/// Minimal UIA peer: reports ControlType.ComboBox and implements ExpandCollapse so the input's
/// aria-expanded maps to a real UIA pattern. The web's aria-activedescendant virtual-focus target
/// has no first-class WPF equivalent and is NOT reproduced here (see docs/parity/combobox.md WPF
/// notes for the documented gap); the highlighted row is announced through the status live region
/// instead.
/// </summary>
internal sealed class NaviusComboboxAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    private readonly NaviusComboboxBase _owner;

    public NaviusComboboxAutomationPeer(NaviusComboboxBase owner) : base(owner)
    {
        _owner = owner;
    }

    public ExpandCollapseState ExpandCollapseState =>
        _owner.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    protected override string GetClassNameCore() => nameof(NaviusComboboxBase);

    // The Disabled DP is the combobox's semantic disabled state (the keyboard/mouse handlers all
    // gate on it); it is independent of the inherited IsEnabled that FrameworkElementAutomationPeer
    // reflects by default. Fold it in so a Disabled combobox reports IsEnabled == false to UIA.
    protected override bool IsEnabledCore() => base.IsEnabledCore() && !_owner.Disabled;

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

    public void Expand()
    {
        ThrowIfDisabled();
        _owner.IsOpen = true;
    }

    public void Collapse()
    {
        ThrowIfDisabled();
        _owner.IsOpen = false;
    }

    // A disabled combobox must not be operable through UIA, matching NaviusNumberFieldAutomationPeer.
    private void ThrowIfDisabled()
    {
        if (_owner.Disabled || !_owner.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }
    }
}
