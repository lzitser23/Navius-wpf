# Combobox

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusCombobox<TItem>` | none (renders `ChildContent` inside a `CascadingValue`) | Generic root: owns open state, filter query, filtered items, single/multi committed value(s); cascades `ComboboxContext` |
| `NaviusComboboxInput` | `<input type="text">` | Editable filter textbox; virtual-focus anchor/trigger; owns all keyboard handling |
| `NaviusComboboxTrigger` | `<button type="button">` | Optional dropdown toggle button (`tabindex="-1"`) |
| `NaviusComboboxIcon` | `<span aria-hidden="true">` | Presentational chevron slot inside the Trigger |
| `NaviusComboboxValue` | `<span>` | Reflects committed selection text or Placeholder |
| `NaviusComboboxClear` | `<button type="button">` | Clears whole selection + filter (`tabindex="-1"`) |
| `NaviusComboboxChips` | `<div>` | Container that iterates `SelectedValues` and renders chips (multi-select) |
| `NaviusComboboxChip` | `<span>` | One chip for a selected value; cascades `ComboboxChipContext` |
| `NaviusComboboxChipRemove` | `<button type="button">` | Removes the chip's value (`tabindex="-1"`) |
| `NaviusComboboxPortal` | none (flag-setter, renders `ChildContent`) | Records portal container + `KeepMounted` into context |
| `NaviusComboboxBackdrop` | `<div>` (conditional on `Rendered`) | Optional overlay behind the popup (non-modal, no scroll lock) |
| `NaviusComboboxPositioner` | none (flag-setter, renders `ChildContent`) | Publishes placement options (side/align/offset); actual `<div>` rendered by Popup |
| `NaviusComboboxPopup` | `<div>` (wrapped in positioner `<div>` inside `NaviusPortal`), conditional on `Rendered` | Listbox wrapper surface; virtual focus (`TrapFocus=false`, `MoveFocusInside=false`) |
| `NaviusComboboxArrow` | `<svg>` | Popup-pointing arrow triangle |
| `NaviusComboboxCollection` | none (renders `ChildContent`) | Virtualization boundary passthrough (no-op here) |
| `NaviusComboboxList` | `<ul role="listbox">` | Iterates filtered `Items`, cascades `ComboboxItemContext` per row |
| `NaviusComboboxRow` | `<div role="row">` | Grid-layout row passthrough |
| `NaviusComboboxItem` | `<li role="option">` | One filtered row; click selects, hover highlights |
| `NaviusComboboxItemIndicator` | `<span>` (conditional on selected or `KeepMounted`) | Check glyph shown when item selected |
| `NaviusComboboxGroup` | `<div role="group">` | Groups items; cascades `ComboboxGroupContext` |
| `NaviusComboboxGroupLabel` | `<div>` | Group heading; registers its id into `ComboboxGroupContext` |
| `NaviusComboboxSeparator` | `<div role="separator">` | Divider between groups |
| `NaviusComboboxEmpty` | `<div>` (conditional on open + empty) | "No results" slot |
| `NaviusComboboxStatus` | `<div role="status">` | `aria-live="polite"` result-count announcer |

That is 22 rendering/structural parts (24 `.razor` files total; `NaviusComboboxCollection` and `NaviusComboboxPortal`/`NaviusComboboxPositioner` are flag-setters/passthroughs with no DOM of their own, included above for completeness).

## Parameters

### NaviusCombobox\<TItem\> (root)

| Name | Type | Default | Notes |
|---|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | `Array.Empty<TItem>()` | Full item set to filter; items ARE the values |
| `Value` | `TItem?` | none | Single-select committed value, two-way via `@bind-Value` |
| `ValueChanged` | `EventCallback<TItem?>` | none | see Events |
| `Multiple` | `bool` | `= false;` | Enables multi-select (chips) |
| `Values` | `IReadOnlyList<TItem>` | `Array.Empty<TItem>()` | Multi-select committed values, two-way via `@bind-Values` |
| `ValuesChanged` | `EventCallback<IReadOnlyList<TItem>>` | none | see Events |
| `InputValue` | `string?` | none | Filter text, two-way via `@bind-InputValue` |
| `InputValueChanged` | `EventCallback<string?>` | none | see Events |
| `Open` | `bool` | `= false;` | Controlled open state, two-way via `@bind-Open` |
| `OpenChanged` | `EventCallback<bool>` | none | see Events |
| `DefaultOpen` | `bool` | `= false;` | Initial open state when uncontrolled |
| `ItemToString` | `Func<TItem, string>?` | none | Default `x?.ToString()` |
| `Filter` | `Func<TItem, string, bool>?` | none | Default: case-insensitive `Contains`; empty query shows all |
| `ItemTemplate` | `RenderFragment<TItem>?` | none | Per-row template; default renders item text |
| `ChipTemplate` | `RenderFragment<TItem>?` | none | Per-chip template; default renders label + remove button |
| `Disabled` | `bool` | `= false;` | |
| `ReadOnly` | `bool` | `= false;` | |
| `Placeholder` | `string?` | none | |
| `Dir` | `string?` | none | `"ltr"` or `"rtl"`; falls back to cascaded `NaviusDirection` then `"ltr"` |
| `ChildContent` | `RenderFragment?` | none | |

### NaviusComboboxInput

| Name | Type | Default | Notes |
|---|---|---|---|
| `Placeholder` | `string?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched values |

### NaviusComboboxTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxIcon

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxValue

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | Render fragment over selection; falls back to `SelectedLabel` then `Placeholder` |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxClear

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxChips

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxChip

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `object?` | none | The boxed selected value this chip represents |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxChipRemove

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Container` | `string?` | none | CSS selector of custom mount container; null teleports into `document.body` |
| `KeepMounted` | `bool` | `= false;` | Keep popup mounted while closed |

### NaviusComboboxBackdrop

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxPositioner

No component-local `[Parameter]` properties found in this file (inherits `OverlayPositionerBase`, which is outside this family's file set; `DefaultSide`/`DefaultAlign` are overridden C# properties, not `[Parameter]`s).

### NaviusComboboxPopup

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |

(`KeepMounted`, `Attributes`, `Element` etc. are inherited from `OverlayAnchoredPopupBase`, outside this family's file set.)

### NaviusComboboxArrow

| Name | Type | Default | Notes |
|---|---|---|---|
| `Width` | `double` | `= 10;` | |
| `Height` | `double` | `= 5;` | |
| `ChildContent` | `RenderFragment?` | none | Default renders a downward `<polygon>` triangle |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxCollection

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |

### NaviusComboboxList

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxRow

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxItem

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxItemIndicator

| Name | Type | Default | Notes |
|---|---|---|---|
| `KeepMounted` | `bool` | `= false;` | Keep indicator mounted even when unselected, for CSS exit animations |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxGroupLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxEmpty

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusComboboxStatus

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | Falls back to an auto count message: `"{count} result(s)"` |
| `Attributes` | `IDictionary<string, object>?` | none | |

## Events

| Part | Event | Type | Fires when |
|---|---|---|---|
| `NaviusCombobox<TItem>` | `ValueChanged` | `EventCallback<TItem?>` | Single-select value committed (select, clear, remove) |
| `NaviusCombobox<TItem>` | `ValuesChanged` | `EventCallback<IReadOnlyList<TItem>>` | Multi-select values list committed (toggle, backspace-remove, chip-remove, clear) |
| `NaviusCombobox<TItem>` | `InputValueChanged` | `EventCallback<string?>` | Filter text changes (typing, revert-on-close, clear-on-select/clear) |
| `NaviusCombobox<TItem>` | `OpenChanged` | `EventCallback<bool>` | Open state changes (only invoked when `OpenChanged` has a delegate, i.e. controlled) |

No other part declares an `EventCallback<T>` parameter; `NaviusComboboxInput`, `NaviusComboboxTrigger`, `NaviusComboboxClear`, `NaviusComboboxChipRemove`, and `NaviusComboboxItem` use plain `@onclick`/`@onkeydown`/`@oninput` handlers wired internally to `ComboboxContext` methods (`SetQueryAsync`, `SelectAsync`, `RemoveValueAsync`, `RemoveLastValueAsync`, `ClearAsync`, `RequestSetAsync`), not exposed as component parameters.

## State + data attributes

**`data-*` attributes rendered:**

| Attribute | Part(s) | Meaning |
|---|---|---|
| `data-navius-combobox-input` | Input | static marker |
| `data-readonly` | Input | present when `ReadOnly` |
| `data-disabled` | Input, Clear, Trigger | present when `Disabled` |
| `data-navius-combobox-trigger` | Trigger | static marker |
| `data-popup-open` | Trigger | present when `Open` |
| `data-pressed` | Trigger | present when `Open` |
| `data-navius-combobox-icon` | Icon | static marker |
| `data-placeholder` | Value | present when NOT `HasSelection` |
| `data-navius-combobox-value` | Value | static marker |
| `data-navius-combobox-clear` | Clear | static marker |
| `data-navius-combobox-chips` | Chips | static marker |
| `data-navius-combobox-chip` | Chip | static marker |
| `data-navius-combobox-chip-remove` | ChipRemove | static marker |
| `data-navius-combobox-arrow` | Arrow | static marker |
| `data-open` / `data-closed` | Backdrop, Popup | discrete open/closed presence state |
| `data-starting-style` / `data-ending-style` | Backdrop, Popup | transition-in/out presence state |
| `data-navius-combobox-backdrop` | Backdrop | static marker |
| `data-navius-combobox-positioner` | Popup (positioner div) | static marker |
| `data-empty` | Popup, List | present when `IsEmpty` (no filtered rows) |
| `data-navius-combobox-popup` | Popup | static marker |
| `data-navius-combobox-list` | List | static marker |
| `data-navius-combobox-row` | Row | static marker |
| `data-highlighted` | Item | present when `ItemContext.IsHighlighted` |
| `data-selected` | Item, ItemIndicator | present when selected |
| `data-navius-combobox-item` | Item | static marker |
| `data-navius-combobox-item-indicator` | ItemIndicator | static marker |
| `data-navius-combobox-group` | Group | static marker |
| `data-navius-combobox-group-label` | GroupLabel | static marker |
| `data-navius-combobox-separator` | Separator | static marker |
| `data-navius-combobox-empty` | Empty | static marker |
| `data-navius-combobox-status` | Status | static marker |
| `data-side` / `data-align` | Popup, positioner div | mirrored by the shared positioning engine (from `OverlayAnchoredPopupBase`/positioner infra, not defined in this family's own files) |

**Public state surface (context classes):**

- `ComboboxContext.Open` (bool), `Modal` (always `false`, non-modal), `Multiple` (bool), `Disabled`, `ReadOnly`, `Placeholder`
- `ComboboxContext.Query` (live filter text, separate from committed value)
- `ComboboxContext.HighlightedIndex` (int, -1 = none)
- `ComboboxContext.Items` (`IReadOnlyList<ComboboxItemData>`, filtered rows), `ItemCount`, `IsEmpty`
- `ComboboxContext.SelectedValues` (`IReadOnlyList<object>`, boxed; one entry for single-select, N for multi), `SelectedLabel` (string, comma-joined for multi), `HasSelection`
- `ComboboxContext.ActiveDescendantId` (string?, null when closed or nothing highlighted)
- `ComboboxContext.IsSelected(object value)` (bool, via injected typed equality comparer)
- `ComboboxContext.Dir` / `IsRtl`
- `ComboboxItemData(object Value, string Text, int Index)`: one filtered row
- `ComboboxItemContext(object Value, int Index, bool IsHighlighted, bool IsSelected, string OptionId)`: cascaded per-row state for Item/ItemIndicator
- `ComboboxChipContext(object Value)`: cascaded to ChipRemove so it knows which value to remove
- `ComboboxGroupContext.LabelId` (string?): cascaded from Group to GroupLabel/aria-labelledby wiring

Single vs multi-select mode is driven entirely by `ComboboxContext.Multiple` (set from the root's `Multiple` parameter): single-select commits one value and closes the popup; multi-select toggles a value, clears the query, and keeps the popup open (chips accumulate).

## Keyboard

All keyboard handling lives in `NaviusComboboxInput.OnKeyDownAsync`. No other part (`NaviusComboboxItem`, `NaviusComboboxChip`, `NaviusComboboxChipRemove`) has an `@onkeydown` handler; `NaviusComboboxChipRemove` and `NaviusComboboxTrigger` are `tabindex="-1"` so they never receive Tab focus.

| Key | Behavior |
|---|---|
| `ArrowDown` | If closed: opens the popup. If open: moves highlight down by 1 (no wrap, clamped to last row) |
| `ArrowUp` | If closed: opens the popup and highlights the LAST row. If open: moves highlight up by 1 (no wrap, clamped to first row) |
| `Enter` | If open: commits (selects) the currently highlighted row, if any |
| `Escape` | If open: requests close (reverts filter text to the committed value's label) |
| `Tab` | If open: requests close (same revert behavior as Escape); does NOT preventDefault, so focus still moves per browser default |
| `Backspace` | Multi-select only: if the filter is empty AND there is a selection, removes the LAST selected value (chip) |
| `Home` / `PageUp` | If open: highlights the first row |
| `End` / `PageDown` | If open: highlights the last row |

Not handled anywhere in the code: `ArrowLeft`/`ArrowRight` (no chip roving via arrow keys), `Delete` (only `Backspace` removes a chip), `Space` (no special handling; falls through to normal text input).

## Accessibility

- `NaviusComboboxInput`: `role="combobox"`, `aria-expanded` (`"true"`/`"false"` reflecting `Context.Open`), `aria-controls` (`Context.ContentId`, the List's id), `aria-autocomplete="list"`, `aria-activedescendant` (`Context.ActiveDescendantId`, null when closed or nothing highlighted), `autocomplete="off"`, `disabled`, `readonly`.
- `NaviusComboboxList`: `role="listbox"`, `id` = `Context.ContentId` (the input's `aria-controls` target), `aria-multiselectable="true"` when `Context.Multiple` else omitted.
- `NaviusComboboxItem`: `role="option"`, `id` = `ItemContext.OptionId`, `aria-selected` (`"true"`/`"false"`). No `tabindex` (virtual focus; the item is never a DOM focus target).
- `NaviusComboboxGroup`: `role="group"`, `aria-labelledby` bound to the nested `NaviusComboboxGroupLabel`'s generated id (via `ComboboxGroupContext`).
- `NaviusComboboxRow`: `role="row"`.
- `NaviusComboboxSeparator`: `role="separator"`, `aria-orientation="horizontal"`.
- `NaviusComboboxStatus`: `role="status"`, `aria-live="polite"`.
- `NaviusComboboxIcon`: `aria-hidden="true"`.
- `NaviusComboboxPopup`: `dir="rtl"` when `Context.IsRtl`, else omitted; no explicit ARIA role on the popup wrapper itself (the `role="listbox"` lives on the List inside it).

**Focus management:** this is a virtual-focus pattern throughout. DOM focus never leaves the input. `NaviusComboboxPopup` runs with `TrapFocus=false` and `MoveFocusInside=false` (inherited from `OverlayAnchoredPopupBase`), so opening the popup does not move focus into it and closing does not need to restore focus (it was never moved). `NaviusComboboxChipRemove` and `NaviusComboboxTrigger` are `tabindex="-1"`, kept out of the Tab order. After `NaviusComboboxChipRemove.OnClickAsync` removes a chip (which takes transient DOM focus on click, then unmounts), it explicitly calls `Context.TriggerElement.FocusAsync()` to return focus to the input. `NaviusComboboxClear.OnClickAsync` does the same after clearing. The popup's dismiss layer treats the optional `NaviusComboboxTrigger` button as "inside" (via `DismissSecondaryReference`) so a click there toggles rather than racing an outside-dismiss-then-reopen.

## WPF strategy

Tier B (custom lookless control).

`System.Windows.Controls.ComboBox` is a poor fit: it couples the text box and the popup list as one editable/non-editable control without first-class multi-select-with-chips, and its keyboard model assumes DOM/visual-tree focus can move into the popup's items, which conflicts with this component's virtual-focus design (focus always stays in the text box; the "highlighted" row is purely a data pointer, never a WPF `IsKeyboardFocused` item). Build a custom lookless `Control` (or `ComboboxRoot` + templated parts) combining a `TextBox` (the Input, doubling as trigger/anchor), a `Popup` (the Popup/Positioner/Portal stack collapses into WPF's built-in `Popup` placement/`PlacementTarget` machinery, so side/align/offset options map onto `Popup.Placement`, `Popup.HorizontalOffset`/`VerticalOffset`), and an `ItemsControl` or `ListBox` in `SelectionMode="None"` (since selection state is driven by the C# `ComboboxContext`, not WPF's native `ListBoxItem.IsSelected`) for the List/Item rows. Chips (`NaviusComboboxChips`/`Chip`/`ChipRemove`) map to a `WrapPanel` or `ItemsControl` bound to `SelectedValues`, each chip a `ContentControl` with an embedded remove `Button` (`IsTabStop="False"` mirrors `tabindex="-1"`).

AutomationPeer mapping: the Input's `role="combobox"` maps to `ComboBoxAutomationPeer` (or a custom peer implementing `IExpandCollapseProvider` for `aria-expanded` and exposing `Value` for the committed selection text). The List's `role="listbox"` maps to a `ListAutomationPeer` implementing `ISelectionProvider`; each Item's `role="option"` maps to a `ListBoxItemAutomationPeer`-like peer implementing `ISelectionItemProvider`, with `AutomationProperties.AutomationId` set to `ItemContext.OptionId` so it can substitute for `aria-activedescendant`/`id` targeting (WPF has no direct `aria-activedescendant` equivalent; instead raise `AutomationEvents.ElementSelected` or expose the highlighted item through `SelectionItemPattern` so Narrator announces the highlighted row without moving keyboard focus). `role="group"`/`aria-labelledby` maps to `GroupItem` with `AutomationProperties.LabeledBy` pointed at the label element. `role="status"`/`aria-live="polite"` has no built-in WPF equivalent; raise `AutomationEvents.LiveRegionChanged` on the status element (`AutomationProperties.LiveSetting="Polite"`) each time the count changes.

Will NOT translate cleanly: (1) portal rendering: `NaviusComboboxPortal`'s arbitrary CSS-selector mount container has no WPF analogue; WPF's `Popup` already renders in a separate top-level window/adorner layer, so the portal concept collapses into "use `Popup`" but the `Container` parameter (mount into a specific DOM node) is meaningless and should be dropped or reinterpreted as `Popup.PlacementTarget`/`Popup.CustomPopupPlacementCallback`; (2) virtualized list rendering: `NaviusComboboxCollection` is a documented no-op passthrough here (Base UI's virtualization boundary was never implemented), so there is nothing to port, but a WPF port doing real long-list virtualization should use `VirtualizingStackPanel` inside the `ItemsControl`, which is a net-new concern with no source-of-truth in this codebase; (3) `aria-activedescendant`-driven virtual focus has no first-class WPF equivalent (WPF focus is always a real element); this needs a synthetic approach (owner-draw highlight + AutomationPeer notifications) rather than a direct port; (4) the CSS `data-starting-style`/`data-ending-style`/`data-open`/`data-closed` presence-driven enter/exit transitions map to WPF `VisualStateManager` states or a `Storyboard` triggered on the control's own dependency properties, not a direct 1:1.

## Open questions

- `NaviusComboboxPositioner` in this family has no local `[Parameter]`s in its own `.razor.cs`/`.razor` file; its actual side/align/offset parameters live on the shared `OverlayPositionerBase` outside this batch's file set. The WPF port needs that base class's contract to fully specify the Positioner's parameter surface.
- Likewise `NaviusComboboxPopup`'s `KeepMounted`/`Attributes`/`Element` and `NaviusComboboxBackdrop`'s `Entering`/`Exiting`/`Rendered` presence flags are inherited from `OverlayPresence`/`OverlayAnchoredPopupBase` (outside this family). Confirm whether the WPF parity doc for "Overlays" (if it exists as its own batch) is the source of truth for those, to avoid duplicating/diverging definitions here.
- `Tab` closes the popup but the code does not call `preventDefault` equivalent (no `e.preventDefault()` call is present in `OnKeyDownAsync`); confirm whether Blazor's `@onkeydown` without explicit `stopPropagation`/`preventDefault` on Tab actually lets the browser continue its native focus-move in practice, since the WPF port's `PreviewKeyDown` handling needs to decide whether to mark the key handled.
- No `ArrowLeft`/`ArrowRight` roving between chips is implemented; if the WPF port is expected to support chip-focus navigation (a common combobox-with-chips UX elsewhere), that would be new behavior beyond what this codebase does, not a port.
- The default chip's remove glyph is a literal `"×"` character baked into `NaviusCombobox.BuildChipFragment`; confirm whether the WPF default chip template should hardcode the same glyph or take a themed icon.
- `ComboboxContext.Modal` is hardcoded `false` (always non-modal); confirm the WPF `Popup` should likewise never use `StaysOpen="false"` scroll-lock/modal behavior, or whether WPF app shells need a modal variant not present in this Blazor source.

## WPF implementation notes

Tier B, as this doc's own WPF strategy section already concludes: native `ComboBox` couples the textbox and popup into one control and assumes real focus can move into popup items, which conflicts with this component's virtual-focus model. The port is a custom lookless `Control` pair: `NaviusComboboxBase` (non-generic, all template-bound state) + `NaviusCombobox<TItem>` (typed API), templated by a single style in `Themes/Combobox.xaml` (not merged into Generic.xaml; consumers merge it themselves, per this repo's per-family precedent).

**Generic-base-class styling.** WPF resolves default styles per CLOSED generic type, so one `<Style TargetType>` cannot target the open generic. Every DP the ControlTemplate binds lives on non-generic `NaviusComboboxBase`; `NaviusCombobox<TItem>` overrides `DefaultStyleKeyProperty` to the base type. Because this family's style lives in a page-merged dictionary rather than Generic.xaml, `DefaultStyleKey` lookup (which only searches theme dictionaries) never sees it, and implicit-style lookup keys off the closed generic `GetType()`; the base constructor therefore calls `SetResourceReference(StyleProperty, typeof(NaviusComboboxBase))`, a deferred dynamic reference that resolves the base-keyed style from the ambient resource scope. A consumer-set `Style` replaces the reference and wins.

**Part collapses.** The web's 22 rendering parts collapse into one templated control:

- `Portal`/`Positioner`/`Popup`/`Backdrop`/`Arrow`: absorbed into a `NaviusAnchoredPopup` (PART_Popup) + `Border` (PART_PopupContent). Portal's CSS `Container` is meaningless in WPF (dropped); Positioner's side/align/offset become `Side`/`Align`/`SideOffset`/`AlignOffset` DPs on the root. No Backdrop (non-modal, none needed) and no Arrow (this wave's one-ink hairline aesthetic uses none).
- `Collection`/`List`/`Row`/`Item`/`ItemIndicator`: absorbed into an `ItemsControl` (PART_List) over `ComboboxRowVm` records (Value, Text, Index, IsHighlighted, IsSelected). Rows are plain `DataTemplate` content, never `Selector` items; the indicator is a check glyph toggled by a `DataTrigger` on `IsSelected`. Collection was already a documented no-op upstream; nothing to port.
- `Input`: PART_Input, a borderless `TextBox` inside the field border; doubles as anchor/trigger. All keyboard handling lives on its `PreviewKeyDown`, mirroring the contract's single-handler note.
- `Trigger`/`Icon`: no separate toggle button part; a presentational chevron `TextBlock` sits in the field (the input itself is the trigger). Clicking a dedicated toggle button was optional upstream; dropped as a part, not as behavior (opening is via typing/ArrowDown, closing via Escape/Tab/outside-press).
- `Value`: represented by the input's own text (single-select commits write the committed label into the filter text), plus a placeholder `TextBlock` shown when query and selection are both empty.
- `Clear`: PART_Clear, an `IsTabStop=False` button shown when `HasSelection`; clears value(s) + query and refocuses the input.
- `Chips`/`Chip`/`ChipRemove`: PART_Chips, an `ItemsControl` + `WrapPanel` over `ComboboxChipVm`; each chip's remove button is `IsTabStop=False` and executes `RemoveChipCommand` with the chip's VALUE as the parameter.
- `Group`/`GroupLabel`/`Separator`: not ported in this pass; the WPF `Items` surface is a flat `IReadOnlyList<TItem>` (grouping was optional composition upstream, and the flat filtered list is the core contract).
- `Empty`: a "No results" `TextBlock` toggled by the read-only `IsEmpty` DP.
- `Status`: see live region below.

**Escape/dismiss + virtual focus.** Real keyboard focus lives in PART_Input and only there; the highlighted row is the `HighlightedIndex` DP (a data pointer refreshed onto row VMs), never a WPF focus target. Opening pushes an `OverlayStack` session with `TrapFocus=false`, `RestoreFocus=false`, `Modal=false` (matching `TrapFocus=false`/`MoveFocusInside=false`/`Modal=false` upstream) and `CloseOnOutsideClick=true`; the popup content is registered as an input root so outside-press routing works inside the Popup's own HwndSource. `CloseOnEscape` is deliberately false on the session: Escape is handled on the input's PreviewKeyDown instead, so closing always runs the close-and-revert path (filter text reverts to the committed label). Tab closes the same way but is NOT marked Handled, so real Tab navigation proceeds (the contract's no-preventDefault note).

**Chip-remove-by-value regression fix.** `ComboboxEngine.RemoveValue<T>(values, toRemove, comparer)` removes from the FULL committed-values list by equality; nothing in the removal path ever consults an index into the filtered/displayed rows. Tested two ways: a pure engine test constructs a committed list ("Cherry", "Apple", "Mango") whose Apple index differs from Apple's index in the filtered display list and asserts the right value is removed; a `[StaFact]` test narrows the display with a filter, executes `RemoveChipCommand` with "Apple", and asserts `Values` becomes ("Cherry", "Mango").

**Status live region.** The web's `role="status"`/`aria-live="polite"` count announcer maps to Toast's precedent: a visible count `TextBlock` (PART_Status) with `AutomationProperties.LiveSetting="Polite"`, plus `AutomationPeer.RaiseNotificationEvent` (CurrentThenMostRecent) raised on every filtered-count change once loaded. RaiseNotificationEvent needs Win10 1709+ and a listening AT; the LiveSetting is the fallback.

**Keyboard-table deltas.** ArrowDown/ArrowUp (open-if-closed; ArrowUp highlights LAST on open), Enter (commit highlighted), Escape (close + revert), Tab (close + revert, not Handled), Backspace (multi only: empty query + selection removes last value), Home/PageUp (first), End/PageDown (last) are all implemented per the table, with no wrap and clamping via `ComboboxEngine.MoveHighlight`. ArrowLeft/ArrowRight chip roving, Delete, and Space remain unhandled, same as upstream. One WPF-ism: printable typing is the TextBox's own; typing opens the popup via the Query change callback rather than a key handler.

**AutomationPeer / aria-activedescendant gap.** `NaviusComboboxAutomationPeer` is minimal: ControlType.ComboBox + `IExpandCollapseProvider` (the `aria-expanded` mapping). `aria-activedescendant` has no first-class WPF equivalent (WPF focus is always a real element); per-row option peers with SelectionItemPattern and ElementSelected events are NOT implemented in this pass, so a screen reader does not announce the highlighted row as focus moves. This is the same gap the doc's own strategy section flags; the status live region partially compensates. Open item.

**Dropped parameters.** `Attributes` and `Class` on every part (web-only, per repo precedent), `Dir` (WPF `FlowDirection` is the platform channel), `DefaultOpen` (WPF two-way `IsOpen` binding covers controlled/uncontrolled), Portal `Container`/`KeepMounted` (no DOM; WPF Popup always keeps its child instantiated within the template), `ItemIndicator.KeepMounted` (CSS exit-animation concern). `ItemTemplate`/`ChipTemplate` are `DataTemplate`s (the WPF analogue of `RenderFragment<TItem>`), swapped in code because `RelativeSource FindAncestor` cannot cross the Popup's separate visual tree.

## M6 audit (2026-07-09)

Adversarially re-verified `NaviusComboboxBase`/`NaviusCombobox<TItem>` against this doc's claims.
Full keyboard-table walkthrough (ArrowDown/ArrowUp open-if-closed with ArrowUp not pre-highlighting
last on open here -- correctly matching the doc's own note that Combobox's open-from-keyboard path
calls `SetHighlight` separately from Autocomplete's, Enter/Escape/Tab-not-handled/Backspace
multi-only/Home-PageUp/End-PageDown) confirmed wired on `PART_Input.PreviewKeyDown`, matching the
"no deltas" claim. `OnIsOpenChanged` -> `CloseCore()` -> `RevertQuery()` confirmed for the
Escape/Tab revert-to-committed-label behavior. `NaviusComboboxAutomationPeer`'s minimal
`ComboBox` + `IExpandCollapseProvider`-only shape, with the `aria-activedescendant` gap
explicitly undocumented as deferred (not falsely claimed as done), matches the code. No confirmed
or plausible disparities found.

## Post-release fixes (2026-07-11)

- **Disabled combobox now reports disabled through UIA (PR #3).**
  `NaviusComboboxAutomationPeer.IsEnabledCore` now folds in the `Disabled` DP
  (`base.IsEnabledCore() && !_owner.Disabled`). The `Disabled` DP is the combobox's own semantic
  disabled state and is independent of the inherited `IsEnabled` the base peer reflects by default,
  so previously a `Disabled` combobox reported `IsEnabled == true` to UIA.
- **Expand/Collapse now guard against disabled (PR #3).** `IExpandCollapseProvider.Expand`/`Collapse`
  throw `ElementNotEnabledException` when `Disabled` or not `IsEnabled`; previously `Expand` opened
  the popup unconditionally, so a disabled combobox's popup could still be opened through UIA.
