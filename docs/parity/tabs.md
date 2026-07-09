# Tabs

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTabs | `<div data-navius-tabs>` | Root: owns selected value (controlled/uncontrolled), cascades `TabsContext`, computes `data-activation-direction` |
| NaviusTabsList | `<div role="tablist" data-navius-tabs-list>` | Container for tab triggers; owns arrow-key/Home/End navigation and `Loop` behavior |
| NaviusTabsTab | `<button role="tab" data-navius-tabs-tab>` | One tab trigger; roving tabindex, click/keyboard selection, registers itself with the context |
| NaviusTabsPanel | `<div role="tabpanel" data-navius-tabs-panel>` (conditionally rendered) | Content panel associated with a tab value; rendered only when selected unless `KeepMounted` |

## Parameters

### NaviusTabs

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string?` | null | Controlled selected tab value |
| ValueChanged | `EventCallback<string?>` | | Presence of a delegate (`ValueChanged.HasDelegate`) determines controlled-ness |
| DefaultValue | `string?` | null | Uncontrolled initial value |
| Orientation | `string` | "horizontal" | `"horizontal"` or `"vertical"` |
| ActivationMode | `string` | "automatic" | `"automatic"`: focusing a tab via keyboard activates it. `"manual"`: arrows move focus only, Enter/Space/click activates |
| Dir | `string?` | null | `"ltr"`/`"rtl"`; falls back to cascaded `NaviusDirection`, then `"ltr"` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTabsList

| Name | Type | Default | Notes |
|---|---|---|---|
| Loop | `bool` | true | Arrow navigation wraps at edges when true; stops at first/last when false |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTabsTab

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | "" | Identifies the tab; used to derive deterministic trigger/panel ids |
| Disabled | `bool` | false | |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTabsPanel

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | "" | Matches the associated tab's `Value` |
| KeepMounted | `bool` | false | Keep the panel in the DOM while inactive (`data-hidden` + `hidden` attribute), for external animation |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTabs | ValueChanged | `EventCallback<string?>`, fired on tab selection (click, keyboard activation) |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `data-navius-tabs`, `data-orientation`, `data-activation-direction` | `data-activation-direction` is one of `none`/`left`/`right`/`up`/`down`, computed from the old/new selected index and orientation |
| Root | `dir="rtl"` | Present when effective direction is rtl |
| List | `role="tablist"`, `aria-orientation`, `data-orientation`, `data-activation-direction`, `data-navius-tabs-list` | Mirrors root state |
| Tab | `role="tab"`, `id` (deterministic `{BaseId}-trigger-{slug}`), `aria-selected`, `aria-controls` (points at panel id), `tabindex` (0 for the roving tab stop, -1 otherwise), native `disabled` | |
| Tab | `data-active` (present when selected, no `data-state`), `data-orientation`, `data-activation-direction`, `data-disabled`, `data-navius-tabs-tab` | |
| Panel | `role="tabpanel"`, `id` (`{BaseId}-panel-{slug}`), `aria-labelledby` (points at trigger id), `tabindex="0"` | |
| Panel | `data-hidden` (present when not selected but kept mounted), `data-index`, `data-orientation`, `data-activation-direction`, `hidden` attribute (native, true when not selected), `data-navius-tabs-panel` | No `data-state` on the panel |
| TabsContext (C# state) | `Selected`, `ActivationDirection`, `Focused` (drives roving tab stop in manual mode), `Loop`, `Orientation`, `ActivationMode`, `Dir`, `BaseId`, registered trigger list (value/element/disabled) | Shared cascaded state; `Changed` event drives part re-render. `TabStopValue` = `Focused ?? Selected` in manual mode, else `Selected` |

## Keyboard

Handled on `NaviusTabsList` (`@onkeydown`, arrows/Home/End) and `NaviusTabsTab` (`@onkeydown`, Enter/Space).

| Key | Behavior |
|---|---|
| ArrowRight (horizontal, ltr) / ArrowLeft (horizontal, rtl) | Move to next enabled tab (`Context.MoveAsync(1)`) |
| ArrowLeft (horizontal, ltr) / ArrowRight (horizontal, rtl) | Move to previous enabled tab (`Context.MoveAsync(-1)`) |
| ArrowDown (vertical) | Move to next enabled tab |
| ArrowUp (vertical) | Move to previous enabled tab |
| Home | Move to first enabled tab (`MoveToEdgeAsync(last: false)`) |
| End | Move to last enabled tab (`MoveToEdgeAsync(last: true)`) |
| Enter / Space | On a focused tab, selects it (manual-mode activation; harmless re-select in automatic mode) |

Move behavior: in `"automatic"` `ActivationMode`, moving focus also selects the target tab and moves DOM focus to it. In `"manual"` mode, moving only updates roving focus (`Context.Focused`); actual selection requires Enter/Space/click. `Loop=true` (default) wraps at edges; `Loop=false` clamps (no movement past the first/last enabled tab). Disabled tabs are skipped entirely.

## Accessibility

- List: `role="tablist"`, `aria-orientation`.
- Tab: `role="tab"`, `aria-selected`, `aria-controls` linking to its panel's id, deterministic `id` for `aria-labelledby` wiring from the panel.
- Panel: `role="tabpanel"`, `aria-labelledby` linking to its tab's id, `tabindex="0"`.
- Roving tabindex: only the active tab stop (`TabStopValue`) has `tabindex="0"`; all others are `-1`. `TabStopValue` tracks `Selected` in automatic mode, or `Focused ?? Selected` in manual mode.
- Focus management: `MoveAsync`/`MoveToEdgeAsync` call `ElementReference.FocusAsync()` on the target trigger element after updating context state, so DOM focus follows keyboard navigation regardless of activation mode.
- Disabled tabs are excluded from both roving navigation and have native `disabled` attribute (also removes them from the natural tab order).

## WPF strategy

Tier A: derive from `System.Windows.Controls.TabControl` (with `TabItem` for the tab/`ContentPresenter` for the panel), which already ships `TabControlAutomationPeer`/`TabItemAutomationPeer` mapping to UIA `SelectionPattern`/`role="tab"`/`role="tabpanel"` semantics, native arrow-key navigation between items, and `SelectedItem`/`SelectionChanged` matching `Value`/`ValueChanged`. Differences to bridge: WPF `TabControl` navigation does not natively distinguish automatic vs. manual activation mode (`ActivationMode`) or a `Loop` toggle, so both need custom `KeyDown` handling layered on top (or a full replacement of the default `KeyboardNavigation` behavior). `data-activation-direction` (directional transition data for animation hooks) has no WPF built-in; expose it as an attached/dependency property updated on selection change, consumed by `VisualStateManager` triggers. `KeepMounted` panels map to a custom `ContentPresenter` template that keeps inactive `TabItem` content visually collapsed (`Visibility=Collapsed`) instead of Blazor's conditional render + `hidden` attribute.

## Open questions

- Does the WPF port need to support `ActivationMode="manual"` (focus-without-select), or is `TabControl`'s single automatic-selection model sufficient for v1.
- `Loop` (wrap-at-edges) is not a `TabControl` built-in; confirm whether both loop and no-loop variants are needed or if the port standardizes on one.
- `data-activation-direction` exists purely to drive CSS transition direction; decide whether the WPF port needs an equivalent for `VisualStateManager` transitions or can drop it.

## WPF implementation notes

Implemented in `Controls/Tabs/NaviusTabs.cs` (derives `TabControl`) and `Controls/Tabs/NaviusTabItem.cs` (derives `TabItem`), styled by `Themes/Tabs.xaml`.

- Part collapse: the four contract parts (NaviusTabs / NaviusTabsList / NaviusTabsTab / NaviusTabsPanel) collapse into two WPF types. `NaviusTabsList`'s arrow-key/Home/End navigation moves onto the root (`NaviusTabs`, since WPF's internal `TabPanel` is not a separately addressable public part), and `NaviusTabsTab` + `NaviusTabsPanel` unify into `NaviusTabItem` (`Header` = trigger content, `Content` = panel content), since those already live on one `TabItem` object natively.
- Open question 1 (ActivationMode) resolved: implemented in full. `ActivationMode="automatic"` (default) makes arrow/Home/End navigation both move focus and select, matching native `TabControl`'s own default feel; `"manual"` moves focus only, requiring Enter/Space/click to select. WPF's own directional navigation does not know either mode, so `KeyboardNavigation.DirectionalNavigation` is switched off and both modes are hand-rolled in a `PreviewKeyDown` handler.
- Open question 2 (Loop) resolved: implemented as a `Loop` bool DP (default true), wrap vs. clamp handled by the same custom key handler.
- Open question 3 (`data-activation-direction`) resolved: exposed as a read-only `ActivationDirection` string DP (`"none"`/`"left"`/`"right"`/`"up"`/`"down"`), recomputed in `OnSelectionChanged` from the old/new selected index and `Orientation`, for `Style`/`VisualStateManager` triggers to consume.
- `KeepMounted` on the Panel is dropped. Native `TabControl` only ever instantiates a single shared `ContentPresenter` bound to the selected item's content; there is no per-item persistent panel to "keep mounted while hidden" without a much larger custom-template rewrite. Treated as an intentional Tier A tradeoff, not an oversight.
- `DefaultValue` collapses into `Value`: like the RadioGroup family, this port exposes one `Value` DP used both controlled and uncontrolled (native `TabControl` selecting its first item on load already covers the "uncontrolled default" case without a separate property).
- `data-navius-tabs`/`data-orientation`/`dir` marker attributes are dropped; the equivalent state is already queryable via native/added DPs (`IsSelected`, `IsEnabled`, `Orientation`, `ActivationDirection`) and consumed directly by `Style` triggers instead of synthetic `data-*` attached properties.
- Disabled tabs map straight to `TabItem.IsEnabled`; no separate `Disabled` DP.
