# TimePicker

Elective component (not part of Base UI). Built on top of the Popover engine (`NaviusPopover`/`NaviusPopoverPortal`/`NaviusPopoverPositioner`/`NaviusPopoverPopup`/`NaviusPopoverTrigger`, outside this batch) and the `TimeInput` family (`NaviusTimeInput`, this batch). An editable segmented time input plus a popup with one scrollable listbox column per unit.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTimePicker | Wraps `<NaviusPopover>` (no own DOM element) | Root: owns value + open state (controlled/uncontrolled), cascades `TimePickerContext` |
| NaviusTimePickerInput | `<NaviusTimeInput data-navius-time-picker-input>` | Embedded segmented time input bound to the picker's shared value; works whether or not the popup is open |
| NaviusTimePickerTrigger | `<NaviusPopoverTrigger data-navius-time-picker-trigger>` | The clock button that toggles the popup; delegates ARIA to the Popover trigger |
| NaviusTimePickerContent | `<NaviusPopoverPortal><NaviusPopoverPositioner><NaviusPopoverPopup data-navius-time-picker-popup role="dialog">` | Portals + anchors the popup, hosts the column listboxes |
| NaviusTimePickerColumn | `<div role="listbox" data-navius-time-picker-column>` | One scrollable listbox column for a single unit (hour/minute/second/dayPeriod); auto-generates its options, roving tabindex, typeahead |
| NaviusTimePickerOption | `<button role="option" data-navius-time-picker-option>` | One option inside a column; thin focus-aware button, not hand-placed (columns render them from computed option lists) |

## Parameters

### NaviusTimePicker

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `TimeOnly?` | null | Controlled (`@bind-Value`) |
| ValueChanged | `EventCallback<TimeOnly?>` | | |
| DefaultValue | `TimeOnly?` | null | Uncontrolled initial value |
| Open | `bool` | false | Controlled (`@bind-Open`) |
| OpenChanged | `EventCallback<bool>` | | |
| DefaultOpen | `bool` | false | Uncontrolled initial open state |
| Granularity | `string` | "minute" | Which columns render: `hour`/`minute`/`second` |
| HourCycle | `int?` | null | `12` or `24`; defaults to culture short-time pattern sniff (`H` present => 24) |
| MinuteStep | `int` | 1 | Clamped to `>= 1` in context sync |
| SecondStep | `int` | 1 | Clamped to `>= 1` |
| Disabled | `bool` | false | |
| ChildContent | `RenderFragment?` | null | |

### NaviusTimePickerContent

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Side | `string` | "bottom" | Popover positioning side |
| Align | `string` | "start" | Popover positioning alignment |
| SideOffset | `double` | 4 | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTimePickerColumn

| Name | Type | Default | Notes |
|---|---|---|---|
| Unit | `string` | "hour" | `hour`/`minute`/`second`/`dayPeriod` |
| Class | `string?` | null | Applied to the listbox container |
| OptionClass | `string?` | null | Applied to each option button |

### NaviusTimePickerInput / NaviusTimePickerTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes (both parts); no other public parameters, all state comes from cascaded `TimePickerContext` |

### NaviusTimePickerOption (not hand-placed; documented for completeness)

`Value` (int), `Selected` (bool), `Highlighted` (bool), `Disabled` (bool), `IsFocusTarget` (bool), `Focus` (bool, one-shot), `Class` (string?), `OnClick` (EventCallback), `ChildContent` (RenderFragment?); all computed and passed by the owning `NaviusTimePickerColumn`.

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTimePicker | ValueChanged | `EventCallback<TimeOnly?>`, fired whenever the composed value changes (from typed input or column selection) |
| NaviusTimePicker | OpenChanged | `EventCallback<bool>`, fired when the popover open state changes |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Popup | `role="dialog"` (via Popover engine), `data-navius-time-picker-popup`, popover engine's `data-open`/`data-closed`/`data-starting-style`/`data-ending-style` (animation hooks) | |
| Trigger | Popover trigger's `aria-haspopup="dialog"`, `aria-expanded`, `aria-controls`, `data-popup-open`, plus `data-navius-time-picker-trigger`, `disabled` mirrors `Ctx.Disabled` | |
| Column | `role="listbox"`, `aria-label` ("Hours"/"Minutes"/"Seconds"/"AM/PM"), `data-navius-time-picker-column`, `data-unit` | |
| Option | `role="option"`, `aria-selected`, `aria-disabled`, `tabindex` (0 for the roving target, -1 otherwise), `data-navius-time-picker-option`, `data-value`, `data-selected`, `data-highlighted`, `data-disabled` | |
| Input | `data-navius-time-picker-input` attribute passed through to the embedded `NaviusTimeInput`'s root `<div>` | |
| TimePickerContext (C# state) | `Value`, `HourCycle`, `Granularity`, `MinuteStep`, `SecondStep`, `Disabled` | Cascaded (fixed) from root; `Changed` event notifies columns to re-render |

## Keyboard

### NaviusTimePickerInput

Delegates entirely to `NaviusTimeInput`'s segment keyboard model (see `time-input.md`); works regardless of popup open state.

### NaviusTimePickerColumn (listbox)

| Key | Behavior |
|---|---|
| ArrowDown | Move roving-active option down one (clamped at last) |
| ArrowUp | Move roving-active option up one (clamped at first) |
| Home | Move to first option |
| End | Move to last option |
| Digit (0-9) | Typeahead: appends to a buffer, jumps to the first option whose value string starts with the buffer; resets buffer to the single digit if no match |
| Enter / Space | Native `<button>` activation (no explicit handler; `role="option"` is a `<button>`), triggers `OnClick` → `SelectAsync` → commits the unit via `Ctx.SetUnitAsync` |

Roving tabindex tracks the selected option until the user navigates within the column (`_navigated` flag), after which the last-navigated option stays the roving target.

## Accessibility

- Trigger: `aria-haspopup="dialog"`, `aria-expanded`, `aria-controls` (all via the underlying Popover trigger).
- Popup: `role="dialog"` (via Popover popup).
- Column: `role="listbox"`, `aria-label` naming the unit.
- Option: `role="option"`, `aria-selected`, `aria-disabled`.
- Roving tabindex within each column (`tabindex="0"` on the focus target, `-1` on the rest); one-shot `Focus` parameter drives `ElementReference.FocusAsync()` after navigation, mirroring the pattern used by `NaviusFieldSegment`/`NaviusSortableItem`.
- The embedded `NaviusTimePickerInput` carries all of `NaviusTimeInput`'s accessibility (spinbutton segments, `aria-describedby`, etc.).

## WPF strategy

Tier B: custom lookless control, composed from Tier-A pieces. The popup/trigger shell should reuse whatever WPF Popover primitive results from porting the Popover family (a `Popup`-based control with `Placement` matching `Side`/`Align`/`SideOffset`), while each `NaviusTimePickerColumn` maps naturally to a `ListBox` (native `ListBoxAutomationPeer` -> UIA `SelectionPattern`, matching `role="listbox"`/`role="option"`) with `SelectionMode="Single"`, native typeahead (WPF `ListBox` already supports digit-typeahead via `TextSearch`, though its matching semantics differ slightly from the custom buffer-reset logic here and should be verified against `Typeahead()`'s behavior). `NaviusTimePickerOption` becomes a `ListBoxItem`-derived or `ItemContainerStyle`-templated item; roving tabindex is native to `ListBox` and does not need custom porting. The embedded segmented input reuses the `NaviusTimeInput` WPF port directly. `TimePickerContext`'s unit-composition math (`SetUnitAsync`, `UnitValue`) is pure C# and ports unchanged.

## Open questions

- Depends on the Popover family's WPF shape (not in this batch) for trigger/popup/positioning; this strategy is provisional until that family is ported.
- WPF `ListBox` typeahead (`TextSearch.TextPath`) may not reproduce the exact reset-on-no-match buffer behavior in `Typeahead()`; decide whether to keep native `ListBox` search or override with the custom buffer logic.
- Confirm whether `NaviusTimePickerOption`'s `Highlighted` vs `Selected` distinction (roving-active vs. actually-chosen unit value) needs two separate visual states in the WPF `ItemContainerStyle`, since native `ListBox` conflates focus and selection more tightly than this component does.
