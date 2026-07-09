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

## WPF implementation notes

This is the realized WPF port. It confirms the "WPF strategy" section's Tier B conclusion and pins down the choices that section left open.

### Tier B: one lookless control, not a part-by-part API

`NaviusAutocomplete<TItem>` is a single lookless `Control` whose `ControlTemplate` (in `Themes/Autocomplete.xaml`) composes a `TextBox` (`PART_Input`) and a `NaviusAnchoredPopup` (`PART_Popup`) hosting an `ItemsControl` (`PART_List`). The "Open questions" composite-vs-composable question is answered in favor of the composite control: a single shared state object (open / query / highlighted index / filtered rows) drives the template the way `AutocompleteContext` drives the Blazor parts. Files: `Controls/Autocomplete/NaviusAutocompleteBase.cs`, `NaviusAutocomplete.cs`, `AutocompleteEngine.cs`, `NaviusAutocompleteAutomationPeer.cs`.

### Part collapse

The web part tree is absorbed into the one control + its template:

- `Portal` / `Positioner` -> `NaviusAnchoredPopup` (a WPF `Popup` placed by `PlacementMath`); there is no DOM-portal concept to preserve. Side/Align/SideOffset/AlignOffset are template-bound DPs on the root (defaults `Side=Bottom`, `Align=Start` per the Positioner defaults). Collision `Flip`/`AvoidCollisions`/`Sticky`/`HideWhenDetached`/`ArrowPadding` are provided by the shared anchored-popup engine and are not re-surfaced as autocomplete DPs.
- `Collection` / `List` / `Item` / `Row` -> the `ItemsControl` and its item template. Rows are plain `Border`/`TextBlock` `DataTemplate`s over a lightweight `AutocompleteRow` view-model, NOT `Selector`/`ListBoxItem` targets, so they never take real focus or selection.
- `Backdrop` -> dropped (the contract's backdrop is non-modal with no scroll lock; dismissal is handled by the overlay stack).
- `Trigger` / `Icon` -> dropped for this budget (the input itself is the trigger; ArrowDown/ArrowUp open it). No separate dropdown toggle button.
- `Value` / `Clear` -> the root's `Value` DP is both the live input text and the committed value (the contract maps both onto `Value`/`ValueChanged`). No separate Clear button; clearing is just emptying the text.
- `Arrow` -> not rendered (this wave's hard rule is hairline borders, no shadows; an arrow glyph was out of scope).
- `Group` / `GroupLabel` / `Separator` -> not ported (flat list only), consistent with the "Open questions" note that grouped/grid layout is unexercised in the source.
- `Attributes` (arbitrary HTML attribute capture) and `Class` -> dropped on every part, per this repo's established web-only-parameter precedent. `Dir`/RTL is not surfaced as a DP either (WPF `FlowDirection` is the platform-native channel).

### Generic-control styling pattern

WPF resolves `DefaultStyleKey` per closed generic type, so a single `<Style TargetType>` cannot target an open generic. All template-bound state lives on the non-generic `NaviusAutocompleteBase : Control`; `NaviusAutocomplete<TItem>` overrides `DefaultStyleKeyProperty` back to `typeof(NaviusAutocompleteBase)`, so every closed instantiation shares the one style whose `ControlTemplate` `TemplateBinding`s freely against the base. One extra wrinkle: `DefaultStyleKey` only consults theme dictionaries (Generic.xaml, which this family deliberately does not touch), and implicit-style lookup keys on the concrete closed-generic type, so neither built-in path finds a base-typed style merged into consumer resources; the base therefore resolves its style itself at Initialized/Loaded via `TryFindResource(typeof(NaviusAutocompleteBase))`, unless a Style was set locally. The `TItem`-typed inputs (`Items`, `ItemToString`, `Filter`) live on the generic and feed the base via an abstract `Recompute()` that rebuilds the object-typed filtered rows; `Value`/`ValueChanged` are `string?` on the base directly (the contract types `Value` as `string`, not `TItem`).

### Escape / dismiss strategy

On open, the control pushes an `OverlayStack` session (like `NaviusPopover`) with `CloseOnEscape=true`, `CloseOnOutsideClick=true`, and registers the popup content as an input root so outside-press routing works inside the popup's own `HwndSource`. Escape is also handled directly on `PART_Input`'s `PreviewKeyDown` (focus is on the input, in the main window's `HwndSource`, so its handler runs first). Crucially the session uses `TrapFocus=false` and `RestoreFocus=false`: this is the WPF mapping of the web Popup's `TrapFocus=false`/`MoveFocusInside=false` overrides. Focus never moves into the popup on open and is never restored on close, because it never left the input.

### Strict virtual focus

Real WPF keyboard focus is pinned to `PART_Input` at all times. This is enforced structurally, not by moving a roving tab stop: `PART_List` (the `ItemsControl`, deliberately not a `ListBox`/`Selector`), the enclosing `ScrollViewer`, and `PART_PopupContent` are all `Focusable="False"` with `KeyboardNavigation.TabNavigation="None"`; rows are non-focusable `DataTemplate`s. The highlighted row is a pure data pointer: `HighlightedIndex` on the root plus an observable `AutocompleteRow.IsHighlighted` flag that a `DataTrigger` restyles, so the visual highlight moves with zero focus or selection change. Tested via `PopupListAndContent_AreNotFocusable_SoFocusNeverLeavesTheInput` (asserts the list/popup are non-focusable while the input is), `Open_PushesANonFocusTrappingDismissableOverlaySession` (asserts `TrapFocus`/`RestoreFocus` are false), and the keyboard tests that move `HighlightedIndex` (the data pointer) without any selection or tab-stop change.

### role="status" announcer

Following the `NaviusToast` precedent, `NaviusAutocompleteStatus`'s `role="status"` / `aria-live="polite"` result-count announcer is realized as UIA's notification event: the control keeps a `StatusText` read-only DP ("{n} result(s)") and calls `AutomationPeer.RaiseNotificationEvent` (Other / CurrentThenMostRecent, the polite processing) whenever the filtered rows change while the popup is open, instead of reproducing the web's hidden duplicate-announcer div. On OS/AT combinations where the notification event is unsupported it degrades silently; a fuller peer with a `LiveSetting` fallback is deferred with the virtual-focus peer below.

### Keyboard table deltas

Full parity with the contract's table (ArrowDown, ArrowUp, Enter, Escape, Tab, Home/PageUp, End/PageDown), all handled on `PART_Input.PreviewKeyDown`. `Tab` closes the popup without marking the event handled so normal Tab focus navigation still proceeds, exactly as the contract specifies. No deltas.

### AutomationPeer / aria-activedescendant gap (deferred)

The contract's own Open Questions flag `aria-activedescendant`/virtual focus as the biggest translation gap requiring a custom `AutomationPeer` "before parity can be claimed", and WPF/UIA has no first-class virtual-focus primitive. Under this budget `NaviusAutocompleteAutomationPeer` is minimal: it reports the root as `ControlType.ComboBox` and exposes `Value` as its name, but it does NOT model the highlighted option as a UIA focused/selected descendant (no `IExpandCollapseProvider`/`ISelectionProvider` wired to `HighlightedIndex`). A faithful peer surfacing the active descendant to screen readers is explicitly deferred, consistent with the contract flagging it as unresolved.

## M6 audit (2026-07-09)

Adversarially re-verified `NaviusAutocompleteBase`/`NaviusAutocomplete<TItem>`/`AutocompleteEngine`/
`NaviusAutocompleteAutomationPeer` against this doc's claims. Full keyboard-table walkthrough
(ArrowDown/ArrowUp open-then-highlight-last-on-Up/Enter/Escape/Tab-not-handled/Home-PageUp/End-PageDown)
confirmed wired on `PART_Input.PreviewKeyDown` exactly as documented, including the specific
ordering nuance that `IsOpen=true` synchronously runs `Recompute()`/`SetRows()` (which resets
`HighlightedIndex=-1`) before the ArrowUp handler's subsequent `HighlightedIndex = FilteredRows.Count-1`
line executes, so "opens and highlights the last row" is correct rather than being clobbered.
`AutocompleteEngine.MoveHighlight`'s clamp-without-wrap math matches. The claimed test names
(`PopupListAndContent_AreNotFocusable_SoFocusNeverLeavesTheInput`, `Open_PushesANonFocusTrappingDismissableOverlaySession`)
were confirmed to exist in `AutocompleteTests.cs`. The virtual-focus/no-real-focus-in-popup claim
was verified in `Themes/Autocomplete.xaml`: `PART_PopupContent`, the `ScrollViewer`, and `PART_List`
are all `Focusable="False"`.

**PLAUSIBLE, not fixed (cosmetic doc imprecision).** The doc states PART_List/ScrollViewer/PART_PopupContent
"are all `Focusable="False"` with `KeyboardNavigation.TabNavigation="None"`," but
`Themes/Autocomplete.xaml` only sets `KeyboardNavigation.TabNavigation="None"` on `PART_List`, not
on the `ScrollViewer` or `PART_PopupContent` (they only carry `Focusable="False"`). Functionally
inconsequential (non-focusable elements are already excluded from Tab cycling), so not treated as a
behavioral disparity and not fixed.
