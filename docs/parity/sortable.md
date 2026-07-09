# Sortable

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSortable | `<div role="list" data-navius-sortable>` + a visually hidden `<div role="status" aria-live="polite" data-navius-sortable-status>` | Root: owns the ordered key list, cascades `SortableContext`, drives the JS pointer-drag engine (`createSortable`) and the C#-owned APG "grab and move" keyboard reducer, announces transitions via the live region |
| NaviusSortableItem | `<div role="listitem" data-navius-sortable-item>` | One reorderable row; drag target and roving-tabindex keyboard focus target, keyed by `Value` |
| NaviusSortableItemHandle | `<span data-navius-sortable-handle>` | Optional drag handle; when present, scopes pointer drag start to this element only |

## Parameters

### NaviusSortable

| Name | Type | Default | Notes |
|---|---|---|---|
| Values | `IReadOnlyList<string>?` | null | Ordered item keys, controlled (bind with `@bind-Values`) |
| ValuesChanged | `EventCallback<IReadOnlyList<string>>` | | |
| DefaultValues | `IReadOnlyList<string>?` | null | Uncontrolled initial order |
| Orientation | `SortableOrientation` (`Vertical`\|`Horizontal`\|`Grid`) | `Vertical` | Drives the engine's midpoint math; keyboard stays linear (next/prev) even for `Grid` |
| Disabled | `bool` | false | Reactive: destroys/recreates the JS sortable engine when toggled |
| OnReorder | `EventCallback<SortableReorderEventArgs>` | | Fired once per committed reorder (pointer or keyboard) with old/new index |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusSortableItem

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | required | Stable key the root tracks order by |
| Label | `string?` | null | Accessible name for live-region announcements; defaults to `Value` |
| Disabled | `bool` | false | Per-item disabled; skipped during roving keyboard navigation |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusSortableItemHandle

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusSortable | ValuesChanged | `EventCallback<IReadOnlyList<string>>`, fired on every committed order mutation (drop, keyboard drop, Escape restore) |
| NaviusSortable | OnReorder | `EventCallback<SortableReorderEventArgs>` (`record SortableReorderEventArgs(int OldIndex, int NewIndex)`), fired once per committed reorder when old != new index, both pointer and keyboard paths |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `data-navius-sortable`, `role="list"` | Marker |
| Root | `data-orientation` | `"horizontal"` / `"vertical"` / `"grid"` |
| Root | `data-disabled`, `aria-disabled` | Present when `Disabled` |
| Root | `data-dragging` | Present while a pointer drag or keyboard grab is active |
| Root | live region: `role="status" aria-live="polite" data-navius-sortable-status`, visually-hidden clip-rect styling | Screen-reader announcements ("Grabbed...", "Moved...", "Dropped...", "Reorder cancelled...") |
| Item | `data-navius-sortable-item`, `data-navius-sortable-id="{Value}"`, `role="listitem"` | Marker + stable id |
| Item | `data-keyboard-grabbed`, `aria-grabbed` | Present while this item is keyboard-grabbed |
| Item | `data-disabled` | Present when item or root disabled |
| Item | `aria-roledescription="sortable item"` | |
| Item | `tabindex` | `0` for the single roving-tabindex-active item, `-1` otherwise |
| Item | `data-dragging`, `data-drop-target` | Painted directly by the JS engine on the DOM during pointer drag, never rendered by C# (a Blazor re-render leaves them untouched, "passthrough") |
| Handle | `data-navius-sortable-handle`, `aria-hidden="true"` | Scopes pointer-drag start when present (`SortableContext.HasHandle`); aria-hidden because keyboard reordering acts on the whole item, not the handle |
| SortableContext (C# state) | `Keys`, `Disabled`, `Orientation`, `ActiveKey` (roving tabindex), `GrabbedKey` (keyboard grab), `HasHandle`, per-item labels and disabled set | Shared cascaded state; `Changed` event drives part re-render |

## Keyboard

APG "grab and move" model, implemented entirely in C# on `NaviusSortable.HandleItemKeyDownAsync`, routed from each item's `@onkeydown`.

| Key | Behavior |
|---|---|
| Space / Enter (not grabbing) | Grab the focused item: records original order/index, sets `GrabbedKey`, announces "Grabbed... Use the arrow keys to move, space to drop, escape to cancel." |
| ArrowDown / ArrowRight (not grabbing) | Move roving focus to the next enabled item (does not reorder) |
| ArrowUp / ArrowLeft (not grabbing) | Move roving focus to the previous enabled item |
| Home / End (not grabbing) | Move roving focus to first/last enabled item |
| Space / Enter (grabbing) | Drop: commits the move, clears `GrabbedKey`, announces "Dropped...", fires `OnReorder` if position changed |
| Escape (grabbing) | Cancel: restores the original order captured at grab time, announces "Reorder cancelled...", does not fire `OnReorder` |
| ArrowDown / ArrowRight (grabbing) | Move the grabbed item one enabled slot forward: order mutates and `ValuesChanged`/announcement fire immediately, but `OnReorder` fires only on the later drop/commit |
| ArrowUp / ArrowLeft (grabbing) | Move the grabbed item one enabled slot backward |
| Home / End (grabbing) | Move the grabbed item to the first/last enabled slot |

Disabled rows are skipped by roving navigation (`NextEnabled`/`FirstEnabled`/`LastEnabled`) so focus never lands on an unreachable row.

Pointer drag (mouse/touch) is handled by the JS engine (`createSortable`) via `[JSInvokable] OnDragStart`/`OnDragOver`/`OnDrop`/`OnCancel` callbacks on the root; the engine reports per-container indices and paints `data-dragging`/`data-drop-target` directly on the DOM. If a handle (`NaviusSortableItemHandle`) is present, pointer drag is scoped to it via a `[data-navius-sortable-handle]` selector passed into `SortableOptions.Handle`. If the JS module fails to load, keyboard reordering still fully works.

Cross-list (`Group`) transfer between separate `NaviusSortable` containers is explicitly NOT supported (the engine reports indices relative to a single container only).

## Accessibility

- Root: `role="list"`, `aria-disabled` when disabled.
- Item: `role="listitem"`, `aria-roledescription="sortable item"`, `aria-grabbed` (true while keyboard-grabbed).
- Roving tabindex: exactly one enabled item has `tabindex="0"`; the rest are `tabindex="-1"`. Falls back to the first enabled item if the active/seed key vanishes or becomes disabled.
- Live region (`role="status" aria-live="polite"`) announces grab, move, drop, and cancel transitions with human-readable position text ("Position N of M").
- Handle is `aria-hidden="true"` by default (mouse-only affordance; keyboard operates on the whole item), overridable via `Attributes`.
- Focus management: after a keyboard move or Escape-restore, focus is explicitly re-requested onto the moved/restored item (`RequestFocus`/`ConsumeFocus`, applied via `ElementReference.FocusAsync()` in `OnAfterRender`) so focus follows the item across re-renders.

## WPF strategy

Tier B: custom lookless control. WPF has no built-in reorderable-list control comparable to this (drag-to-reorder `ListBox` requires manual `AdornerLayer`/`DragDrop` implementation regardless), so this should be a custom `Control`/`ItemsControl` pair (`SortableList` + `SortableListItem`) built on `System.Windows.Controls.Primitives.Selector` or plain `ItemsControl`, using `AutomationPeer` overrides to expose `ListAutomationPeer`/`ListItemAutomationPeer`-like `role="list"`/`role="listitem"` semantics and `RaiseAutomationEvent`/`LiveRegion` (UIA `LiveSetting`) for the announcements instead of a visually-hidden `aria-live` div. Pointer drag maps to WPF's native `DragDrop.DoDragDrop`/`PreviewMouseMove` pattern (replacing `createSortable`); the keyboard grab-and-move reducer (Space/Enter grab, arrows move, Space/Enter drop, Escape cancel) ports as pure C# state-machine logic onto `PreviewKeyDown`. `data-dragging`/`data-drop-target`/`data-keyboard-grabbed` become boolean dependency properties driving `Style` triggers. The handle-scoped drag start (`[data-navius-sortable-handle]`) maps to checking `e.OriginalSource` ancestry against a named/tagged handle element in `PreviewMouseLeftButtonDown`.

## Open questions

- WPF drag visuals (ghost/insertion indicator) need a concrete design; the web version leans on the JS engine painting `data-dragging`/`data-drop-target` directly, which has no WPF equivalent without an `AdornerLayer`.
- Grid orientation's "nearest cell by 2D distance" pointer logic needs a WPF-native hit-test replacement; keyboard-linear behavior can port directly.
- Cross-list group transfer is out of scope here too, confirm the WPF port also defers it rather than scope-creeping beyond parity.
- Live-region announcement text is English-only, hardcoded in C#; decide if the WPF port needs localization now or can carry the same hardcoded strings.
