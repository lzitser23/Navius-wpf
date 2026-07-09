# PreviewCard

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusPreviewCard` | none (`<CascadingValue>` only) | Root (Base UI `PreviewCard.Root`, the renamed `HoverCard`). Owns open state (controlled via `Open`/`OpenChanged`, or uncontrolled via `DefaultOpen`) plus hover-intent open/close timing (`OpenDelay`, `CloseDelay`). |
| `NaviusPreviewCardArrow` | `<svg data-navius-preview-card-arrow>` | Directional indicator inside the popup; registers its element so the positioner keeps it pointing at the trigger. Must be placed inside `NaviusPreviewCardPopup`. |
| `NaviusPreviewCardBackdrop` | `<div data-navius-preview-card-backdrop>` inside `NaviusPortal` | Scrim sibling of the positioner; present in the contract but rarely used since preview cards are non-modal. |
| `NaviusPreviewCardPopup` | `<div data-navius-preview-card-positioner><div data-navius-preview-card-popup>...</div></div>` inside `NaviusPortal` | The hover preview panel. Non-modal, not `role="dialog"`: no focus trap, scroll lock, or focus move. |
| `NaviusPreviewCardPortal` | none (`@ChildContent` only) | Flag-setter: records custom mount container / `KeepMounted` into the context; the Popup performs the actual teleport via `NaviusPortal`. |
| `NaviusPreviewCardPositioner` | none (`@ChildContent` only) | Flag-setter: owns placement (side/align/offsets/collision); defaults `Sticky` to `"partial"`. |
| `NaviusPreviewCardTrigger` | `<a data-navius-preview-card-trigger>` | Anchor for the preview card; opens on pointer-enter (after `OpenDelay`) and immediately on keyboard focus; closes on pointer-leave or blur (after `CloseDelay`). |

## Parameters

**NaviusPreviewCard**

| Name | Type | Default | Notes |
|---|---|---|---|
| `OpenDelay` | `int` | `600` | Hover-intent delay before opening, ms (Base UI spec default 600). |
| `CloseDelay` | `int` | `300` | Grace delay before closing after leave/blur, ms (spec default 300). |
| `Open` | `bool` | `false` | Controlled open state; provide together with `OpenChanged`. |
| `OpenChanged` | `EventCallback<bool>` | none | Presence of a delegate on this (`IsControlled => OpenChanged.HasDelegate`) puts the root in controlled mode, unlike `NaviusPopover` which checks whether `Open` was set. |
| `DefaultOpen` | `bool` | `false` | Initial open state when uncontrolled. |
| `ChildContent` | `RenderFragment?` | `null` | |

**NaviusPreviewCardArrow**

| Name | Type | Default | Notes |
|---|---|---|---|
| `Width` | `double` | `10` | |
| `Height` | `double` | `5` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

No `ChildContent` parameter (unlike Popover's Arrow); the polygon markup is fixed.

**NaviusPreviewCardBackdrop** (inherits `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| `KeepMounted` | `bool` | `false` | Own parameter (`OverlayPresence` itself declares no `[Parameter]`s). Combines with `Context.PortalKeepMounted` for `ShouldStayMounted`. |
| `Attributes` | `IDictionary<string, object>?` | none | Own parameter, `CaptureUnmatchedValues`. |

**NaviusPreviewCardPopup** (inherits `OverlayAnchoredPopupBase` → `OverlayPopupBase` → `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Own parameter. |
| `KeepMounted` | `bool` | `false` | Inherited from `OverlayPopupBase`; combines with `Context.PortalKeepMounted`. |
| `OnOpenAutoFocus` | `EventCallback<NaviusOpenAutoFocusEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. Effectively inert here since `MoveFocusInside` is overridden `false` (protected override, not a parameter), so focus is never moved on open regardless. |
| `OnCloseAutoFocus` | `EventCallback<NaviusCloseAutoFocusEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnEscapeKeyDown` | `EventCallback<NaviusEscapeKeyDownEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnPointerDownOutside` | `EventCallback<NaviusPointerDownOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnFocusOutside` | `EventCallback<NaviusFocusOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `OnInteractOutside` | `EventCallback<NaviusInteractOutsideEventArgs>` | none | Inherited from `OverlayPopupBase`, cancelable. |
| `Attributes` | `IDictionary<string, object>?` | none | Inherited from `OverlayPopupBase`, `CaptureUnmatchedValues`. |

**NaviusPreviewCardPortal**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Container` | `string?` | `null` | CSS selector of a custom mount container; null teleports into `document.body`. |
| `KeepMounted` | `bool` | `false` | Keep the popup mounted while closed (for exit animations). |

**NaviusPreviewCardPositioner** (inherits `OverlayPositionerBase`, no own `[Parameter]`s)

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Inherited; renders the Popup. |
| `Side` | `string?` | `null` → `"bottom"` | Inherited; not overridden. |
| `Align` | `string?` | `null` → `"center"` | Inherited; not overridden. |
| `SideOffset` | `double` | `0` | Inherited. |
| `AlignOffset` | `double` | `0` | Inherited. |
| `Flip` | `bool` | `true` | Inherited. |
| `AvoidCollisions` | `bool` | `true` | Inherited. |
| `CollisionPadding` | `double?` | `null` | Inherited. |
| `Sticky` | `string?` | `null` → `"partial"` | Inherited parameter; PreviewCard's Positioner overrides `DefaultSticky` to `"partial"` (comment: preserves the pre-rename default of keeping the card anchored on cross-axis overflow; Base UI's own default is `false`/unset). |
| `HideWhenDetached` | `bool` | `false` | Inherited. |
| `ArrowPadding` | `double` | `0` | Inherited. |
| `Attributes` | `IDictionary<string, object>?` | none | Inherited, `CaptureUnmatchedValues`. |

**NaviusPreviewCardTrigger**

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` | none | `CaptureUnmatchedValues`. |

## Events

| Part | EventCallback | Notes |
|---|---|---|
| `NaviusPreviewCard` | `OpenChanged` (`EventCallback<bool>`) | Fired on every effective open/close (both timer-delayed and immediate paths route through the same `SetAsync`). |
| `NaviusPreviewCardPopup` | `OnOpenAutoFocus`, `OnCloseAutoFocus`, `OnEscapeKeyDown`, `OnPointerDownOutside`, `OnFocusOutside`, `OnInteractOutside` | All inherited from `OverlayPopupBase`; cancelable. |

`NaviusPreviewCardTrigger` (`@onpointerenter="Context.OpenDelayed"`, `@onpointerleave="Context.CloseDelayed"`, `@onfocus="Context.OpenNow"`, `@onblur="Context.CloseDelayed"`) and `NaviusPreviewCardPopup` (`@onpointerenter="Context.OpenNow"`, `@onpointerleave="Context.CloseDelayed"`) wire internal handlers to the context's timing methods but expose no `EventCallback` parameters of their own.

## State + data attributes

- Marker attributes (always present): `data-navius-preview-card-arrow`, `data-navius-preview-card-backdrop`, `data-navius-preview-card-popup`, `data-navius-preview-card-positioner`, `data-navius-preview-card-trigger`.
- `NaviusPreviewCardTrigger`: `data-popup-open` (present only while open). No `data-pressed` (unlike Popover's Trigger, which tracks pointer-down state).
- `NaviusPreviewCardBackdrop` / `NaviusPreviewCardPopup`: `data-open`/`data-closed` (discrete) and `data-starting-style`/`data-ending-style` (transition frame only), from `OverlayPresence`.
- The Positioner div (rendered by `NaviusPreviewCardPopup`) receives `data-side`/`data-align` (and positioning CSS custom properties) from the JS engine, mirrored onto the Popup by `OverlayAnchoredPopupBase`.
- `PreviewCardContext` public state: `Open`, `Modal` (hardcoded `false`, get-only), `ContentId`, `TriggerElement`/`HasTrigger`, `PositionReference` (always `TriggerElement`, no separate Anchor part), `PortalContainer`, `PortalKeepMounted`, `Options`, `PositionerAttributes`, `ArrowElement`/`HasArrow`.

## Keyboard

| Key | Behavior |
|---|---|
| Focus (Tab into trigger) | `NaviusPreviewCardTrigger`'s `@onfocus` calls `Context.OpenNow`: opens immediately, no `OpenDelay`. |
| Blur (Tab out of trigger) | `@onblur` calls `Context.CloseDelayed`: closes after `CloseDelay`. |
| Escape | Not exercised by the found e2e tests (`wave1.spec.ts` only covers the hover-reveal case), but inherited from `OverlayPopupBase`: `CloseOnEscape` defaults `true` and is not overridden by `NaviusPreviewCardPopup`, so Escape routes through `OnDismiss("escape")` → `OnEscapeKeyDown` (cancelable) → `PreviewCardContext.RequestCloseAsync()` (→ `CloseNow`, no delay). |
| Tab (inside popup) | No focus trap: `TrapFocus => OverlayContext.Modal` and `Modal` is hardcoded `false`, so Tab is never trapped; the popup also never receives focus in the first place since `MoveFocusInside` is overridden `false`. |

## Accessibility

- `NaviusPreviewCardPopup` sets only `id="@Context.ContentId"`; no `role` attribute (not `role="dialog"`, unlike Popover) and no `tabindex`.
- No `aria-labelledby`/`aria-describedby` wiring exists: there are no Title/Description parts for PreviewCard.
- `NaviusPreviewCardArrow`: `aria-hidden="true"`.
- Focus never moves into the card (`MoveFocusInside` overridden `false`), so the trigger retains focus throughout hover interactions; `OverlayPopupBase`'s "return focus to trigger" logic on close is also gated by `MoveFocusInside` and effectively a no-op here since focus never left.
- No focus trap and no scroll lock (`TrapFocus`/`LockPageScroll` both derive from `Modal`, hardcoded `false`).

## WPF strategy

**Tier B** (custom lookless control deriving from the shared Overlays base).

Base on the same Overlays shared base as Popover (`overlays.md`), but non-modal end to end: no focus trap, no scroll lock, no focus move (`MoveFocusInside = false`), matching a WPF control that never calls `Focus()` on open. The hover-intent timing (`OpenDelay`/`CloseDelay` via `CancellationTokenSource` + `Task.Delay`) maps to `DispatcherTimer` starts/stops on `MouseEnter`/`MouseLeave`/`GotFocus`/`LostFocus`, with `GotFocus` bypassing the timer entirely (immediate open) as in the Blazor code. Because the popup itself must also extend the trigger's "stay open" region (`@onpointerenter="Context.OpenNow"` / `@onpointerleave="Context.CloseDelayed"` on the popup, so the cursor can travel from trigger to card), the WPF port needs the popup content and the trigger to share one hover-intent state machine, which a native `ToolTip` cannot do (WPF tooltips close when the pointer leaves the anchor, not when it enters the tooltip). Since there's no ARIA role on the Popup, no `AutomationPeer` role mapping is needed beyond a generic pane/group peer for the panel; the Trigger, rendered as an `<a>` in Blazor, should map to a focusable `Button`/`HyperlinkButton`-style WPF element so `GotFocus`/`LostFocus` and `AutomationControlType.Hyperlink` (or `Button`) semantics carry over.

## Open questions

- Whether the WPF port should keep `Modal` as a dead always-`false` property on the context (matching the interface) or drop it since `PreviewCardContext.Modal` is a hardcoded constant with no way to change it.
- Whether Escape-closes-immediately (`CloseNow`, no `CloseDelay`) needs an explicit test/spec confirmation before porting, since it's inferred from inherited `OverlayPopupBase` defaults rather than a PreviewCard-specific e2e assertion.
- How the "popup extends the hover region" behavior (pointer-enter on the popup itself keeps it open) should be modeled in WPF, given the Popup and Trigger are visually disjoint (arbitrary screen position) once positioned.
