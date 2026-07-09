# ADR-0007: Windows high contrast support

Status: accepted

## Context

High contrast is a native-Windows accessibility obligation the web port never had: browsers own
high-contrast rendering themselves (forced-colors mode overrides author CSS), but a WPF app's
`SolidColorBrush` token values are exactly what a Windows high-contrast theme needs to see
replaced with the OS's own palette. `ThemeManager` (`src/Navius.Wpf.Primitives/Theming/`) already
had a `NaviusTheme` enum (`Light`/`Dark`) and an `Apply(theme, scope)` that swaps the
`Navius.Tokens.Theme`-marked `Themes/Tokens.*.xaml` dictionary in a resource scope, firing
`ThemeChanged`. This ADR adds a third `NaviusTheme.HighContrast` value plus a token dictionary and
an opt-in OS-sync helper.

## Decision: token mapping

`Themes/Tokens.HighContrast.xaml` maps every `Navius.*` key from `Tokens.Light.xaml` to a
`SolidColorBrush` whose `Color` is bound via `DynamicResource` to a `SystemColors.*ColorKey`
(never a literal color), so live OS palette changes (switching between high-contrast themes, or
any system color update) repaint the brush without an app restart or an explicit `Apply` call --
these are WPF's own "system resource keys," resolved independently of any live visual tree, which
is why this works even for a token dictionary that hasn't been attached to a window yet.

| Navius token | SystemColors key | Rationale |
|---|---|---|
| Background / Foreground | Window / WindowText | Page surface + body text |
| Card / Popover (+Foreground) | Window / WindowText | HC collapses "elevated surface" distinctions; borders carry separation instead |
| Primary / PrimaryForeground | Highlight / HighlightText | The OS's own selection/accent pair |
| Secondary / Accent (+Foreground) | Control / ControlText | HC also collapses these two "less prominent than Primary" roles onto one control pair |
| Muted / MutedForeground | Control / GrayText | De-emphasized text uses the dedicated GrayText role |
| Destructive / DestructiveForeground | WindowText / Window | No system "danger/red" key exists in `SystemColors`; falls back to WindowText per the brief's own rule. Destructive affordances must rely on icon/label, not hue, in HC |
| Border / Input | WindowText | Borders must stay visible against Window regardless of which HC theme (black/white/other) is active |
| Ring (focus) | Highlight | Matches the OS's own focus-visual convention in high contrast |

`Navius.Radius.Small/Control/Card` (`CornerRadius`, not colors) are kept identical to
`Tokens.Light.xaml`/`Tokens.Dark.xaml`; there is no high-contrast-specific radius concern.
`tests/Navius.Wpf.Tests/ThemeManagerTests.cs`'s
`TokenDictionary_HighContrast_CoversSameKeysAsLight` diffs the two dictionaries' key sets so this
can't silently drift.

## Decision: system sync design

`ThemeManager.EnableSystemHighContrastSync()` is an opt-in (not automatic on module load) that
subscribes to `SystemParameters.StaticPropertyChanged`, applying `NaviusTheme.HighContrast`
whenever `SystemParameters.HighContrast` is true (checked immediately on enable, and again on
every subsequent change) and restoring whatever theme was active immediately before the switch
once it turns back off. The decision logic is factored into a separate, public
`SyncSystemHighContrastState(bool systemHighContrastEnabled)` that takes the OS flag as a
parameter instead of reading `SystemParameters.HighContrast` itself, so `ThemeManagerTests` can
drive both the enter and restore transitions deterministically without flipping the real OS
setting. It is `public` rather than the more natural `internal` because this assembly has no
`InternalsVisibleTo` -- the same testability tradeoff `NaviusTree.HandleKey()` /
`NaviusRating.HandleKey()` already make elsewhere in this codebase. Both transitions go through
the existing `Apply(NaviusTheme)` path, so `ThemeChanged` fires for them exactly as it does for any
other theme switch; the public API surface otherwise stays as it was (no changes to `Apply`,
`Current`, or `ThemeChanged`'s existing shape).

`apps/Navius.Wpf.Gallery/Pages/HighContrastPage.xaml(.cs)` (`AutomationId="HighContrastDemo"`, not
wired into navigation this wave -- MainWindow is orchestrator-owned) demonstrates the plain
`Apply(NaviusTheme.HighContrast)` / `Apply(NaviusTheme.Light)` path with a couple of tokened
controls, rather than the OS-sync helper (which needs a real high-contrast toggle to observe).

## Residual UIA findings (from the Controls/ peer sweep)

Grepped every `OnCreateAutomationPeer` across `Controls/` (~40 peer classes) looking for controls
whose primary value is invisible over UIA (the M3 Select lesson: a custom control whose real state
never reaches an automation pattern), then read each peer rather than guessing from the class name.
Most of the sweep is already solid: `NaviusMeter` (`IRangeValueProvider` via the inherited
`ProgressBarAutomationPeer` + `ItemStatus`), `NaviusSortableItem` (position/size-of-set pushed as
attached properties), and `NaviusColorPicker` (`IValueProvider` over the hex string, with a
comment noting this exact bug class was "found and fixed" in the 2026-07-09 M6 audit) all already
carry their real state over UIA -- these are not gaps. Two real candidates surfaced instead, both
left unedited per this wave's scope ("fix gaps ONLY in Tree ... and REPORT the rest as findings"):

- **NaviusOneTimePasswordField** (`Controls/OneTimePasswordField/NaviusOneTimePasswordField.cs:451`):
  `NaviusOneTimePasswordFieldAutomationPeer` is a bare `FrameworkElementAutomationPeer` with
  `AutomationControlType.Group` and no `GetNameCore` override or value/pattern of any kind. Per-cell
  `AutomationProperties.SetName` ("Character N of Length", line 264) is set as you tab through, but
  the container itself exposes neither the assembled `Value` nor even a non-revealing completion
  flag (`OneTimePasswordBuffer.IsComplete`). Caveat: this may be an intentional omission rather than
  an oversight, the same way `PasswordBoxAutomationPeer` deliberately implements no `IValueProvider`
  to avoid leaking secret text over UIA -- worth the owning family confirming intent one way or the
  other rather than assuming it is a bug.
- **NaviusComboboxBase** (`Controls/Combobox/NaviusComboboxBase.cs:700`):
  `NaviusComboboxAutomationPeer` implements only `IExpandCollapseProvider` (open/closed); there is
  no `GetNameCore` override and no `IValueProvider`, unlike the near-identical sibling
  `NaviusAutocompleteAutomationPeer` (`Controls/Autocomplete/NaviusAutocompleteAutomationPeer.cs:27`),
  which explicitly forwards `Autocomplete.Value` into `GetNameCore` when no explicit name is set.
  The combobox's typed/selected text IS technically reachable by a UIA client that descends into
  its `PART_Input` `TextBox` child (a native `TextBoxAutomationPeer` carries `IValueProvider`
  itself), so this is milder than Select's original gap, not a total blackout -- but it is an
  inconsistency against Autocomplete's own precedent and worth the owning family's judgment call on
  whether the container should mirror Autocomplete's pattern.

### Tree: SelectionItemPattern gap (fixed)

`docs/parity/tree.md`'s "WPF implementation notes" flagged that this control's selection is fully
custom-tracked on `NaviusTreeNode.IsSelected` (both Single and Multiple modes), so native
`TreeViewItem.IsSelected` -- and therefore the native `TreeViewItemAutomationPeer`'s
`ISelectionItemProvider.IsSelected` -- was always `false`, making custom multi-select invisible to
`SelectionItemPattern`. Fixed by:

- `NaviusTreeItem.OnCreateAutomationPeer()` now returns `NaviusTreeItemAutomationPeer`
  (`TreeViewItemAutomationPeer` subclass) instead of relying on the native default. Re-declaring
  `ISelectionItemProvider` on the subclass gives it its own interface-map entry, so UIA clients see
  the subclass's implementation instead of the base's.
- `ISelectionItemProvider.IsSelected` reads the bound `NaviusTreeNode.IsSelected` directly.
  `AddToSelection`/`RemoveFromSelection`/`Select` route through new `NaviusTree.AddNodeToSelection`/
  `RemoveNodeFromSelection`/`SelectNodeExclusive` methods (public for the same
  InternalsVisibleTo-free testability tradeoff as above), which apply UIA's own semantics (`Select`
  always replaces the whole selection even in multi-select; `AddToSelection` on a single-select
  tree with a different existing selection throws `InvalidOperationException`, per the UIA
  contract).
- `NaviusTreeItem` now subscribes to its bound node's `PropertyChanged` (via `DataContextChanged`,
  since containers are recycled under virtualization) and calls the realized peer's
  `RaiseSelectionEvents(bool)`, which raises both the `SelectionItemPatternIdentifiers.IsSelectedProperty`
  property-change event and `AutomationEvents.SelectionItemPatternOnElementSelected` /
  `...OnElementRemovedFromSelection`, so a UIA client observes live selection updates, not just a
  point-in-time snapshot.
- `docs/parity/tree.md`'s own "Delta / accessibility gap" paragraph is updated to record this as
  fixed rather than open.

Tests: `tests/Navius.Wpf.Tests/TreeTests.cs` --
`ItemAutomationPeer_IsCustomPeer_StillDerivesTreeViewItemPeer`,
`ItemAutomationPeer_IsSelected_ReflectsNodeSelection_NotNativeIsSelected`,
`ItemAutomationPeer_WithoutTreeAncestor_RoutedMembersNoOpInsteadOfThrowing`,
`ItemAutomationPeer_RaiseSelectionEvents_DoesNotThrowWithoutListener`,
`SelectNodeExclusive_ReplacesSelectionWithJustThatNode_EvenInMultipleMode`,
`SelectNodeExclusive_DisabledNode_NoOp`, `AddNodeToSelection_Multiple_AddsWithoutClearingExisting`,
`AddNodeToSelection_Single_NoExistingSelection_Selects`,
`AddNodeToSelection_Single_DifferentExistingSelection_Throws`,
`RemoveNodeFromSelection_RemovesJustThatNode`,
`RemoveNodeFromSelection_NotSelected_NoOpDoesNotFireEvent`.
