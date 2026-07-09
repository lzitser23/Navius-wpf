# AspectRatio

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusAspectRatio | `<div>` (outer) wrapping a `<div>` (inner) | Constrains its content to a fixed width:height ratio using the padding-bottom percentage-hack; the outer `<div>` sizes the box, the inner `<div>` absolutely positions the content within it |

## Parameters

### NaviusAspectRatio

| Name | Type | Default | Notes |
|---|---|---|---|
| Ratio | double | `1.0` | Desired width:height ratio. `EffectiveRatio` falls back to `1.0` if `Ratio <= 0` |
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values (`CaptureUnmatchedValues = true`); forwarded to the inner `<div>` (`ForwardedAttributes`), with `style` stripped and merged separately |

## Events

None. No `EventCallback<T>` parameters in this family.

## State + data attributes

| Attribute | Element | Notes |
|---|---|---|
| `data-navius-aspect-ratio` | outer `<div>` | Marker |
| `data-navius-aspect-ratio-inner` | inner `<div>` | Marker |

No public state enums or context class; the component is stateless and computes its layout purely from `Ratio` and (optionally) a consumer-supplied `style` attribute.

Computed inline styles (not data attributes, but part of the rendered contract):
- Outer: `position:relative;width:100%;padding-bottom:{100.0/EffectiveRatio}%` (the classic padding-bottom aspect-ratio hack).
- Inner: `position:absolute;top:0;right:0;bottom:0;left:0`, appended after any consumer-supplied `style` value (the consumer's style is spread first, then the positioning keys are appended so positioning always wins, mirroring the spec's `{...style, position:'absolute', top/right/bottom/left:0}` behavior).

## Keyboard

No keyboard interaction implemented in this family.

## Accessibility

No ARIA role or `aria-*` attribute is wired by this component. It is a pure layout primitive; the outer/inner `<div>` pair carries no semantic role, and no focus management is implemented (there is nothing interactive to manage focus for).

## WPF strategy

Tier A (derive from a `Viewbox`-adjacent layout, or more precisely a custom `Panel`/`ContentControl` implementing `MeasureOverride`/`ArrangeOverride` to enforce a fixed aspect ratio, similar in spirit to WPF's `Viewbox` but constraining shape rather than scaling content). No stock WPF control directly enforces "child fills 100% width, height is computed from a ratio" the way this CSS padding-bottom hack does; the closest built-in primitive, `Viewbox`, scales content to fit rather than reshaping a container, so a custom control overriding `MeasureOverride` to compute `availableSize.Width / Ratio` for the desired height is the correct 1:1 behavioral port. There is no ARIA role here to map (the component renders no semantic role in the web version either), so no `AutomationPeer` role mapping is needed beyond the default `FrameworkElementAutomationPeer`. The CSS-specific padding-bottom percentage hack and the "style spread with positioning appended last" merge logic (`InnerStyle`/`ForwardedAttributes`) do NOT translate: WPF has no CSS-style cascade to replicate, so the custom control simply lays out its single child to fill the computed box directly via `ArrangeOverride`, with no equivalent of stripping/reordering a `style` attribute needed.

## Open questions

- Whether the WPF port needs an equivalent of `ForwardedAttributes`/arbitrary-attribute-forwarding at all, given WPF's `DependencyProperty` model has no free-form attribute bag comparable to Blazor's `CaptureUnmatchedValues`; likely a no-op or replaced by standard WPF property inheritance/binding on the custom control itself.
- Whether the ratio should be exposed as a `double` (`Width/Height`) as in the source, or as a more WPF-idiomatic `double Ratio` plus convenience static values, matching call sites elsewhere in the port.
- Whether `EffectiveRatio`'s silent fallback to `1.0` for non-positive `Ratio` values should be preserved as-is or should assert/throw in the WPF port (the source is silent about invalid input beyond this fallback).
