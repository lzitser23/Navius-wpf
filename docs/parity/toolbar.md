# Toolbar

WAI-ARIA APG "Toolbar" pattern: a flat container of focusable controls sharing a single roving Tab stop.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusToolbar | `<div role="toolbar" data-navius-toolbar>` | Root: cascades `ToolbarContext`, engages the JS roving-focus engine over all registered items |
| NaviusToolbarButton | `<button data-navius-toolbar-item>` | A focusable toolbar control (button); registers with `ToolbarContext` |
| NaviusToolbarLink | `<a data-navius-toolbar-item>` | A focusable toolbar control (anchor); registers with `ToolbarContext`; no intrinsic disabled state |
| NaviusToolbarSeparator | `<div role="separator" data-navius-toolbar-separator>` | Non-focusable visual divider between groups; not a Tab stop, excluded from roving selector |
| NaviusToolbarToggleGroup | `<div role="group" data-navius-toolbar-togglegroup>` | A set of toggle buttons inside the toolbar (Base UI's `Toolbar.ToggleGroup`); owns pressed-set selection but shares the toolbar's roving focus rather than creating its own |
| NaviusToolbarToggleItem | `<button data-navius-toolbar-item>` | A single toggle button inside a `NaviusToolbarToggleGroup`; registers with both `ToolbarContext` (for roving) and `ToolbarToggleGroupContext` (for pressed state) |

## Parameters

### NaviusToolbar

| Name | Type | Default | Notes |
|---|---|---|---|
| Orientation | `string` | "horizontal" | `"horizontal"` or `"vertical"`; drives the arrow-key axis |
| Loop | `bool` | true | Arrow navigation wraps past first/last when true |
| Dir | `string?` | null | `"ltr"`/`"rtl"`; falls back to cascaded `NaviusDirection` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes (consumer supplies `aria-label`/`aria-labelledby` here) |

### NaviusToolbarButton

| Name | Type | Default | Notes |
|---|---|---|---|
| Disabled | `bool` | false | |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusToolbarLink

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | `href` supplied via `Attributes` |

### NaviusToolbarSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusToolbarToggleGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| Type | `string` | "single" | `"single"` (at most one pressed) or `"multiple"` (any number pressed) |
| Value | `IReadOnlyList<string>?` | null | Controlled pressed set |
| ValueChanged | `EventCallback<IReadOnlyList<string>>` | | Controlled-ness determined by `ValueChanged.HasDelegate` |
| DefaultValue | `IReadOnlyList<string>?` | null | Uncontrolled initial pressed set |
| Disabled | `bool` | false | Disables every item in the group |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusToolbarToggleItem

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | "" | Identifies the item in the group's pressed set |
| Disabled | `bool` | false | Effective disabled is `Disabled || GroupContext.Disabled` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusToolbarToggleGroup | ValueChanged | `EventCallback<IReadOnlyList<string>>`, fired on toggle-item click when controlled |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="toolbar"`, `aria-orientation`, `data-navius-toolbar`, `data-orientation`, `dir` (emitted only when explicitly set via param or cascade, not defaulted to "ltr") | |
| Button/Link | `tabindex` (0 for the seated item, -1 otherwise), `data-orientation`, `data-navius-toolbar-item`; Button additionally: native `disabled`, `data-disabled` | Link has no intrinsic disabled state in HTML and always participates in roving |
| Separator | `role="separator"`, `aria-orientation` (perpendicular to the toolbar's own orientation), `data-orientation`, `data-navius-toolbar-separator` | |
| ToggleGroup | `role="group"`, `data-navius-toolbar-togglegroup`, `data-orientation` (mirrors toolbar), `data-disabled` | |
| ToggleItem | `aria-pressed`, `tabindex` (0 for the seated item, -1 otherwise), native `disabled`, `data-pressed`, `data-disabled`, `data-orientation`, `data-navius-toolbar-item` | Shares the toolbar's `data-navius-toolbar-item` marker so the roving selector picks it up alongside plain buttons/links |
| ToolbarContext (C# state) | `Orientation`, registered items (key + live disabled probe) in DOM order | Shared across the whole toolbar including toggle-group items; `Changed` event; `IsTabStop(key)` = first enabled item in registration order |
| ToolbarToggleGroupContext (C# state) | `Type`, `Disabled`, pressed-value `HashSet<string>` | Separate from `ToolbarContext`; owns only pressed selection, not focus |

## Keyboard

| Key | Behavior |
|---|---|
| Left/Right (horizontal) or Up/Down (vertical) | Move DOM focus between enabled toolbar items (button, link, or toggle item indiscriminately); handled entirely by the JS engine (`CreateRovingFocusAsync`) over `[data-navius-toolbar-item]:not([disabled]):not([data-disabled])`, honoring `Loop` and `Dir` (RTL flip for horizontal) |
| Home / End | Jump focus to first/last enabled item (engine-provided) |
| Space / Enter (on a focused button/toggle item) | Native `<button>` activation; on a toggle item, toggles its pressed state via `GroupContext.RequestToggleAsync` |

Crucially, `NaviusToolbarToggleGroup` does NOT create its own roving-focus controller: its `NaviusToolbarToggleItem` children register with the toolbar's single shared `ToolbarContext` (carrying the same `data-navius-toolbar-item` marker as plain buttons/links), so the whole toolbar including embedded toggle groups is one composite Tab stop and one arrow-navigation domain.

Toggle selection logic (`ComputeNext`, identical between `ToolbarToggleGroup` and standalone `ToggleGroup`): in `"single"` type, clicking the pressed item clears selection to empty; clicking another item replaces the selection. In `"multiple"` type, clicking toggles that value's set membership.

## Accessibility

- Root: `role="toolbar"`, `aria-orientation`. Consumer must supply `aria-label`/`aria-labelledby` via `Attributes` (not defaulted).
- Separator: `role="separator"` with `aria-orientation` perpendicular to the toolbar's own flow direction (a horizontal toolbar renders a vertical separator).
- ToggleGroup: `role="group"`.
- ToggleItem: `aria-pressed`.
- Single shared Tab stop across the entire toolbar (including nested toggle groups): the first enabled item in DOM/registration order seats `tabindex="0"` at rest; the roving engine takes over once focus moves. Disabled items and links (links never disabled) are excluded/included per their own rules from the roving selector.
- When an item's disabled state flips at runtime, it notifies `ToolbarContext.NotifyChangedAsync()` so peers re-render (the seated Tab stop can move).

## WPF strategy

Tier B: custom lookless control composing Tier-A pieces. WPF's `ToolBar` control exists but has different semantics (overflow menu, `ToolBarTray`) not present here; this component is closer to a plain `ItemsControl`/`StackPanel`-based container implementing the APG toolbar pattern manually, similar to `ToolbarGroup` (this batch's `ToggleGroup`) but flat across mixed control types (buttons, hyperlinks, toggle buttons, separators). WPF's native `KeyboardNavigation.TabNavigation="Once"` + `DirectionalNavigation` on a `StackPanel`-hosting `Control` can approximate the single-Tab-stop/roving-arrow-navigation model, but `Loop` wrap-around still needs custom `PreviewKeyDown` handling as noted for `ToggleGroup`. `AutomationPeer`: no direct UIA "toolbar" pattern maps 1:1 to `role="toolbar"`, but `ToolBarAutomationPeer` (from WPF's own `ToolBar`) is the closest built-in and could be reused for the container even if the internals are custom; `Separator`'s `role="separator"` maps to WPF's native `Separator` control/`SeparatorAutomationPeer`. `NaviusToolbarToggleGroup`/`NaviusToolbarToggleItem` should reuse whatever base the standalone `ToggleGroup`/`ToggleGroupItem` WPF port produces, but explicitly wired NOT to create its own focus scope, consistent with the source's design.

## Open questions

- WPF's own `ToolBar` control brings overflow-menu behavior this component does not have; confirm the port intentionally avoids `ToolBar` and builds a plain `ItemsControl`-based container instead, to avoid accidentally inheriting overflow semantics that break parity.
- The shared-roving-domain design (toggle-group items participate in the parent toolbar's Tab stop, not their own) needs the WPF `ToggleGroup`/`Toolbar` ports to share one focus-scope implementation; sequence this after (or together with) the standalone `ToggleGroup` port to avoid duplicating the roving logic.
- No 1:1 UIA pattern exists for `role="toolbar"`; decide between reusing `ToolBarAutomationPeer` (semantically close but from a different-behaved native control) versus a custom `AutomationPeer` subclass.
