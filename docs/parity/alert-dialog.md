# AlertDialog

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusAlertDialog | none (renders `ChildContent` inside a `CascadingValue`) | Root; owns open/closed state (controlled via `@bind-Open` or uncontrolled via `DefaultOpen`), cascades `AlertDialogContext` |
| NaviusAlertDialogTrigger | `<button type="button">` | Opens the dialog; captures its own `ElementReference` so focus can be restored to it on close |
| NaviusAlertDialogPortal | none (renders `ChildContent`) | API-parity wrapper around Backdrop + Popup; pushes `Container`/`ForceMount` into the shared context (actual teleport happens per-part via `NaviusPortal`) |
| NaviusAlertDialogBackdrop | `<div>` (inside `NaviusPortal`, conditionally rendered) | Purely visual scrim; clicking it does NOT close the dialog |
| NaviusAlertDialogPopup | `<div>` (inside `NaviusPortal`, conditionally rendered) | The focus-trapped, always-modal panel, `role="alertdialog"` |
| NaviusAlertDialogTitle | `<h2>` | Labels the dialog (`aria-labelledby` target) |
| NaviusAlertDialogDescription | `<p>` | Describes the dialog (`aria-describedby` target) |
| NaviusAlertDialogAction | `<button type="button">` | The confirming/destructive action; closes the dialog on click |
| NaviusAlertDialogCancel | `<button type="button">` | The cancelling action; closes the dialog on click and receives initial focus (APG) |

## Parameters

### NaviusAlertDialog

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | `false` | Controlled open state; use with `OpenChanged` (`@bind-Open`) |
| OpenChanged | EventCallback<bool> | none | Controlled-ness is determined by `OpenChanged.HasDelegate` |
| DefaultOpen | bool | `false` | Initial open state when used uncontrolled |
| ChildContent | RenderFragment? | none | |

### NaviusAlertDialogTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the `<button>` |

### NaviusAlertDialogPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Container | string? | none | CSS selector for the portal mount target; null = `document.body` |
| ForceMount | bool | `false` | Force-mounts both Backdrop and Popup while closed so an external presence/animation lib controls unmount |

### NaviusAlertDialogBackdrop

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | `false` | Keep the backdrop mounted while closed (for exit animations) |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values |

### NaviusAlertDialogPopup

Inherits `OverlayPopupBase` (shared with Dialog/Drawer); all listed parameters below are declared on the base class, plus its own `ChildContent`.

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | Own parameter |
| KeepMounted | bool | `false` | Inherited; keep the popup mounted (hidden) while closed |
| OnOpenAutoFocus | EventCallback<NaviusOpenAutoFocusEventArgs> | none | Inherited; cancelable, `PreventDefault` keeps focus where it is when the popup opens |
| OnCloseAutoFocus | EventCallback<NaviusCloseAutoFocusEventArgs> | none | Inherited; cancelable, `PreventDefault` skips returning focus to the trigger on close |
| OnEscapeKeyDown | EventCallback<NaviusEscapeKeyDownEventArgs> | none | Inherited; cancelable, `PreventDefault` keeps the popup open on Escape |
| OnPointerDownOutside | EventCallback<NaviusPointerDownOutsideEventArgs> | none | Inherited; cancelable (never fires a close for AlertDialog since `CloseOnOutside` is overridden false) |
| OnFocusOutside | EventCallback<NaviusFocusOutsideEventArgs> | none | Inherited; cancelable |
| OnInteractOutside | EventCallback<NaviusInteractOutsideEventArgs> | none | Inherited; cancelable |
| Attributes | IDictionary<string, object>? | none | Inherited; captured unmatched values |

### NaviusAlertDialogTitle

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | |

### NaviusAlertDialogDescription

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | |

### NaviusAlertDialogAction

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | |

### NaviusAlertDialogCancel

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusAlertDialog | OpenChanged | `EventCallback<bool>` |
| NaviusAlertDialogPopup | OnOpenAutoFocus | `EventCallback<NaviusOpenAutoFocusEventArgs>` (inherited from `OverlayPopupBase`) |
| NaviusAlertDialogPopup | OnCloseAutoFocus | `EventCallback<NaviusCloseAutoFocusEventArgs>` (inherited) |
| NaviusAlertDialogPopup | OnEscapeKeyDown | `EventCallback<NaviusEscapeKeyDownEventArgs>` (inherited) |
| NaviusAlertDialogPopup | OnPointerDownOutside | `EventCallback<NaviusPointerDownOutsideEventArgs>` (inherited) |
| NaviusAlertDialogPopup | OnFocusOutside | `EventCallback<NaviusFocusOutsideEventArgs>` (inherited) |
| NaviusAlertDialogPopup | OnInteractOutside | `EventCallback<NaviusInteractOutsideEventArgs>` (inherited) |

`Action` and `Cancel` do not expose their own close/click `EventCallback`; consumers wire destructive logic via native `@onclick` alongside the built-in `Context.RequestCloseAsync` handler.

## State + data attributes

| Attribute | Part | Notes |
|---|---|---|
| `aria-haspopup="dialog"`, `data-navius-alert-dialog-trigger` | Trigger | Marker + ARIA |
| `aria-expanded`, `aria-controls`, `data-popup-open` | Trigger | Reflect `Context.Open` / `Context.ContentId` |
| `data-open` / `data-closed` | Backdrop, Popup | Discrete open-state attrs (Base UI pattern) |
| `data-starting-style` / `data-ending-style` | Backdrop, Popup | Transition-phase attrs (entering/exiting) |
| `data-navius-alert-dialog-backdrop` | Backdrop | Marker |
| `data-navius-alert-dialog-popup` | Popup | Marker |
| `data-navius-alert-dialog-title` | Title | Marker |
| `data-navius-alert-dialog-description` | Description | Marker |
| `data-navius-alert-dialog-action` | Action | Marker |
| `data-navius-alert-dialog-cancel` | Cancel | Marker |

`AlertDialogContext` (public surface, implements `IOverlayContext`): `Open`, `Modal` (always `true` for this family), `ContentId`, `TitleId`, `DescriptionId`, `TriggerElement`/`HasTrigger`, `CancelElement`/`HasCancel`, `PortalContainer`, `PortalForceMount`, `Changed` event, `RequestSetAsync(bool)`, `RequestCloseAsync()`, `RequestToggleAsync()`.

## Keyboard

No `@onkeydown` markup exists directly in this family's `.razor` files. Escape handling is provided by the shared `OverlayPopupBase` that `NaviusAlertDialogPopup` inherits: a JS-side "dismissable layer" (created in `EngageAsync`) invokes the `[JSInvokable] OnDismiss(string reason)` C# callback.

| Key | Behavior |
|---|---|
| Escape | Closes the dialog (`OnDismiss("escape")` -> fires `OnEscapeKeyDown` if wired -> unless `PreventDefault`ed, calls `Context.RequestCloseAsync()`). `CloseOnEscape` defaults to `true` and AlertDialog does not override it. |
| Tab / Shift+Tab | Focus is trapped inside the Popup while open (`TrapFocus` = `OverlayContext.Modal`, always true here), engaged via `Interop.CreateFocusTrapAsync`; the actual tab-cycling logic lives in JS interop, not in this family's C#/Razor code. |

Note: an outside pointer-down does NOT close the dialog: `NaviusAlertDialogPopup` overrides `CloseOnOutside => false`, and the Backdrop's own click handler is absent (purely visual). This is a click/pointer behavior, not a keyboard one, but is relevant context for why Escape is the only dismissal key.

## Accessibility

- Trigger: `aria-haspopup="dialog"`, `aria-expanded`, `aria-controls` (pointing at `Context.ContentId`).
- Popup: `role="alertdialog"`, `aria-modal="true"`, `aria-labelledby="@Context.TitleId"`, `aria-describedby="@Context.DescriptionId"`, `tabindex="-1"`, `id="@Context.ContentId"`.
- Title: `<h2 id="@Context.TitleId">`. Description: `<p id="@Context.DescriptionId">`.
- Focus management: on open, initial focus targets the Cancel control (`InitialFocusSelector => "[data-navius-alert-dialog-cancel]"`) per the APG recommendation that the least-destructive action receive focus; if `OnOpenAutoFocus` is prevented, focus instead targets the Popup itself (`ResolveInitialFocus` override: `$"#{Context.ContentId}"`). Focus is trapped inside the Popup the entire time it is open (`TrapFocus` always true, since `Modal` is always true for AlertDialog). On close, focus returns to the Trigger element (`TriggerElement`) unless `OnCloseAutoFocus` is prevented, via the shared `OverlayPopupBase.DisengageAsync` / focus-trap release logic.
- Page scroll is locked while the dialog is open (`LockPageScroll` = `Modal`, always true here).
- Portal: Backdrop and Popup are each independently teleported to `document.body` (or `Container`) via `NaviusPortal`, escaping ancestor overflow/transform/z-index stacking contexts.

## WPF strategy

Tier B (custom lookless control), specifically a `Window`-hosted or adorner-layer-hosted modal popup rather than deriving from any single stock control. Model the Root (`NaviusAlertDialog`) as a lookless `ContentControl` owning `IsOpen`/`Open` state and an `AlertDialogContext`-equivalent DependencyObject; the Popup becomes a child `Window` (`WindowStyle=None`, owned by the main window, `ShowInTaskbar=false`) or a `Popup`/`AdornerLayer` overlay hosting a `ContentControl` with `AutomationProperties.Name` set from the Title element and role mapped via a custom `AutomationPeer` overriding `GetAutomationControlType() => AutomationControlType.Window` combined with UIA's `Dialog`/`alertdialog` semantics (WPF has no native `role="alertdialog"`; the closest UIA control type plus `LocalizedControlType="alert dialog"` is the practical mapping). `aria-modal` maps to the `Window`/`Popup` genuinely blocking interaction with the owner (e.g. via `Window.ShowDialog()` or manually disabling the owner). Focus trap maps to WPF's `KeyboardNavigation.TabNavigation="Cycle"` scoped to the popup's `FocusScope`, plus explicit initial-focus-to-Cancel logic in `Loaded`. Escape-to-close maps directly to a `PreviewKeyDown` handler or `KeyBinding` on `Key.Escape`. The `NaviusPortal`/teleport-to-`document.body` mechanism does NOT translate cleanly: WPF has no DOM-style node relocation, so the Backdrop/Popup pairing should become a genuine top-level `Window` (or `Popup` with `AllowsTransparency` for the backdrop scrim) rather than an in-tree relocated element; the CSS-driven `data-starting-style`/`data-ending-style` enter/exit transitions become WPF `Storyboard`s triggered off `IsOpen` changes.

## Open questions

- Whether the Backdrop + Popup pair should be two separate top-level `Window`s (matching the two independently-portaled/teleported parts in the source) or a single `Window` whose content is a `Grid` with a full-bleed scrim behind the panel (simpler, but diverges from the "Backdrop is portaled as a sibling of the popup" comment in the source).
- Whether `Container`/portal-target selection (a CSS selector string in the web version) has any WPF equivalent worth preserving, or should be dropped since WPF's popup hosting model (owner window, adorner layer) doesn't use CSS selectors.
- How to represent `OnOpenAutoFocus`/`OnCloseAutoFocus`/`OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside` as WPF routed events vs plain CLR events, and whether all six cancelable hooks are worth porting 1:1 or can be consolidated given WPF's different outside-interaction model (no single "dismissable layer" concept).
- Exact animation timing/easing for the enter/exit transitions is not specified in the reviewed code (it defers to CSS the consumer supplies via `data-starting-style`/`data-ending-style`/`data-open`/`data-closed` selectors), so the WPF port has no source-of-truth duration/easing to match; this needs a design decision, not just a port.

## WPF implementation notes

Shipped in `Navius.Wpf.Primitives.Controls.AlertDialog.NaviusAlertDialog`. Answers this doc's open questions concretely (see `docs/parity/dialog.md`'s "## WPF implementation notes" for the shared plumbing all three overlay families use: `Controls.OverlaySurface.NaviusOverlaySurfaceBase` + `Controls.OverlaySurface.NaviusOverlayLayer`):

- **One Window vs. two, Backdrop vs. Popup.** Neither: like Dialog/Drawer, this is a single lookless `ContentControl` whose own `ControlTemplate` contains both the backdrop (`Overlays.OverlayBackdrop`) and the panel, not two independently-portaled top-level `Window`s. This sidesteps the doc's "two windows vs. one window with a scrim" question entirely — there is no second top-level surface to reason about.
- **Always modal, no Modal DP.** Unlike `NaviusDialog`/`NaviusDrawer` (which expose a settable `Modal` DP), `NaviusAlertDialog` has none: `ModalEffective` is hard-coded `protected override bool ModalEffective => true;`. This is a deliberate API-surface choice (not just a default), matching the web contract's "AlertDialog is always modal" rather than exposing a property a consumer could set to `false` against the contract.
- **No outside dismissal.** `CloseOnOutsideClickEffective` is hard-coded `false` (also not a DP), matching `NaviusAlertDialogPopup.CloseOnOutside => false` in the source.
- **Initial focus on Cancel.** Implemented via an attached `NaviusAlertDialog.IsCancelButtonProperty` (bool) a consumer sets on the button that plays Cancel's role, e.g. `alertDialog:NaviusAlertDialog.IsCancelButton="True"` — the WPF analogue of the source's `InitialFocusSelector => "[data-navius-alert-dialog-cancel]"` CSS selector, since WPF has no selector-based lookup. Resolution is a pure static helper, `Controls.AlertDialog.AlertDialogFocus.FindCancelElement(DependencyObject root)`, walking the **logical** tree (matching `NaviusRadioGroup`/`NaviusCheckboxGroup`'s own descendant search) so it works without a layout pass or a live `PresentationSource`, and is unit-testable directly. `NaviusOverlaySurfaceBase.Engage()` calls this via the overridable `ResolveInitialFocusElement()` hook immediately after `OverlayStack.Push` (which already moved focus to the first focusable descendant via its own focus-trap engagement), then re-focuses the Cancel element — so Cancel wins even if it is not first in visual/logical order.
- **Six cancelable hooks -> one.** The source's `OnOpenAutoFocus`/`OnCloseAutoFocus`/`OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside` collapse into the single cancelable `Closing` event already defined on `Overlays.OverlaySession` (reused as-is, not redefined per-family): `EventHandler<Overlays.OverlayClosingEventArgs>? Closing`, carrying an `Overlays.OverlayCloseReason` (`EscapeKey` | `OutsidePress` | `Programmatic`). This resolves the doc's open question in favor of consolidation: WPF's outside-interaction model has no per-phase (pointer-down vs. focus vs. generic-interact) distinction to preserve, so a single reason enum is sufficient.
- **Role/AutomationPeer.** `NaviusAlertDialogAutomationPeer` overrides `GetAutomationControlTypeCore() => AutomationControlType.Window`, `IsDialogCore() => true` (same as Dialog/Drawer), and additionally `GetLocalizedControlTypeCore() => "alert dialog"` — the closest native UIA mapping to `role="alertdialog"`, since WPF has no built-in alert-dialog control type.
- **Enter/exit animation.** Same 150ms `Opacity` fade as Dialog (see dialog.md's implementation notes); no family-specific override, since the source gives no duration/easing to match.

## M6 audit (2026-07-09)

Re-verified `NaviusAlertDialog`/`AlertDialogFocus`/`NaviusAlertDialogAutomationPeer` against this
doc's claims (`ModalEffective`/`CloseOnOutsideClickEffective` hard-coded, `IsCancelButton` attached
property + logical-tree search for initial focus, `AutomationControlType.Window` +
`IsDialogCore()=true` + `GetLocalizedControlTypeCore()="alert dialog"`) against
`AlertDialogTests.cs`; all confirmed. `Themes/AlertDialog.xaml` uses only `DynamicResource` tokens.

**PLAUSIBLE, not fixed (shared substrate outside this batch's editable scope).** The doc's own
"Six cancelable hooks -> one" claim collapses `OnOpenAutoFocus`/`OnCloseAutoFocus`/`OnEscapeKeyDown`/
`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside` into the single cancelable `Closing`
event on `Overlays.OverlaySession`. Inspecting the shared substrate
(`src/Navius.Wpf.Primitives/Overlays/OverlaySession.cs`, `OverlayOptions.cs`) shows only a
cancelable `Closing` event and non-cancelable `Opened`/`Closed` events exist -- there is no
cancelable hook at all for the OPEN side (`OnOpenAutoFocus`'s "PreventDefault keeps focus where it
is when the popup opens" behavior). The "six -> one" framing therefore overstates what was ported:
it's closer to "five close-related hooks -> one `Closing` event, `OnOpenAutoFocus` dropped
entirely, undocumented as a drop." This affects every family built on `NaviusOverlaySurfaceBase`/
`OverlayStack` (Dialog, Drawer, Popover, Autocomplete, Combobox, etc.), not just AlertDialog, so it
is reported here rather than fixed: `Overlays/` is explicitly audit-and-report-only for this
batch, and the fix (adding a cancelable open-focus hook to the shared session/options types) would
touch infrastructure other concurrently-running auditors' families also depend on.

## Post-release fixes (2026-07-11)

- **Reopen-during-exit race fixed.** The shared `NaviusOverlaySurfaceBase` gained an engage-generation
  guard: `Engage()` bumps a counter, and the exit-completion callback captures that counter when the
  close begins and no-ops if a newer `Engage` has happened since. Closing an AlertDialog and reopening
  it within the ~150ms exit fade no longer lets the stale exit callback run `layer.RemoveSurface` /
  `Visibility = Collapsed` / `IsOpen = false` against the freshly reopened session and collapse it.
  Regression test lives on the shared base's Dialog coverage
  (`DialogTests.Reopen_DuringExitAnimation_KeepsTheNewSessionOpen`); AlertDialog shares the fix since
  it shares the base (PR #3).
