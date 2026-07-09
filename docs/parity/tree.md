# Tree

The full WAI-ARIA APG TreeView contract. Generic over the node value type (`NaviusTree<TValue>`); a non-generic `TreeContext` (values boxed to `object`) is cascaded so the part components stay non-generic. Two composition modes: markup (author `NaviusTreeItem`/`NaviusTreeGroup`/... as `ChildContent`) or data-driven (pass `Items` + `ItemTemplate`, walked automatically via the internal `NaviusTreeItems` helper).

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTree\<TValue\> | `<div role="tree" data-navius-tree>` | Root: owns selection + expansion sets (controlled/uncontrolled), cascades `TreeContext`, owns the single keyboard handler for the whole tree |
| NaviusTreeItem | `<div role="treeitem" data-navius-tree-item>` | One node; roving-tabindex focusable, registers itself with the context (value/parent/level/disabled/label), re-cascades its value/level for nested items |
| NaviusTreeGroup | `<div role="group" data-navius-tree-group>` (conditionally rendered) | Child container; its presence marks the parent node expandable (grants `aria-expanded`); lazy-mountable (`KeepMounted`) |
| NaviusTreeItemContent | `<div data-navius-tree-item-content>` | Presentational label-row wrapper inside an item; carries `data-*` styling hooks only, no role/focus/keyboard |
| NaviusTreeItemTrigger | `<span data-navius-tree-item-trigger>` | Expand/collapse + label click target; rendered as a `<span>` (not a nested `<button>`) so it carries no second tab stop inside the focusable `role="treeitem"` |
| NaviusTreeItemIndicator | `<span aria-hidden="true" data-navius-tree-item-indicator>` | Purely visual expand/collapse chevron; the item's own `aria-expanded` is the accessible source of truth |
| NaviusTreeItems\<TValue\> (internal) | No own DOM; recurses `NaviusTreeItem`/`NaviusTreeGroup` | Data-driven mode's internal recursion helper, not hand-placed by consumers |

## Parameters

### NaviusTree\<TValue\>

| Name | Type | Default | Notes |
|---|---|---|---|
| SelectionMode | `string` | "single" | `"none"` \| `"single"` \| `"multiple"` |
| FocusMode | `string` | "roving" | `"roving"` (per-item tabindex) \| `"activedescendant"` (container-owned focus, for virtualized trees) |
| SelectedValue | `TValue?` | default | Controlled single-select value (`@bind-SelectedValue`) |
| SelectedValueChanged | `EventCallback<TValue?>` | | |
| DefaultSelectedValue | `TValue?` | default | Uncontrolled initial single-select value |
| SelectedValues | `IReadOnlyList<TValue>?` | null | Controlled multi-select values (`@bind-SelectedValues`) |
| SelectedValuesChanged | `EventCallback<IReadOnlyList<TValue>>` | | |
| DefaultSelectedValues | `IReadOnlyList<TValue>?` | null | Uncontrolled initial multi-select values |
| ExpandedValues | `IReadOnlyCollection<TValue>?` | null | Controlled expanded set (`@bind-ExpandedValues`) |
| ExpandedValuesChanged | `EventCallback<IReadOnlyCollection<TValue>>` | | |
| DefaultExpandedValues | `IReadOnlyCollection<TValue>?` | null | Uncontrolled initial expanded set |
| Disabled | `bool` | false | Disables the whole tree, cascades to every node |
| Orientation | `string` | "vertical" | `"vertical"` \| `"horizontal"`; drives `data-orientation` only, `aria-orientation` stays `"vertical"` always (the APG tree keyboard model is vertical regardless) |
| Dir | `string?` | null | `"ltr"`/`"rtl"`, falls back to cascaded `NaviusDirection`; flips horizontal expand/collapse arrow keys |
| Label | `string?` | null | `aria-label` |
| LabelledBy | `string?` | null | `aria-labelledby` |
| Items | `IReadOnlyList<TreeNode<TValue>>?` | null | Data-driven mode's hierarchical node set; when set, `ChildContent` is ignored |
| ItemTemplate | `RenderFragment<TreeNode<TValue>>?` | null | Data-driven mode's per-node label-row template |
| OnSelectionChange | `EventCallback<IReadOnlyList<TValue>>` | | Fires with the full selection on every change (controlled or not) |
| OnExpandedChange | `EventCallback<IReadOnlyList<TValue>>` | | Fires with the full expanded set on every change (controlled or not) |
| ChildContent | `RenderFragment?` | null | Markup composition mode |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTreeItem

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `object` | required | Node identity (used for selection/expansion state) |
| Disabled | `bool` | false | Cannot be selected, skipped by keyboard navigation |
| TextValue | `string?` | null | Overrides typeahead-match text (defaults to the node's rendered value) |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusTreeGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | `bool` | false | Keep subtree mounted (hidden) while collapsed instead of removing it from the DOM |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | |

### NaviusTreeItemContent / NaviusTreeItemTrigger / NaviusTreeItemIndicator

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | (all three) |
| Attributes | `IDictionary<string,object>?` | null | (all three) |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTree | SelectedValueChanged / SelectedValuesChanged | Fired on selection change (single/multi respectively) |
| NaviusTree | ExpandedValuesChanged | Fired on expansion set change |
| NaviusTree | OnSelectionChange | `EventCallback<IReadOnlyList<TValue>>`, always fires alongside the above regardless of controlled-ness |
| NaviusTree | OnExpandedChange | `EventCallback<IReadOnlyList<TValue>>`, always fires alongside expansion changes |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="tree"`, `id` (`BaseId`), `aria-label`, `aria-labelledby`, `aria-multiselectable` (present when `SelectionMode="multiple"`), `aria-orientation="vertical"` (always), `aria-activedescendant` (activedescendant mode only), `tabindex="0"` (activedescendant mode only), `dir` | |
| Root | `data-orientation`, `data-disabled`, `data-navius-tree` | |
| Item | `role="treeitem"`, `id` (`Tree.ItemId(Value)`), `aria-level`, `aria-setsize`, `aria-posinset`, `aria-expanded` (parents only, omitted entirely on leaves), `aria-selected` (only when `SelectionEnabled`), `aria-disabled`, `tabindex` (`-1` in activedescendant mode; `0` for the roving tab-stop node, `-1` otherwise, in roving mode) | |
| Item | `data-expanded`, `data-selected`, `data-disabled`, `data-focused` (active node), `data-level`, `data-leaf` (present on non-expandable nodes), `data-navius-tree-item` | |
| Group | `role="group"`, `id` (`Tree.GroupId(Value)`), `hidden` (native, present when collapsed), `data-expanded`, `data-level`, `data-navius-tree-group` | Rendered only when `Item.IsExpanded || KeepMounted` |
| Content | `data-navius-tree-item-content`, `data-level`, `data-expanded`, `data-selected`, `data-disabled` | Presentational only |
| Trigger | `data-navius-tree-item-trigger`, `data-expanded`, `data-selected`, `data-disabled` | |
| Indicator | `aria-hidden="true"`, `data-navius-tree-item-indicator`, `data-state="expanded"|"collapsed"`, `data-expanded`, `data-leaf` (present on non-expandable nodes) | |
| TreeContext (C# state) | `SelectionMode`, `FocusMode`, `RootDisabled`, `Orientation`, `Dir`, `BaseId`, selected/expanded `HashSet<object>`, registered items (value/parent/level/disabled/expandable/label), `_active` (roving/activedescendant target), `_anchor` (multi-select range anchor), typeahead buffer+timestamp | Shared cascaded state; `Changed` event drives part re-render |
| TreeItemContext (C# state) | `Value`, `Level`, `Expandable` (set by the Group on mount/unmount), `IsExpanded`, `IsSelected`, `IsDisabled`, `IsActive` | Per-node, cascaded from `NaviusTreeItem` to its Content/Trigger/Indicator/Group parts |

## Keyboard

Single handler on the root's `role="tree"` element (`OnKeyDownAsync` -> `TreeContext.HandleKeyDownAsync`), catching the bubbled keydown from whichever item has focus. Works identically in both roving and activedescendant focus modes.

| Key | Behavior |
|---|---|
| ArrowDown | Move to next enabled visible node (no wrap, per APG) |
| ArrowUp | Move to previous enabled visible node (no wrap) |
| Shift+ArrowDown / Shift+ArrowUp (multi-select) | Move and extend selection to the new node (toggles it into the selection) |
| Home | Focus the first enabled visible node |
| End | Focus the last enabled visible node |
| Ctrl+Shift+Home (multi-select) | Select range from current to the start |
| Ctrl+Shift+End (multi-select) | Select range from current to the end |
| Enter | Activate the focused node: toggle expansion (if expandable), then apply selection (single: replace; multiple: toggle) |
| Space | Select the focused node (single: replace; multiple: toggle) |
| Shift+Space (multi-select) | Select the contiguous span from the last anchor to the current node |
| `*` (asterisk) | Expand all sibling nodes at the current node's level |
| ArrowRight (ltr) / ArrowLeft (rtl) ("expand key") | On a collapsed expandable node: expand it. On an expanded expandable node: move focus into its first child. On a leaf: no-op |
| ArrowLeft (ltr) / ArrowRight (rtl) ("collapse key") | On an expanded expandable node: collapse it. Otherwise: move focus to the parent node |
| Ctrl+A / Ctrl+a (multi-select) | Toggle select-all/deselect-all across all enabled visible nodes |
| Any single printable character (no Ctrl/Alt/Meta) | Typeahead: matches against node labels, cycles through repeats of the same character, resets buffer after 500ms of inactivity |

Page-scroll is suppressed (`@onkeydown:preventDefault`) for the navigation keys the tree owns: ArrowUp/Down/Left/Right, Home, End, Space. Tab and typing keep their default browser behavior.

Visible order is computed as a pre-order DFS of the mounted subtree, descending only into expanded nodes (`TreeContext.VisibleOrder()`); collapsed nodes are never navigated into regardless of their DOM mount state (`KeepMounted` affects rendering, not navigation).

## Accessibility

- Root: `role="tree"`, `aria-multiselectable` (multi mode), `aria-orientation="vertical"` (always, regardless of the `Orientation` styling parameter), `aria-activedescendant` + `tabindex="0"` in activedescendant mode.
- Item: `role="treeitem"`, `aria-level`/`aria-setsize`/`aria-posinset` computed from registration order among siblings, `aria-expanded` present only on nodes with a mounted `NaviusTreeGroup` child (leaves never carry it, matching APG), `aria-selected` present only when selection is enabled.
- Roving tabindex (roving mode): exactly one node has `tabindex="0"`, resolved as the active node if valid, else the first selected node, else the first enabled node. Activedescendant mode instead keeps the container focused (`tabindex="0"` on the tree) and updates `aria-activedescendant` to point at the logically active node's id, with individual items carrying `tabindex="-1"`.
- Focus sync: a node that receives DOM focus by any means (Tab-in, click landing on it, programmatic) calls back into `SetActiveFromFocusAsync` so subsequent keyboard actions act from the visibly focused node, not a stale one.
- Disabled nodes are skipped entirely by keyboard navigation, excluded from typeahead matching and select-all, and never become the active/tabbable node.

## WPF strategy

Tier A: derive from `System.Windows.Controls.TreeView`/`TreeViewItem`, which already ships `TreeViewAutomationPeer`/`TreeViewItemAutomationPeer` mapping to UIA `SelectionPattern`/`ExpandCollapsePattern` (a reasonable analogue to `role="tree"`/`role="treeitem"`/`aria-expanded`/`aria-selected`), native roving-tabindex-equivalent focus handling, and virtualization support relevant to the `FocusMode="activedescendant"` case. Gaps to bridge: native `TreeView` selection is single-select only (no built-in `SelectionMode="multiple"` with Ctrl/Shift range semantics), so multi-select with anchor-based range extension (`Shift+Arrow`, `Ctrl+Shift+Home/End`, `Shift+Space` contiguous span, `Ctrl+A`) needs a custom selection-management layer on top, similar in spirit to this codebase's own `TreeContext` (which is itself framework-agnostic pure C# and ports almost unchanged: `VisibleOrder`, `MoveAsync`, `SelectSpanAsync`, `TypeaheadAsync`, etc. all operate on plain object graphs with no Blazor dependency). The `*` (expand-siblings) shortcut and 500ms-reset typeahead buffer are not native `TreeView` behaviors and need custom `PreviewKeyDown` handling regardless of base class. `NaviusTreeGroup`'s lazy-mount/`KeepMounted` distinction maps to WPF's native `TreeViewItem` virtualization/`IsExpanded` binding, though "kept mounted but hidden" vs. "removed from the visual tree" is a `VirtualizingPanel`/`Visibility` decision to make explicitly.

## Open questions

- Native WPF `TreeView` has no built-in multi-select; the port must choose between reimplementing selection entirely custom (matching `TreeContext`'s design closely) or adopting a known multi-select-`TreeView` community pattern; recommend porting `TreeContext`'s C# logic directly since it's already framework-agnostic and behaviorally specified.
- `FocusMode="activedescendant"` (container-owned focus, for virtualized trees) has no obvious `TreeViewItem` equivalent since WPF's `TreeView` always gives individual items real keyboard focus; confirm whether virtualized WPF trees actually need this mode or whether `VirtualizingStackPanel` recycling makes it moot.
- Data-driven mode (`Items` + `ItemTemplate`, walked by the internal `NaviusTreeItems`) maps naturally to WPF's `HierarchicalDataTemplate`; decide whether the WPF port exposes an equivalent declarative data-template path or requires manual `TreeViewItem` composition only.
- `TreeNode<TValue>`, the plain data model, has no framework dependency and should port unchanged as the WPF port's hierarchical data source type.

## WPF implementation notes

Implemented under `src/Navius.Wpf.Primitives/Controls/Tree/`: `NaviusTreeNode` (the ported, non-generic `TreeNode<TValue>`, boxed `Value: object` like `TreeContext`, with `IsExpanded`/`IsSelected` living on the node and raising `INotifyPropertyChanged` so state survives container recycling), `NaviusTreeSelectionMode` (`None`/`Single`/`Multiple`), `TreeSelectionState` (the pure, framework-free port of `TreeContext`'s `VisibleOrder`/`MoveAsync`/`SelectSpanAsync`/`ToggleSelectAllAsync`/`TypeaheadAsync`, operating directly on `NaviusTreeNode` graphs, unit-testable without an STA thread or Application), `NaviusTreeItem` (derives `TreeViewItem`), `NaviusTree` (derives `TreeView`), `NaviusTreeAutomationPeer`, `TreeContainerLocator` (best-effort container realization for virtualized keyboard navigation), `TreeVisualHelpers`, and `BoolInverseConverter`. Theme: `Themes/Tree.xaml` (implicit `HierarchicalDataTemplate` keyed to `NaviusTreeNode`, implicit `Style` for `NaviusTreeItem`). Gallery: `apps/Navius.Wpf.Gallery/Pages/TreePage.xaml(.cs)` with a small multi-select fixture plus a 10,000-node demo (`AutomationId="Tree10kDemo"`, 100 groups x 99 children + 100 parents). Tests: `tests/Navius.Wpf.Tests/TreeTests.cs`, 65 tests.

**Selection is fully custom, for both Single and Multiple modes.** The recommended approach ("port `TreeContext`'s selection logic directly") was taken further than originally scoped: rather than using native `TreeView.SelectedItem`/`TreeViewItem.IsSelected` for Single mode and only overlaying a custom system for Multiple mode, BOTH modes route through the same `TreeSelectionState` + `NaviusTreeNode.IsSelected` mechanism. Reason: native WPF `TreeView` enforces single-selection internally at the `TreeView.ChangeSelection` level (setting `IsSelected=true` on a second item silently unselects the first via a routed-event listener on the `TreeView`), which cannot represent a multi-select set, and having two parallel selection systems (native for Single, custom for Multiple) was judged more complex and error-prone than one uniform system. **Delta / accessibility gap**: because native `TreeViewItem.IsSelected` is never set, UIA's `SelectionItemPattern.IsSelected` does not reflect this port's actual selection state for either mode. `NaviusTreeAutomationPeer` re-implements `ISelectionProvider.CanSelectMultiple`/`IsSelectionRequired` to reflect `SelectionMode` correctly, and `GetSelection()` is best-effort (only returns providers for currently realized containers, consistent with keeping virtualization on), but individual items' `SelectionItemPattern.IsSelected` is not wired. This is flagged as an open UIA gap, not attempted in this pass, in the spirit of "record deltas" rather than silently under-delivering.

**Mouse click semantics extend the contract.** The web's `TreeContext.ActivateAsync` (used uniformly for both keyboard Enter and mouse click, per the actual ground-truth source, not just the doc prose) does not distinguish Ctrl/Shift-click at all: a plain click in Multiple mode always *toggles* the clicked node, with no native-feeling Ctrl-to-toggle / Shift-to-range-select mouse conventions. Per the "A11y tiebreak: WAI-ARIA APG + native WPF over suspicious contract lines" instruction, this was judged a case where the web's mouse handling under-specifies desktop conventions (rather than a deliberate design decision), so the WPF port ADDS Ctrl+Click (toggle) and Shift+Click (range-select from the last anchor) on top of the literal ported default (plain click still toggles/replaces and toggles expansion exactly per `ActivateAsync`). This is a recorded, deliberate addition beyond the contract, not a silent deviation.

**`FocusMode="activedescendant"` was dropped.** Per the doc's own open question, virtualization is handled natively by `VirtualizingStackPanel` recycling (explicitly pinned on via `VirtualizingPanel.IsVirtualizing`/`VirtualizationMode=Recycling` in `Themes/Tree.xaml`, at every nesting level so the 10k-node gate stays virtualized at any depth, not just the top level), so a second "container-owned focus" mode was not implemented; every node gets real WPF keyboard focus (roving mode only).

**Roving tabindex is approximate, not strictly enforced.** APG's "exactly one Tab stop" model is approximated by WPF's native Tab-in behavior (focuses the last-focused item, or the first item on first entry) rather than hand-managing `KeyboardNavigation.IsTabStop` across container recycling; this was judged not worth the complexity given virtualization already makes strict per-container `IsTabStop` bookkeeping fragile. Recorded as a known simplification.

**Keyboard navigation is fully custom** (`NaviusTree.HandleKey(Key, ModifierKeys)`, public rather than the more natural `internal` so it's directly unit-testable without constructing real `KeyEventArgs`/a live input pipeline, mirroring `NaviusRating.HandleKey()`'s established tradeoff in this codebase), covering Up/Down/Home/End/Enter/Space/Shift-variants/Ctrl+A/Ctrl+Shift+Home+End/ltr-rtl-aware Left-Right/typeahead/`*` (both `Key.Multiply` and `Shift+8`) exactly per the contract's keyboard table, driven entirely off the `NaviusTreeNode` DATA model (not realized containers) so it is correct even for off-screen, unrealized nodes at 10k scale. Programmatic focus for off-screen targets uses `TreeContainerLocator`, which walks the ancestor chain and reflection-invokes `VirtualizingPanel`'s protected `BringIndexIntoView` (the standard WPF workaround for programmatic navigation in a virtualized `ItemsControl`) rather than requiring full realization up front.

**Data-driven mode only.** Manual `NaviusTreeItem`/`NaviusTreeGroup`/etc. markup composition (the web's other mode) was not ported; `NaviusTree.RootNodes` + the implicit `HierarchicalDataTemplate` in `Themes/Tree.xaml` is the only supported authoring path, matching the doc's own suggestion that this is the natural WPF mapping. `Orientation`, `Label`/`LabelledBy`, and the whole-tree `Disabled` parameter were not given wrapper properties: `Orientation` is out of scope (WPF has no horizontal-tree convention and the task brief doesn't call for it), `Label`/`LabelledBy` map directly to native `AutomationProperties.Name`/`LabeledBy`, and `Disabled` maps directly to native `IsEnabled` (which already cascades to every descendant for free).
