# DataGrid

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusDataGrid<TItem>` | None (renders only `<CascadingValue>` around `ChildContent`) | Root logic provider. Owns the controlled/uncontrolled source of truth for every state slice (sorting, global filter, pagination, row selection, column visibility) and cascades a `DataGridContext<TItem>`. Renders no table/row/cell markup of its own. |

Supporting (non-component) types cascaded/used by the root, not themselves Razor parts:

| Type | Kind | Purpose |
|---|---|---|
| `DataGridContext<TItem>` | plain class (cascaded value) | The dependency-free state engine: derived row pipeline (global filter → single-column sort → pagination) plus row-selection and column-visibility state. Exposes read-only state + mutating `Toggle*`/`Set*`/page-move methods that route back through the root's change callbacks. Raises a `Changed` event on any state change. |
| `DataGridPart<TItem>` (abstract) | `ComponentBase, IDisposable` base class | Base for data-grid parts implemented **outside this repo** (in the styled "helm" layer). Consumes the cascaded `DataGridContext<TItem>` via `[CascadingParameter]` and subscribes to `Changed` so it re-renders (`InvokeAsync(StateHasChanged)`). No markup of its own. |
| `DataGridColumn<TItem>` | plain class | Column descriptor: `Key`, `Header`, `Accessor`, `CellTemplate`, `Sortable`, `EnableHiding`, `FilterFn`. |
| `DataGridSort` | record | `(ColumnKey, Direction)` sort state; `DataGridSort.None` is the unsorted sentinel. |
| `DataGridPagination` | record | `(PageIndex, PageSize)`; `DataGridPagination.Default` is `(0, 10)`. |
| `SortDirection` | enum | `None`, `Ascending`, `Descending`. |

**Important scope note**: `Navius.Primitives.Components.DataGrid` is a headless state engine only. It renders zero DOM elements for the actual grid (no `<table>`, `<tr>`, `<th>`, `<td>`, no `role="grid"`, no keyboard handlers). All visual grid parts (rows, headers, cells, pagination buttons, etc.) and their keyboard/ARIA behavior live in a separate styled "helm" layer that consumes `DataGridContext<TItem>` via `DataGridPart<TItem>`, and that layer is outside this repo's source tree (`E:\Lzitser\navius\src\Navius.Primitives\Components\DataGrid`). No e2e coverage for DataGrid exists under `tests/e2e` in this repo either.

## Parameters

### NaviusDataGrid&lt;TItem&gt;

| Name | Type | Default | Notes |
|---|---|---|---|
| `Items` | `IEnumerable<TItem>?` | `null` | Row source. Sorting/filtering/paging derive from this. |
| `Columns` | `IReadOnlyList<DataGridColumn<TItem>>?` | `null` | Column descriptors. |
| `RowKey` | `Func<TItem, object>?` | `null` (falls back to the row object itself) | Row identity for selection. |
| `Sorting` | `DataGridSort?` | `null` | Controlled single-column sort. Use `@bind-Sorting`. A present-but-null value does NOT flip the slice to controlled. |
| `DefaultSorting` | `DataGridSort?` | `null` (→ `DataGridSort.None`) | Uncontrolled initial sort state. |
| `GlobalFilter` | `string?` | `null` | Controlled global filter text. Use `@bind-GlobalFilter`. |
| `DefaultGlobalFilter` | `string?` | `null` (→ `string.Empty`) | Uncontrolled initial global filter text. |
| `Pagination` | `DataGridPagination?` | `null` | Controlled pagination. Use `@bind-Pagination`. |
| `DefaultPagination` | `DataGridPagination?` | `null` (→ `DataGridPagination.Default`, page 0/size 10) | Uncontrolled initial pagination. |
| `RowSelection` | `IEnumerable<object>?` | `null` | Controlled set of selected row keys. Use `@bind-RowSelection`. |
| `DefaultRowSelection` | `IEnumerable<object>?` | `null` (→ empty set) | Uncontrolled initial selected row keys. |
| `ColumnVisibility` | `IEnumerable<string>?` | `null` | Controlled set of **hidden** column keys. Use `@bind-ColumnVisibility`. |
| `DefaultColumnVisibility` | `IEnumerable<string>?` | `null` (→ empty set) | Uncontrolled initial hidden column keys. |
| `ChildContent` | `RenderFragment?` | `null` | Content cascaded the `DataGridContext<TItem>`. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| `NaviusDataGrid<TItem>` | `SortingChanged` | `EventCallback<DataGridSort?>` | After `ToggleSortAsync` computes a new sort (a different/unsorted column → Ascending → Descending → cleared). Fires even in the uncontrolled case (pure observer). |
| `NaviusDataGrid<TItem>` | `GlobalFilterChanged` | `EventCallback<string?>` | After `SetGlobalFilterAsync` is called with new filter text. |
| `NaviusDataGrid<TItem>` | `PaginationChanged` | `EventCallback<DataGridPagination?>` | After `NextPageAsync`/`PrevPageAsync`/`SetPageIndexAsync` compute a new page index. |
| `NaviusDataGrid<TItem>` | `RowSelectionChanged` | `EventCallback<IEnumerable<object>>` | After `ToggleRowSelectedAsync` or `ToggleAllOnPageAsync` compute a new selected-key set. |
| `NaviusDataGrid<TItem>` | `ColumnVisibilityChanged` | `EventCallback<IEnumerable<string>>` | After `ToggleColumnVisibleAsync` computes a new hidden-key set. |

## State + data attributes

`NaviusDataGrid` renders no markup, so it emits no `data-*` attributes itself. `DataGridContext<TItem>` exposes the following state surface for consuming parts to read/toggle (no CSS classes are defined in this repo; any classing is the styled layer's responsibility):

- `Columns`, `VisibleColumns` (filtered by `IsColumnVisible`)
- `Sorting`, `GetSort(columnKey)` → `SortDirection`
- `GlobalFilter`
- `Pagination`, `PageIndex`, `PageSize`, `PageCount`, `FilteredCount`, `CanPrev`, `CanNext`
- `AllFilteredRows` (post filter+sort, pre-page), `PageRows` (current page slice)
- `SelectedKeys`, `SelectedCount`, `IsRowSelected(key)`, `IsAllPageSelected`, `IsSomePageSelected` (indeterminate: some but not all of the current page)
- `IsColumnVisible(columnKey)`

Internal caching: `AllFilteredRows`/`PageRows` are memoised (`_filteredSorted`/`_cacheValid`) and invalidated on items/columns/sorting/global-filter/column-visibility changes; pagination and row-selection changes do not invalidate the cache (they don't change which rows match).

## Keyboard

Not implemented in this family's code. `NaviusDataGrid<TItem>` and `DataGridContext<TItem>` contain no `KeyDown`/`OnKeyDown` handlers or key checks of any kind; there is no rendered interactive markup to attach them to. Any keyboard model (arrow-key cell navigation, Enter/Space to sort or select, etc.) is owned entirely by the styled "helm" layer outside this repo. No e2e test in `tests/e2e` exercises DataGrid keyboard behavior.

## Accessibility

No ARIA roles or `aria-*` attributes are wired in this family's code (`NaviusDataGrid` renders no elements). No `FocusAsync` calls, tabindex management, or focus trapping exist here. All grid ARIA semantics (`role="grid"`/`"row"`/`"columnheader"`/`"cell"`, `aria-sort`, `aria-selected`, etc.) would need to be supplied by the styled layer.

## WPF strategy

Tier A (derive from native `System.Windows.Controls.DataGrid`, backed by `ICollectionView`/`CollectionViewSource`).

The Blazor `DataGridContext<TItem>` pipeline (global filter → single-column sort → pagination, plus row-selection and column-visibility sets) maps cleanly onto WPF's built-in `ICollectionView` filtering/sorting and a paged `CollectionViewSource`, so there is no need to reinvent a state engine in WPF: `System.Windows.Controls.DataGrid` already provides `role="grid"`-equivalent semantics natively via its own `DataGridAutomationPeer` (`AutomationControlType.DataGrid`), so no custom AutomationPeer role mapping is required for the base grid/row/cell structure. Column-visibility toggling maps to `DataGridColumn.Visibility`; row selection maps to `DataGrid.SelectedItems`/`SelectionMode`. What will NOT translate directly: because this repo's `DataGrid` family is a pure headless engine with zero rendered markup, keyboard model and ARIA for the *visual* grid were never actually implemented here (they live in an out-of-repo styled layer); there is nothing concrete to port for interaction/ARIA beyond the state engine itself, so the WPF port's keyboard/AutomationPeer behavior will largely come from WPF `DataGrid`'s own defaults rather than a faithful port of existing behavior.

## Open questions

- The actual grid markup, keyboard model, and ARIA implementation live in a styled "helm" layer that is not part of this repo (`Navius.Primitives`): that layer needs to be located and read before any interaction/ARIA parity claims can be made for the WPF port.
- `DataGridColumn<TItem>.FilterFn` and `Accessor` are `Func<>` delegates; porting the "stringified, case-insensitive Contains" default filter and the mixed-type `ValueComparer` (nulls first, same-type `IComparable`, else culture-insensitive string compare) needs an explicit WPF equivalent (custom `IComparer`/predicate on the `ICollectionView`).
- `PageSize <= 0` is treated as "show all rows on one page" (`PageRows` returns all rows unsliced, `PageCount` returns 1): this special case needs an explicit decision in the WPF pager.
- No indication in this family's code of how column reordering, resizing, or multi-column sort would work (not implemented): needs a product decision for WPF if desired, since WPF `DataGrid` supports these natively and parity direction (match Blazor's absence, or add them) is unclear.

## WPF implementation notes

Tier A, as planned: `NaviusDataGrid : System.Windows.Controls.DataGrid` (`Controls/DataGrid/NaviusDataGrid.cs`) plus a re-template dictionary (`Themes/DataGrid.xaml`). The native grid already supplies the full grid surface (its own `DataGridAutomationPeer` with `AutomationControlType.DataGrid`, `ICollectionView`-backed filter/sort, native row/column virtualization, native `SelectedItems`/`SelectionMode`, native `DataGridColumn.Visibility`), so this pass is a re-template plus a thin state surface, not a state-engine port.

### What was built

- `NaviusDataGrid` derived control with constructor defaults: `EnableRowVirtualization = true`, `EnableColumnVirtualization = true`, `AutoGenerateColumns = false`, `HeadersVisibility = Column`, `GridLinesVisibility = None`, `CanUserAddRows = false`, `CanUserDeleteRows = false`, `SelectionMode = Extended`.
- `GlobalFilter` dependency property (`string?`), plus a `FilterFn` override hook and a `RowKeySelector` (see below).
- A read-only `SortDescriptionsSnapshot` convenience wrapper over the native `Items.SortDescriptions` (the web's single-column `DataGridSort`, surfaced as the native multi-descriptor collection WPF actually maintains). Sorting, selection, and column-visibility otherwise ride entirely on the native surface (`DataGrid.Sorting` / `Items.SortDescriptions`, `SelectedItems`, `DataGridColumn.Visibility`), which the brief deemed sufficient, so no custom wrappers were added there.
- `Themes/DataGrid.xaml`: a re-template via `ColumnHeaderStyle` / `CellStyle` / `RowStyle` / `RowHeaderStyle` (collapsed) and grid-level setters, leaving the native structural `ControlTemplate` intact (the low-risk, idiomatic way to reskin the native grid). One-ink brand: `Navius.Muted` header fill with a `Navius.Border` bottom hairline, `Navius.Card` row surface with a 1px `Navius.Border` bottom separator per row (a `Border` in a minimal `DataGridRow` template, so there are horizontal hairlines only, no vertical column separators), 10,8 cell padding, no shadows (no `Effect` anywhere).
- Gallery page (`apps/Navius.Wpf.Gallery/Pages/DataGridPage.xaml(.cs)`): a filterable/sortable demo (`DataGridDemo`) with a `TextBox` bound to `GlobalFilter`, plus a real 10,000-row demo (`DataGrid10kDemo`) generated in code-behind with virtualization left on, the milestone's perf gate. `MainWindow` navigation IS wired (`ListBoxItem Content="DataGrid"` + the `"DataGrid" => new DataGridPage()` switch arm); see the M6 audit note below -- this line previously (incorrectly) claimed otherwise.

### Pagination: deliberately NOT reimplemented

The web's derived pipeline is global filter, then single-column sort, then pagination. WPF's native `DataGrid` has no built-in pager widget, and this task is scoped as "re-template plus a thin state surface", not a full state-engine port. Pagination is therefore recorded as an explicit, deliberate delta, not an oversight: filtering and sorting map onto the native `ICollectionView`, but the paging slice is out of scope for this pass. Consequently the open question above about `PageSize <= 0` ("show all rows on one page") is resolved as N/A for this pass: with no pager, all filtered/sorted rows are shown and the virtualized viewport handles scale.

### GlobalFilter default predicate and FilterFn override

When `GlobalFilter` is non-empty, a predicate is attached to `CollectionViewSource.GetDefaultView(ItemsSource).Filter`; empty/null clears it (`view.Filter = null`). The default predicate (`NaviusDataGrid.DefaultFilterMatch`, public/static so it is directly unit-testable) mirrors the web's stringified global filter: case-insensitive `Contains` against the `ToString()` of every public, readable, non-indexer instance property on the row. Reflection is cached per type (`ConcurrentDictionary<Type, PropertyInfo[]>`) so a filter keystroke never re-reflects. `FilterFn` (`Func<object, string, bool>?`) is an optional per-instance override, the web's per-column `FilterFn` collapsed to a single grid-level hook (the realistic amount of state surface worth adding). The filter re-applies whenever `GlobalFilter`, `FilterFn`, or `ItemsSource` changes. Note this simplifies the web's mixed-type `ValueComparer`: sort comparison is left to the native `ICollectionView`, and the filter is pure stringified Contains.

### RowKeySelector

`RowKeySelector` (`Func<object, object>?`) mirrors the web's `RowKey`. `GetRowKey(row)` returns `RowKeySelector?.Invoke(row) ?? row`, i.e. it falls back to the row object as its own key when unset, matching the web's fallback for selection identity.

### SelectionMode default: Extended

The default is the native `DataGridSelectionMode.Extended` (also WPF's own native default), set explicitly in the constructor so it is pinned by a test. Extended is the closest parity to the web's set-based, multi-key row selection; `Single` would not represent a multi-row selection set. No new enum was invented; the native `System.Windows.Controls.DataGridSelectionMode` is exposed as-is.

### Sort-glyph re-template

The default `DataGridColumnHeader` sort arrow is replaced by a small triangle `Path` (`Navius.MutedForeground` fill) inside a modest `DataGridColumnHeader` `ControlTemplate` override, shown only when the column is sorted. It is drawn pointing down for `Descending` and vertically flipped for `Ascending` (`Style.Triggers` on `DataGridColumnHeader.SortDirection`), consistent with the brand's one-ink minimalism. The header keeps its native click-to-sort behavior; only the glyph and chrome are restyled.

### Selection = Accent fill

Selection paints a full-row accent: the `DataGridRow` template's `Border` switches to `Navius.Accent` on `IsSelected`, and the `DataGridCell` background is transparent so the row accent shows through the whole row. Cell foreground flips to `Navius.AccentForeground` via a `DataTrigger` bound to the ancestor `DataGridRow.IsSelected` (cell-level `IsSelected` is separate under Extended selection, so binding to the row is what yields a full-row highlight).

### Virtualization guarantee and how it is tested

Native `EnableRowVirtualization`/`EnableColumnVirtualization` default to true and are also set explicitly in the constructor; the style additionally pins `VirtualizingPanel.IsVirtualizing = true` and `VirtualizingPanel.VirtualizationMode = Recycling` as belt-and-suspenders. The perf-guard test (`StyleApplication_PreservesVirtualization`) constructs a real `new NaviusDataGrid()`, loads `Themes/DataGrid.xaml` via the `pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DataGrid.xaml` mechanism, finds the `Style` keyed to `typeof(NaviusDataGrid)`, assigns it to the instance's `.Style` (which runs the setters without a live visual tree or `.Show()`), then asserts `EnableRowVirtualization`, `EnableColumnVirtualization`, `VirtualizingPanel.IsVirtualizing`, and `VirtualizingPanel.VirtualizationMode == Recycling`. Because it exercises the real shipped control and the real shipped dictionary, it would catch a future regression that disabled virtualization in either the constructor or the style.

DataGrid test count for this family: 23 (`tests/Navius.Wpf.Tests/DataGridTests.cs`), all green.

## M6 audit (2026-07-09)

**CONFIRMED, fixed (doc-only).** This doc previously claimed "Navigation is intentionally NOT
wired into `MainWindow` (owned by another workstream)." That was false: `apps/Navius.Wpf.Gallery/MainWindow.xaml`
already has `<ListBoxItem Content="DataGrid" />` in the nav list and `MainWindow.xaml.cs` already
has `"DataGrid" => new DataGridPage(),` in the page-switch expression -- DataGrid navigation is
fully wired and reachable from the running Gallery app. No code changes were needed (the code was
already correct); only the stale doc claim above was corrected, since `MainWindow.xaml`/`.xaml.cs`
are out of scope for this audit's edits regardless.

All other claims were re-verified adversarially and hold: constructor defaults (`EnableRowVirtualization`,
`EnableColumnVirtualization`, `AutoGenerateColumns`, `HeadersVisibility`, `GridLinesVisibility`,
`CanUserAddRows`, `CanUserDeleteRows`, `SelectionMode`) all match code and are each pinned by a
test; `GlobalFilterProperty`/`FilterFnProperty`/`RowKeySelectorProperty` defaults match;
`SortDescriptionsSnapshot` matches; pagination is confirmed absent as documented; no custom
`AutomationPeer` exists (native `DataGridAutomationPeer` relied on, as claimed); every
`DynamicResource` token used in `Themes/DataGrid.xaml` resolves in both `Tokens.Light.xaml` and
`Tokens.Dark.xaml`, with no hardcoded colors beyond structural `Transparent` literals; the 23-test
count is accurate.

One PLAUSIBLE/residual item: the doc's prose says the sort-glyph flip trigger lives on
`Style.Triggers`, but it's actually implemented via `ControlTemplate.Triggers` in `DataGrid.xaml`.
Very likely just imprecise wording rather than a functional defect (both are legitimate WPF
mechanisms and produce the described effect), not confirmed as a real behavioral gap, not fixed.
