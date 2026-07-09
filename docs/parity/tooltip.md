# Tooltip

Built on the shared anchored-overlay machinery (`OverlayAnchoredPopupBase`, `OverlayPositionerBase`, `IAnchoredOverlayContext` in `Navius.Primitives.Components.Overlays`, outside this batch).

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTooltipProvider | No own DOM; cascades `TooltipProviderContext` | Shared cross-tooltip defaults (delay, skip-delay, hoverable) and the skip-delay grace window; wrap once near the app root |
| NaviusTooltip | No own DOM; cascades `TooltipContext` + itself (as `NaviusTooltipRoot`) | Root: owns open state (controlled/uncontrolled) and show/hide timing (hover-intent delay, instant-open reasons) |
| NaviusTooltipTrigger | `<button data-navius-tooltip-trigger>` | Opens on hover (after delay) or keyboard focus (immediately); closes on leave/blur/Escape/activation |
| NaviusTooltipPortal | No own DOM (flag-setter only) | Records custom mount container + `KeepMounted` into `TooltipContext`; teleport performed by the Popup |
| NaviusTooltipPositioner | No own DOM (flag-setter only, inherits `OverlayPositionerBase`) | Owns placement (side/align/offsets/collision); publishes options into the context, default side "top" |
| NaviusTooltipPopup | `<NaviusPortal><div data-navius-tooltip-positioner><div role="tooltip" data-navius-tooltip-popup>` | The tooltip bubble; non-modal, non-focusable, hoverable by default |
| NaviusTooltipArrow | `<svg data-navius-tooltip-arrow aria-hidden="true">` | SVG triangle pointing at the trigger; registers its element into the positioner's arrow middleware |

## Parameters

### NaviusTooltipProvider

| Name | Type | Default | Notes |
|---|---|---|---|
| DelayDuration | `int` | 700 | Default hover-intent delay (ms) inherited by descendant tooltips that don't set their own |
| SkipDelayDuration | `int` | 300 | Grace window (ms) after a tooltip closes during which the next one opens instantly |
| DisableHoverableContent | `bool` | false | When true, tooltips close on trigger leave even when the pointer moves over the content |
| ChildContent | `RenderFragment?` | null | |

### NaviusTooltip

| Name | Type | Default | Notes |
|---|---|---|---|
| DelayDuration | `int?` | null | Falls back to Provider's `DelayDuration`, then `OpenDelay` |
| OpenDelay | `int` | 200 | Legacy alias for `DelayDuration`, kept for back-compat |
| DisableHoverableContent | `bool?` | null | Overrides the provider's setting for this tooltip |
| Open | `bool` | false | Controlled (`@bind-Open`) |
| OpenChanged | `EventCallback<bool>` | | |
| DefaultOpen | `bool` | false | Uncontrolled initial open state |
| ChildContent | `RenderFragment?` | null | |

### NaviusTooltipTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTooltipPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Container | `string?` | null | CSS selector of custom mount container; null teleports into `document.body` |
| KeepMounted | `bool` | false | Keep popup mounted while closed, for exit animations |

### NaviusTooltipPositioner

Inherits `OverlayPositionerBase` (placement params: side/align/offsets/collision, defined in the shared Overlays base, not repeated here). `DefaultSide = "top"`.

### NaviusTooltipPopup

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| (inherited from `OverlayAnchoredPopupBase`) | | | `KeepMounted`, `Attributes`, etc. from the shared base, not repeated here |

### NaviusTooltipArrow

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | `double` | 10 | |
| Height | `double` | 5 | |
| Attributes | `IDictionary<string,object>?` | null | |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTooltip | OpenChanged | `EventCallback<bool>`, fired whenever open state changes (hover delay elapsed, focus, Escape, leave, activation) |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Trigger | `aria-describedby` (points at popup content id, only while open), `data-popup-open`, `data-navius-tooltip-trigger` | |
| Popup positioner div | `data-navius-tooltip-positioner`, inline `style="position:fixed;top:0;left:0;margin:0"`, engine-provided positioning attributes (`data-side`/`data-align`, written by the shared overlay engine, not shown directly in this file) | |
| Popup | `role="tooltip"`, `id` (`Context.ContentId`), `data-open`/`data-closed`, `data-instant` (present when opened via focus or the skip-delay window, i.e. `TooltipOpenReason.Instant`), `data-starting-style`/`data-ending-style` (enter/exit animation hooks), `data-navius-tooltip-popup` | |
| Arrow | `data-navius-tooltip-arrow`, `aria-hidden="true"`, engine-written `data-side` | |
| TooltipContext (C# state) | `Open`, `OpenReason` (`Delayed`\|`Instant`), `IsInstant` (derived), `PointerInContent`, `ContentId`, `TriggerElement`/`HasTrigger`, `PortalContainer`/`PortalKeepMounted`, `Options` (placement), `ArrowElement`/`HasArrow` | Implements `IAnchoredOverlayContext`; `Changed`/`ArrowChanged` events |
| TooltipProviderContext (C# state) | `DelayDuration`, `SkipDelayDuration`, `DisableHoverableContent`, internal skip-window stopwatch/`_lastCloseMs` | Not cascaded as reactive state (no `Changed` event); read once per tooltip's timing decisions |

## Keyboard

Handled on `NaviusTooltipTrigger`'s `@onkeydown`/`@onfocus`/`@onblur`.

| Key / Event | Behavior |
|---|---|
| Focus (keyboard) | Opens immediately, no delay (`Context.OpenNow`) |
| Blur | Closes (`Context.Close`, subject to the hoverable-content grace window) |
| Escape | Force-closes immediately, bypassing the hoverable grace (`Root.ForceCloseAsync`) |
| Space / Enter (while open) | Force-closes immediately (activating the trigger dismisses the tooltip so it doesn't linger over the activated control) |
| Pointer enter (trigger) | Opens after delay (`Context.OpenDelayed`; skips the delay entirely if `Provider.ShouldSkipDelay()` is true) |
| Pointer leave (trigger) | Closes, with a 60ms grace window (`HoverGraceMs`) if hoverable content is enabled, to bridge the trigger→popup gap without flicker |
| Pointer down (trigger) | If open, force-closes (pressing the trigger dismisses the tooltip) |
| Pointer enter/leave (popup, when hoverable) | Enter sets `PointerInContent = true` (keeps it open); leave clears it and force-closes |

## Accessibility

- Trigger: `aria-describedby` linking to the popup's `id` only while open, so screen readers announce the tooltip content when it appears.
- Popup: `role="tooltip"`; not focusable, non-modal (`Modal => false` on `TooltipContext`, `MoveFocusInside => false` on the Popup); no focus trap, scroll lock, or backdrop, unlike Popover/Menu/Dialog.
- Arrow: `aria-hidden="true"` (purely decorative).
- `data-instant` distinguishes an instantly-opened tooltip (focus, or the cross-tooltip skip-delay grace window) from a hover-delayed one, primarily for CSS transition control rather than a11y per se.
- The skip-delay window (`TooltipProviderContext`) lets a user tabbing/hovering rapidly between adjacent controls see tooltips without repeated full delays, an established tooltip usability pattern.

## WPF strategy

Tier A (with custom timing logic layered on): derive from `System.Windows.Controls.ToolTip`/`ToolTipService`, which WPF already implements as a `Popup`-based non-modal, non-focusable overlay with its own `ToolTipAutomationPeer` mapping to UIA (`role="tooltip"` equivalent) and built-in `ToolTipService.InitialShowDelay`/`BetweenShowDelay`/`ShowDuration` attached properties that closely mirror `DelayDuration`/`SkipDelayDuration`. The custom parts here (hoverable-content grace window bridging trigger→popup, `data-instant` distinguishing focus-open from hover-open, per-tooltip vs. provider-level delay override) go beyond `ToolTip`'s stock behavior and need a thin custom layer: a `Popup`-hosted lookless `Control` reusing `ToolTipService`'s positioning where possible, with the 60ms `HoverGraceMs` and skip-delay stopwatch logic ported as plain C# timers (`DispatcherTimer` replacing `Task.Delay`+`CancellationTokenSource`). `TooltipProviderContext`'s cross-tooltip skip-window state should become an app-wide singleton/attached-property service analogous to `ToolTipService`'s static config, since WPF tooltips are not naturally nested in a provider tree the way Blazor's cascading-parameter model requires.

## Open questions

- Depends on the shared anchored-overlay base (`OverlayAnchoredPopupBase`/`OverlayPositionerBase`, outside this batch) for portal/positioner/enter-exit-animation plumbing; the WPF strategy here assumes that shared machinery becomes a `Popup`-based base reused by Tooltip, Popover, and Menu overlay families; confirm sequencing.
- WPF's native `ToolTip` opens/closes based on `IsMouseOver`/`ToolTipService` internals that may fight a fully custom timing implementation; decide whether to build entirely custom (`Popup` + manual triggers) rather than trying to graft custom delay logic onto `ToolTipService`.
- The skip-delay "recently open" grace window is inherently a cross-control, app-wide concept; needs a clear owner in the WPF port (static service vs. DI-registered singleton) since there is no Blazor-style component tree cascade to lean on.
