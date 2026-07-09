# Separator

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSeparator | `div` (`role="separator"` when not `Decorative`; a plain `div` with no role when `Decorative`) | The only part in this family; a single self-contained line/divider element. |

## Parameters

**NaviusSeparator**

| Name | Type | Default | Notes |
|---|---|---|---|
| Orientation | `string` | `"horizontal"` | Normalized for output: any value other than `"vertical"` renders as horizontal (the spec's `isValidOrientation` guard), so an invalid token like `"diagonal"` never leaks into `data-orientation`. |
| Decorative | `bool` | `false` | When `true`, renders a plain `div` with no `role` and no `aria-orientation` (purely visual, removed from the accessibility tree). |
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

## Events

None. `NaviusSeparator` exposes no `EventCallback` parameters.

## State + data attributes

- `data-orientation`: always present, normalized to exactly `"horizontal"` or `"vertical"` (never an invalid pass-through value).
- `data-navius-separator`: marker attribute, always present.

No other public state; the component is a pure function of its two parameters.

## Keyboard

None. Separator is non-interactive and not part of the tab order; it has no keyboard handling of any kind.

## Accessibility

- Non-decorative (`Decorative=false`, default): `role="separator"`, `aria-orientation` (only rendered when `Orientation == "vertical"`; the horizontal default omits `aria-orientation` entirely, matching the spec's `orientation === 'vertical' ? orientation : undefined`).
- Decorative (`Decorative=true`): no `role`, no `aria-orientation` at all, removing the element from the accessibility tree (purely presentational).
- No focus management: never focusable, no `tabindex` in either mode.

## WPF strategy

Tier B (custom lookless control). WPF ships `System.Windows.Controls.Separator`, but it has no first-class `Orientation` property of its own (it visually adapts to a hosting `ItemsControl`'s orientation, e.g. inside a `Menu`/`ToolBar`, rather than being independently orientable), so it can't cleanly express this component's standalone `Orientation="vertical"`. Build a small custom `Control` (or retemplate `Separator`) with its own `Orientation` dependency property and a template that swaps a horizontal vs. vertical line/rule. Map non-decorative mode to a custom `AutomationPeer` returning `AutomationControlType.Separator` (mirroring `role="separator"`) plus an orientation-aware `GetOrientation()` override; map `Decorative=true` to `AutomationPeer.IsControlElementForAutomation()`/`IsContentElementForAutomation()` returning `false` (removed from the accessibility tree), since WPF has no direct "omit role" concept like a conditionally-absent HTML `role` attribute.

## Open questions

- Whether `Decorative` should be modeled as two different automation-peer behaviors (as above) or as two structurally different lookless templates, given the source literally renders two different `div`s (with/without `role`) rather than toggling an attribute.
- Whether the WPF port needs a dependency-property-level guard reproducing `isValidOrientation`'s silent fallback-to-horizontal behavior, or whether an `OrientationConverter`/enum (`Horizontal`/`Vertical`) makes invalid values structurally impossible, obsoleting the guard entirely.
