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

## M6 audit (2026-07-09)

CONFIRMED CATALOG GAP, reported loudly per audit scope: **no WPF implementation of Toolbar exists at all.**

Verified with a repo-wide search for every "toolbar" file naming variant (`find . -iname "*toolbar*"`, excluding `bin/`/`obj/`/`.git/`): the only match in the entire repository is this doc file itself, `docs/parity/toolbar.md`. Specifically confirmed absent:

- No `src/Navius.Wpf.Primitives/Controls/Toolbar/` directory (every other ported family in this batch has one).
- No `NaviusToolbar.cs`, `NaviusToolbarButton.cs`, `NaviusToolbarLink.cs`, `NaviusToolbarSeparator.cs`, `NaviusToolbarToggleGroup.cs`, or `NaviusToolbarToggleItem.cs` anywhere in the tree.
- No `Themes/Toolbar.xaml` in `src/Navius.Wpf.Ui/Themes/`.
- No `tests/Navius.Wpf.Tests/ToolbarTests.cs`.
- This doc has no "WPF implementation notes" section at all (every implemented family in this catalog has one; its absence here is consistent with, and further confirms, the gap).

This is not a partial/plausible gap, it is total: the family described above (root + button + link + separator + toggle-group + toggle-item, 5 parts) does not exist in Navius.Wpf in any form. Per audit instructions this gap is reported only, not built, in this pass.

Residual/follow-up: when this family is eventually built, the shared-roving-domain design with `ToggleGroup` (open question 2 above) and the `Loop`-wrap `PreviewKeyDown` pattern already implemented for `NaviusToggleGroup` (see `docs/parity/toggle-group.md`, "WPF implementation notes") should be reused rather than re-derived, since `ToggleGroup`'s WPF port already answered the open questions this doc raises about roving focus and deselect-to-empty behavior.

Resolution: built 2026-07-09, closing this gap. See "WPF implementation notes" below.

## WPF implementation notes

Built as `src/Navius.Wpf.Primitives/Controls/Toolbar/` (six files: `NaviusToolbar.cs`,
`NaviusToolbarAutomationPeer.cs`, `NaviusToolbarButton.cs`, `NaviusToolbarLink.cs`,
`NaviusToolbarToggleGroup.cs`, `NaviusToolbarToggleItem.cs`, plus the `IToolbarItem` marker
interface) with `src/Navius.Wpf.Primitives/Themes/Toolbar.xaml`, tests in
`tests/Navius.Wpf.Tests/ToolbarTests.cs` (28 `[StaFact]` tests), and a self-contained gallery page
at `apps/Navius.Wpf.Gallery/Pages/ToolbarPage.xaml(.cs)`.

**Open question 3 (AutomationPeer) resolved**: a custom `NaviusToolbarAutomationPeer :
FrameworkElementAutomationPeer` is used, not the native `ToolBarAutomationPeer`. The native peer
belongs to a control with overflow-menu semantics this component does not have; borrowing it
would misreport capabilities the control doesn't provide. The custom peer reports
`AutomationControlType.ToolBar` and an orientation-aware `GetOrientationCore()`.

**Open question 1 (avoid native `ToolBar`) resolved**: confirmed, `NaviusToolbar` is a plain
lookless `ContentControl` (same shape as `NaviusRadioGroup`/`NaviusToggleGroup`), not a `ToolBar`
subclass, so no `ToolBarTray`/overflow behavior leaks in.

**Open question 2 (shared roving domain with `ToggleGroup`) resolved, with a deviation from the
doc's own suggestion**: the doc invited reusing "whatever base the standalone
`ToggleGroup`/`ToggleGroupItem` WPF port produces" for `NaviusToolbarToggleGroup`. It does NOT
subclass `NaviusToggleGroup`: that class's private `UpdateRovingTabStops` unconditionally owns
`IsTabStop` assignment for its own items (even with `RovingFocus=false`, every enabled item
becomes its own independent Tab stop), which would fight `NaviusToolbar` owning the single shared
roving domain across mixed control types. Instead:

- The single shared Tab stop across heterogeneous item types (button, link, toggle item) is
  implemented via a marker interface, `IToolbarItem`, implemented by `NaviusToolbarButton`,
  `NaviusToolbarLink`, and `NaviusToolbarToggleItem` (not by `NaviusToolbarToggleGroup` itself,
  which is never focusable and never a Tab stop -- `Focusable = false` in its constructor).
  `NaviusToolbar`'s roving scan walks the logical tree for any `Control` implementing
  `IToolbarItem`, via `LogicalTreeWalker.Descendants<Control>(this).OfType<IToolbarItem>()`. That
  walk recurses through a nested `NaviusToolbarToggleGroup` for free, which is exactly what gives
  toggle items inside the group the toolbar's single shared Tab stop and arrow-key domain, with
  zero special-casing for the nesting.
- `NaviusToolbarToggleGroup`'s pressed-set semantics (`Type` "single"/"multiple", the
  clear-on-reclick / replace-on-different-item / toggle-membership `ComputeNext` logic, driven off
  `ToggleButton.Checked`/`Unchecked` routed-event bubbling) are **ported, not inherited**, from
  `NaviusToggleGroup`. This is the "compose the existing controls" instruction interpreted as
  semantic reuse rather than literal subclassing, given the tab-stop-ownership conflict above.
- `NaviusToolbarToggleItem`'s Space/Enter `OnKeyDown` override -- including the `IsRepeat` guard
  that skips a repeated Space key-down but allows repeated Enter -- is copied verbatim from
  `NaviusToggleGroupItem` (`Controls/ToggleGroup/NaviusToggleGroupItem.cs`), per this doc's own
  residual/follow-up instruction to reuse the M6 Space-dead fix rather than re-derive it.

**`NaviusToolbarSeparator`: resolved as reuse, no new type**, following the Menu precedent
(`docs/parity/menu.md`: "reuses `Navius.Wpf.Primitives.Controls.NaviusSeparator`... safely skipped
by roving nav since it isn't a `MenuItem`"). `NaviusSeparator` already ships
`AutomationControlType.Separator` and, since it does not implement `IToolbarItem`, is
automatically excluded from `NaviusToolbar`'s roving scan with no extra wiring -- the same
"safely skipped, not resubclassed" reasoning Menu used. Contract delta: the web contract's
`NaviusToolbarSeparator` auto-computes `aria-orientation` perpendicular to the toolbar's own
orientation; this WPF port does not reproduce that auto-flip (matching the plain, unaware
`NaviusSeparator` reuse), so the consumer sets `Orientation` on the separator explicitly, same as
every other `NaviusSeparator` usage in this codebase (see `ToolbarPage.xaml`, which sets
`Orientation="Vertical"` on separators inside a horizontal toolbar).

**`NaviusToolbarButton`**: a two-line subclass of `NaviusButton` (only `IToolbarItem` and its own
`DefaultStyleKeyProperty` override added), inheriting soft-disabled mode, the `OnClick` funnel, and
`NaviusButtonAutomationPeer` for free -- the contract's parameter table for this part lists nothing
beyond what `NaviusButton` already provides.

**`NaviusToolbarLink`**: derives from the native `Button` (not `NaviusButton`), adding only a
`Uri` dependency property. `Command`/`CommandParameter`/`CommandTarget` come from `Button` for
free, satisfying the "Uri/Command surface" instruction. It deliberately has no `Disabled`
property, matching the contract's "no intrinsic disabled state... always participates in roving."

**`NaviusToolbarToggleItem.Disabled`**: the contract's per-item table lists `Disabled: bool,
Effective disabled is Disabled || GroupContext.Disabled`. Implemented as a `Disabled` dependency
property on the item plus a `Disabled` dependency property on `NaviusToolbarToggleGroup`;
`UpdateEffectiveDisabled()` (called on either property changing) sets native `IsEnabled = !(Disabled
|| ancestor group's Disabled)`, found via `LogicalTreeWalker.Ancestor<NaviusToolbarToggleGroup>`.

**Known simplification**: `NaviusToolbar`'s Tab stop is recomputed fresh on `OnContentChanged` and
on every roving keypress, not via per-item `IsEnabledChanged` subscriptions, so the contract's "an
item's disabled state flips at runtime, peers re-render so the seated Tab stop can move" is only
reproduced at the next roving interaction rather than instantly. `NaviusRadioGroup`/
`NaviusToggleGroup` make the same simplification for their own same-type item disable cascades, so
this is consistent with existing precedent rather than a new gap.

**`DefaultValue` (contract's uncontrolled-initial-value parameter) dropped**: not modeled, matching
`NaviusToggleGroup`'s own WPF port, which already drops this Blazor-specific
controlled/uncontrolled distinction in favor of a single plain `Value` dependency property.

**Verification**: `dotnet build src/Navius.Wpf.Primitives` and `dotnet test
tests/Navius.Wpf.Tests --filter "FullyQualifiedName~Toolbar"` both green (28/28 tests), including
real routed-key-event Space/Enter activation tests on both `NaviusToolbarButton` and
`NaviusToolbarToggleItem` (via the `HwndSource`-hosted pattern `SwitchTests.cs` uses post-M6, not
fabricated `KeyEventArgs` fed directly to `OnKeyDown`), a `Space` `IsRepeat` no-flap test, an
`Enter` auto-repeat-is-allowed test, roving/wrap/clamp/Home/End/RTL-mirroring tests, a test proving
toggle items nested inside `NaviusToolbarToggleGroup` share the parent toolbar's single roving
domain, and `AutomationPeer` tests for both the root's `ToolBar`+orientation and the toggle group's
`Group` control type.
