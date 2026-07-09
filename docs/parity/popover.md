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

## WPF implementation notes

Shipped as `Navius.Wpf.Primitives.Controls.NaviusPopover` (`src/Navius.Wpf.Primitives/Controls/Popover/NaviusPopover.cs`), a lookless `ContentControl` built on `NaviusAnchoredPopup`.

- **Part collapse.** Trigger and Positioner collapse onto the root control: `Content` is the Trigger (rendered inside a `Button` template part), `PopoverContent`/`Side`/`Align`/`SideOffset`/`AlignOffset` are the Positioner. `Anchor` collapses too (first open question above, resolved): there is no separate `NaviusPopoverAnchor` part; the trigger is always the position reference.
- **Close.** `NaviusPopoverClose` becomes a static `RoutedCommand` (`NaviusPopover.CloseCommand`) rather than a dedicated part class. A `CommandBinding` is added directly to the popup content root (`PART_PopupContent`, not class-registered on `NaviusPopover`) because the popup content lives inside a `Popup`'s own disconnected visual root, which a class-level `CommandBinding` on the anchor's ancestor scope would never see routed through it. Any button inside `PopoverContent` with `Command="{x:Static navius:NaviusPopover.CloseCommand}"` closes the popover.
- **Title/Description collapse.** No separate part classes; `Title`/`Description` are `object` properties on the root, rendered as `TextBlock`s in the popup template and mirrored onto `AutomationProperties.Name`/`HelpText` on `PART_PopupContent` (resolving the "Title/Description registers presence" open question in favor of a direct bind, since WPF automation properties have no dangling-IDREF risk).
- **`Modal` vs. `TrapFocus`.** The web only engages a focus trap when `Modal` is true; `Navius.Wpf.Primitives.Overlays.OverlayOptions.TrapFocus` conflates "move focus into the popup on open" and "cycle Tab within it" into one flag with no way to request the first without the second. Splitting that flag was out of scope for this batch (`Overlays/` ownership boundary), so the WPF `NaviusPopover` always sets `TrapFocus = true` regardless of `Modal`; `Modal` is still tracked and forwarded to `OverlayOptions.Modal` (available to consumers via `OverlaySession.IsModal`, e.g. to decide whether to render a backdrop) but does not currently gate focus behavior differently. Documented here per the "resolve `Modal` focus-trap" open question above.
- **Arrow simplified.** No `PlacementResult.ArrowOffset`-driven arrow: `NaviusAnchoredPopup` does not currently surface `ArrowOffset`, and editing it was out of scope. No arrow glyph ships in the default template.
- **Automation.** `aria-haspopup`/`aria-expanded`/`role="dialog"` are approximated only via `AutomationProperties.AutomationId` on the trigger/popup parts, not a custom `AutomationPeer`. Per the WAI-ARIA APG a real dialog-role peer with an `ExpandCollapseState`-aware trigger peer would be more correct; deferred as a follow-up given the size of this batch.
- **Enter/exit animation.** A plain WPF `DoubleAnimation` (opacity 0→1 + a small `TranslateTransform` offset resolved from `EffectiveSide`) plays over 150ms on open; no exit animation (see the same tradeoff explained in `docs/parity/tooltip.md`).
- **No cancelable dismiss callbacks.** `OnEscapeKeyDown`/`OnPointerDownOutside`/etc. are not exposed as public events; dismissal always proceeds once requested (see the corresponding note in `docs/parity/tooltip.md`).

## ArrowOffset surface (M3)

Orchestrator note, added alongside the M3 ScrollArea/Menu-Menubar wave (not part of the M2 batch documented above). Closes the "Arrow simplified" gap called out in the WPF implementation notes: `NaviusAnchoredPopup` (`src/Navius.Wpf.Primitives/Controls/NaviusAnchoredPopup.cs`) now surfaces `PlacementMath`'s `PlacementResult.ArrowOffset`:

- `ArrowSize` (double, input DP, default 0): fed straight into `AnchoredPlacementOptions.ArrowSize`. 0 keeps arrow computation disabled, matching `AnchoredPlacementOptions`'s own default, so this is opt-in and non-breaking for every existing consumer (Tooltip/Popover/PreviewCard all currently leave it unset).
- `ArrowOffsetX` / `ArrowOffsetY` (double, read-only DPs): the arrow glyph's local-to-popup offset from the current/last `UpdatePlacement()` pass. `double.NaN` when `ArrowSize` is 0 (arrow computation disabled) or before the first placement.
- `ArrowOffsetXText` / `ArrowOffsetYText` (double, attached read-only properties on `Child`): mirror `ArrowOffsetX`/`ArrowOffsetY` onto the popup content root, the same pattern `EffectiveSideText` already uses, so a popup-content template can position an arrow glyph via `{Binding Path=(controls:NaviusAnchoredPopup.ArrowOffsetXText), RelativeSource={RelativeSource Self}}` without a reference back to the owning `NaviusAnchoredPopup`.

This is additive-only: no existing `NaviusAnchoredPopup` member changed shape, and `Tooltip`/`Popover`/`PreviewCard` were deliberately **not** touched to consume it - per this wave's scope, their owners adopt `ArrowSize`/`ArrowOffsetX(Text)`/`ArrowOffsetY(Text)` (and add an arrow glyph to their default templates) in a future wave. `PlacementMath`'s own arrow-offset math was already covered by `PlacementMathTests` (centering, edge-clamping, and the `ArrowSize == 0` null case) before this change; no gap was found there. New coverage for this change lives in `tests/Navius.Wpf.Tests/AnchoredPopupTests.cs` (DP wiring: `ArrowSize`/`ArrowOffsetX`/`ArrowOffsetY` defaults and round-trip, the attached `ArrowOffsetXText`/`ArrowOffsetYText` mirrors default to `NaN` and null-check like `EffectiveSideText` does, and that changing `ArrowSize` safely re-triggers placement without throwing when no `Anchor` is set yet). A full live-placement assertion (`ArrowOffsetX`/`Y` populated from a real anchored pass) is not covered: like `EffectiveSide` elsewhere in this suite, `UpdatePlacement()` only runs once `Anchor` has a real `PresentationSource` (a shown/hosted `HwndSource` with the anchor as `RootVisual`), which no existing test in this codebase establishes - this is a pre-existing gap in the suite, not one newly introduced here.
