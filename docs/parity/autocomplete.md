# Autocomplete

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusAutocomplete<TItem>` | none (renders `CascadingValue` + `ChildContent` only) | Root: owns query/open/selection state, filtering, and cascades `AutocompleteContext` |
| `NaviusAutocompleteArrow` | `<svg>` (default child `<polygon>`) | Positioner-aligned arrow pointing at the input |
| `NaviusAutocompleteBackdrop` | `<div>` (conditional on `Rendered`) | Optional overlay behind the popup (non-modal; no scroll lock) |
| `NaviusAutocompleteClear` | `<button type="button">` | Clears the query and refocuses the input |
| `NaviusAutocompleteCollection` | none (passthrough of `ChildContent`) | Virtualization boundary placeholder |
| `NaviusAutocompleteEmpty` | `<div>` (conditional on `Open && IsEmpty`) | "No results" slot |
| `NaviusAutocompleteGroup` | `<div role="group">` | Groups rows; cascades `AutocompleteGroupContext` |
| `NaviusAutocompleteGroupLabel` | `<div>` | Group heading; registers its id into `AutocompleteGroupContext` |
| `NaviusAutocompleteIcon` | `<span aria-hidden="true">` | Presentational chevron slot inside the Trigger |
| `NaviusAutocompleteInput` | `<input type="text">` | The editable combobox textbox; trigger + anchor + virtual-focus owner |
| `NaviusAutocompleteItem` | `<li role="option">` | One filtered row |
| `NaviusAutocompleteList` | `<ul role="listbox">` | The listbox; iterates filtered rows, cascades `AutocompleteItemContext` per row |
| `NaviusAutocompletePopup` | `<div>` (positioner wrapper `<div>` + popup `<div>`, inside `NaviusPortal`) | Surface wrapping the listbox; owns anchored positioning + dismissable layer |
| `NaviusAutocompletePortal` | none (flag-setter; renders `ChildContent`) | Records custom mount container / `KeepMounted`; actual teleport done by `NaviusAutocompletePopup` via `NaviusPortal` |
| `NaviusAutocompletePositioner` | none (flag-setter; renders `ChildContent`) | Publishes placement options (side/align/offsets/collision) into the context |
| `NaviusAutocompleteRow` | `<div role="row">` | Passthrough for grid-layout lists |
| `NaviusAutocompleteSeparator` | `<div role="separator">` | Divider between groups |
| `NaviusAutocompleteStatus` | `<div role="status">` | `aria-live="polite"` result-count announcer |
| `NaviusAutocompleteTrigger` | `<button type="button">` | Optional dropdown toggle button |
| `NaviusAutocompleteValue` | `<span>` | Displays the current value/text (ChildContent, else live query) |

## Parameters

### NaviusAutocomplete&lt;TItem&gt; (root)

| Name | Type | Default | Notes |
|---|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | `Array.Empty<TItem>()` | Full item set to filter |
| `Value` | `string?` | none | Input text / committed value |
| `ValueChanged` | `EventCallback<string?>` | none | Two-way with `Value` |
| `Open` | `bool` | `false` | Controlled open state |
| `OpenChanged` | `EventCallback<bool>` | none | Two-way with `Open`; presence of delegate makes Open controlled |
| `DefaultOpen` | `bool` | `false` | Initial open state when uncontrolled |
| `ItemToString` | `Func<TItem, string>?` | none | Item → display/match text; default `x?.ToString()` |
| `Filter` | `Func<TItem, string, bool>?` | none | `(item, query) => keep`; default case-insensitive `Contains`, empty query shows all |
| `ItemTemplate` | `RenderFragment<TItem>?` | none | Per-row template; default renders row text via `NaviusAutocompleteItem` |
| `Dir` | `string?` | none | `"ltr"`/`"rtl"`; falls back to cascaded `NaviusDirection`, then `"ltr"` |
| `ChildContent` | `RenderFragment?` | none | |

### NaviusAutocompleteArrow

| Name | Type | Default | Notes |
|---|---|---|---|
| `Width` | `double` | `10` | |
| `Height` | `double` | `5` | |
| `ChildContent` | `RenderFragment?` | none | Overrides the default `<polygon>` |
| `Attributes` | `IDictionary<string, object>?` | none | Captured unmatched attributes |

### NaviusAutocompleteBackdrop

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteClear

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteCollection

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |

### NaviusAutocompleteEmpty

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteGroupLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteIcon

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteInput

| Name | Type | Default | Notes |
|---|---|---|---|
| `Placeholder` | `string?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteItem

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteList

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompletePopup

Inherits `OverlayAnchoredPopupBase` → `OverlayPopupBase` → `OverlayPresence` (shared overlay machinery).

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | Declared on this part |
| `KeepMounted` | `bool` | `false` | Inherited from `OverlayPopupBase`; keep popup mounted (hidden) while closed |
| `Attributes` | `IDictionary<string, object>?` | none | Inherited from `OverlayPopupBase` |

### NaviusAutocompletePortal

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Container` | `string?` | none | CSS selector of custom mount container; null teleports into `document.body` |
| `KeepMounted` | `bool` | `false` | Keep popup mounted while closed (for exit animations) |

### NaviusAutocompletePositioner

Inherits `OverlayPositionerBase`.

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Side` | `string?` | none | Falls back to `"bottom"` (this part's `DefaultSide`) |
| `Align` | `string?` | none | Falls back to `"start"` (this part's `DefaultAlign`) |
| `SideOffset` | `double` | `0` | |
| `AlignOffset` | `double` | `0` | |
| `Flip` | `bool` | `true` | Folded into `AvoidCollisions` |
| `AvoidCollisions` | `bool` | `true` | |
| `CollisionPadding` | `double?` | none | |
| `Sticky` | `string?` | none | `"partial"` \| `"always"` |
| `HideWhenDetached` | `bool` | `false` | |
| `ArrowPadding` | `double` | `0` | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteRow

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteStatus

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | Overrides the default `"{count} result(s)"` message |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAutocompleteValue

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | Overrides the default live query text |
| `Attributes` | `IDictionary<string, object>?` | none | |

## Events

| Part | Event | Type |
|---|---|---|
| `NaviusAutocomplete<TItem>` (root) | `ValueChanged` | `EventCallback<string?>` |
| `NaviusAutocomplete<TItem>` (root) | `OpenChanged` | `EventCallback<bool>` |
| `NaviusAutocompletePopup` (inherited from `OverlayPopupBase`) | `OnOpenAutoFocus` | `EventCallback<NaviusOpenAutoFocusEventArgs>` |
| `NaviusAutocompletePopup` (inherited) | `OnCloseAutoFocus` | `EventCallback<NaviusCloseAutoFocusEventArgs>` |
| `NaviusAutocompletePopup` (inherited) | `OnEscapeKeyDown` | `EventCallback<NaviusEscapeKeyDownEventArgs>` |
| `NaviusAutocompletePopup` (inherited) | `OnPointerDownOutside` | `EventCallback<NaviusPointerDownOutsideEventArgs>` |
| `NaviusAutocompletePopup` (inherited) | `OnFocusOutside` | `EventCallback<NaviusFocusOutsideEventArgs>` |
| `NaviusAutocompletePopup` (inherited) | `OnInteractOutside` | `EventCallback<NaviusInteractOutsideEventArgs>` |

All other parts route user interaction through `AutocompleteContext` methods (`SetQueryAsync`, `SelectAsync`, `RequestSetAsync`, etc.) rather than exposing their own `EventCallback` parameters.

## State + data attributes

`AutocompleteContext` (shared state, cascaded non-generic):

- `Open` (bool), `Modal` (always `false`)
- `Query` (string, live input text)
- `HighlightedIndex` (int, `-1` when none)
- `Items` (`IReadOnlyList<AutocompleteItemData>`, the filtered rows), `ItemCount`, `IsEmpty`
- `SelectedValues` (`IReadOnlyCollection<object>`)
- `ActiveDescendantId` (string?, computed from `Open`/`HighlightedIndex`)
- `Dir` / `IsRtl`

`AutocompleteGroupContext`: `LabelId` (string?, published by a nested `NaviusAutocompleteGroupLabel`).

`AutocompleteItemContext` (per-row, cascaded by the List): `Value`, `Index`, `IsHighlighted`, `IsSelected`, `OptionId`.

Rendered `data-*` markers:

| Attribute | Where |
|---|---|
| `data-navius-autocomplete-arrow` | Arrow |
| `data-navius-autocomplete-backdrop` | Backdrop |
| `data-open` / `data-closed` / `data-starting-style` / `data-ending-style` | Backdrop, Popup (presence machine) |
| `data-navius-autocomplete-clear` | Clear |
| `data-navius-autocomplete-empty` | Empty |
| `data-navius-autocomplete-group` | Group |
| `data-navius-autocomplete-group-label` | GroupLabel |
| `data-navius-autocomplete-icon` | Icon |
| `data-navius-autocomplete-input` | Input |
| `data-navius-autocomplete-item` | Item |
| `data-highlighted` / `data-selected` | Item |
| `data-navius-autocomplete-list` | List |
| `data-empty` | List, Popup |
| `data-navius-autocomplete-positioner` | Positioner wrapper div |
| `data-navius-autocomplete-popup` | Popup |
| `data-side` / `data-align` / `data-anchor-hidden` | Popup element, mirrored by the shared anchored-popup engine (`OverlayAnchoredPopupBase`) |
| `data-navius-autocomplete-row` | Row |
| `data-navius-autocomplete-separator` | Separator |
| `data-navius-autocomplete-status` | Status |
| `data-navius-autocomplete-trigger` | Trigger |
| `data-popup-open` | Trigger |
| `data-navius-autocomplete-value` | Value |

## Keyboard

All handling lives in `NaviusAutocompleteInput.OnKeyDownAsync`; focus never leaves the input (virtual focus, `aria-activedescendant`, no roving tabindex).

| Key | Behavior |
|---|---|
| `ArrowDown` | If closed: opens the popup. If open: `MoveHighlightAsync(+1, loop: false)` |
| `ArrowUp` | If closed: opens the popup and highlights the last row. If open: `MoveHighlightAsync(-1, loop: false)` |
| `Enter` | If open: `CommitHighlightedAsync()` (selects the highlighted row, if any) |
| `Escape` | If open: closes the popup |
| `Tab` | If open: closes the popup (does not prevent default tab movement) |
| `Home` / `PageUp` | If open: highlights the first row |
| `End` / `PageDown` | If open: highlights the last row |

`NaviusAutocompleteItem` has no `tabindex` and no key handler (virtual focus; selection is via click, highlight via pointer hover/move). `NaviusAutocompleteClear` and `NaviusAutocompleteTrigger` are `tabindex="-1"` (out of Tab order; mouse/programmatic activation only), with no key handlers.

## Accessibility

- Input: `role="combobox"`, `aria-expanded` (`"true"`/`"false"`), `aria-controls` (→ List's `ContentId`), `aria-autocomplete="list"`, `aria-activedescendant` (→ highlighted option id, null when closed/none highlighted), `autocomplete="off"`.
- List: `role="listbox"`, `id` = `ContentId` (the input's `aria-controls` target).
- Item: `role="option"`, `id` = `OptionId`, `aria-selected` (`"true"`/`"false"`).
- Group: `role="group"`, `aria-labelledby` → the nested GroupLabel's generated id.
- Row: `role="row"`.
- Separator: `role="separator"`, `aria-orientation="horizontal"`.
- Status: `role="status"`, `aria-live="polite"`.
- Icon: `aria-hidden="true"`.
- Focus management: focus stays in the Input at all times (virtual focus). The Popup (`NaviusAutocompletePopup`) runs with `TrapFocus=false` and `MoveFocusInside=false` (overridden explicitly): it never moves focus into the popup on open and never restores it to the input on close, because focus never left. Non-modal (`Modal => false` on the context), so no scroll lock.

## WPF strategy

Tier B (custom lookless control)

An `AutoCompleteBox`-style composite (editable `TextBox` + a popup listbox driven by virtual focus via `AutomationProperties.LabeledBy`/`AutomationProperties.ItemStatus` style patterns, since native WPF `AutomationPeer` virtual-focus support is `SelectionItemPattern` + `ExpandCollapsePattern` on the owning control) should be built as a lookless `Control` (or `TextBox`-derived `Control` with a `Popup` in its `ControlTemplate`), not composed from independent WPF built-ins, so the single shared state object (open/query/highlighted index/items/selection) can drive the template the way `AutocompleteContext` drives the Blazor parts. `role="combobox"` + `aria-activedescendant` maps to a custom `AutomationPeer` implementing `IExpandCollapseProvider` (open state) and exposing the highlighted child via `GetFocusedElement`/`ISelectionProvider`, since WPF has no first-class virtual-focus/activedescendant primitive (this is the biggest translation gap). `role="listbox"`/`role="option"` map to `ListBox`/`ListBoxItem` peers if a `ListBox` is used inside the popup template (recommended, since it gets `SelectionItemPattern` for free), but real DOM focus must NOT move into it (WPF `ListBox` naturally wants keyboard focus on selection; will need `Focusable="False"` on items plus manual highlight-brush styling instead of relying on `IsSelected`/native focus visuals). Popup positioning/collision avoidance (side/align/flip/avoid-collisions/sticky) maps to WPF `Popup.Placement`/`PlacementTarget`/`CustomPopupPlacementCallback`, but there is no DOM portal concept to preserve, and the presence-based enter/exit animation (`data-starting-style`/`data-ending-style` + `WaitForAnimationsAsync`) needs a WPF storyboard-based open/close transition instead of CSS/JS-driven timing.

## Open questions

- Whether the WPF port should expose one composite `AutoCompleteBox`-like control (closer to WinForms/WPF toolkit conventions) or preserve the fully composable part-by-part API (Root/Input/Popup/List/Item/...) as separate attached-property-driven pieces; the latter is truer to the Blazor source but is unusual for WPF consumers.
- How `aria-activedescendant`/virtual focus should be surfaced to Windows accessibility tooling (UIA) given WPF has no built-in equivalent; needs a custom `AutomationPeer` design before parity can be claimed.
- `NaviusAutocompleteCollection`, `NaviusAutocompleteRow`, and `NaviusAutocompleteGroup`/`GroupLabel` support a grid/grouped layout not exercised elsewhere in the read source; unclear whether the WPF port needs full grid-row semantics (`ListView`/`GridView`) or can treat these as pass-through container styling.
- The exit-animation "keep mounted while animating out" behavior (`WaitForAnimationsAsync`) is CSS/JS-timed in Blazor; the WPF equivalent (Storyboard `Completed` callback) needs its own design rather than a direct port.
