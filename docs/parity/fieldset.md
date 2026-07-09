# Fieldset

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusFieldset | `fieldset[data-navius-fieldset]` | Root. Native `<fieldset>` grouping related fields under a shared legend; cascades a `FieldsetContext` carrying `Disabled`. |
| NaviusFieldsetLegend | `div[data-navius-fieldset-legend]` | Rendered as a `<div>` (not a native `<legend>`) for positioning freedom; reflects the fieldset's disabled state. |

## Parameters

### NaviusFieldset

| Name | Type | Default | Notes |
|---|---|---|---|
| Disabled | bool | `false` | Sets the native `disabled` attribute (disabling every contained control for free) and cascades a new `FieldsetContext { Disabled = ... }` whenever it changes. |
| ChildContent | RenderFragment? | `null` | Fieldset body (legend + fields). |
| Attributes | IDictionary<string,object>? | `null` | `[Parameter(CaptureUnmatchedValues = true)]`; splatted onto the `<fieldset>`. |

### NaviusFieldsetLegend

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Legend content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<div>`. |

## Events

None. No `[Parameter]` `EventCallback` properties exist anywhere in this family.

## State + data attributes

| Attribute | Element | Set when |
|---|---|---|
| `data-navius-fieldset` | NaviusFieldset `<fieldset>` | always |
| `disabled` (native) | NaviusFieldset `<fieldset>` | `Disabled` true |
| `data-disabled` | NaviusFieldset `<fieldset>` | `Disabled` true |
| `data-navius-fieldset-legend` | NaviusFieldsetLegend `<div>` | always |
| `data-disabled` | NaviusFieldsetLegend `<div>` | cascaded `FieldsetContext.Disabled` true |

`FieldsetContext.Disabled` is also read by `NaviusField` (Field family): a field's effective `Disabled` is `Disabled || (Fieldset?.Disabled ?? false)`, so an enclosing fieldset's disabled state cascades into every `NaviusField` inside it, not just the legend. `FieldsetContext` is an immutable `sealed class` with only one property (`Disabled`); `NaviusFieldset` allocates a new instance (rather than mutating) whenever `Disabled` changes, so cascaded consumers re-render.

## Keyboard

No component-level keyboard handling; neither `.razor` file registers a `KeyDown`/`OnKeyDown` handler. When `Disabled` is true, the native `<fieldset disabled>` attribute disables every contained control (native browser behavior, not custom code, per the code comment "the native fieldset disables every contained control for free"). No `tests/e2e` files exist for this family (confirmed by glob search) to cross-check.

## Accessibility

- `NaviusFieldset` renders a real `<fieldset>`, so it carries native fieldset/group semantics implicitly.
- `NaviusFieldsetLegend` is a plain `<div>`, not a native `<legend>`. The code comment states it is "automatically associated as the fieldset's label," but no `id`/`aria-labelledby` (or any other ARIA) wiring exists in the component code to establish that association explicitly; see Open Questions.
- No `FocusAsync`, `tabindex`, or `aria-*` attributes appear anywhere in this family's code.

## WPF strategy

Tier A (derive from native `GroupBox`). WPF's `IsEnabled` already inherits down the visual/logical tree the same way the native `<fieldset disabled>` "disables every contained control for free," so the core disabled-cascade behavior translates directly without reimplementing propagation logic; `GroupBox.Header` maps to the legend. What will not translate cleanly: `GroupBox`'s built-in `ControlTemplate` positions its header inline with the border in a fixed spot, whereas `NaviusFieldsetLegend` is deliberately a free-floating `<div>` "for positioning freedom," so a re-templated `GroupBox` (or a custom lookless control) is needed to preserve that flexibility. The legend-to-fieldset accessible-name association claimed by the Blazor code comment is unverified in the actual markup (see Open Questions) and should not be assumed as a parity target without re-checking real browser/AT behavior first.

## Open questions

- The code comment on `NaviusFieldsetLegend` claims it is "automatically associated as the fieldset's label," but the rendered markup has no `id`/`aria-labelledby`/`for` wiring between the `<div>` legend and the `<fieldset>`. Native `<fieldset>` auto-naming from a `<legend>` only applies to a real `<legend>` element as first child, which this is not (it's a `<div>`). Needs verification of actual runtime/AT behavior before deciding whether the WPF `GroupBox.Header`/`AutomationProperties.LabeledBy` needs explicit wiring to preserve (or correct) this.
- `FieldsetContext` exposes only `Disabled`; confirm no other fieldset-scoped state (e.g. a shared `Name`) is needed for the WPF `GroupBox`-based port.
- No visual styling is asserted by this component beyond `data-disabled` (styling is left to consuming CSS); the WPF port needs a defined "disabled" visual to hit parity, since `GroupBox`'s default disabled look may differ from the web theme's.
