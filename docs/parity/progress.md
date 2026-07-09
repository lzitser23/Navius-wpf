# Progress

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusProgress | `div` (`role="progressbar"`) | Root. Computes derived state (value, max, fraction, percentage, indeterminate) in `ProgressContext` and cascades it to all descendants. |
| NaviusProgressIndicator | `div` | Visual fill element; mirrors the root's discrete state attributes. Content/sizing is left to the consumer. |
| NaviusProgressTrack | `div` | Visual rail that contains the Indicator. Mirrors the root's discrete state attributes. |
| NaviusProgressValue | `span` | Renders the formatted value text; defaults to the rounded percentage (`{}%`), empty while indeterminate. `ChildContent` overrides the default text. |
| NaviusProgressLabel | `span` | Accessible label; registers itself with the context so the root can wire `aria-labelledby` to its id. |

## Parameters

**NaviusProgress**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `double?` | `null` | `null` renders an indeterminate progress bar. Validated, not clamped: NaN, infinite, negative, or > Max is treated as invalid and coerced to `null` (indeterminate), with a `Debug.WriteLine` warning. |
| Max | `double` | `100` | A value `<= 0` falls back to `100`. |
| GetValueLabel | `Func<double?, double, string?>?` | `null` | Builds `aria-valuetext` for a determinate value. When omitted, defaults to `Math.Round((value/max)*100) + "%"`. Never invoked while indeterminate. |
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`, splatted onto the root `div`. |

**NaviusProgressIndicator**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusProgressTrack**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusProgressValue**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | `null` | When provided, overrides the default rounded-percentage text. |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusProgressLabel**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | `null` | |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

## Events

None. No part in the Progress family exposes an `EventCallback` parameter; Progress is a read-only display primitive with no user-driven value (no `ValueChanged`).

## State + data attributes

`ProgressContext` computes three discrete, mutually-informative state attributes shared by the root and every part (present as `""` when active, omitted/`null` otherwise):

- `data-complete`: set when determinate and `Value >= Max`.
- `data-indeterminate`: set when `Value is null`.
- `data-progressing`: set when determinate and `Value < Max`.

Each part also carries its own scoped marker attribute: `data-navius-progress` (root), `data-navius-progress-indicator`, `data-navius-progress-track`, `data-navius-progress-value`, `data-navius-progress-label`.

Public derived state exposed via `ProgressContext`: `Value`, `Max`, `IsIndeterminate`, `Fraction` (0..1, clamped), `Percentage` (`Fraction * 100`), `LabelId` (stable GUID-based id), `HasLabel` (true once a Label part has registered; triggers a re-render via the `Changed` event so the root can add `aria-labelledby` after first render).

## Keyboard

None. Progress is a non-interactive, read-only indicator; it does not handle any keyboard input and is not part of the tab order.

## Accessibility

- Root: `role="progressbar"`, `aria-valuemin="0"`, `aria-valuemax="{Max}"`, `aria-valuenow` (the current value, omitted/`null` while indeterminate), `aria-valuetext` (via `GetValueLabel` or the default rounded-percentage string; omitted while indeterminate), `aria-labelledby` (set to the Label part's id only once a Label has registered, otherwise omitted).
- No focus management: the root and all parts are plain `div`/`span` elements with no `tabindex`; nothing in the family is keyboard-focusable.

## WPF strategy

Tier A (derive from native WPF control). Base the root on `System.Windows.Controls.ProgressBar`: its built-in `ProgressBarAutomationPeer` implements `IRangeValueProvider`, mapping cleanly onto `aria-valuemin`/`aria-valuemax`/`aria-valuenow`. Retemplate it (lookless) to expose named parts `PART_Track` and `PART_Indicator` matching Track/Indicator, and add attached-property equivalents of the discrete state attributes via triggers/VisualStateManager driven off `IsIndeterminate` and `Value >= Maximum`. `IsIndeterminate` maps directly to `ProgressBar.IsIndeterminate`. `GetValueLabel`/`aria-valuetext` and the Label-registration-driven `aria-labelledby` have no first-class WPF automation equivalent; they will need a custom `AutomationPeer` override (`GetItemStatus`/`HelpText`) or `AutomationProperties.LabeledBy` binding, since `ProgressBarAutomationPeer` does not natively surface value text.

## Open questions

- Whether `IsValidValue`'s strict validate-don't-clamp behavior (value outside `[0, Max]` becomes fully indeterminate, not clamped) should be preserved exactly, since WPF's `ProgressBar` silently clamps `Value` to `[Minimum, Maximum]` by default.
- How to replicate the `HasLabel`/`Changed`-event driven `aria-labelledby` wiring (a child registering itself and forcing a parent re-render) in WPF's synchronous layout/binding model, since AutomationPeers are typically queried on demand rather than pushed to.
- Whether `GetValueLabel`'s free-form `Func<double?, double, string?>` should become a `IValueConverter`, a delegate property, or a routed "formatting" event in the WPF port.
