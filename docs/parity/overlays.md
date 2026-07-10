# Overlays

Not a component family with `Navius*` parts. This is shared infrastructure at
`Navius.Primitives/Components/Overlays/` : abstract/base C# classes consumed by
Popover, PreviewCard, NavigationMenu, and Select. "Parts" below are the base
classes themselves; each row describes the capability a base class contributes
to its subclasses rather than a rendered UI part.

## Parts

| Part | Base class name | Purpose |
|---|---|---|
| Context contract | `IOverlayContext` / `IAnchoredOverlayContext` (`OverlayContext.cs`) | The shared open/close surface every per-component context (PopoverContext, PreviewCardContext, etc.) implements: `Open`, `Modal`, `ContentId`, trigger element, `Changed` event, `RequestCloseAsync()`. The anchored variant adds `PositionReference`, arrow wiring, and placement `Options`. Cascaded per-component; the reusable base classes below read it only through this interface so they stay component-agnostic. |
| Presence / mount lifecycle | `OverlayPresence` | The Base UI presence + enter/exit state machine shared by every animated overlay part (Backdrop, Popup). Owns the discrete `data-open`/`data-closed` + `data-starting-style`/`data-ending-style` render-gate booleans (`Rendered`, `Entering`, `Exiting`), and the mount/engage/disengage/unmount sequencing (`EngageAsync`/`DisengageAsync`/`ReEngageAsync`, `OnAfterRenderAsync` orchestration, `NextFrameAsync`/`WaitForAnimationsAsync` interop calls). |
| Dismissable popup lifecycle | `OverlayPopupBase` (inherits `OverlayPresence`) | Adds the dismissable layer (Escape / outside pointer-down), focus trap, scroll lock, and focus management (open-auto-focus, close-auto-focus, return focus to trigger) on top of presence. Dialog-family popups (Dialog/Alert Dialog/Drawer) inherit this directly. |
| Anchored positioning | `OverlayAnchoredPopupBase` (inherits `OverlayPopupBase`) | Adds the engine positioner for popups anchored to a trigger (Popover, Tooltip, Preview Card): positions the Positioner div against the context's `PositionReference`, wires in the arrow when present, re-engages the positioner if an arrow registers late or on re-open, and mirrors `data-side`/`data-align`/`data-anchor-hidden` onto the Popup element. |
| Placement flag-setter | `OverlayPositionerBase` | Collects placement parameters (side/align/offsets/collision/sticky/arrow padding) and publishes them into the anchored context via `SetPositioner(...)`; renders only `ChildContent` (the Popup). The Popup, not this class, renders the actual positioning `<div>`. |

## Parameters

These base classes are not directly instantiated; the tables below list what
each contributes to subclasses (`[Parameter]`-attributed where noted; other
rows are protected members subclasses read/override, not Blazor parameters).

**IOverlayContext / IAnchoredOverlayContext** (`OverlayContext.cs`, not a component, no `[Parameter]`s)

| Name | Type | Default | Notes |
|---|---|---|---|
| `Open` | `bool` (get) | n/a | Current open state, read by `OverlayPresence.IsOpen`. |
| `Modal` | `bool` (get) | n/a | When true, popups trap focus and lock scroll (read by `OverlayPopupBase.TrapFocus`/`LockPageScroll` defaults). |
| `ContentId` | `string` (get) | n/a | Stable id wired onto the Popup `id` and Trigger `aria-controls`. |
| `TriggerElement` / `HasTrigger` | `ElementReference` / `bool` | n/a | Trigger element the dismissable layer treats as "inside". |
| `PositionReference` (anchored only) | `ElementReference` | n/a | Element the popup anchors to (explicit anchor if present, else trigger). |
| `ArrowElement` / `HasArrow` (anchored only) | `ElementReference` / `bool` | n/a | Arrow element wired into the positioner when an Arrow part is mounted. |
| `Options` (anchored only) | `PositionOptions` | n/a | Placement options collected by the Positioner part. |
| `PositionerAttributes` (anchored only) | `IDictionary<string, object>?` | n/a | Unmatched attributes set on the Positioner part (e.g. `class`). |

**OverlayPresence** (protected members, no `[Parameter]`s of its own)

| Name | Type | Default | Notes |
|---|---|---|---|
| `Element` | `ElementReference` (protected) | n/a | The animating element; subclass razor must capture `@ref="Element"`. |
| `Rendered` | `bool` (protected) | n/a | Render gate (`@if (Rendered)` in the razor). |
| `Entering` / `Exiting` | `bool` (protected) | `false` | True for one frame (`data-starting-style`) / while the exit transition runs (`data-ending-style`). |
| `OverlayContext` | `IOverlayContext` (protected abstract) | n/a | Supplied by the razor subclass via cascaded context. |
| `ShouldStayMounted` | `bool` (protected virtual) | `false` | Keep the node mounted while closed instead of unmounting (backs `KeepMounted`/portal `keepMounted`). |

**OverlayPopupBase** (inherits `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| `KeepMounted` | `bool` | `false` | `[Parameter]`. Keep the popup mounted (hidden) while closed so an exit transition can run. |
| `OnOpenAutoFocus` | `EventCallback<NaviusOpenAutoFocusEventArgs>` | none | `[Parameter]`, cancelable. |
| `OnCloseAutoFocus` | `EventCallback<NaviusCloseAutoFocusEventArgs>` | none | `[Parameter]`, cancelable. |
| `OnEscapeKeyDown` | `EventCallback<NaviusEscapeKeyDownEventArgs>` | none | `[Parameter]`, cancelable. |
| `OnPointerDownOutside` | `EventCallback<NaviusPointerDownOutsideEventArgs>` | none | `[Parameter]`, cancelable. |
| `OnFocusOutside` | `EventCallback<NaviusFocusOutsideEventArgs>` | none | `[Parameter]`, cancelable. |
| `OnInteractOutside` | `EventCallback<NaviusInteractOutsideEventArgs>` | none | `[Parameter]`, cancelable. |
| `Attributes` | `IDictionary<string, object>?` | none | `[Parameter(CaptureUnmatchedValues = true)]`. |
| `CloseOnEscape` | `bool` (protected virtual) | `true` | Alert Dialog does not override this (it overrides `CloseOnOutside`). |
| `CloseOnOutside` | `bool` (protected virtual) | `true` | Alert Dialog overrides to `false`. |
| `TrapFocus` | `bool` (protected virtual) | `= OverlayContext.Modal` | |
| `LockPageScroll` | `bool` (protected virtual) | `= OverlayContext.Modal` | |
| `MoveFocusInside` | `bool` (protected virtual) | `true` | Tooltip/Preview Card override to `false`. |
| `InitialFocusSelector` | `string?` (protected virtual) | `null` | Alert Dialog targets Cancel. |

**OverlayAnchoredPopupBase** (inherits `OverlayPopupBase`, no additional `[Parameter]`s)

| Name | Type | Default | Notes |
|---|---|---|---|
| `PositionerElement` | `ElementReference` (protected) | n/a | The Positioner div the engine transforms; razor captures `@ref="PositionerElement"`. |
| `AnchoredContext` | `IAnchoredOverlayContext` (protected abstract) | n/a | Supplied by the razor subclass. |

**OverlayPositionerBase** (`ComponentBase`, flag-setter)

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Renders the Popup. |
| `Side` | `string?` | `null` (falls back to `DefaultSide`, `"bottom"`) | |
| `Align` | `string?` | `null` (falls back to `DefaultAlign`, `"center"`) | |
| `SideOffset` | `double` | `0` | |
| `AlignOffset` | `double` | `0` | |
| `Flip` | `bool` | `true` | |
| `AvoidCollisions` | `bool` | `true` | |
| `CollisionPadding` | `double?` | `null` | |
| `Sticky` | `string?` | `null` (falls back to `DefaultSticky`, `null`) | "partial" \| "always"; Preview Card's Positioner overrides `DefaultSticky` to `"partial"`. |
| `HideWhenDetached` | `bool` | `false` | |
| `ArrowPadding` | `double` | `0` | |
| `Attributes` | `IDictionary<string, object>?` | none | `[Parameter(CaptureUnmatchedValues = true)]`. |

## Events

`OverlayPopupBase` exposes six cancelable `EventCallback` parameters (see table above): `OnOpenAutoFocus`, `OnCloseAutoFocus`, `OnEscapeKeyDown`, `OnPointerDownOutside`, `OnFocusOutside`, `OnInteractOutside`. Each args type has `DefaultPrevented`; `PreventDefault()` on it stops the corresponding base behavior (keeping focus, keeping the popup open, etc.).

`IOverlayContext` and `IAnchoredOverlayContext` also expose plain .NET events (not `EventCallback` parameters): `Changed` (open state changed, parts re-render) and `ArrowChanged` (arrow registered/unregistered, anchored popup re-engages the positioner).

## State + data attributes

- `data-open` / `data-closed` (discrete, mutually exclusive presence attributes) and `data-starting-style` / `data-ending-style` (present only during the enter/exit transition frame) are written by the subclass razor from `OverlayPresence`'s `IsOpen`, `Entering`, `Exiting`.
- `OverlayAnchoredPopupBase` mirrors `data-side` / `data-align` / `data-anchor-hidden` (and CSS custom properties such as `--anchor-*` / `--available-*` / `--transform-origin`, written by the JS engine) from the Positioner div onto the Popup element.
- Public runtime state lives on the per-component context object (`Open`, `Modal`, `ContentId`, trigger/anchor/arrow element + presence flags, `Options`, `PositionerAttributes`), not on the base classes themselves.

## Keyboard

| Key | Behavior |
|---|---|
| Escape | If `CloseOnEscape` (default `true`), the JS dismissable layer invokes `OverlayPopupBase.OnDismiss("escape")`, which fires `OnEscapeKeyDown` (cancelable) and, unless prevented, calls `OverlayContext.RequestCloseAsync()`. |
| Tab (within popup) | When `TrapFocus` is true (default `= Modal`), a focus trap (`Interop.CreateFocusTrapAsync`) is engaged around `Element`; trap release behavior is implemented in JS interop, not in these C# files. |

Outside pointer-down closes via the same dismissable layer (`OnDismiss("outside")`, firing `OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside`) when `CloseOnOutside` is true; this is pointer, not keyboard, but shares the cancelable-callback path.

## Accessibility

- The base classes themselves set no ARIA role or label attributes; `id`/`role`/`aria-*` are wired by each concrete Popup razor (e.g. Popover's Popup sets `role="dialog"`, `aria-labelledby`, `aria-describedby`) using `ContentId` from the context.
- `OverlayPopupBase` owns focus mechanics: on engage, moves focus into the popup (`MoveFocusInside`, honoring `OnOpenAutoFocus`'s `PreventDefault`) or traps it (`TrapFocus`, with an optional `InitialFocusSelector`); on disengage, restores focus to the trigger unless a trap already restored it, unless `OnCloseAutoFocus` prevented it, and only if focus hasn't already moved elsewhere (`IsFocusRestorableAsync`).
- `LockPageScroll` (default `= Modal`) locks page scroll via interop while engaged.

## WPF strategy

**Tier B** (custom lookless control / shared base, not a single native WPF control).

This family should become a shared WPF base rather than a single control: an `OverlayPresenceBase` (a lookless `ContentControl` or `Popup`-hosting helper) implementing the mount/animate lifecycle via `VisualStateManager` states (`Open`/`Closed`, `Opening`/`Closing`) in place of the `data-open`/`data-starting-style` attribute dance; an `OverlayPopupBase`-equivalent adding a `FocusManager`/`KeyDown`-based Escape handler, a manual "outside click" hook (since WPF `Popup` has `StaysOpen`, which is close but not identical to the dismissable-layer's cancelable-callback model), and `FocusTrap` behavior built on `KeyboardNavigation` plus explicit `PreviewKeyDown` Tab-cycling; and an `OverlayAnchoredPopupBase`-equivalent wrapping `System.Windows.Controls.Primitives.Popup` with `Placement`/`PlacementTarget`/`CustomPopupPlacementCallback` standing in for the JS positioning engine (side/align/offset/collision/sticky map fairly directly; `HideWhenDetached`/`data-anchor-hidden` needs a manual `LayoutUpdated`/visibility-tracking substitute since WPF's `Popup` does not natively hide on anchor-clip). Popover, PreviewCard, NavigationMenu, and Select WPF ports would all derive from this shared base rather than reimplementing presence/dismiss/focus/positioning each time. ARIA roles set by concrete Popup razors map to `AutomationPeer` overrides on the corresponding WPF control (e.g. `role="dialog"` → a peer with `AutomationControlType.Pane` plus a dialog `LocalizedControlType`, `aria-labelledby`/`aria-describedby` → `AutomationProperties.LabeledBy`/`GetName` from the id-registered Title/Description). The cancelable `EventCallback` args (`OnOpenAutoFocus` etc.) translate to ordinary cancelable CLR routed events (`RoutedEventArgs.Handled` or a custom `Cancel` flag). The `data-starting-style`/`data-ending-style` two-frame commit trick (needed in the browser to force a CSS transition) has no WPF equivalent and should be replaced outright by `Storyboard`-driven enter/exit animations keyed off the same state machine.

## Open questions

- Whether WPF's native `Popup.StaysOpen = false` dismiss behavior is close enough to reuse, or whether a hand-rolled global mouse hook is needed to match the cancelable `OnPointerDownOutside`/`OnInteractOutside` contract exactly.
- Whether the focus trap should be a reusable attached behavior or a per-control implementation, given WPF has no built-in equivalent to a JS focus-trap library.
- How `HideWhenDetached` (anchor scrolled out of view) is best detected in WPF, where there is no ResizeObserver/IntersectionObserver equivalent; likely `LayoutUpdated` + manual bounds checks against the anchor's ancestor scroll viewers.
- Whether `OverlayPresence`'s two-phase enter (`Entering` frame then drop it) is even necessary in WPF, where `Storyboard.Begin` does not require a forced-reflow trick to guarantee a transition runs.
