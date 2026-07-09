using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.Autocomplete;

/// <summary>
/// Non-generic, lookless Tier B base for <see cref="NaviusAutocomplete{TItem}"/>. Holds every
/// visual/template-bound piece of state so a single <c>Themes/Autocomplete.xaml</c> style can
/// target this type: WPF resolves <c>DefaultStyleKey</c> per closed generic, so the generic
/// subclass points its style key back here and every <c>NaviusAutocomplete&lt;T&gt;</c> shares
/// one template (see docs/parity/autocomplete.md, "WPF implementation notes").
///
/// Composes a <see cref="TextBox"/> (PART_Input) that owns real keyboard focus at all times, and a
/// <see cref="NaviusAnchoredPopup"/> (PART_Popup) hosting an <see cref="ItemsControl"/> (PART_List)
/// of non-focusable rows. The highlighted row is a pure data pointer (<see cref="HighlightedIndex"/>
/// / <see cref="AutocompleteRow.IsHighlighted"/>), never a real focus or selection target -- this is
/// the strict virtual-focus model: focus never leaves the input, not even the roving-tabindex kind.
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartList, Type = typeof(ItemsControl))]
public abstract class NaviusAutocompleteBase : Control
{
    private const string PartInput = "PART_Input";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";
    private const string PartList = "PART_List";

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusAutocompleteBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusAutocompleteBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty HighlightedIndexProperty = DependencyProperty.Register(
        nameof(HighlightedIndex), typeof(int), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(-1, OnHighlightedIndexChanged));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(PlacementSide), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(PlacementAlign), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(PlacementAlign.Start));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset), typeof(double), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(4d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset), typeof(double), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(0d));

    /// <summary>Optional per-row template. When null, the theme's default row template is used.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(null, OnItemTemplateChanged));

    private static readonly DependencyPropertyKey FilteredRowsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FilteredRows), typeof(ObservableCollection<AutocompleteRow>), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FilteredRowsProperty = FilteredRowsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsEmpty), typeof(bool), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(true));

    public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StatusText), typeof(string), typeof(NaviusAutocompleteBase),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusTextProperty = StatusTextPropertyKey.DependencyProperty;

    private const string DefaultRowTemplateKey = "Navius.Autocomplete.DefaultRowTemplate";

    private TextBox? _inputPart;
    private ItemsControl? _listPart;
    private FrameworkElement? _popupContentPart;
    private OverlaySession? _session;
    private bool _isCommitting;
    private bool _themeStyleResolved;

    protected NaviusAutocompleteBase()
    {
        SetValue(FilteredRowsPropertyKey, new ObservableCollection<AutocompleteRow>());
        Loaded += (_, _) => ResolveThemeStyle();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        ResolveThemeStyle();
    }

    /// <summary>
    /// WPF implicit-style lookup keys on the concrete (closed generic) type, and the theme-style
    /// path (DefaultStyleKey) only consults Generic.xaml, which this family deliberately does not
    /// touch. So the shared base-typed style from the consumer-merged Themes/Autocomplete.xaml is
    /// resolved here explicitly, by its base-type key, at Initialized/Loaded time. A locally set
    /// Style always wins.
    /// </summary>
    private void ResolveThemeStyle()
    {
        if (_themeStyleResolved || ReadLocalValue(StyleProperty) != DependencyProperty.UnsetValue)
        {
            return;
        }

        if (TryFindResource(typeof(NaviusAutocompleteBase)) is Style style)
        {
            _themeStyleResolved = true;
            Style = style;
        }
    }

    /// <summary>Controlled/uncontrolled open state (bindable both ways, mirroring the web Open/OpenChanged pair).</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Input text and committed value in one (the web contract maps both onto the root's Value/ValueChanged).</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Index of the highlighted row, or <c>-1</c> for none. A pure data pointer, never a focus target.</summary>
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

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>The current filtered rows the listbox binds to.</summary>
    public ObservableCollection<AutocompleteRow> FilteredRows =>
        (ObservableCollection<AutocompleteRow>)GetValue(FilteredRowsProperty);

    /// <summary>True when there are no filtered rows (drives the "No results" slot).</summary>
    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

    /// <summary>The role="status" text ("{n} result(s)"); also announced via the automation peer.</summary>
    public string StatusText => (string)GetValue(StatusTextProperty);

    /// <summary>
    /// Rebuilds <see cref="FilteredRows"/> from the current items/query. Implemented by the generic
    /// subclass (which owns the typed items, ItemToString and Filter); the base calls it whenever
    /// the query changes or the popup opens.
    /// </summary>
    protected abstract void Recompute();

    /// <summary>
    /// Replaces the filtered rows in place (keeps the same collection instance so the bound
    /// <see cref="ItemsControl"/> updates incrementally), resets the highlight, refreshes the
    /// status text, and announces the result count when the popup is open.
    /// </summary>
    protected void SetRows(IReadOnlyList<AutocompleteRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var collection = FilteredRows;
        collection.Clear();
        foreach (var row in rows)
        {
            collection.Add(row);
        }

        HighlightedIndex = -1;
        SetValue(IsEmptyPropertyKey, rows.Count == 0);
        SetValue(StatusTextPropertyKey, $"{rows.Count} result{(rows.Count == 1 ? string.Empty : "s")}");

        if (IsOpen)
        {
            Announce();
        }
    }

    public override void OnApplyTemplate()
    {
        if (_inputPart is not null)
        {
            _inputPart.PreviewKeyDown -= OnInputPreviewKeyDown;
        }

        if (_listPart is not null)
        {
            _listPart.PreviewMouseLeftButtonUp -= OnListPreviewMouseLeftButtonUp;
            _listPart.PreviewMouseMove -= OnListPreviewMouseMove;
        }

        base.OnApplyTemplate();

        _inputPart = GetTemplateChild(PartInput) as TextBox;
        if (_inputPart is not null)
        {
            _inputPart.PreviewKeyDown += OnInputPreviewKeyDown;
        }

        _listPart = GetTemplateChild(PartList) as ItemsControl;
        if (_listPart is not null)
        {
            _listPart.PreviewMouseLeftButtonUp += OnListPreviewMouseLeftButtonUp;
            _listPart.PreviewMouseMove += OnListPreviewMouseMove;
            ApplyRowTemplate();
        }

        _popupContentPart = GetTemplateChild(PartPopupContent) as FrameworkElement;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusAutocompleteAutomationPeer(this);

    private void ApplyRowTemplate()
    {
        if (_listPart is null)
        {
            return;
        }

        // TemplateBinding would push null when no custom ItemTemplate is set; assign the theme's
        // default row template explicitly instead so rows are always styled (highlight + text).
        _listPart.ItemTemplate = ItemTemplate ?? TryFindResource(DefaultRowTemplateKey) as DataTemplate;
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAutocompleteBase)d).ApplyRowTemplate();

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusAutocompleteBase)d;
        control.Recompute();

        // Typing (a user edit that is not a commit) opens the popup so results are visible.
        if (!control._isCommitting && !control.IsOpen && control.IsInputFocused)
        {
            control.IsOpen = true;
        }
    }

    private bool IsInputFocused => _inputPart is not null && _inputPart.IsKeyboardFocusWithin;

    private static void OnHighlightedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusAutocompleteBase)d;
        var index = (int)e.NewValue;
        foreach (var row in control.FilteredRows)
        {
            row.IsHighlighted = row.Index == index;
        }
    }

    // ----- Keyboard: all handling lives here; focus never leaves PART_Input. -----

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var count = FilteredRows.Count;

        switch (e.Key)
        {
            case Key.Down:
                if (!IsOpen)
                {
                    IsOpen = true;
                }
                else
                {
                    HighlightedIndex = AutocompleteEngine.MoveHighlight(HighlightedIndex, count, +1);
                }

                e.Handled = true;
                break;

            case Key.Up:
                if (!IsOpen)
                {
                    IsOpen = true;
                    HighlightedIndex = FilteredRows.Count - 1;
                }
                else
                {
                    HighlightedIndex = AutocompleteEngine.MoveHighlight(HighlightedIndex, count, -1);
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
                // Close but do NOT mark handled, so normal Tab focus navigation still proceeds.
                if (IsOpen)
                {
                    IsOpen = false;
                }

                break;

            case Key.Home:
            case Key.PageUp:
                if (IsOpen && count > 0)
                {
                    HighlightedIndex = 0;
                    e.Handled = true;
                }

                break;

            case Key.End:
            case Key.PageDown:
                if (IsOpen && count > 0)
                {
                    HighlightedIndex = count - 1;
                    e.Handled = true;
                }

                break;
        }
    }

    private void CommitHighlighted()
    {
        if (HighlightedIndex >= 0 && HighlightedIndex < FilteredRows.Count)
        {
            Commit(FilteredRows[HighlightedIndex]);
        }
    }

    private void Commit(AutocompleteRow row)
    {
        _isCommitting = true;
        try
        {
            Value = row.Text;
            IsOpen = false;
        }
        finally
        {
            _isCommitting = false;
        }
    }

    private void OnListPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var row = FindRow(e.OriginalSource as DependencyObject);
        if (row is not null)
        {
            Commit(row);
            e.Handled = true;
        }
    }

    private void OnListPreviewMouseMove(object sender, MouseEventArgs e)
    {
        var row = FindRow(e.OriginalSource as DependencyObject);
        if (row is not null && row.Index != HighlightedIndex)
        {
            HighlightedIndex = row.Index;
        }
    }

    private static AutocompleteRow? FindRow(DependencyObject? origin)
    {
        var current = origin;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: AutocompleteRow row })
            {
                return row;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    // ----- Open/close: overlay session for outside-press dismissal, virtual focus (no trap). -----

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusAutocompleteBase)d;
        if ((bool)e.NewValue)
        {
            control.Recompute();
            control.OpenCore();
        }
        else
        {
            control.RequestClose();
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

        var stack = OverlayStack.GetFor(window);

        // TrapFocus/RestoreFocus false: virtual focus keeps real keyboard focus pinned to the input,
        // so the overlay must never move focus into the popup nor restore it on close.
        _session = stack.Push(this, new OverlayOptions
        {
            Modal = false,
            CloseOnEscape = true,
            CloseOnOutsideClick = true,
            TrapFocus = false,
            RestoreFocus = false,
        });

        if (_popupContentPart is not null)
        {
            _session.RegisterInputRoot(_popupContentPart);
        }

        _session.Closed += OnSessionClosed;
    }

    private void RequestClose() => _session?.RequestClose(OverlayCloseReason.Programmatic);

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        IsOpen = false;
    }

    private void Announce()
    {
        var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            StatusText,
            Guid.NewGuid().ToString());
    }
}

/// <summary>
/// Lightweight per-row wrapper carried to the listbox template. <see cref="Value"/> is the original
/// item, <see cref="Text"/> its display string, <see cref="Index"/> its position in the filtered
/// list, and <see cref="IsHighlighted"/> the pointer-driven highlight flag (observable so the row
/// template restyles without regenerating the list). Rows are never focus or selection targets.
/// </summary>
public sealed class AutocompleteRow : INotifyPropertyChanged
{
    private bool _isHighlighted;

    public AutocompleteRow(object? value, string text, int index)
    {
        Value = value;
        Text = text;
        Index = index;
    }

    public object? Value { get; }

    public string Text { get; }

    public int Index { get; }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value)
            {
                return;
            }

            _isHighlighted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
