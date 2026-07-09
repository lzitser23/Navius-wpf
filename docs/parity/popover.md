# Popover

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusPopover` | none (`<CascadingValue>` only) | Root. Owns open state (controlled via `Open`/`OpenChanged`, or uncontrolled via `DefaultOpen`) and `Modal`; cascades `PopoverContext`. |
| `NaviusPopoverAnchor` | `<div data-navius-popover-anchor>` | Optional explicit positioning reference; when present the content anchors here instead of the trigger. |
| `NaviusPopoverArrow` | `<span data-navius-popover-arrow><svg>...</svg></span>` (or custom `ChildContent`) | Directional indicator; registers itself with the context so the popup wires it into the positioner. |
| `NaviusPopoverBackdrop` | `<div data-navius-popover-backdrop>` inside `NaviusPortal` | Dimming scrim, meaningful in modal mode; presentational only, outside dismissal is owned by the Popup. |
| `NaviusPopoverClose` | `<button data-navius-popover-close>` | Any button inside the content that closes the popover on click. |
| `NaviusPopoverDescription` | `<p data-navius-popover-description>` | Describes the popup; registers presence so the Popup wires `aria-describedby` only when mounted. |
| `NaviusPopoverPopup` | `<div data-navius-popover-positioner><div data-navius-popover-popup role="dialog">...</div></div>` inside `NaviusPortal` | The floating panel. |
| `NaviusPopoverPortal` | none (`@ChildContent` only) | Flag-setter: records custom mount container / `KeepMounted` into the context; actual teleport is performed by Popup/Backdrop via `NaviusPortal`. |
| `NaviusPopoverPositioner` | none (`@ChildContent` only) | Flag-setter: owns placement (side/align/offsets/collision), publishes options into the context. |
| `NaviusPopoverTitle` | `<h2 data-navius-popover-title>` | Labels the popup; registers presence so the Popup wires `aria-labelledby` only when mounted. |
| `NaviusPopoverTrigger` | `<button data-navius-popover-trigger>` | Toggles the popover; captures its element as the default position reference. |

## Parameters

**NaviusPopover**

| Name | Type | Default | Notes |
|---|---|---|---|
| `Open` | `bool` | `false` | Controlled open state; presence of this parameter in `SetParametersAsync` flips the root into controlled mode. |
| `OpenChanged` | `EventCallback<bool>` | none | |
| `DefaultOpen` | `bool` | `false` | Initial open state when uncontrolled. |
| `Modal` | `bool` | `false` | When true, the open content traps focus and locks page scroll (read by `PopoverContext.Modal`, consumed by `OverlayPopupBase`). |
| `ChildContent` | `RenderFragment?` | `null` | |

**NaviusPopoverAnchor**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

**NaviusPopoverArrow**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Overrides the default triangle SVG when set. |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |
| `Width` | `double` | `10` | |
| `Height` | `double` | `5` | |

**NaviusPopoverBackdrop** (inherits `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| `KeepMounted` | `bool` | `false` | Own parameter (not inherited: `OverlayPresence` declares no `[Parameter]`s itself). Combines with `Context.PortalKeepMounted` to set `ShouldStayMounted`. |
| `Attributes` | `IDictionary<string, object>?` | none | Own parameter, `CaptureUnmatchedValues`. |

**NaviusPopoverClose**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

**NaviusPopoverDescription**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

**NaviusPopoverPopup** (inherits `OverlayAnchoredPopupBase` → `OverlayPopupBase` → `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Own parameter. |
| `KeepMounted` | `bool` | `false` | Inherited from `OverlayPopupBase`; combines with `Context.PortalKeepMounted` (`ShouldStayMounted` override). |
| `OnOpenAutoFocus` | `EventCallback<NaviusOpenAutoFocusEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnCloseAutoFocus` | `EventCallback<NaviusCloseAutoFocusEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnEscapeKeyDown` | `EventCallback<NaviusEscapeKeyDownEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnPointerDownOutside` | `EventCallback<NaviusPointerDownOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnFocusOutside` | `EventCallback<NaviusFocusOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnInteractOutside` | `EventCallback<NaviusInteractOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `Attributes` | `IDictionary<string, object>?` | none | Inherited from `OverlayPopupBase`, `CaptureUnmatchedValues`. |

**NaviusPopoverPortal**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Container` | `string?` | `null` | CSS selector of a custom mount container; null teleports into `document.body`. |
| `KeepMounted` | `bool` | `false` | Keep popup + backdrop mounted while closed. |

**NaviusPopoverPositioner** (inherits `OverlayPositionerBase`, no own `[Parameter]`s)

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Inherited; renders the Popup. |
| `Side` | `string?` | `null` → `"bottom"` | Inherited; Popover's `DefaultSide` is the base default (`"bottom"`, not overridden). |
| `Align` | `string?` | `null` → `"center"` | Inherited; not overridden. |
| `SideOffset` | `double` | `0` | Inherited. |
| `AlignOffset` | `double` | `0` | Inherited. |
| `Flip` | `bool` | `true` | Inherited. |
| `AvoidCollisions` | `bool` | `true` | Inherited. |
| `CollisionPadding` | `double?` | `null` | Inherited. |
| `Sticky` | `string?` | `null` | Inherited; Popover does not override `DefaultSticky` (stays `null`). |
| `HideWhenDetached` | `bool` | `false` | Inherited. |
| `ArrowPadding` | `double` | `0` | Inherited. |
| `Attributes` | `IDictionary<string, object>?` | none | Inherited, `CaptureUnmatchedValues`. |

**NaviusPopoverTitle**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

**NaviusPopoverTrigger**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

## Events

| Part | EventCallback | Notes |
|---|---|---|
| `NaviusPopover` | `OpenChanged` (`EventCallback<bool>`) | Fired whenever the effective open state changes (controlled: forwards `Open` requests; uncontrolled: fired after internal state updates). |
| `NaviusPopoverPopup` | `OnOpenAutoFocus`, `OnCloseAutoFocus`, `OnEscapeKeyDown`, `OnPointerDownOutside`, `OnFocusOutside`, `OnInteractOutside` | All inherited from `OverlayPopupBase`; each is cancelable via `args.DefaultPrevented`/`PreventDefault()`. |

`NaviusPopoverTrigger` and `NaviusPopoverClose` wire internal handlers (`@onclick="Context.RequestToggleAsync"`, `@onclick="Context.RequestCloseAsync"`, plus pointer down/up/leave on the Trigger for `data-pressed`) but expose no `EventCallback` parameters of their own.

## State + data attributes

- Marker attributes (always present, presence-only): `data-navius-popover-anchor`, `data-navius-popover-arrow`, `data-navius-popover-backdrop`, `data-navius-popover-close`, `data-navius-popover-description`, `data-navius-popover-popup`, `data-navius-popover-positioner`, `data-navius-popover-title`, `data-navius-popover-trigger`.
- `NaviusPopoverTrigger`: `data-popup-open` (present only while open), `data-pressed` (present while pointer is down).
- `NaviusPopoverBackdrop` / `NaviusPopoverPopup`: `data-open`/`data-closed` (discrete, mutually exclusive) and `data-starting-style`/`data-ending-style` (present only during the corresponding transition frame), from `OverlayPresence`.
- The Positioner div (rendered by `NaviusPopoverPopup`) receives `data-side`/`data-align`/`data-anchor-hidden` and CSS custom properties (`--anchor-*`, `--available-*`, `--transform-origin`) from the JS positioning engine; `data-side`/`data-align` are mirrored onto the Popup element by `OverlayAnchoredPopupBase`.
- `PopoverContext` public state: `Open`, `ContentId`, `Modal`, `TriggerElement`/`HasTrigger`, `AnchorElement`/`HasAnchor`, `PositionReference`, `PortalContainer`, `PortalKeepMounted`, `Options`, `PositionerAttributes`, `TitleId`/`HasTitle`, `DescriptionId`/`HasDescription`, `ArrowElement`/`HasArrow`.

## Keyboard

| Key | Behavior |
|---|---|
| Escape | Closes the popover (confirmed by e2e test `navius.spec.ts`: "opens anchored, dismisses on outside click and Esc"). Routed through `OverlayPopupBase.OnDismiss("escape")` → `OnEscapeKeyDown` (cancelable) → `PopoverContext.RequestCloseAsync()` unless prevented. |
| Tab (inside popup) | Focus trap engages only when `Modal` is true (`TrapFocus => OverlayContext.Modal`); non-modal popovers do not trap Tab. |

Outside pointer click also closes (verified by the same e2e test); not a keyboard interaction but shares the dismissable-layer path.

## Accessibility

- `NaviusPopoverTrigger`: `aria-haspopup="dialog"`, `aria-expanded` (`"true"`/`"false"`), `aria-controls="@Context.ContentId"`.
- `NaviusPopoverPopup`: `role="dialog"`, `id="@Context.ContentId"`, `tabindex="-1"`, `aria-labelledby` set to `Context.TitleId` only when `HasTitle`, `aria-describedby` set to `Context.DescriptionId` only when `HasDescription` (no dangling IDREFs when Title/Description aren't mounted).
- `NaviusPopoverArrow`: `aria-hidden="true"`.
- Focus management is inherited from `OverlayPopupBase`: moves focus into the popup on open (`MoveFocusInside` default `true`, honoring `OnOpenAutoFocus`), traps it when `Modal`, and returns focus to the trigger on close (unless `OnCloseAutoFocus` prevents it or focus already moved elsewhere).

## WPF strategy

**Tier B** (custom lookless control deriving from the shared Overlays base).

Base on the Overlays shared base (see `overlays.md`): a lookless `Popover` control wrapping WPF `Popup` (`Placement`/`PlacementTarget`/`CustomPopupPlacementCallback` for the anchored-positioning parameters, `StaysOpen="False"` plus a custom outside-click hook for the dismissable layer) with `VisualStateManager` states for `data-open`/`data-closed`/entering/exiting. `Modal` maps to a WPF-side focus trap + scroll lock (no native WPF "modal popup" primitive; likely a `Window`-level input-capture behavior or a manual `KeyboardNavigation` cycle). `role="dialog"` maps to an `AutomationPeer` override returning `AutomationControlType.Pane` (or `Window`) with a dialog `LocalizedControlType`; `aria-labelledby`/`aria-describedby` map to `AutomationProperties.LabeledBy` sourced from the Title/Description parts' registered presence. `data-popup-open`/`data-pressed` on the trigger have no WPF ARIA equivalent and would surface only as style triggers on a `ToggleButton`-based trigger template. The ADR-0007 cancelable-callback superset (`OnEscapeKeyDown`, `OnPointerDownOutside`, etc., justified in the Overlays base because Blazor cannot call `event.preventBaseUIHandler`) translates more naturally to WPF, which already has real cancelable routed events, so this deviation may not need to be preserved as a "superset" in the WPF port.

## Open questions

- Whether Anchor (`NaviusPopoverAnchor`) is common enough to keep as a first-class WPF part, or should collapse into a `PlacementTarget` override parameter on the control.
- How `Modal` popover focus-trap + scroll-lock should be implemented given WPF's `Popup` does not participate in the visual tree's normal focus scope the way a `Window` does.
- Whether Title/Description "registers presence" (ref-counted mount tracking to avoid dangling `aria-labelledby`/`aria-describedby`) needs a WPF equivalent, or whether `AutomationProperties.LabeledBy` can be bound directly since WPF has no risk of a dangling ARIA IDREF.
