# Collapsible

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusCollapsible` | `<div>` | Root; owns open/closed state via `CollapsibleContext`, cascades it to children |
| `NaviusCollapsibleTrigger` | `<button type="button">` | Toggles the collapsible open state on click |
| `NaviusCollapsiblePanel` | `<div>` (conditionally rendered) | Animatable panel content; manages mount/unmount and enter/exit transition-phase attributes |

## Parameters

### NaviusCollapsible

| Name | Type | Default | Notes |
|---|---|---|---|
| `Open` | `bool` | `false` | Controlled open state; controlled-ness is detected via `OpenChanged.HasDelegate` |
| `OpenChanged` | `EventCallback<bool>` | none | Pairs with `Open`; its presence (`HasDelegate`) determines whether the component is controlled |
| `DefaultOpen` | `bool` | `false` | Uncontrolled initial open state |
| `Disabled` | `bool` | `false` | Blocks `RequestSetOpenAsync` (both trigger clicks and any other requester) when true |
| `ChildContent` | `RenderFragment?` | `none` | |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes |

### NaviusCollapsibleTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `none` | |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes |

### NaviusCollapsiblePanel

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `none` | |
| `KeepMounted` | `bool` | `false` | Keep the panel mounted (hidden) while closed instead of removing it from the DOM |
| `HiddenUntilFound` | `bool` | `false` | Keep mounted as `hidden="until-found"` so browser in-page find can reveal it (implies `KeepMounted`); a find-match opens the collapsible |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes |

## Events

| Part | Event | Payload |
|---|---|---|
| `NaviusCollapsible` | `OpenChanged` | `bool` |

`NaviusCollapsibleTrigger` and `NaviusCollapsiblePanel` expose no `[Parameter] EventCallback` of their own; both route user interaction back through `CollapsibleContext.RequestSetAsync`/`RequestToggleAsync`, which ultimately raises the root's `OpenChanged`.

## State + data attributes

`NaviusCollapsible` (root div): no data-* state attributes at all (the code comment notes "Base UI Collapsible.Root exposes no data-* state attributes"), only `data-navius-collapsible` marker.

`NaviusCollapsibleTrigger` (button): `data-panel-open` (present when open), `data-disabled` (present when disabled), `data-navius-collapsible-trigger` marker, plus `aria-expanded` / `aria-controls` (see Accessibility).

`NaviusCollapsiblePanel` (div): `data-open` (present when open), `data-closed` (present when not open), `data-starting-style` (present while entering, before the first animation frame commits), `data-ending-style` (present while the exit transition runs), `data-navius-collapsible-panel` marker.

`CollapsibleContext` (internal shared state class): `Open` (bool, settable only internally via `SetOpenInternalAsync`), `Disabled`, `PanelId` (stable GUID-based id wiring trigger `aria-controls` to the panel's `id`).

## Keyboard

No keyboard interaction implemented in this family. `NaviusCollapsibleTrigger` renders a native `<button type="button">` with only an `@onclick` handler; there is no `@onkeydown` anywhere in the Collapsible family, so any Space/Enter activation comes solely from native button click semantics, not a custom handler.

## Accessibility

- `NaviusCollapsibleTrigger`: `aria-expanded="true"/"false"` reflecting `Context.Open`; `aria-controls` set to `Context.PanelId`, wiring it to the panel's `id`
- `NaviusCollapsiblePanel`: `id="@Context.PanelId"` (the `aria-controls` target); `hidden` attribute set to `""` (or `"until-found"` when `HiddenUntilFound`) once fully closed and settled, and only when the panel is being kept mounted (`Mounted`/`KeepMounted`/`HiddenUntilFound`), never while animating in/out
- `HiddenUntilFound` wires a `beforematch` JS listener (`ObserveBeforeMatchAsync`) so the browser's in-page find revealing the hidden panel calls back into `[JSInvokable] OnBeforeMatch()`, which opens the collapsible via `Context.RequestSetAsync(true)`
- No explicit focus-management code (e.g. no focus is moved into/out of the panel on open/close)

## WPF strategy

Tier A (derive from native `System.Windows.Controls.Expander`). `Expander.IsExpanded` maps directly to `Open`/`DefaultOpen`/`OpenChanged`; `aria-expanded`/`role` implied by trigger maps to `ExpanderAutomationPeer` with `ExpandCollapsePattern` (WPF's built-in peer already reports `ExpandCollapseState`). The trigger/panel split (`NaviusCollapsibleTrigger` + `NaviusCollapsiblePanel` as separate cascading-context-driven parts) does not exist natively in `Expander` (which owns its own header/content templating); a lookless-control approach with a custom `ControlTemplate` exposing named `Header`/`Content` template parts is the closest fit if the trigger/panel separation must be preserved as distinct addressable elements. The panel's enter/exit "starting-style"/"ending-style" transition-phase choreography and JS-measured natural-size CSS variables (`--collapsible-panel-width/height`) are a browser-CSS-transition pattern with no WPF equivalent; they should be reimplemented as WPF/XAML storyboard animations (e.g. animating `Height`/`RowDefinition.Height` with a measured `DesiredSize`), and the `HiddenUntilFound`/browser-find-triggered-open behavior has no WPF analog at all and should be dropped or retired for this port.

## Open questions

- Is `HiddenUntilFound` (browser in-page-find integration) in scope for the WPF port at all, given WPF has no equivalent "find in page" affordance to hook into?
- Should the WPF port preserve the exact "no data-state on root" asymmetry (root carries no open/closed attribute; only the trigger and panel do), or should a `IsExpanded` dependency property be exposed uniformly on the root for XAML style triggers?
- The panel's `KeepMounted`/unmount-after-exit-animation timing (awaiting `WaitForAnimationsAsync` before removing from the visual tree) depends on the web animation engine; what is the equivalent completion signal for a WPF storyboard (e.g. `Storyboard.Completed`) and does the port need to replicate the "mount at starting-style for one frame, then transition" two-phase sequencing?

## WPF implementation notes

Implemented as a lookless Tier B family: `Controls/Collapsible/NaviusCollapsible.cs` (root), `NaviusCollapsibleTrigger.cs`, `NaviusCollapsiblePanel.cs`, styled by `Themes/Collapsible.xaml`.

- The WPF strategy's suggested `Expander` derivation was not used. `Expander` fuses header and content into one control and cannot preserve the contract's three separately addressable parts, so this instead follows `NaviusRadioGroup`'s "root owns state, discovers Trigger/Panel descendants via the logical tree" shape: `NaviusCollapsible` listens for the bubbled `ButtonBase.ClickEvent` from any descendant `NaviusCollapsibleTrigger` and pushes `Open` back down to every descendant Trigger/Panel, with no explicit registration step.
- Open question 1 (`HiddenUntilFound`) resolved: dropped entirely. There is no WPF "find in page" affordance to hook a reveal callback into, so this parameter does not exist on `NaviusCollapsiblePanel`.
- Open question 2 (root data-state asymmetry) resolved: preserved as-is. `NaviusCollapsible`'s automation peer reports a plain `Group` with no expand/collapse pattern; `ExpandCollapsePattern`/`aria-expanded`-equivalent state lives only on the Trigger's peer (`NaviusCollapsibleTriggerAutomationPeer`, implementing both `IInvokeProvider` and `IExpandCollapseProvider`, the same dual-pattern shape WPF's own `ExpanderAutomationPeer` uses).
- Open question 3 (completion signal / two-phase sequencing) resolved: the enter/exit transition is a plain `DoubleAnimation` on `Height`, measured via `UIElement.Measure` (no SizeObserver equivalent needed), factored into a shared `PanelHeightAnimator` helper (`Controls/Internal/PanelHeightAnimator.cs`, also reused by Accordion). "Exit completes before collapse" uses `DoubleAnimation.Completed` as the signal; no two-phase "mount at starting-style for one frame" sequencing is needed since Height animates from a hard 0.
- `KeepMounted`: `Visibility.Collapsed` is used uniformly for the closed state. `KeepMounted=false` (default) additionally caches and clears `Content` once the close animation's `Completed` callback fires, approximating DOM removal; `KeepMounted=true` leaves `Content` in place.
- Logical open/closed state (the `Open` DP, the Trigger's `IsPanelOpen`, the Panel's `IsOpen`, and the automation peer's `ExpandCollapseState`) is intentionally decoupled from animation completion: it flips synchronously on toggle so it stays correct and unit-testable even in a headless test host where nothing pumps the animation clock to completion. Only the cosmetic Height/Visibility transition is asynchronous.
- Disabled is not reimplemented as its own named parameter: the root's native `IsEnabled` is reused, but WPF's `IsEnabled` does **not** automatically cascade through a `ContentControl`'s logical `Content` the way it does through a `Panel`'s `Children` (verified empirically; a bare `ContentControl.IsEnabled=false` does not disable a `Button` set as its `Content`, with or without `ApplyTemplate`). `SyncDescendants` therefore explicitly pushes `IsEnabled` down onto the Trigger, matching "Disabled blocks RequestSetOpenAsync."

## M6 audit (2026-07-09)

Adversarially re-verified `NaviusCollapsible`/`NaviusCollapsibleTrigger`/`NaviusCollapsiblePanel`
against this doc's claims: the root-owns-state/discovers-descendants-via-logical-tree shape, the
`IsEnabled`-does-not-auto-cascade claim (this is the empirical finding `accordion.md` and this doc
both cite -- cross-checked and internally consistent between the two families), the dual-pattern
`NaviusCollapsibleTriggerAutomationPeer` (Invoke + ExpandCollapse, mirroring `ExpanderAutomationPeer`),
the root's plain `Group`-type peer with no data-state attribute (matching the contract's "root
carries no data-* state" asymmetry), `KeepMounted`/`PanelHeightAnimator` mount lifecycle, and
`HiddenUntilFound`'s documented drop. All check out against the code and `CollapsibleTests.cs`. No
confirmed or plausible disparities found.
