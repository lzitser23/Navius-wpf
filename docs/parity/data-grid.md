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
