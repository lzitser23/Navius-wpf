# Menubar

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusMenubar | `<div role="menubar">` | Root: a horizontal (or vertical) row of menus; owns which single menu is open |
| NaviusMenubarMenu | none (CascadingValue only) | One menu within the bar, identified by `Value`; creates the per-menu context |
| NaviusMenubarTrigger | `<button type="button">` (role="menuitem") | Top-level menu button; toggles its menu, "follows focus" when another menu is open |
| NaviusMenubarPortal | none (renders `ChildContent`) | Flag-setter: records custom mount container + keep-mounted into the menu context |
| NaviusMenubarPositioner | none (renders `ChildContent`; flag-setter) | Publishes placement options for the menu's Popup (default align "start") |
| NaviusMenubarPopup | `<div role="menu">` inside a positioning `<div>`, inside `<NaviusPortal>` | The open menu's surface: positioning, dismissable layer, roving focus, adjacent-menu arrow-key handling |
| NaviusMenubarArrow | `<svg>` with `<polygon>` | Optional arrow pointing from Popup at its Trigger; `aria-hidden="true"` |
| NaviusMenubarItem | `<div role="menuitem">` | A selectable item inside a menu |
| NaviusMenubarCheckboxItem | `<div role="menuitemcheckbox">` | Tri-state (true/false/indeterminate) checkable item |
| NaviusMenubarItemIndicator | `<span>` (conditional) | Check/dot glyph shown when the parent Checkbox/Radio item is checked |
| NaviusMenubarGroup | `<div role="group">` | Groups items; references a Label via `aria-labelledby` |
| NaviusMenubarLabel | `<div>` | Non-interactive heading; registers its id into `MenubarGroupContext` when nested in a Group |
| NaviusMenubarRadioGroup | `<div role="group">` | Cascades a selected value to `NaviusMenubarRadioItem` children |
| NaviusMenubarRadioItem | `<div role="menuitemradio">` | Single-select item inside a RadioGroup |
| NaviusMenubarSeparator | `<div role="separator">` | Visual divider |
| NaviusMenubarSub | none (CascadingValue only) | A nested submenu; owns its own independent open state |
| NaviusMenubarSubTrigger | `<div role="menuitem">` | Opens a submenu (hover/click/arrow-key); the submenu's positioning anchor |
| NaviusMenubarSubContent | `<div role="menu">` (conditional on `Open`/`ForceMount`), inside `<NaviusPortal>` | The floating submenu surface, own positioning + roving focus + dismissable layer |

## Parameters

### NaviusMenubar

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string? | null | Controlled open-menu value (the open menu's `Value`, or null); pair with `ValueChanged` |
| ValueChanged | EventCallback\<string?\> | none | |
| DefaultValue | string? | null | Uncontrolled initial open-menu value |
| Orientation | string | "horizontal" | "horizontal" or "vertical"; drives the trigger-roving axis |
| Dir | string? | null | "ltr"/"rtl"; overrides cascaded direction; flips ArrowLeft/Right roving and submenu keys |
| Loop | bool | false | Keyboard focus loops from the last trigger back to the first (and vice versa) |
| Modal | bool | true | Applies to whichever menu is open; drives scroll-lock + outside-pointer guard |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarMenu

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string | (required, `default!`) | Identity of this menu; throws `InvalidOperationException` if null/empty |
| ChildContent | RenderFragment? | null | |

### NaviusMenubarTrigger

(inherits `MenubarPart`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Disabled | bool | false | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Container | string? | null | CSS selector of a custom mount container; null = `document.body` |
| KeepMounted | bool | false | Keep the popup mounted while closed |

### NaviusMenubarPositioner

(inherits `OverlayPositionerBase`; overrides `DefaultAlign` = "start")

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Side | string? | null | Falls back to base `DefaultSide` = "bottom" |
| Align | string? | null | Falls back to `DefaultAlign` = "start" (overridden here, vs. "center" for Menu) |
| SideOffset | double | 0 | |
| AlignOffset | double | 0 | |
| Flip | bool | true | |
| AvoidCollisions | bool | true | |
| CollisionPadding | double? | null | |
| Sticky | string? | null | |
| HideWhenDetached | bool | false | |
| ArrowPadding | double | 0 | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarPopup

(inherits `OverlayAnchoredPopupBase` -> `OverlayPopupBase` -> `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Loop | bool | false | Arrow navigation wraps at the ends within this menu |
| KeepMounted | bool | false | (inherited) |
| OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | none | (inherited) Cancelable |
| OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | none | (inherited) Cancelable |
| OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | none | (inherited) Cancelable |
| OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | none | (inherited) Cancelable |
| OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | none | (inherited) Cancelable |
| OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | none | (inherited) Cancelable |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarArrow

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | double | 10 | |
| Height | double | 5 | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires on click or Enter/Space; `PreventDefault()` keeps the menu open |
| Disabled | bool | false | |
| TextValue | string? | null | Overrides typeahead match text (`data-navius-text-value`) |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarCheckboxItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Checked | bool? | null | Controlled tri-state; `null` = indeterminate |
| CheckedChanged | EventCallback\<bool?\> | none | Only invoked when controlled (`CheckedChanged.HasDelegate`) |
| DefaultChecked | bool? | null | Uncontrolled initial state |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires after toggling; `PreventDefault()` keeps the menu open |
| Disabled | bool | false | |
| TextValue | string? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarItemIndicator

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| ForceMount | bool | false | Keep mounted even when unchecked |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarRadioGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Value | string? | null | Controlled selected value |
| ValueChanged | EventCallback\<string?\> | none | |
| DefaultValue | string? | null | Uncontrolled initial value |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarRadioItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Value | string | (required, `default!`) | Throws `InvalidOperationException` if null/empty |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires after selection; `PreventDefault()` keeps the menu open |
| Disabled | bool | false | |
| TextValue | string? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarSub

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | false | Controlled submenu open state |
| OpenChanged | EventCallback\<bool\> | none | Invoked both when controlled AND uncontrolled (uncontrolled branch also calls `OpenChanged.InvokeAsync`) |
| DefaultOpen | bool | false | |
| ChildContent | RenderFragment? | null | |

### NaviusMenubarSubTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Disabled | bool | false | |
| TextValue | string? | null | |
| Dir | string? | null | Overrides cascaded `NaviusDirection`; falls back to "ltr" |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenubarSubContent

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |
| Side | string | "right" | Submenus open to the inline-end; flips to "left" internally when rtl and `Side == "right"` |
| Align | string | "start" | |
| SideOffset | double | 0 | |
| AlignOffset | double | 0 | |
| AvoidCollisions | bool | true | Also folded into `Flip` for the positioner call |
| CollisionPadding | double? | null | |
| Loop | bool | false | |
| ForceMount | bool | false | Keep mounted while closed |
| Container | string? | null | Portal container selector; defaults to `document.body` |
| Dir | string? | null | Overrides cascaded `NaviusDirection`; falls back to "ltr" |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusMenubar | ValueChanged | EventCallback\<string?\> | The open menu changes (trigger toggle, item select-close, dismiss, adjacent-menu arrow key) |
| NaviusMenubarSub | OpenChanged | EventCallback\<bool\> | Submenu open state changes; invoked even when uncontrolled |
| NaviusMenubarItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | Item clicked or Enter/Space; cancelable |
| NaviusMenubarCheckboxItem | CheckedChanged | EventCallback\<bool?\> | Item activated; toggles true<->false (indeterminate/false -> true); only invoked when controlled |
| NaviusMenubarCheckboxItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | After toggling; cancelable |
| NaviusMenubarRadioGroup | ValueChanged | EventCallback\<string?\> | A RadioItem is selected; only invoked when controlled |
| NaviusMenubarRadioItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | After selecting; cancelable |
| NaviusMenubarPopup | OnOpenAutoFocus / OnCloseAutoFocus / OnEscapeKeyDown / OnPointerDownOutside / OnFocusOutside / OnInteractOutside | (see Parameters) | Same semantics as `OverlayPopupBase` (shared with Menu) |

## State + data attributes

| Attribute / class | Where | Meaning |
|---|---|---|
| `data-navius-menubar` | Root | Marker |
| `data-orientation` | Root ("horizontal"/"vertical"), Popup/SubContent (always "vertical") | Reflects `Orientation` on root; menu content is always vertically oriented |
| `data-navius-menubar-trigger` | Trigger | Marker (roving-focus target selector for the trigger row) |
| `data-popup-open` | Trigger, SubTrigger | Present when that (sub)menu is open |
| `data-disabled` / `aria-disabled` | Trigger, Item, CheckboxItem, RadioItem, SubTrigger | Present when `Disabled` |
| `data-navius-menubar-positioner` | Positioner div | Marker; engine writes `--anchor-*`/`--available-*` vars + `data-side`/`data-align`/`data-anchor-hidden` here |
| `data-navius-menubar-popup` | Popup | Marker |
| `data-open` / `data-closed` | Popup, SubContent | Discrete open/closed state (C#-owned) |
| `data-starting-style` / `data-ending-style` | Popup | Enter/exit transition frames |
| `data-navius-menubar-arrow` | Arrow | Marker |
| `data-navius-menubar-item` | Item | Marker |
| `data-navius-menubar-checkbox-item` | CheckboxItem | Marker |
| `data-navius-menubar-radio-item` | RadioItem | Marker |
| `data-navius-text-value` | Item, CheckboxItem, RadioItem, SubTrigger | Typeahead match-text override |
| `data-checked` / `data-unchecked` / `data-indeterminate` | CheckboxItem, RadioItem (unchecked/checked only), ItemIndicator | Tri-state (checkbox) / bi-state (radio) reflection |
| `data-navius-menubar-item-indicator` | ItemIndicator | Marker |
| `data-navius-menubar-group` | Group | Marker |
| `data-navius-menubar-label` | Label | Marker |
| `data-navius-menubar-radio-group` | RadioGroup | Marker |
| `data-navius-menubar-separator` | Separator | Marker |
| `data-navius-menubar-sub-content` | SubContent | Marker |
| `data-side` / `data-align` | SubContent | Reflects resolved `EffectiveSide`/`Align` directly (not engine-mirrored like the root Popup) |
| `data-navius-menubar-sub-trigger` | SubTrigger | Marker |
| `data-highlighted` | Roving-focus target items and triggers | Toggled by the JS roving-focus engine (`DataHighlight: true`) |

Internal (non-DOM) state: `MenubarContext.OpenValue` (at most one open menu across the whole bar, tracked by menu `Value`; `_order` list tracks registration order for adjacent-menu navigation), `MenubarMenuContext.Open` (derived: `== root.OpenValue`), `MenubarSubContext.Open` (independent per submenu, supports arbitrary nesting), `MenubarItemIndicatorContext.IsChecked` (function-based, re-evaluated per render), `MenubarRadioGroupContext` (value getter + select callback), `MenubarMenuContext.SuppressAdjacentOnce` (one-shot flag a SubTrigger sets so its own open-key press doesn't also bubble into the parent Popup's adjacent-menu move).

## Keyboard

| Key | Behavior |
|---|---|
| ArrowLeft / ArrowRight (focus on a trigger, no menu open) | Roves focus along the trigger row; confirmed by e2e (`wave2.spec.ts` "Menubar: ... ArrowRight roves to the next trigger"). Implemented by the JS `RovingFocus` engine (orientation from `Orientation`, `Loop`), not directly visible in this folder's C# |
| Home / End (focus on a trigger) | Documented in `MenubarContext`'s XML comments as engine-provided roving to first/last trigger; not exercised by any e2e test found in this repo |
| ArrowDown / ArrowUp / Enter / Space (on Trigger, its menu closed) | Opens that menu (`NaviusMenubarTrigger.OnKeyDownAsync`), suppressing the key's default browser action via `@onkeydown:preventDefault="_preventNextKey"` |
| Focus moves to a different trigger while another menu is open | "Follow focus": that trigger's menu opens automatically (`OnFocusInAsync`) |
| ArrowDown / ArrowUp (inside an open Popup) | Roves focus between enabled items (vertical), skipping `data-disabled`; JS `RovingFocus`, same pattern as Menu |
| ArrowLeft / ArrowRight (inside an open Popup, at the top level) | Closes the current menu and opens the adjacent one (`NaviusMenubarPopup.OnKeyDownAsync` -> `MenubarContext.MoveToAdjacentAsync`), honoring `Loop`; direction is rtl-aware |
| Enter / Space (on Item / CheckboxItem / RadioItem) | Activates the item |
| ArrowRight (ArrowLeft in rtl) / Enter / Space (on SubTrigger) | Opens the submenu; sets `MenuContext.SuppressAdjacentOnce = true` so the same keypress doesn't also bubble into the parent Popup's adjacent-menu handler |
| ArrowLeft (ArrowRight in rtl) (inside SubContent) | Closes only that submenu, restores focus to its SubTrigger |
| Escape | Closes the open (sub)menu via its JS dismissable layer calling back into `OnDismiss("escape")` / the shared `OverlayPopupBase.OnDismiss` |

Note: `NaviusMenubarTrigger`'s doc comment claims "ArrowUp opens too, focusing the LAST item (spec parity)", but no code in this folder passes a "focus last item" hint into `NaviusMenubarPopup.EngageAsync`'s `RovingFocusOptions` (it always just sets `AutoFocus: !OpenAutoFocusPrevented`), see Open Questions.

## Accessibility

- `role="menubar"` on the root; `role="menu"` on Popup and SubContent (with `aria-orientation="vertical"`); `role="menuitem"` on Trigger, Item, SubTrigger; `role="menuitemcheckbox"` on CheckboxItem; `role="menuitemradio"` on RadioItem; `role="group"` on Group and RadioGroup; `role="separator"` + `aria-orientation="horizontal"` on Separator.
- `aria-haspopup="menu"`, `aria-expanded`, `aria-controls` on Trigger and SubTrigger (controls only set while open for SubTrigger: `aria-controls="@(Sub.Open ? Sub.ContentId : null)"`).
- Popup and SubContent additionally carry `aria-labelledby` pointing at their owning Trigger/SubTrigger id (`Context.TriggerId` / `Sub.TriggerId`), unlike the Menu family's Popup, which has no `aria-labelledby`.
- `aria-checked` tri-state on CheckboxItem (`"true"`/`"false"`/`"mixed"`); bi-state on RadioItem.
- `aria-disabled="true"` on Item/CheckboxItem/RadioItem/SubTrigger when `Disabled` (Trigger uses the native `disabled` attribute instead).
- `aria-labelledby` on Group, pointed at a nested Label's generated id (`MenubarGroupContext`).
- `tabindex="-1"` on all roving-focus targets (triggers, items, sub-triggers) and on Popup/SubContent; only one trigger is ever in the natural Tab order at a time (roving tabindex over the trigger row).
- `dir="rtl"` reflected on the root (when `Dir` explicitly set), Popup, and SubContent per their resolved direction.
- Focus management: same `OverlayPopupBase` engage/disengage pattern as Menu (focus moves into the popup on open unless `OnOpenAutoFocus` prevented, roving then focuses the first item; focus returns to the trigger on close unless `OnCloseAutoFocus` prevented). `NaviusMenubarSubContent` explicitly refocuses its own `SubTrigger` on close. The root menubar's own `RovingFocus` mounts with `AutoFocus: false` so the bar never steals focus on initial render.

## WPF strategy

Tier A (derive from native `System.Windows.Controls.Menu` / `MenuItem`).

This family is the closest 1:1 match to a native WPF control among the three: WPF's `Menu` is exactly a horizontal top-level bar of `MenuItem`s, each of which already supports nested submenus (`MenuItem.Items`), roving/typeahead keyboard navigation, hover-to-switch between open siblings ("follow focus" is largely free), checkable items (`IsCheckable`/`IsChecked`), and `MenuItemAutomationPeer`/`MenuAutomationPeer` mapping directly to UIA `MenuBar`/`Menu`/`MenuItem` control types (matching `role="menubar"`/`"menu"`/`"menuitem"`). Things that will not translate cleanly: the JS-interop-driven `Positioner`/`DismissableLayer`/`RovingFocus` engine and its CSS-var-based placement (WPF's native `Popup`/submenu placement replaces all of it); the `data-starting-style`/`data-ending-style` CSS-transition choreography (would need WPF `Storyboard`/`VisualStateManager`); the `Portal`/`Container` custom-mount-point concept (no DOM-teleport equivalent needed); and the tri-state indeterminate `CheckboxItem` (native `MenuItem` has no built-in indeterminate visual, needs a custom template, same gap as in Menu).

## Open questions

- `NaviusMenubarTrigger`'s doc comment asserts ArrowUp opens the menu focusing the *last* item ("spec parity"), but the visible C# in this folder never threads that distinction through to `NaviusMenubarPopup`'s `RovingFocusOptions`: confirm whether this is implemented inside the opaque JS roving-focus engine or is an actual gap to fix/decide during the port.
- Home/End roving over top-level triggers is only asserted in doc comments, not confirmed by any e2e test found (`wave2.spec.ts` only exercises ArrowRight). Needs verification against the JS engine or Base UI spec before porting.
- Controlled/uncontrolled detection is inconsistent across parts within this family and against the sibling Menu family: `NaviusMenubar`/`NaviusMenubarRadioGroup`/`NaviusMenubarCheckboxItem` use `XxxChanged.HasDelegate`, while `NaviusMenubarSub` tracks whether `Open` was literally passed via `SetParametersAsync` (matching the Menu family's `NaviusMenuSub`/`NaviusMenuCheckboxItem`/`NaviusMenuRadioGroup` pattern). The WPF port (likely via `DependencyProperty` + `CoerceValueCallback` or two-way bindings) should pick one canonical strategy rather than preserving this split.
- `NaviusMenubarCheckboxItem`'s uncontrolled branch does not invoke `CheckedChanged` (`_internalChecked = next;` only), whereas `NaviusMenubarSub`'s uncontrolled branch explicitly does invoke `OpenChanged`, and the Menu family's `NaviusMenuCheckboxItem` invokes `CheckedChanged` even when uncontrolled. Confirm which behavior is intentional before deciding the WPF two-way-binding contract.
- `NaviusMenubarSubContent.AvoidCollisions` is folded into the positioner call as `Flip: AvoidCollisions` (no separate `Flip` parameter, unlike `NaviusMenuSubContent`/the shared `OverlayPositionerBase`), confirm whether this coupling is intentional or should be split into independent `Flip`/`AvoidCollisions` parameters for the port.
- Full key set for the JS `RovingFocus` engine (typeahead debounce/casing, PageUp/PageDown if any) is not visible in this folder; only ArrowLeft/Right/Down/Up and the explicit C# key handlers documented above are confirmed.

## WPF implementation notes

Implemented in `src/Navius.Wpf.Primitives/Controls/Menubar/` (+ `Themes/Menubar.xaml`, gallery `Pages/MenubarPage.xaml(.cs)`, tests `tests/Navius.Wpf.Tests/MenubarTests.cs`, 15 tests).

### Type collapse (native Menu/MenuItem absorbs most parts)

Only 9 public control types are implemented; the rest of the 18-part web contract collapses into native `Menu`/`MenuItem` behavior for free, matching the "WPF strategy" section above:

| Web part(s) | WPF type | Notes |
|---|---|---|
| `NaviusMenubar` | `NaviusMenubar : Menu` | Value/DefaultValue/ValueChanged, Orientation (swaps ItemsPanel), Dir, Loop (best-effort, see below), Modal (API-parity only, no extra behavior) |
| `NaviusMenubarMenu` + `Trigger` + `Portal` + `Positioner` + `Popup` + `Arrow` | `NaviusMenubarMenu : MenuItem` | A top-level `MenuItem`'s Header *is* the trigger; its Items *are* the positioned, dismissable, portal-free popup. `Value` required, validated in `OnInitialized` (throws `InvalidOperationException`, mirrors the web contract) |
| `NaviusMenubarItem` | `NaviusMenubarItem : MenuItem` | `Disabled` maps directly to native `IsEnabled` (no separate DP) |
| `NaviusMenubarCheckboxItem` + `ItemIndicator` | `NaviusMenubarCheckboxItem : MenuItem` | Own nullable `Checked` DP (tri-state) mirrored one-way into native `IsChecked` (indeterminate -> false, `MenuItemAutomationPeer`'s `IToggleProvider` reports a binary approximation); glyph is template markup (Themes/Menubar.xaml), no separate `ItemIndicator` type |
| `NaviusMenubarRadioItem` + `NaviusMenubarRadioGroup` | `NaviusMenubarRadioItem : MenuItem`, `NaviusMenubarRadioGroup : DependencyObject` | **Structural delta**: RadioGroup is a non-visual coordinator, not a nesting wrapper; see "RadioGroup does not nest" below |
| `NaviusMenubarGroup` + `NaviusMenubarLabel` | `NaviusMenubarLabel : MenuItem` only | No `NaviusMenubarGroup` type; grouping is a Label + items + `NaviusMenubarSeparator` run with no wrapping element (see "Group has no wrapper" below) |
| `NaviusMenubarSeparator` | `NaviusMenubarSeparator : Separator` | Thin subclass only, for a consistent namespace/DefaultStyleKey; native `Separator` already works inside `Menu`/`MenuItem.Items` |
| `NaviusMenubarSub` + `NaviusMenubarSubTrigger` + `NaviusMenubarSubContent` | `NaviusMenubarSubTrigger : MenuItem` | Nest the submenu's items directly in a SubTrigger's own `Items`, exactly like plain native WPF submenu nesting; `OpenChanged` wraps `SubmenuOpened`/`SubmenuClosed` |

`Attributes` (captured-unmatched-attributes) is not ported anywhere in this family: there is no WPF equivalent to a Blazor unmatched-parameter bag.

### RadioGroup does not nest

The web contract nests `NaviusMenubarRadioItem`s inside a `NaviusMenubarRadioGroup` element. Native `Menu`/`MenuItem` only treat `MenuItem`/`Separator` as "their own container" (`IsItemItsOwnContainerOverride`); wrapping items in an arbitrary `ItemsControl` would make WPF auto-generate a blank `MenuItem` container around it (`Header` = the wrapper), and there is no way to verify without an interactive runtime whether the *nested* items would still get correct `Role`/keyboard-navigation treatment from that generated container. Instead, `NaviusMenubarRadioGroup` is a plain, non-visual `DependencyObject` coordinator: declare one (e.g. as a resource) and point each `NaviusMenubarRadioItem.Group` at it:

```xml
<menubar:NaviusMenubarRadioGroup x:Key="ThemeGroup" Value="light" />
...
<menubar:NaviusMenubarRadioItem Value="light" Group="{StaticResource ThemeGroup}" Header="Light" />
```

### Group has no wrapper

Same reasoning as RadioGroup. No `NaviusMenubarGroup` type is implemented; express a labeled group the idiomatic native way, a `NaviusMenubarLabel` followed by its items followed by a `NaviusMenubarSeparator`, all as flat siblings (see `Pages/MenubarPage.xaml`'s Edit menu for a working example).

### CheckboxItem/RadioItem never call `base.OnClick()`

Discovered via the test suite: native `MenuItem.OnClick()`, when `IsCheckable=true`, *unconditionally* toggles `IsChecked` as part of its own click handling, regardless of what a subclass already set. Since both `NaviusMenubarCheckboxItem` and `NaviusMenubarRadioItem` set `IsChecked` themselves (from their own tri-state/Group-driven logic) before the click finishes, calling `base.OnClick()` afterward double-toggles it back to the wrong value. Both types now raise the `Click` routed event manually instead of calling `base.OnClick()`. Net effect: a bound `Command`/`CommandParameter` on either type will not execute (this family's interaction model is OnSelect-driven, not Command-driven, so this is an accepted delta, not a regression from anything working before). `NaviusMenubarItem` (non-checkable) is unaffected and still calls `base.OnClick()` conditionally on `PreventDefault()`.

### Controlled/uncontrolled: one canonical strategy picked (per Open Questions)

Per the parity doc's own open question, this port does not preserve the web contract's split `HasDelegate` vs. explicit-`SetParametersAsync`-tracking strategies. Every stateful DP here (`NaviusMenubar.Value`, `NaviusMenubarCheckboxItem.Checked`, `NaviusMenubarRadioGroup.Value`) is a plain two-way-bindable `DependencyProperty` (`BindsTwoWayByDefault` where relevant) whose paired event always raises on change, controlled or not. This is the same canonical choice made for the Checkbox/RadioGroup families elsewhere in this repo.

### Loop=false is best-effort

Native WPF top-level menu keyboard navigation always wraps; there is no built-in "don't wrap" switch. `NaviusMenubar.Loop=false` is implemented as a `PreviewKeyDown` interceptor that swallows the ArrowLeft/ArrowRight keypress that *would* wrap past the first/last top-level trigger (based on `IsKeyboardFocusWithin`), rather than a first-class navigation-engine setting.

### Headless-testing limitation: `IsSubmenuOpen` needs a real window

Confirmed via direct experimentation while writing `MenubarTests.cs`: setting `MenuItem.IsSubmenuOpen = true` in code is silently coerced back to `false` unless the element is part of a real, shown `Window` (native submenu-open state appears to require live keyboard/HWND focus infrastructure that a headless STA test never has, even after `Measure`/`Arrange`/`UpdateLayout`). This is a WPF platform constraint, not specific to this port. Tests that need to exercise "a submenu just opened/closed" instead raise `MenuItem.SubmenuOpenedEvent`/`SubmenuClosedEvent` directly via `RaiseEvent`, which correctly exercises `NaviusMenubar`'s own reactive code without depending on the native coercion gate. `Bar_SettingValue_UpdatesValueAndAttemptsToOpenMatchingMenu` only asserts the `Value` bookkeeping side for the same reason.

### Overlap with the Menu family

Per the task brief, this agent built its own types under `Controls/Menubar/` rather than depending on or editing the sibling Menu family's in-flight files, even though several of them are conceptually identical native-`MenuItem`-derived types (`NaviusMenubarItem`/`NaviusMenuItem`, `NaviusMenubarSeparator`/`NaviusMenuSeparator`, `NaviusMenubarLabel`/`NaviusMenuLabel`, the checkbox/radio-item tri-state-glyph-and-double-toggle-workaround pattern, etc., assuming the Menu agent reached similar native-substrate conclusions independently). The orchestrator should audit both `Controls/Menu/` and `Controls/Menubar/` once both land and consider extracting a shared base (e.g. a common `MenuItemPart : MenuItem` for the Disabled-is-IsEnabled/TextValue/OnClick-Select-event pattern) if the duplication is as large as expected, rather than keeping two independent copies long-term.
