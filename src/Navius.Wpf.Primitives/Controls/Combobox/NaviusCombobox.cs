using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Combobox;

/// <summary>XAML-friendly object-typed Combobox root over the existing generic state machine.</summary>
public class NaviusCombobox : NaviusCombobox<object>
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(NaviusCombobox),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(NaviusCombobox),
        new PropertyMetadata(string.Empty, OnDisplayMemberPathChanged));

    public NaviusCombobox()
    {
        ItemToString = FormatItem;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusCombobox)d;
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            CollectionChangedEventManager.RemoveHandler(oldCollection, control.OnCollectionChanged);
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            CollectionChangedEventManager.AddHandler(newCollection, control.OnCollectionChanged);
        }

        control.RefreshItems();
    }

    private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCombobox)d).ItemToString = ((NaviusCombobox)d).FormatItem;

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshItems();

    private void RefreshItems() =>
        Items = ItemsSource?.Cast<object>().ToArray() ?? Array.Empty<object>();

    private string FormatItem(object item)
    {
        if (string.IsNullOrWhiteSpace(DisplayMemberPath))
        {
            return item?.ToString() ?? string.Empty;
        }

        return item?.GetType().GetProperty(DisplayMemberPath)?.GetValue(item)?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Generic Combobox root. Owns the typed API (Items / Value / Values / ItemToString / Filter) and
/// implements the state machine declared as protected virtuals on <see cref="NaviusComboboxBase"/>,
/// delegating every pure transition to <see cref="ComboboxEngine"/> so the type-agnostic filter /
/// toggle / remove / highlight math stays independently unit-testable.
///
/// DefaultStyleKey is overridden onto the non-generic base type: WPF resolves default styles per
/// closed generic type, so this is what lets every NaviusCombobox&lt;T&gt; instantiation share the
/// single Themes/Combobox.xaml style keyed to NaviusComboboxBase.
/// </summary>
public class NaviusCombobox<TItem> : NaviusComboboxBase
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IReadOnlyList<TItem>), typeof(NaviusCombobox<TItem>),
        new PropertyMetadata(Array.Empty<TItem>(), OnItemsChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(TItem), typeof(NaviusCombobox<TItem>),
        new FrameworkPropertyMetadata(default(TItem), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<TItem>), typeof(NaviusCombobox<TItem>),
        new FrameworkPropertyMetadata(Array.Empty<TItem>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuesChanged));

    public static readonly DependencyProperty ItemToStringProperty = DependencyProperty.Register(
        nameof(ItemToString), typeof(Func<TItem, string>), typeof(NaviusCombobox<TItem>),
        new PropertyMetadata(null, OnFilterInputChanged));

    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(Func<TItem, string, bool>), typeof(NaviusCombobox<TItem>),
        new PropertyMetadata(null, OnFilterInputChanged));

    private static readonly IEqualityComparer<TItem> Comparer = EqualityComparer<TItem>.Default;

    private bool _syncing;

    static NaviusCombobox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCombobox<TItem>),
            new FrameworkPropertyMetadata(typeof(NaviusComboboxBase)));
    }

    public NaviusCombobox()
    {
        Loaded += (_, _) => SyncFromState();
    }

    /// <summary>Raised when the single-select committed value changes.</summary>
    public event EventHandler<TItem?>? ValueChanged;

    /// <summary>Raised when the multi-select committed values list changes.</summary>
    public event EventHandler<IReadOnlyList<TItem>>? ValuesChanged;

    public IReadOnlyList<TItem> Items
    {
        get => (IReadOnlyList<TItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public TItem? Value
    {
        get => (TItem?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IReadOnlyList<TItem> Values
    {
        get => (IReadOnlyList<TItem>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Maps an item to its display label. Defaults to <c>x?.ToString()</c>.</summary>
    public Func<TItem, string>? ItemToString
    {
        get => (Func<TItem, string>?)GetValue(ItemToStringProperty);
        set => SetValue(ItemToStringProperty, value);
    }

    /// <summary>Row-match predicate. Defaults to a case-insensitive substring on the item's label.</summary>
    public Func<TItem, string, bool>? Filter
    {
        get => (Func<TItem, string, bool>?)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    private IReadOnlyList<TItem> SafeItems => Items ?? Array.Empty<TItem>();

    private IReadOnlyList<TItem> SafeValues => Values ?? Array.Empty<TItem>();

    private Func<TItem, string> Stringify => ItemToString ?? (x => x?.ToString() ?? string.Empty);

    // ---- Base virtual overrides: the typed state machine ----

    protected override void RecomputeRows()
    {
        var stringify = Stringify;
        var filtered = ComboboxEngine.Filter(SafeItems, Query, stringify, Filter);

        var rows = new List<ComboboxRowVm>(filtered.Count);
        for (var i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            rows.Add(new ComboboxRowVm(item!, stringify(item), i)
            {
                IsSelected = IsValueSelected(item),
            });
        }

        SetFilteredRows(rows);
    }

    protected override void CommitRow(ComboboxRowVm row)
    {
        var item = (TItem)row.Value;

        if (Multiple)
        {
            SetValuesInternal(ComboboxEngine.ToggleMultiple(SafeValues, item, Comparer));
            SetQuerySilently(string.Empty);
            RecomputeRows();
            // Multi-select keeps the popup open so chips accumulate; single-select closes below.
        }
        else
        {
            SetValueInternal(item);
            SetQuerySilently(Stringify(item));
            IsOpen = false;
        }
    }

    protected override void RemoveSelectedValue(object value)
    {
        var item = (TItem)value;

        if (Multiple)
        {
            SetValuesInternal(ComboboxEngine.RemoveValue(SafeValues, item, Comparer));
            RecomputeRows();
        }
        else
        {
            SetValueInternal(default);
            SetQuerySilently(string.Empty);
            RecomputeRows();
        }
    }

    protected override void RemoveLastSelectedValue()
    {
        SetValuesInternal(ComboboxEngine.RemoveLast(SafeValues));
        RecomputeRows();
    }

    protected override void ClearAll()
    {
        if (Multiple)
        {
            SetValuesInternal(Array.Empty<TItem>());
        }
        else
        {
            SetValueInternal(default);
        }

        SetQuerySilently(string.Empty);
        RecomputeRows();
    }

    protected override void RevertQuery() =>
        SetQuerySilently(Multiple || Value is null ? string.Empty : Stringify(Value));

    protected override void OnUserQueryChanged()
    {
        if (!IsOpen)
        {
            // Setting IsOpen true recomputes rows (via the base OnIsOpenChanged path).
            IsOpen = true;
        }
        else
        {
            RecomputeRows();
        }

        SetHighlightedRow(Rows.Count > 0 ? 0 : -1);
    }

    // ---- Typed setters that also fire the CLR change events and refresh derived UI ----

    private void SetValueInternal(TItem? value)
    {
        _syncing = true;
        try
        {
            Value = value;
        }
        finally
        {
            _syncing = false;
        }

        UpdateSelectionState();
        ValueChanged?.Invoke(this, value);
    }

    private void SetValuesInternal(IReadOnlyList<TItem> values)
    {
        _syncing = true;
        try
        {
            Values = values;
        }
        finally
        {
            _syncing = false;
        }

        UpdateSelectionState();
        ValuesChanged?.Invoke(this, values);
    }

    private void UpdateSelectionState()
    {
        SetHasSelection(Multiple ? SafeValues.Count > 0 : Value is not null);
        RefreshChips();
    }

    private void RefreshChips()
    {
        if (!Multiple)
        {
            SetSelectedChips(Array.Empty<ComboboxChipVm>());
            return;
        }

        var stringify = Stringify;
        SetSelectedChips(SafeValues.Select(v => new ComboboxChipVm(v!, stringify(v))).ToList());
    }

    private bool IsValueSelected(TItem item) =>
        Multiple
            ? SafeValues.Any(v => Comparer.Equals(v, item))
            : Value is not null && Comparer.Equals(Value, item);

    private void SyncFromState()
    {
        if (!Multiple && Value is not null)
        {
            SetQuerySilently(Stringify(Value));
        }

        UpdateSelectionState();
        RecomputeRows();
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCombobox<TItem>)d).RecomputeRows();

    private static void OnFilterInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCombobox<TItem>)d).RecomputeRows();

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusCombobox<TItem>)d;
        if (control._syncing)
        {
            return;
        }

        // External (binding-driven) value change: mirror it into the query label + rows.
        if (!control.Multiple)
        {
            control.SetQuerySilently(control.Value is null ? string.Empty : control.Stringify(control.Value));
        }

        control.UpdateSelectionState();
        control.RecomputeRows();
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusCombobox<TItem>)d;
        if (control._syncing)
        {
            return;
        }

        control.UpdateSelectionState();
        control.RecomputeRows();
    }
}
