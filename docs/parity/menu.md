# Menu

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusMenu | none (CascadingValue only) | Root: owns open state (controlled/uncontrolled), cascades `MenuContext` |
| NaviusMenuTrigger | `<button type="button">` | Toggles the menu; positioning anchor |
| NaviusMenuPortal | none (renders `ChildContent`) | Flag-setter: records custom mount container + keep-mounted into context (actual teleport done by Popup via `NaviusPortal`) |
| NaviusMenuPositioner | none (renders `ChildContent`; flag-setter) | Publishes placement options (side/align/offsets/collision) into context; Popup renders the actual positioning `<div>` |
| NaviusMenuPopup | `<div role="menu">` inside a positioning `<div>`, inside `<NaviusPortal>` | The menu surface: engages anchored positioning, dismissable layer, roving focus |
| NaviusMenuArrow | `<svg>` (default `<polygon>` triangle) | Optional arrow pointing at the trigger, registered into context for positioning |
| NaviusMenuItem | `<div role="menuitem">` | A selectable menu item |
| NaviusMenuCheckboxItem | `<div role="menuitemcheckbox">` | Tri-state (true/false/indeterminate) checkable item |
| NaviusMenuItemIndicator | `<span>` (conditional) | Check/dot glyph shown when the parent Checkbox/Radio item is checked |
| NaviusMenuGroup | `<div role="group">` | Groups items; references a Label via `aria-labelledby` |
| NaviusMenuGroupLabel | `<div>` | Heading for a Group; registers its id into `MenuGroupContext` |
| NaviusMenuRadioGroup | `<div role="group">` | Cascades a selected value to `NaviusMenuRadioItem` children |
| NaviusMenuRadioItem | `<div role="menuitemradio">` | Single-select item inside a RadioGroup |
| NaviusMenuSeparator | `<div role="separator">` | Visual divider between items/groups |
| NaviusMenuSub | none (CascadingValue only) | Owns a nested submenu's own open state, cascades `MenuSubContext` |
| NaviusMenuSubTrigger | `<div role="menuitem">` | Opens a submenu (hover / click / arrow-key); the submenu's positioning anchor |
| NaviusMenuSubContent | `<div role="menu">` (conditional on `Open`/`ForceMount`) | The floating submenu surface, own roving focus + dismissable layer |

## Parameters

### NaviusMenu

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | false | Controlled open state |
| OpenChanged | EventCallback\<bool\> | none | Presence of a delegate makes the component controlled |
| DefaultOpen | bool | false | Uncontrolled initial open state |
| Modal | bool | true | Drives scroll-lock + outside-pointer guard on the Popup |
| Dir | string? | null | "ltr"/"rtl"; falls back to cascaded `NaviusDirection`, then "ltr"; flips submenu arrow keys |
| ChildContent | RenderFragment? | null | |

### NaviusMenuTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Disabled | bool | false | Non-interactive; reflects `data-disabled` and native `disabled` |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Container | string? | null | CSS selector of a custom mount container; null teleports into `document.body` |
| KeepMounted | bool | false | Keep the popup mounted while closed (for exit animations) |

### NaviusMenuPositioner

(inherits `OverlayPositionerBase`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Side | string? | null | Falls back to base `DefaultSide` = "bottom" (not overridden for Menu) |
| Align | string? | null | Falls back to base `DefaultAlign` = "center" (not overridden for Menu) |
| SideOffset | double | 0 | Distance in px from the anchor along the side |
| AlignOffset | double | 0 | Offset in px along the alignment axis |
| Flip | bool | true | Flip to the opposite side on collision |
| AvoidCollisions | bool | true | Avoid collisions with the viewport boundary |
| CollisionPadding | double? | null | Padding in px between the popup and collision boundary |
| Sticky | string? | null | "partial"/"always"; falls back to base `DefaultSticky` = null for Menu |
| HideWhenDetached | bool | false | Hide popup (`data-anchor-hidden`) when the anchor is fully clipped/detached |
| ArrowPadding | double | 0 | Padding in px between the arrow and popup edges |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes, applied to the positioning div |

### NaviusMenuPopup

(inherits `OverlayAnchoredPopupBase` -> `OverlayPopupBase` -> `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Loop | bool | false | When true, arrow navigation wraps at the ends |
| KeepMounted | bool | false | Keep the popup mounted (hidden) while closed |
| OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | none | Cancelable; PreventDefault keeps focus where it is on open |
| OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | none | Cancelable; PreventDefault skips returning focus to the trigger on close |
| OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on Escape |
| OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on outside pointer-down |
| OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open when focus moves outside |
| OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on any outside interaction |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuArrow

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | double | 10 | SVG width |
| Height | double | 5 | SVG height |
| ChildContent | RenderFragment? | null | Overrides the default triangle `<polygon>` |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires on click or Enter/Space; `PreventDefault()` keeps the menu open, otherwise it closes |
| Disabled | bool | false | Skipped by roving focus (`data-disabled`); does not activate |
| TextValue | string? | null | Overrides the text typeahead matches against |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuCheckboxItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Checked | bool? | null | Controlled tri-state (true / false / null=indeterminate); use `@bind-Checked` |
| CheckedChanged | EventCallback\<bool?\> | none | |
| DefaultChecked | bool? | null | Uncontrolled initial state (null = indeterminate) |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires after toggling; `PreventDefault()` keeps the menu open |
| Disabled | bool | false | |
| TextValue | string? | null | Overrides typeahead match text |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuItemIndicator

| Name | Type | Default | Notes |
|---|---|---|---|
| ForceMount | bool | false | Keep the indicator mounted even when unchecked, for CSS exit animations |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuGroupLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuRadioGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Value | string? | null | Controlled selected value; use `@bind-Value` |
| ValueChanged | EventCallback\<string?\> | none | |
| DefaultValue | string? | null | Uncontrolled initial selected value |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuRadioItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Value | string | (required, `default!`) | This item's value; checked when it equals the group's value |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Fires after selecting; `PreventDefault()` keeps the menu open |
| Disabled | bool | false | |
| TextValue | string? | null | Overrides typeahead match text |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuSub

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | false | Controlled submenu open state |
| OpenChanged | EventCallback\<bool\> | none | Presence of a delegate makes the submenu controlled |
| DefaultOpen | bool | false | Uncontrolled initial state |
| ChildContent | RenderFragment? | null | |

### NaviusMenuSubTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Disabled | bool | false | |
| TextValue | string? | null | Overrides typeahead match text |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMenuSubContent

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |
| SideOffset | double | 0 | Distance in px from the trigger |
| AlignOffset | double | 0 | Offset in px along the alignment axis |
| Loop | bool | false | When true, arrow navigation wraps at the ends |
| ForceMount | bool | false | Keep the submenu mounted while closed (for exit animations) |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusMenu | OpenChanged | EventCallback\<bool\> | Controlled root open state is requested to change (trigger toggle, item select-close, dismiss, Escape) |
| NaviusMenuSub | OpenChanged | EventCallback\<bool\> | Controlled submenu open state is requested to change |
| NaviusMenuItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | Item clicked or Enter/Space pressed; cancelable (`PreventDefault()` keeps menu open) |
| NaviusMenuCheckboxItem | CheckedChanged | EventCallback\<bool?\> | Item activated (click/Enter/Space); toggles true<->false (indeterminate -> true) |
| NaviusMenuCheckboxItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | After the checked state toggles; cancelable |
| NaviusMenuRadioGroup | ValueChanged | EventCallback\<string?\> | A RadioItem in the group is selected |
| NaviusMenuRadioItem | OnSelect | EventCallback\<NaviusSelectEventArgs\> | After the item asks the group to select its value; cancelable |
| NaviusMenuPopup | OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | Before focus moves into the popup on open; cancelable |
| NaviusMenuPopup | OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | Before focus returns to the trigger on close; cancelable |
| NaviusMenuPopup | OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | Escape pressed while popup open; cancelable (via JS dismissable-layer callback) |
| NaviusMenuPopup | OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | Pointer-down outside the popup; cancelable |
| NaviusMenuPopup | OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | Focus moves outside the popup; cancelable |
| NaviusMenuPopup | OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | Any outside interaction; cancelable |

## State + data attributes

| Attribute / class | Where | Meaning |
|---|---|---|
| `data-navius-menu-trigger` | Trigger | Marker |
| `data-popup-open` | Trigger, SubTrigger | Present when the (sub)menu is open |
| `data-disabled` | Trigger, Item, CheckboxItem, RadioItem, SubTrigger | Present when `Disabled` |
| `data-navius-menu-positioner` | Positioner div | Marker; engine writes `--anchor-*`/`--available-*`/`--transform-origin` vars and `data-side`/`data-align`/`data-anchor-hidden` here |
| `data-navius-menu-popup` | Popup | Marker |
| `data-open` / `data-closed` | Popup, SubContent | Discrete open/closed state (C#-owned) |
| `data-starting-style` / `data-ending-style` | Popup | Present for one frame while entering / while the exit transition runs |
| `data-navius-menu-arrow` | Arrow | Marker |
| `data-navius-menu-item` | Item, CheckboxItem, RadioItem, SubTrigger | Marker (roving-focus target role selector) |
| `data-navius-menu-checkbox-item` | CheckboxItem | Marker |
| `data-navius-menu-radio-item` | RadioItem | Marker |
| `data-navius-menu-sub-trigger` | SubTrigger | Marker |
| `data-navius-text-value` | Item, CheckboxItem, RadioItem, SubTrigger | Typeahead match-text override |
| `data-checked` / `data-unchecked` / `data-indeterminate` | CheckboxItem, RadioItem (unchecked/checked only), ItemIndicator | Tri-state (checkbox) / bi-state (radio) reflection |
| `data-navius-menu-item-indicator` | ItemIndicator | Marker |
| `data-navius-menu-group` | Group | Marker |
| `data-navius-menu-group-label` | GroupLabel | Marker |
| `data-navius-menu-radio-group` | RadioGroup | Marker |
| `data-navius-menu-separator` | Separator | Marker |
| `data-navius-menu-sub-content` | SubContent | Marker; also used by `MenuConstants.RovingSelector` to exclude nested-submenu items from the root's roving set |
| `data-highlighted` | Roving-focus target items | Toggled by the JS roving-focus engine (`DataHighlight: true`), not written directly by C# |

Internal (non-DOM) state: `MenuContext.Open`, `MenuSubContext.Open` (independent per submenu), `MenuItemStateContext.Checked` (bool? tri-state, cascaded from Checkbox/RadioItem to its Indicator), `MenuRadioContext.Value`, `MenuGroupContext.LabelId`.

## Keyboard

| Key | Behavior |
|---|---|
| ArrowDown / ArrowUp / Enter / Space (on Trigger, menu closed) | Opens the root menu (`NaviusMenuTrigger.OnKeyDownAsync`) |
| (menu open) first item | Focused automatically on open, confirmed by e2e (`RovingFocusOptions.AutoFocus`) |
| ArrowDown / ArrowUp (inside open menu) | Moves roving focus between enabled items, skipping `data-disabled` items; confirmed by e2e test (`navius.spec.ts` "Menu (roving tabindex)"). Implemented by the JS `RovingFocus` engine (`MenuConstants.RovingSelector`, orientation "vertical"), not directly visible in this folder's C# |
| Home | Moves focus to the first enabled item; confirmed by e2e test |
| Enter / Space (on Item / CheckboxItem / RadioItem) | Activates the item (`ActivateAsync`) |
| ArrowRight (ArrowLeft in rtl) / Enter / Space (on SubTrigger) | Opens the submenu if not already open (`NaviusMenuSubTrigger.OnKeyDownAsync`) |
| ArrowLeft (ArrowRight in rtl) (inside SubContent) | Closes only that submenu, returns focus to its SubTrigger (`NaviusMenuSubContent.OnKeyDownAsync`); the root menu stays open |
| Escape (root Popup) | Closes the root menu via the JS dismissable layer calling back into `OverlayPopupBase.OnDismiss("escape")`; cancelable via `OnEscapeKeyDown` |
| Escape (SubContent) | Closes only the submenu via its own dismissable layer calling `NaviusMenuSubContent.OnDismiss` |

Note: `Loop` (wrap-around), End, PageUp/PageDown, and typeahead-by-character are configured into the JS `RovingFocusOptions` (`Loop`, `Selector`, `Dir`) but their key-handling logic lives in JS interop, outside this component folder, and is not directly readable from the C# source.

## Accessibility

- `role="menu"` on Popup and SubContent; `role="menuitem"` on Item and SubTrigger; `role="menuitemcheckbox"` on CheckboxItem; `role="menuitemradio"` on RadioItem; `role="group"` on Group and RadioGroup; `role="separator"` + `aria-orientation="horizontal"` on Separator.
- `aria-haspopup="menu"`, `aria-expanded`, `aria-controls` (= popup/sub-content id) on Trigger and SubTrigger.
- `aria-checked` tri-state on CheckboxItem (`"true"`/`"false"`/`"mixed"`); bi-state on RadioItem (`"true"`/`"false"`).
- `aria-disabled="true"` on Item/CheckboxItem/RadioItem/SubTrigger when `Disabled` (Trigger instead uses the native `disabled` attribute).
- `aria-labelledby` on Group, pointed at the `GroupLabel`'s generated id (`MenuGroupContext`).
- `tabindex="-1"` on all roving-focus targets and on the Popup/SubContent root; focus is managed entirely by roving tabindex, not native Tab order.
- `dir="rtl"` reflected on Popup/SubContent when `MenuContext.IsRtl`.
- Focus management: `OverlayPopupBase.EngageAsync` moves focus into the popup on open (`Element.FocusAsync()`) unless `OnOpenAutoFocus` is prevented; `RovingFocus` (JS) then focuses the first item. `OverlayPopupBase.DisengageAsync` returns focus to the trigger on close (`TriggerElement.FocusAsync()`) unless `OnCloseAutoFocus` is prevented or focus already moved elsewhere. `NaviusMenuSubContent.RestoreFocusAsync` explicitly refocuses its own `SubTrigger` when the submenu closes. Menu uses roving focus, not a focus trap (`TrapFocus => false` on `NaviusMenuPopup`).

## WPF strategy

Tier A (derive from native `System.Windows.Controls.ContextMenu` / `MenuItem`).

WPF's `Menu`/`MenuItem`/`ContextMenu` system already implements roving/typeahead keyboard navigation, submenu opening, and `MenuItemAutomationPeer` (which maps to UIA `MenuItem`/`Menu` control types, matching `role="menu"`/`role="menuitem"`). Model `NaviusMenu` as a `ToggleButton` (or plain `Button`) trigger paired with a `ContextMenu` (or borderless `Popup` containing a `Menu`) set to open programmatically, and map `NaviusMenuItem`/`CheckboxItem`/`RadioItem` to `MenuItem` with `IsCheckable`/`IsChecked`/`GroupName`. Several things will not translate cleanly: the JS-interop-driven `Positioner`/`RovingFocus`/`DismissableLayer` engine (WPF's native popup placement, `Placement`/`PlacementTarget`/`HorizontalOffset`, and built-in menu roving replace all of it); the CSS-var-based transform-origin/collision positioning and `data-starting-style`/`data-ending-style` enter/exit choreography (would become WPF `Storyboard`/`VisualStateManager` transitions instead); the `Portal`/`Container` custom-mount-point concept (WPF popups render in their own window layer, no DOM teleport equivalent); and the tri-state indeterminate `CheckboxItem` (native `MenuItem` has no indeterminate visual state, so it needs a custom `ThreeState` template).

## Open questions

- The exact key set handled by the JS `RovingFocus` engine (End, PageUp/PageDown, typeahead-by-character, wrap timing) is not visible in this folder's C# and is only partially confirmed by e2e tests (ArrowDown/ArrowUp/Home). Full parity requires inspecting the JS interop source or the underlying Base UI spec.
- `CheckboxItem`'s indeterminate state (`Checked == null`) can only be reached programmatically (`DefaultChecked`/`Checked` set to null); `ActivateAsync` always resolves indeterminate -> checked on user interaction, and there is no UI gesture that produces indeterminate. Confirm whether the WPF port needs a UI path to indeterminate at all.
- Escape-closes-root-menu is wired through a JS `DismissableLayer` callback (`OnDismiss("escape")`) rather than a `@onkeydown` handler in `NaviusMenuPopup.razor`; the precise trigger conditions (bubble phase, focus-within checks) live in JS and aren't visible here.
- The default `Align="center"` for the root Positioner (vs. WPF's conventional left/start-aligned dropdown menus) is a product decision: keep spec-parity centering or switch to a WPF-idiomatic default.
- `TextValue` (typeahead override) is threaded through Item/CheckboxItem/RadioItem/SubTrigger as `data-navius-text-value`, but the typeahead matching algorithm (debounce window, case sensitivity, reset timing) is implemented in the JS roving-focus engine and not specified in this folder.

## WPF implementation notes

Implemented in `src/Navius.Wpf.Primitives/Controls/Menu/` (namespace `Navius.Wpf.Primitives.Controls.Menus` - see "namespace" note below) and `src/Navius.Wpf.Primitives/Themes/Menu.xaml`. Tier A throughout: every part derives from a native WPF menu control (`ToggleButton`, `ContextMenu`, `MenuItem`), so roving focus, typeahead, mnemonics, `MenuItemAutomationPeer`, and submenu opening/closing (hover-intent, arrow keys, Escape) are inherited for free and are not reimplemented.

**Part collapse.** The contract's 17 parts map onto 8 new classes:

| Contract part(s) | WPF class | Notes |
|---|---|---|
| NaviusMenu | *(none)* | No root class: NaviusMenuTrigger owns the association with a popup directly via its `Menu` property instead of a cascaded context object. |
| NaviusMenuTrigger | `NaviusMenuTrigger : ToggleButton` | `Menu` DP holds the associated `NaviusMenuPopup`; toggling opens/closes it with itself as `PlacementTarget`. `IsChecked` doubles as `data-popup-open`. |
| NaviusMenuPortal | *(none, no-op)* | WPF popups already float in their own window layer; nothing to teleport. |
| NaviusMenuPositioner | folded into `NaviusMenuPopup` | Side/Align/SideOffset/AlignOffset/Loop are properties directly on the popup instead of a separate positioning element. |
| NaviusMenuPopup, NaviusMenuArrow | `NaviusMenuPopup : ContextMenu` | Arrow is not implemented (deferred; native context menus rarely carry one). |
| NaviusMenuItem | `NaviusMenuItem : NaviusMenuItemBase : MenuItem` | |
| NaviusMenuCheckboxItem | `NaviusMenuCheckboxItem : NaviusMenuItemBase` | |
| NaviusMenuItemIndicator | *(none)* | Check/dash/dot glyphs are drawn inline (Path/Rectangle/Ellipse) in each item's own `ControlTemplate` rather than a separate public indicator control, unlike Checkbox/RadioGroup's dedicated Indicator classes. |
| NaviusMenuGroup | *(none)* | A transparent `role="group"` wrapper around a subset of items would break native `MenuItem` arrow-key roving (it only navigates `MenuItem`-typed containers); grouping is visual-only (GroupLabel + ordering), not a distinct accessible group. |
| NaviusMenuGroupLabel | `NaviusMenuGroupLabel : ContentControl` | Non-focusable; safely skipped by native roving nav like `Separator` is. |
| NaviusMenuRadioGroup | *(none)* | Collapses into `NaviusMenuRadioItem.GroupName` (see below), for the same "would break roving nav" reason as NaviusMenuGroup. |
| NaviusMenuRadioItem | `NaviusMenuRadioItem : NaviusMenuItemBase` | |
| NaviusMenuSeparator | *(reuses `Navius.Wpf.Primitives.Controls.NaviusSeparator`)* | Already ships `role="separator"`-equivalent (`AutomationControlType.Separator`) and is safely skipped by roving nav since it isn't a `MenuItem`. |
| NaviusMenuSub, NaviusMenuSubTrigger, NaviusMenuSubContent | *(none - "submenu via native MenuItem nesting")* | A `NaviusMenuItem` with child `NaviusMenuItem`s automatically becomes a submenu header; WPF's `MenuItem.Role == SubmenuHeader` handles hover-intent open, arrow-key open/close, positioning, and its own `PART_Popup` natively. |

**Namespace note.** The folder is `Controls/Menu/` per spec, but the C# namespace is `Navius.Wpf.Primitives.Controls.Menus` (plural), not `...Controls.Menu`. A concurrent agent's `Controls/Menubar/NaviusMenubar.cs` derives from bare `Menu` (intending `System.Windows.Controls.Menu`); a sibling namespace literally named `Menu` under `Controls` shadows that bare reference project-wide (C# namespace-member lookup for an unqualified name checks sibling namespace members before `using`-imported types), breaking their build. Renaming only the namespace (folder path unchanged) avoids the collision without touching their file.

**Side/Align -> PlacementMode mapping.** WPF's `PlacementMode` enum only expresses 4 single-edge placements (`Top`/`Bottom`/`Left`/`Right`) with no independent alignment axis, so it cannot represent the contract's 4-side x 3-align = 12 combinations. `NaviusMenuPopup` always sets `Placement = PlacementMode.Custom` and supplies a `CustomPopupPlacementCallback` that computes the exact offset per `(Side, Align)` pair by reusing `Positioning.PlacementMath.Place` (the same pure math `NaviusAnchoredPopup`/Popover-family Tier B controls use) with `Flip`/`Shift` disabled - native WPF popup placement replaces the JS collision/flip engine per this doc's own WPF strategy note, so placement stays a single fixed choice rather than collision-aware. Reference table (closest native single-edge equivalent, for readers used to plain `PlacementMode`):

| Side | Align=Start | Align=Center | Align=End |
|---|---|---|---|
| Bottom | ~`PlacementMode.Bottom` | `PlacementMode.Bottom`, centered | `PlacementMode.Bottom`, right-aligned |
| Top | ~`PlacementMode.Top` | `PlacementMode.Top`, centered | `PlacementMode.Top`, right-aligned |
| Left | ~`PlacementMode.Left` | `PlacementMode.Left`, centered | `PlacementMode.Left`, bottom-aligned |
| Right | ~`PlacementMode.Right` | `PlacementMode.Right`, centered | `PlacementMode.Right`, bottom-aligned |

Default is Side=Bottom/Align=Center, matching the contract's root Positioner defaults exactly (the doc's own open question about switching to a WPF-idiomatic left-aligned default was resolved in favor of spec parity).

**OnSelect / close-on-activate.** Native `MenuItem.OnClick()` bakes "raise Click, run Command, close the open menu chain" into one non-overridable block with no seam to make the close conditional on a cancelable event. `NaviusMenuItemBase` and its three subclasses therefore bypass `base.OnClick()` entirely for leaf activation (submenu headers still delegate to `base.OnClick()` so native hover/arrow-key submenu opening is untouched) and replicate it explicitly: raise the cancelable `Select` routed event (`NaviusSelectEventArgs`, mirrors the contract's `OnSelect`/`PreventDefault()`), run the command (RoutedCommand targeted at `CommandTarget ?? this`, matching native `ICommandSource` execution), then walk up via `ItemsControl.ItemsControlFromItemContainer` to the owning root `ContextMenu` and set `IsOpen = false` only if `PreventDefault()` was not called. One side effect: native `MenuItem.Click` no longer fires from these controls; `Select` replaces it.

**CheckboxItem tri-state.** Native `MenuItem.IsChecked` is a plain `bool` (`MenuItem` has no `IsThreeState`), so `Checked` (`bool?`) is a new DP layered on top, with a separate `IsIndeterminate` flag driving the dash-glyph visual while native `IsChecked` stays `false`. A click always resolves indeterminate/false to `true`, `true` to `false` - matching `NaviusCheckbox`'s own click-resolution rule. Per this doc's own open question #2, indeterminate has no UI gesture in the source either, so this gap is low-impact. UIA note: `MenuItemAutomationPeer` already implements `IToggleProvider` when `IsCheckable` is true (`ControlType.MenuItem`, not a separate `CheckBox` type - actually the correct native mapping for real Windows apps); its `ToggleState` has no Indeterminate case since native `IsChecked` is a plain bool, so `Checked == null` reports as `Off` at the automation layer. No custom peer was added for this.

**RadioItem group semantics.** `NaviusMenuRadioItem.GroupName` (a new DP, not WPF's attached `RadioButton.GroupName`) plus a click-time walk of `ItemsControl.ItemsControlFromItemContainer(this).Items` unchecking same-`GroupName` siblings, scoped to the immediate owning menu/submenu - exactly the scope `NaviusMenuRadioGroup` would otherwise wrap. Re-clicking the already-checked item is a no-op toggle (still raises `Select`/closes), matching "Space selects, there is no deselect."

**TextValue -> TextSearch.Text.** Resolves this doc's own open question in favor of the native mechanism: `NaviusMenuItemBase.TextValue` writes straight onto WPF's `TextSearch.Text` attached property, which native first-letter typeahead already reads.

**Loop.** Kept as a DP on `NaviusMenuPopup` for parameter-surface parity, but native `MenuItem`/`ContextMenu` arrow-key navigation has no public switch to disable wrap-around, so it currently has no behavioral effect.

**Not implemented / deferred:** `NaviusMenuArrow` (no visual arrow glyph); the cancelable `OnOpenAutoFocus`/`OnCloseAutoFocus`/`OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside` hooks (native `ContextMenu`/`Popup` already provide dismiss-on-outside-click and Escape-to-close; intercepting them would require deep custom `Popup` work disproportionate to the value, and none of these are part of the architecture this port was scoped against); `Modal` (scroll-lock has no obvious native equivalent worth forcing).

**Tests:** `tests/Navius.Wpf.Tests/MenuTests.cs` (23 facts, shared with ContextMenuTests's 6 = 29 total, all `[StaFact]`). Covers template application, trigger open/close/resync-on-external-close, Select raise + PreventDefault + close/keep-open, submenu-header click deferring to base (does not close), Command execution, `TextValue` -> `TextSearch.Text`, checkbox toggle/indeterminate/CheckedChanged, radio single-selection (same group, cross-group independence, re-click no-op), `IsEnabled` cascade, and the `MenuItemAutomationPeer` `IToggleProvider`/`ToggleState` mapping.
