# Drawer

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusDrawer | none (renders `@ChildContent` inside a `CascadingValue`) | Root (Base UI `Drawer.Root`). Owns open state (controlled via `@bind-Open` or uncontrolled via `DefaultOpen`) and the docked `Side`, cascading `DrawerContext` to parts. |
| NaviusDrawerTrigger | `<button type="button">` | Opens/toggles the drawer. Captures itself as `Context.TriggerElement`. |
| NaviusDrawerPortal | none (renders `@ChildContent`; flag-setter only) | Records `Container` + `KeepMounted` into `DrawerContext`; Popup/Backdrop teleport via `NaviusPortal`. |
| NaviusDrawerBackdrop | `<div>` (inside `NaviusPortal`) | Presentational scrim, portaled as a sibling of the popup. Outside dismissal is owned by the Popup's dismissable layer. |
| NaviusDrawerPopup | `<div role="dialog">` (inside `NaviusPortal`) | The draggable drawer sheet. The Popup itself IS the swipe target: engages `CreateSheetSwipeAsync` for drag-to-dismiss along `Side`, in addition to the shared modal dismissable layer / focus trap / scroll lock. |
| NaviusDrawerTitle | `<h2>` | Labels the drawer; `id` read by aria-labelledby on the popup. Registers presence via `Context.RegisterTitle()`. |
| NaviusDrawerDescription | `<p>` | Describes the drawer; `id` read by aria-describedby on the popup. Registers presence via `Context.RegisterDescription()`. |
| NaviusDrawerClose | `<button type="button">` | Any button inside the popup that requests close. |

## Parameters

### NaviusDrawer

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | false | Controlled open state; use with `OpenChanged` (`@bind-Open`). |
| OpenChanged | EventCallback\<bool\> | none | Presence of a delegate makes the component "controlled" (`IsControlled`). |
| DefaultOpen | bool | false | Initial open state when used uncontrolled. |
| Modal | bool | true | Traps focus + locks scroll while open. |
| Side | string | `"bottom"` | The edge the sheet docks to and is dragged toward: `bottom` \| `top` \| `left` \| `right`. No enum/validation in code; free string flowed to `DrawerContext.Side`. |
| ChildContent | RenderFragment? | null | Child parts. |

### NaviusDrawerTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Button content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes, forwarded to the `<button>`. |

### NaviusDrawerPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Wrapped Backdrop/Popup. |
| Container | string? | null | CSS selector for the portal mount target. |
| KeepMounted | bool | false | Sets `Context.PortalForceMount = true` when set (keeps parts mounted while closed). |

### NaviusDrawerBackdrop

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | false | Keeps the backdrop mounted while closed (for exit animations). |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes, forwarded to the `<div>`. |

### NaviusDrawerPopup

Declared directly on the popup:

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Popup content. |

Inherited from `OverlayPopupBase` (since `NaviusDrawerPopup` `@inherits OverlayPopupBase`):

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | false | Keep the popup mounted (hidden) while closed so an exit transition can run. |
| OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | none | Cancelable; PreventDefault keeps focus where it is when the popup opens. |
| OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | none | Cancelable; PreventDefault skips returning focus to the trigger on close. |
| OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | none | Cancelable; PreventDefault keeps the popup open when Escape is pressed. |
| OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on an outside pointer-down. |
| OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open when focus moves outside. |
| OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on any outside interaction. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes, forwarded to the `<div>`. |

### NaviusDrawerTitle

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Title content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

### NaviusDrawerDescription

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Description content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

### NaviusDrawerClose

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Button content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusDrawer | OpenChanged | EventCallback\<bool\> | Any part requests an open-state change (trigger click, close click, dismiss, swipe-dismiss) while controlled. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | Before focus moves into the popup on engage; `PreventDefault` keeps focus elsewhere. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | On disengage (close), before focus would return to the trigger; `PreventDefault` skips return-focus. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | The JS dismissable layer invokes `OnDismiss("escape")`; raised before closing, `PreventDefault` keeps it open. Confirmed by e2e: `tests/e2e/specs/wave2.spec.ts` ("the trigger opens the drawer sheet and Escape closes it") presses `Escape` and expects the sheet to close. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | The JS dismissable layer invokes `OnDismiss("outside")` on an outside pointer-down. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | Raised alongside `OnPointerDownOutside` in the "outside" dismiss path. |
| NaviusDrawerPopup (via OverlayPopupBase) | OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | Raised alongside the other two in the "outside" dismiss path. |
| NaviusDrawerPopup (swipe engine, internal) | `SwipeCallbacks.OnDismiss()` ([JSInvokable], not a public `[Parameter]`) | `Task OnDismiss()` | Invoked from the JS `createSheetSwipe` engine when a drag exceeds the dismiss threshold; calls `Context.RequestCloseAsync()` directly (bypasses the cancelable `OnEscapeKeyDown`/`OnPointerDownOutside`/etc. hooks entirely: there is no consumer-facing cancelable callback for a swipe-dismiss). |
| NaviusDrawerPopup (swipe engine, internal) | `SwipeCallbacks.OnReset()` ([JSInvokable]) | `Task OnReset()` | Invoked when a drag snaps back below threshold; no-op (`Task.CompletedTask`). |

## State + data attributes

| Attribute / class | Part(s) | Meaning |
|---|---|---|
| `data-popup-open` | Trigger | Present when `Context.Open`. |
| `data-navius-drawer-trigger` | Trigger | Static marker attribute. |
| `data-open` | Backdrop, Popup | Present when `Context.Open`. |
| `data-closed` | Backdrop, Popup | Present when not open. |
| `data-starting-style` | Backdrop, Popup | Present for the one frame while `Entering`. |
| `data-ending-style` | Backdrop, Popup | Present while `Exiting` (exit transition running). |
| `data-drawer-direction` | Popup | Set to `Context.Side` (`bottom`/`top`/`left`/`right`); used to indicate the docked edge / drag direction. |
| `data-swiping` | Popup | Mentioned in the file header comment as published by the swipe engine (`createSheetSwipe`) while dragging; the attribute itself is set by the JS engine, not by C# markup in this `.razor` file. |
| `--drawer-swipe-movement-x` / `--drawer-swipe-movement-y` | Popup | CSS custom properties published by the swipe engine while dragging (per the file header comment); not set from C# markup. |
| `data-navius-drawer-backdrop` / `data-navius-drawer-popup` / `data-navius-drawer-title` / `data-navius-drawer-description` / `data-navius-drawer-close` | respective part | Static marker attributes. |
| `id` | Popup | `Context.ContentId` (`navius-drawer-{guid}`). |
| `id` | Title | `Context.TitleId` = `{ContentId}-title`. |
| `id` | Description | `Context.DescriptionId` = `{ContentId}-desc`. |

Internal (non-DOM) state: `DrawerContext.Open`, `DrawerContext.Modal`, `DrawerContext.Side`, `HasTitle`/`HasDescription` (ref-counted), `HasTrigger`, `PortalContainer`/`PortalForceMount`. `OverlayPresence` tracks `Rendered`/`Entering`/`Exiting`.

## Keyboard

| Key | Behavior |
|---|---|
| Escape | Closes the drawer. Confirmed by e2e: `tests/e2e/specs/wave2.spec.ts`, test "the trigger opens the drawer sheet and Escape closes it": opens via the trigger, asserts `role="dialog"`, `aria-modal="true"`, `data-drawer-direction="bottom"`, `data-open=""`, then `page.keyboard.press('Escape')` and (per the test name) expects the sheet to close. Mechanism in code: the shared `OverlayPopupBase.OnDismiss("escape")` path (`CloseOnEscape` defaults true, not overridden by Drawer). |
| Tab (focus cycling) | When `Modal` is true, `TrapFocus` is true and `EngageAsync` (inherited from `OverlayPopupBase`) calls `Interop.CreateFocusTrapAsync`. The Tab-cycling logic itself lives in JS interop outside this family's `.razor`/`.cs` files. |

No explicit `@onkeydown`/`OnKeyDown` handler exists in the Drawer family's own `.razor`/`.cs` files. Drag-to-dismiss (pointer/touch swipe past a threshold along `Side`) is a pointer-driven gesture handled by the JS `createSheetSwipe` engine (`CreateSheetSwipeAsync`), not a keyboard interaction.

## Accessibility

- Popup: `role="dialog"`, `aria-modal="true"` when `Context.Modal` else omitted, `aria-labelledby` set to `Context.TitleId` only when `Context.HasTitle`, `aria-describedby` set to `Context.DescriptionId` only when `Context.HasDescription`, `tabindex="-1"`.
- Trigger: `aria-haspopup="dialog"`, `aria-expanded` reflects `Context.Open`, `aria-controls` set to `Context.ContentId`.
- No dev-time missing-title warning exists for Drawer (unlike `NaviusDialogPopup`, `NaviusDrawerPopup` has no `ILogger`/`LoggerFactory` injection or `EngageAsync` override that checks `Context.HasTitle`).
- Focus management: identical mechanism to Dialog, inherited from `OverlayPopupBase` (focus moves into the popup on engage unless prevented, and either the focus trap restores it or, in the non-modal/no-trap case, `OverlayContext.TriggerElement.FocusAsync()` is called explicitly on disengage).
- Scroll lock: `Interop.LockScrollAsync()`/`UnlockScrollAsync()` when `Modal` (default true).

## WPF strategy

Tier B (custom lookless control).

Base class: like Dialog, a custom lookless `Drawer` control (root/context object + Trigger/Backdrop/Popup/Title/Description/Close parts), most naturally hosted via a `Window.ShowDialog()`-style modal surface or an in-app overlay layer rather than `System.Windows.Controls.Primitives.Popup` directly, since a docked-edge sheet with drag-to-dismiss is closer to a slide-in panel than a positioned popup. Map `role="dialog"` the same way as Dialog (`AutomationControlType.Window`/`Pane`, `AutomationProperties.LabeledBy`/`HelpText`). Things that will NOT translate cleanly: (1) the entire drag-to-dismiss gesture (`CreateSheetSwipeAsync`, `data-swiping`, `--drawer-swipe-movement-x/y` CSS custom properties) is pointer-event/CSS-transform-driven JS interop with no WPF equivalent: needs a native reimplementation using `Manipulation`/`Thumb`/`MouseMove` drag tracking plus a `TranslateTransform` or `RenderTransform` on the sheet, and a decision on the dismiss-threshold algorithm since the JS engine's logic isn't visible from this family's C# source; (2) `Side` (`bottom`/`top`/`left`/`right`) as a free string with no enum needs a proper `Dock`/enum type on the WPF side, and the docked-edge layout (full-bleed to one screen/window edge) needs to be built with a `Grid`/`DockPanel` layout rather than CSS positioning; (3) the same `data-starting-style`/`data-ending-style` CSS-transition choreography as Dialog needs a WPF storyboard reimplementation, likely combined with the slide-in transform driven by `Side`.

## Open questions

- The swipe-dismiss path (`SwipeCallbacks.OnDismiss`) calls `Context.RequestCloseAsync()` directly with no cancelable hook, unlike Escape/outside-pointer dismissal which route through `OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside`. This looks like an intentional asymmetry in the source, but it's a genuine question for the WPF port: should a native drag-to-dismiss gesture be cancelable by consumers, matching the other dismiss paths?
- Snap points, the swipe-area edge, indent/background-scale, multi-drawer nesting, and the virtual-keyboard provider are explicitly called out in the `DrawerContext` doc comment as "deferred (see docs/base-ui-parity.md)": none of that logic exists in this family's code, so the WPF port has no source behavior to mirror for those features yet.
- The exact drag distance/velocity threshold that triggers `OnDismiss` vs `OnReset` in `createSheetSwipe` is not visible in the C# family folder (it's implemented in JS interop outside this folder); the WPF port needs either the JS source or a product decision to replicate the threshold.
- No dev-time "missing title" warning exists for Drawer (present in Dialog); unclear whether this is an intentional omission or a gap to close for the WPF port's parity contract.

## WPF implementation notes

Shipped in `Navius.Wpf.Primitives.Controls.Drawer.NaviusDrawer` (see `docs/parity/dialog.md`'s "## WPF implementation notes" for the shared `NaviusOverlaySurfaceBase`/`NaviusOverlayLayer` plumbing all three overlay families share).

- **Scope: swipe/snap deferred.** Per the task brief for this milestone, drag-to-dismiss (`createSheetSwipe`, `data-swiping`, `--drawer-swipe-movement-x/y`), snap points, and the swipe-area-edge/indent/background-scale/multi-drawer/virtual-keyboard features this doc's own "Open questions" section calls out as already deferred on the web side are **not** implemented here either. This port is keyboard (Escape) + button (any content bound to `NaviusOverlaySurfaceBase.CloseCommand`) dismiss only. `SwipeCallbacks.OnDismiss`/`OnReset` and their cancelable-hook asymmetry (open question above) are therefore moot for this port: there is no swipe path to be cancelable or not.
- **`Side`: enum, not a free string.** `Controls.Drawer.NaviusDrawerSide { Left, Right, Top, Bottom }` replaces the source's unvalidated `Side` string, resolving this doc's "needs a proper Dock/enum type" open question. The default template (`Themes/Drawer.xaml`) docks the panel to the matching edge via `ControlTemplate.Triggers` on `Side` (`HorizontalAlignment`/`VerticalAlignment`/explicit `Width` or `Height` on the `PART_Panel` template part), the WPF equivalent of the web's CSS positioning.
- **Slide animation.** `NaviusDrawer` overrides `PlayEnterAnimation`/`PlayExitAnimation` to, in addition to the inherited 150ms backdrop+panel `Opacity` fade, translate `PART_Panel` via a `TranslateTransform` between `Controls.Drawer.DrawerGeometry.GetOffscreenOffset(Side, panelSize)` and `(0, 0)`. `DrawerGeometry` is a pure static function (`NaviusDrawerSide` + `Size` -> `Vector`) factored out specifically so the per-side offset math is unit-testable without a live visual tree or a running `Storyboard`, per the task's "focus-target selection / geometry logic factored pure" guidance. The panel extent used for the offscreen offset comes from the template part's explicit `Width`/`Height` on its offset axis (set by the `Side` triggers) with a 360px fallback if unset — plain WPF animations, no dependency on `Navius.Wpf.Motion` (this family must not reference it).
- **Role/AutomationPeer.** Same mapping as Dialog: `NaviusDrawerAutomationPeer` overrides `GetAutomationControlTypeCore() => AutomationControlType.Window` and `IsDialogCore() => true` — a docked sheet is still semantically a dialog to assistive tech, matching `role="dialog"` (not `alertdialog`) in the source.
- **Missing-title warning: closed the gap, uniformly.** Resolves this doc's own open question: rather than leaving Drawer without the dev-time warning Dialog has, `NaviusOverlaySurfaceBase.Engage()` applies the same `AutomationProperties.SetName`/`SetHelpText`-from-`Title`/`Description` treatment to all three families, so an unset `Title` is an accessible-name gap regardless of which family exposes it.
- **CloseOnEscape/CloseOnOutside.** Same as Dialog: `CloseOnEscape` fixed `true`; `CloseOnOutsideClick` is a settable DP (default `true`).
