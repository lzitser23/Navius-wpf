using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfDataGrid = System.Windows.Controls.DataGrid;

namespace Navius.Wpf.Primitives.Controls.DataGrid;

/// <summary>
/// Tier A: derives from the native <see cref="System.Windows.Controls.DataGrid"/> (see
/// docs/parity/data-grid.md "WPF strategy"). The Blazor NaviusDataGrid&lt;TItem&gt; is a pure
/// headless state engine (global filter, single-column sort, pagination, row-selection,
/// column-visibility). WPF's DataGrid already ships a full grid with its own
/// DataGridAutomationPeer (AutomationControlType.DataGrid), ICollectionView-backed
/// filtering/sorting, native row/column virtualization, native SelectedItems/SelectionMode, and
/// native DataGridColumn.Visibility, so this port is a re-template plus a thin state surface, not a
/// re-implementation of the state engine.
///
/// This class adds only what the native grid lacks: a grid-level GlobalFilter (mirroring the web's
/// stringified, case-insensitive Contains match, with a FilterFn override hook) and a
/// RowKeySelector (the web's RowKey, falling back to the row object as its own key). Sorting,
/// selection, and column-visibility ride entirely on the native surface.
///
/// Deliberate delta: pagination is NOT reimplemented. Native WPF DataGrid has no built-in pager,
/// and the brief scopes this work as "re-template plus a thin state surface", so the web's
/// pagination slice (including the PageSize&lt;=0 "show all" special case) is out of scope for this
/// pass. See docs/parity/data-grid.md "WPF implementation notes".
/// </summary>
public class NaviusDataGrid : WpfDataGrid
{
    /// <summary>
    /// Controlled global filter text. When non-empty, applies a predicate to the default
    /// <see cref="ICollectionView"/> over <see cref="ItemsControl.ItemsSource"/>. Mirrors the web's
    /// stringified global filter. Null/empty clears the filter.
    /// </summary>
    public static readonly DependencyProperty GlobalFilterProperty = DependencyProperty.Register(
        nameof(GlobalFilter), typeof(string), typeof(NaviusDataGrid),
        new PropertyMetadata(null, OnFilterInputChanged));

    /// <summary>
    /// Optional per-instance override for the match predicate: (row, filterText) =&gt; bool. When
    /// null, the reflection-based default (case-insensitive Contains against every public readable
    /// property's ToString()) is used. Mirrors the web's per-column FilterFn concept, collapsed to a
    /// single grid-level hook (the realistic amount of state surface worth adding here).
    /// </summary>
    public static readonly DependencyProperty FilterFnProperty = DependencyProperty.Register(
        nameof(FilterFn), typeof(Func<object, string, bool>), typeof(NaviusDataGrid),
        new PropertyMetadata(null, OnFilterInputChanged));

    /// <summary>
    /// Row identity selector (the web's RowKey). Falls back to the row object itself as its own key
    /// when null. Used for anything that needs stable row identity (selection tracking).
    /// </summary>
    public static readonly DependencyProperty RowKeySelectorProperty = DependencyProperty.Register(
        nameof(RowKeySelector), typeof(Func<object, object>), typeof(NaviusDataGrid),
        new PropertyMetadata(null));

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ReadablePropertyCache = new();

    static NaviusDataGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusDataGrid), new FrameworkPropertyMetadata(typeof(NaviusDataGrid)));
    }

    public NaviusDataGrid()
    {
        // Perf: keep native row/column virtualization on (the whole reason to build on the native
        // grid rather than reinventing one). The style also sets VirtualizingPanel.* as
        // belt-and-suspenders; the perf-guard test asserts both survive the re-template.
        EnableRowVirtualization = true;
        EnableColumnVirtualization = true;

        // Columns are explicit (matching the web's explicit Columns param), no auto-generation.
        AutoGenerateColumns = false;

        // One-ink brand chrome: column headers only (no row-header gutter), no gridline chrome, and
        // a read-only grid (no add/delete affordances).
        HeadersVisibility = DataGridHeadersVisibility.Column;
        GridLinesVisibility = DataGridGridLinesVisibility.None;
        CanUserAddRows = false;
        CanUserDeleteRows = false;

        // Closest parity to the web's set-based multi-key row selection. Native default is already
        // Extended; set it explicitly so the default is documented in code and pinned by a test.
        SelectionMode = DataGridSelectionMode.Extended;
    }

    /// <summary>Controlled global filter text; bind for @bind-GlobalFilter parity.</summary>
    public string? GlobalFilter
    {
        get => (string?)GetValue(GlobalFilterProperty);
        set => SetValue(GlobalFilterProperty, value);
    }

    /// <summary>Optional (row, filterText) match override; null uses the reflection default.</summary>
    public Func<object, string, bool>? FilterFn
    {
        get => (Func<object, string, bool>?)GetValue(FilterFnProperty);
        set => SetValue(FilterFnProperty, value);
    }

    /// <summary>Row identity selector; null falls back to the row object as its own key.</summary>
    public Func<object, object>? RowKeySelector
    {
        get => (Func<object, object>?)GetValue(RowKeySelectorProperty);
        set => SetValue(RowKeySelectorProperty, value);
    }

    /// <summary>
    /// Read-only snapshot of the native sort state (the web's single-column DataGridSort, exposed
    /// here as the native multi-descriptor collection WPF actually maintains). Convenience wrapper
    /// so consumers/tests can read sort state without mutating Items.SortDescriptions.
    /// </summary>
    public IReadOnlyList<SortDescription> SortDescriptionsSnapshot => Items.SortDescriptions.ToArray();

    /// <summary>
    /// Resolves a row's identity key: <see cref="RowKeySelector"/> when set, else the row itself.
    /// Mirrors the web's RowKey fallback behavior.
    /// </summary>
    public object GetRowKey(object row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return RowKeySelector?.Invoke(row) ?? row;
    }

    /// <summary>
    /// The default global-filter predicate: case-insensitive Contains against the ToString() of
    /// every public, readable, non-indexer instance property on the row. Reflection is cached per
    /// type so a filter keystroke does not re-reflect. Public/static so it is directly unit-testable
    /// without a live grid.
    /// </summary>
    public static bool DefaultFilterMatch(object row, string filterText)
    {
        if (row is null || string.IsNullOrEmpty(filterText))
        {
            return true;
        }

        var properties = ReadablePropertyCache.GetOrAdd(
            row.GetType(),
            static type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray());

        foreach (var property in properties)
        {
            object? value;
            try
            {
                value = property.GetValue(row);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (value?.ToString() is { } text
                && text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);
        ApplyGlobalFilter();
    }

    private static void OnFilterInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDataGrid)d).ApplyGlobalFilter();

    private void ApplyGlobalFilter()
    {
        if (ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view is null)
        {
            return;
        }

        var filterText = GlobalFilter;
        if (string.IsNullOrEmpty(filterText))
        {
            view.Filter = null;
            return;
        }

        var overrideFn = FilterFn;
        view.Filter = overrideFn is null
            ? row => DefaultFilterMatch(row, filterText)
            : row => row is not null && overrideFn(row, filterText);
    }
}
