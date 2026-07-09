# Toast

Mirrors Base UI's unified Toast tree (manager-driven and manual usage share one `NaviusToastRoot`). Built on the shared `OverlayPresence` machine (`Navius.Primitives.Components.Overlays`, outside this batch) for enter/exit animation state.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusToastProvider | No own DOM; cascades `ToastProviderContext` | Root of the unified toast tree: owns/wraps a `ToastManager`, config (limit, timeout, label, swipe), re-renders on manager mutation |
| NaviusToastViewport | `<NaviusPortal><ol role="region" data-navius-toast-viewport>` + hidden announcer `<div>`s | Fixed region hosting live toasts; F6 hotkey focus, hover/focus fan-out, portalled to `document.body`, hosts polite/assertive `aria-live` announcer regions |
| NaviusToastRoot | `<li role="status"|"alert" data-navius-toast>` (conditionally rendered via `OverlayPresence`) | A single toast; manager-driven (`Toast` param) or manual (`Open`/`DefaultOpen`); owns open/enter/exit state, pausable auto-close timer, swipe-to-dismiss, Esc-to-close |
| NaviusToastContent | `<div data-navius-toast-content>` | Stacking element between Root and Title/Description/Action/Close; publishes `--toast-*` CSS vars, never computes a transform itself |
| NaviusToastTitle | `<div data-navius-toast-title>` | Toast title; id referenced by Root's `aria-labelledby` |
| NaviusToastDescription | `<div data-navius-toast-description>` | Toast description; id referenced by Root's `aria-describedby` |
| NaviusToastAction | `<button data-navius-toast-action>` | Actionable button (e.g. "Undo"); runs handler then closes the toast |
| NaviusToastClose | `<button data-navius-toast-close>` | Composable close button; closes on click |
| NaviusToastPortal | No own DOM (flag-setter only) | Records custom mount container + `KeepMounted` into `ToastProviderContext`; actual teleport happens in the Viewport |
| NaviusToastPositioner | THIN STUB: passthrough only, no engine wiring | Placement params for a hypothetical anchored toast; not used by the primary viewport-stacked toast |
| NaviusToastArrow | THIN STUB: passthrough only, no engine wiring | SVG triangle for an anchored toast; only meaningful with the (unwired) Positioner |

## Parameters

### NaviusToastProvider

| Name | Type | Default | Notes |
|---|---|---|---|
| Limit | `int` | 1 | Max simultaneously-visible toasts; rest queue (`data-limited`) |
| Timeout | `int` | 5000 | Default auto-close duration (ms) for descendant toasts |
| Label | `string` | "Notification" | Accessible label prefix announced for each toast |
| SwipeDirection | `string` | "right" | `right`\|`left`\|`up`\|`down` |
| SwipeThreshold | `double` | 50 | Swipe distance (px) required to dismiss |
| Manager | `ToastManager?` | null | Externally-created manager; falls back to DI-injected scoped instance |
| ChildContent | `RenderFragment?` | null | |

### NaviusToastViewport

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Hotkey | `string[]` | `["F6"]` | DOM `KeyboardEvent.key` values that focus the viewport |
| LabelTemplate | `Func<string,string,string>?` | null | Template for accessible label; default `"{label} ({hotkey})"` |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusToastRoot

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Toast | `ToastObject?` | null | Source toast when manager-driven; null for a manual toast |
| Open | `bool` | true | Controlled (manual usage) |
| OpenChanged | `EventCallback<bool>` | | |
| DefaultOpen | `bool` | true | Uncontrolled initial open state (manual) |
| ForceMount | `bool` | false | Keep mounted while closed (CSS-driven presence); maps to `OverlayPresence.ShouldStayMounted` |
| Timeout | `int?` | null | Per-toast override; `null` → `Toast.Timeout` → `Provider.Timeout`; `0` → sticky |
| Priority | `string` | "low" | `low` (role=status, polite) or `high` (role=alert, assertive) |
| Type | `string?` | null | Visual/semantic type (`success`\|`error`\|`loading`); defaults to `Toast.Type`; drives `data-type` |
| SwipeDirection | `string?` | null | Per-toast override; falls back to Provider default |
| OnEscapeKeyDown | `EventCallback<NaviusEscapeKeyDownEventArgs>` | | |
| OnSwipeStart | `EventCallback` | | |
| OnSwipeMove | `EventCallback<(double X, double Y)>` | | |
| OnSwipeEnd | `EventCallback<(double X, double Y)>` | | |
| OnSwipeCancel | `EventCallback` | | |
| OnPause | `EventCallback` | | |
| OnResume | `EventCallback` | | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusToastContent / NaviusToastTitle / NaviusToastDescription / NaviusToastClose

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | (all four) |
| Attributes | `IDictionary<string,object>?` | null | (all four) |

### NaviusToastAction

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| AltText | `string` | "" | Required plain-text description for assistive tech / alternative action; falls back to manager action's alt text |
| OnClick | `EventCallback` | | Optional consumer click handler, invoked before the toast closes |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusToastPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Container | `string?` | null | CSS selector of custom mount container; null teleports into `document.body` |
| KeepMounted | `bool` | false | Keep viewport mounted while empty (for exit animations) |

### NaviusToastPositioner (stub)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Side | `string` | "top" | |
| Align | `string` | "center" | |
| SideOffset | `double` | 0 | |
| AlignOffset | `double` | 0 | |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusToastArrow (stub)

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | `double` | 10 | |
| Height | `double` | 5 | |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusToastRoot | OpenChanged | `EventCallback<bool>` (manual usage) |
| NaviusToastRoot | OnEscapeKeyDown | `EventCallback<NaviusEscapeKeyDownEventArgs>`, cancelable via `DefaultPrevented` |
| NaviusToastRoot | OnSwipeStart / OnSwipeMove / OnSwipeEnd / OnSwipeCancel | Swipe gesture lifecycle, driven by JS interop callbacks |
| NaviusToastRoot | OnPause / OnResume | Auto-close timer pause/resume (hover / focus-within / window-blur) |
| NaviusToastAction | OnClick | `EventCallback`, invoked before `Context.RequestCloseAsync()` |
| ToastManager (C# imperative API, not a Blazor event but drives the tree) | `Changed` | `event Action?`, raised after `Add`/`Close`/`Remove`/`Update`/`Clear` |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="status"` (low priority) / `role="alert"` (high priority), `tabindex="0"`, `aria-live="off"`, `aria-atomic="true"`, `aria-labelledby`/`aria-describedby` (wired only when Title/Description parts present) | |
| Root | `data-navius-toast`, `data-open`/`data-closed`, `data-starting-style`/`data-ending-style` (enter/exit animation hooks from `OverlayPresence`), `data-swiping`, `data-swipe-direction`, `data-expanded`, `data-limited`, `data-type` | |
| Content | `data-navius-toast-content`, `data-behind` (index > 0), `data-expanded`, inline `style` with `--toast-index`/`--toast-height`/`--toast-offset-y`/`--toast-frontmost-height` CSS custom properties | Headless: publishes vars only, never computes a transform |
| Viewport | `role="region"`, `tabindex="-1"`, `aria-label` (templated), `data-navius-toast-viewport`, `data-expanded` | |
| Viewport announcer | Visually-hidden `<div>` with two `role="status"` children: `aria-live="polite"` and `aria-live="assertive"`, each `aria-atomic="true"` | |
| Title / Description | `id` (`TitleId`/`DescriptionId`, used by Root's aria-labelledby/describedby) | |
| ToastContext (C# state) | `TitleId`, `DescriptionId`, `HasTitle`, `HasDescription`, `Toast`, `Type`, `Priority`, `Index`, `Behind` (derived), `Expanded`, `Limited`, `Swiping`, `SwipeDirection`, `Height` | Cascaded per-Root; `Changed` event |
| ToastProviderContext (C# state) | `Manager`, `Timeout`, `Label`, `SwipeDirection`, `SwipeThreshold`, `PortalContainer`, `PortalKeepMounted`, `Gap` (16px default), `Expanded`, height aggregation (`ReportHeight`/`ForgetHeight`/`FrontmostHeight`/`OffsetYFor`) | Cascaded from Provider; `Changed`, `AssertiveRequested`, `PoliteRequested` events |
| ToastManager (C# imperative store) | `Toasts`, `Limit`; per-`ToastObject`: `Open`, `Limited` (queued beyond Limit), `UpdateKey` (bumped on `Update`, replays enter animation) | Not a data attribute, but the authoritative manager-driven state |

## Keyboard

| Key | Behavior |
|---|---|
| Escape (toast focused) | Raises `OnEscapeKeyDown`; unless `DefaultPrevented`, calls `RequestCloseAsync()` |
| F6 (anywhere on the page, configurable via `Hotkey`) | Focuses the Viewport (`tabindex="-1"` lets programmatic focus land there); implemented via JS interop (`CreateToastHotkeyAsync`) |
| Tab | Native tab order; toast root has `tabindex="0"` so the viewport's stack is reachable |

Swipe-to-dismiss is pointer/touch-driven via JS interop (`CreateToastInteractionsAsync`), not keyboard: swipe past `SwipeThreshold` in `EffectiveSwipeDirection` dismisses the toast (`OnSwipeDismissJs` -> `RequestCloseAsync`).

## Accessibility

- Root: `role="status"` (priority `low`, polite) or `role="alert"` (priority `high`, assertive), `aria-live="off"` (the toast itself does not use native live-region announcement; text is duplicated into the Viewport's dedicated announcer regions instead), `aria-atomic="true"`.
- `aria-labelledby`/`aria-describedby` wired to Title/Description ids only when those parts are actually present (`HasTitle`/`HasDescription` tracked via `ToastContext`).
- Viewport: `role="region"` with a templated accessible label (`"{Label} ({Hotkey})"`), `tabindex="-1"` so the F6 hotkey can move focus there without it being in the normal tab sequence.
- Dual announcer regions (visually-hidden, `role="status"`, `aria-live="polite"`/`aria-live="assertive"`, `aria-atomic="true"`) that each `NaviusToastRoot` pushes its resolved title+description text into on engage (`Provider.Announce`), decoupling the announcement from the toast's own DOM removal timing.
- `NaviusToastAction.AltText` is required: a plain-text description of the action for assistive tech and for users who cannot perform the underlying gesture (e.g. swipe-based undo).
- Auto-close timer pauses on hover / focus-within / window-blur (via JS interop `OnPause`/`OnResume` callbacks) so users get more time to read/interact.
- Focus stays reachable: toast `tabindex="0"` keeps it in the natural tab order while queued/limited toasts are not rendered at all (no DOM, no tab stop) until promoted.

## WPF strategy

Tier B: custom lookless control(s) coordinated by a C# manager. `ToastManager`/`ToastObject`/`ToastOptions`/`ToastProviderContext` are essentially framework-agnostic already (an in-memory notification queue with limit/timeout/type/priority) and port to WPF almost unchanged as a plain C# class, likely exposed as an `ObservableCollection<ToastObject>`-backed singleton/DI service. The Viewport maps to a `Window`-less overlay: either a `Popup`/adorner layer anchored to the main window corner, or (more idiomatically for WPF toast libraries) a dedicated always-on-top borderless `Window` per corner hosting an `ItemsControl` bound to `Manager.VisibleOrderedNewestFirst()`. `NaviusToastRoot`'s `role="status"`/`role="alert"` distinction maps to UIA's `LiveSetting` (`Polite`/`Assertive`) raised via `AutomationPeer.RaiseNotificationEvent` (Windows 10+) instead of a duplicated visually-hidden announcer div; this is a materially different (and better-supported) mechanism than the web's aria-live-region duplication trick, so the port should NOT reproduce the duplicate-announcer-region pattern verbatim. The pausable auto-close timer, swipe-to-dismiss (replace with `MouseMove`/`ManipulationDelta` drag-to-dismiss, common in WPF toast implementations), and stacking height aggregation (`ToastProviderContext.ReportHeight`/`OffsetYFor`) all port as pure C# logic feeding `TranslateTransform`/`Canvas.Top` bindings instead of CSS custom properties. `OverlayPresence`'s enter/exit-animation-gated unmount (`data-starting-style`/`data-ending-style`, `WaitForAnimations`) maps to WPF `Storyboard.Completed` callbacks gating final `Manager.Remove(id)`.

## Open questions

- Depends on `OverlayPresence` (`Navius.Primitives.Components.Overlays`, outside this batch) for its entire enter/exit lifecycle; the WPF strategy here is provisional until that shared machine's port shape is decided (likely a shared base for Toast, Popover, Menu overlays).
- `NaviusToastPositioner`/`NaviusToastArrow` are explicitly unwired stubs in the source (anchored-toast is not implemented); confirm the WPF port also skips them rather than building out anchored-toast support that doesn't exist upstream.
- The F6-viewport-focus hotkey and window-blur timer pause are DOM/browser-specific; need WPF equivalents (global `KeyBinding` on the main window, `Window.Deactivated`/`Activated`).
- Should the WPF toast host be one shared always-on-top window (simpler z-order/DPI handling) or an adorner layer within the main window (simpler focus/lifetime tie-in)? This is a real platform decision, not just a mapping exercise.
- UIA `RaiseNotificationEvent` requires Windows 10 version 1709+; confirm minimum supported OS before relying on it over the announcer-div-duplication fallback.
