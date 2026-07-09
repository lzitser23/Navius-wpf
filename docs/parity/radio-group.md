# RadioGroup

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusRadioGroup | `div` (`role="radiogroup"`) | Root. Owns the authoritative selected value (controlled via `Value`/`ValueChanged` or uncontrolled via `DefaultValue`), cascades `RadioGroupContext`, and owns the group-level keyboard model (arrows / Home / End). |
| NaviusRadioGroupItem | `button` (`role="radio"`) + a hidden `NaviusBubbleInput` (`type="radio"`) when `Name` is set on the group | One selectable radio. Roving tabindex; registers its element and disabled state with the group context; renders a visually-hidden native radio mirror for form submission. |
| NaviusRadioGroupIndicator | `span` | Renders only when the parent item is checked (or always when `KeepMounted`). The canonical place for the radio dot; mirrors the item's discrete state via `RadioGroupItemContext`. |

## Parameters

**NaviusRadioGroup**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string?` | `null` | Controlled selected value. |
| ValueChanged | `EventCallback<string?>` | | Presence of a delegate (`ValueChanged.HasDelegate`) determines controlled vs. uncontrolled mode. |
| DefaultValue | `string?` | `null` | Uncontrolled initial value. |
| Disabled | `bool` | `false` | Disables the whole group; blocks selection and keyboard navigation. |
| Required | `bool` | `false` | Also mirrored per-item (`IsRequired = Required || Context.Required`). |
| Name | `string?` | `null` | When set, each item renders a hidden native `radio` input mirror for form submission. |
| Orientation | `string?` | `null` | Spec default is *undefined*: `aria-orientation`/`data-orientation` only render when explicitly set, never forced to `"vertical"`. |
| Loop | `bool` | `true` | Whether arrow navigation wraps around the ends. |
| Dir | `string?` | `null` | Reading direction; falls back to the cascaded `NaviusDirection` value. Under `"rtl"`, horizontal arrows are mirrored. |
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusRadioGroupItem**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | `""` | The radio's value; equality against `Context.Value` determines checked state. |
| Disabled | `bool` | `false` | Combined with group `Disabled` (`IsDisabled = Disabled \|\| Context.Disabled`). |
| Required | `bool` | `false` | Combined with group `Required`. |
| ReadOnly | `bool` | `false` | Focusable but selection cannot change (spec `readOnly`). |
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusRadioGroupIndicator**

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | `bool` | `false` | Keeps the indicator mounted even when unchecked (spec `keepMounted`). |
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

## Events

- **NaviusRadioGroup**: `ValueChanged` (`EventCallback<string?>`) fires only in controlled mode, when a selection is made via click, Space, or arrow/Home/End navigation.
- **NaviusRadioGroupItem** / **NaviusRadioGroupIndicator**: no `EventCallback` parameters; selection is routed back through the cascaded `RadioGroupContext.SelectAsync`.

## State + data attributes

Root: `data-orientation` (only when `Orientation` explicitly set), `data-disabled` (`""` when `Disabled`, else omitted), `data-navius-radio-group`.

Item: `data-checked` / `data-unchecked` (mutually exclusive, always one present), `data-disabled`, `data-readonly`, `data-required` (each `""` when true, else omitted), `data-navius-radio-group-item`.

Indicator: mirrors the item's `data-checked`/`data-unchecked`/`data-disabled`/`data-readonly`/`data-required`, plus `data-navius-radio-group-indicator`. The element itself is conditionally rendered (`Item.IsChecked || KeepMounted`), not just visually hidden.

Public context state: `RadioGroupContext.Value`, `.Name`, `.Disabled`, `.Required`, `.Orientation`, `.Loop`; `RadioGroupItemContext.IsChecked`, `.IsDisabled`, `.IsReadOnly`, `.IsRequired`.

## Keyboard

Handled on the root's `@onkeydown` (group owns arrow/Home/End: automatic activation moves focus and selection together) plus one key on the item:

| Key | Behavior |
|---|---|
| ArrowDown | Move to next enabled item (select + focus). |
| ArrowRight (ArrowLeft under `rtl`) | Same as ArrowDown (horizontal-next, mirrored by direction). |
| ArrowUp | Move to previous enabled item (select + focus). |
| ArrowLeft (ArrowRight under `rtl`) | Same as ArrowUp (horizontal-prev, mirrored by direction). |
| Home | Move to the first enabled item. |
| End | Move to the last enabled item. |
| Space (on the focused item) | Selects the focused radio (Enter is deliberately not an activation key, so native form submit is unaffected). |

Arrow/Home/End skip disabled items. With `Loop=true` (default) movement wraps at the ends; with `Loop=false` movement clamps (no-ops past the ends). All keyboard handling is disabled group-wide when `Disabled` is true.

## Accessibility

- Root: `role="radiogroup"`, `aria-orientation` (only when `Orientation` is explicitly set), `aria-required` (`"true"` when `Required`, else omitted), `dir` (only when explicit/cascaded).
- Item: `role="radio"`, `aria-checked` (`"true"`/`"false"`, always present), `aria-readonly` (only when `ReadOnly`), `aria-required` (only when required).
- Roving tabindex: the checked item is `tabindex="0"`; when nothing is selected, the first enabled item is `tabindex="0"` so the group stays reachable; all other items are `tabindex="-1"`. Disabled items are always `-1`.
- Focus moves synchronously with selection on arrow/Home/End (automatic activation per WAI-ARIA APG), driven from C# via `ElementReference.FocusAsync()` since the rendering engine cannot touch the DOM directly.

## WPF strategy

Tier B (custom lookless control). Native `RadioButton` + `GroupName` gives basic mutual exclusion and default arrow-key traversal, but it does not support: `Loop=false` clamping, `ReadOnly` (focusable-but-immutable), RTL-mirrored horizontal arrows, an undefined-by-default `Orientation`, or a separately-composable Indicator part driven by `keepMounted`. Build a custom lookless `RadioGroup` container (`Selector`-derived or a plain `ItemsControl`/`Panel` with a custom `KeyDown` handler replicating the exact automatic-activation model above) hosting items that derive from `RadioButton` (Tier A for the item itself) with a custom `AutomationPeer` exposing `SelectionItemPattern`/`ToggleProvider` semantics matching `role="radio"`/`aria-checked`. The indicator's conditional-mount-vs-`KeepMounted` behavior and the hidden native-input form mirror have no WPF equivalent and will need bespoke implementation (e.g. a `Visibility`-toggled `ContentPresenter` and no native form submission concept to mirror into).

## Open questions

- Whether the WPF port should lean on `RadioButton.GroupName`'s native mutual exclusion at all, or fully own selection state in the custom context (as the Blazor version does) to keep controlled/uncontrolled parity exact.
- How to express the RTL-mirrored `ArrowLeft`/`ArrowRight` semantics given WPF's `FlowDirection` versus this component's explicit `Dir`/cascaded-`NaviusDirection` string parameter.
- Whether `NaviusBubbleInput`'s hidden-native-input form-submission mirror needs a WPF analog at all, since WPF has no native HTML form submission model.
