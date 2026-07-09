# TagInput

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTagInput | `<div data-navius-tag-input>` | Root: owns the tag collection, chip-navigation highlight, one-shot focus requests; cascades `TagInputContext`; applies commit rules (transform, validate, duplicate, max) |
| NaviusTagInputList | `<div data-navius-tag-input-list>` | Layout-only chip container |
| NaviusTagInputField | `<input data-navius-tag-input-field>` | The text input; commits chips on delimiter keys/chars, drives Backspace/ArrowLeft chip-navigation entry |
| NaviusTag | `<span data-navius-tag>` | One chip; roving tab stop while highlighted, owns arrow/Home/End/Delete/Backspace navigation, cascades `TagValueContext` |
| NaviusTagRemove | `<button data-navius-tag-remove>` | Remove button inside a chip; reads cascaded `TagValueContext` for which tag to remove |

## Parameters

### NaviusTagInput

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `IList<string>?` | null | Controlled tag list (`@bind-Value`) |
| ValueChanged | `EventCallback<IList<string>>` | | |
| DefaultValue | `IList<string>?` | null | Uncontrolled initial tags |
| Delimiters | `IReadOnlyList<TagDelimiter>?` | null | Which keys/chars commit a chip; defaults to `[Enter, Comma]` when unset |
| AllowDuplicates | `bool` | false | |
| MaxTags | `int?` | null | |
| Validate | `Func<string, bool>?` | null | Return false blocks the candidate and raises `OnInvalid` |
| Transform | `Func<string, string>?` | null | Normalizes a candidate after trim (e.g. lowercase) |
| AddOnBlur | `bool` | false | Commit field text on blur |
| Disabled | `bool` | false | |
| OnAdd | `EventCallback<string>` | | |
| OnRemove | `EventCallback<string>` | | |
| OnInvalid | `EventCallback<string>` | | |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTagInputField

| Name | Type | Default | Notes |
|---|---|---|---|
| Placeholder | `string?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTagInputList

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTag

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | "" | The chip's tag string |
| Index | `int` | -1 | Chip position; resolved from `Value` via `Context.IndexOf` when unset (-1) |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusTagRemove

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTagInput | OnAdd | `EventCallback<string>`, fired after a chip is successfully committed |
| NaviusTagInput | OnRemove | `EventCallback<string>`, fired after a chip is removed (via `NaviusTagRemove`, chip Delete/Backspace, or field Backspace path) |
| NaviusTagInput | OnInvalid | `EventCallback<string>`, fired when a candidate is blocked by duplicate check, `MaxTags`, or `Validate` |
| NaviusTagInput | ValueChanged | `EventCallback<IList<string>>`, fired on every committed mutation (add/remove) |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `data-navius-tag-input` | Marker |
| Root | `data-disabled` | Present when `Disabled` |
| Root | `data-empty` | Present when the tag list is empty |
| Field | `data-navius-tag-input-field`, native `disabled` (from `Context.Disabled`) | |
| List | `data-navius-tag-input-list` | Layout only |
| Tag | `data-navius-tag`, `data-highlighted` (present when chip-navigation-highlighted), `tabindex` (0 when highlighted, -1 otherwise) | |
| TagRemove | `data-navius-tag-remove`, `tabindex="-1"` (always out of tab order), `aria-label` (`"Remove {value}"` by default, overridable via `Attributes`) | |
| TagInputContext (C# state) | `Tags`, `HighlightedIndex` (-1 = field holds focus), `Disabled`, `AddOnBlur`, `Delimiters`, `Empty`, one-shot `PendingChipFocus`/`PendingFieldFocus` | Shared cascaded state; `Changed` event drives part re-render |
| TagValueContext (C# state) | `Value` | Cascaded from `NaviusTag` to its nested `NaviusTagRemove` |

## Keyboard

### NaviusTagInputField

| Key | Behavior |
|---|---|
| Enter (if `Enter` in `Delimiters`, default yes) | Commits current field text as a chip (only when text non-empty) |
| Tab (if `Tab` in `Delimiters`) | Same as Enter |
| Comma / Space typed or pasted (if in `Delimiters`) | Detected in `@oninput`; text is split at the delimiter, each completed segment committed as a chip, remainder stays in the field |
| Backspace (field empty) | First press highlights the last chip (`HighlightedIndex = last`, no focus move); if a chip is already highlighted, removes it |
| ArrowLeft (field empty) | Enters chip navigation: highlights and focuses the last chip |

### NaviusTag (chip)

| Key | Behavior |
|---|---|
| ArrowLeft | Highlight+focus the previous chip (no-op at index 0) |
| ArrowRight | Highlight+focus the next chip, or -1 (back to field) if already at the last chip |
| Home | Highlight+focus the first chip |
| End | Highlight+focus the last chip |
| Delete / Backspace | Remove this chip (`Context.RemoveAtAsync`) |

After a chip removal, focus/highlight moves to the next adjacent chip, or back to the field if the list becomes empty.

## Accessibility

- No explicit ARIA roles are wired in the code (no `role="listbox"`/`role="option"` etc. shown); the field is a bare `<input>`.
- `NaviusTagRemove` gets a default `aria-label="Remove {value}"`, overridable by a consumer-supplied `aria-label` via `Attributes` (splatted after, so it wins).
- Roving tabindex on chips: only the highlighted chip has `tabindex="0"`; others are `tabindex="-1"`. The remove button is always `tabindex="-1"` (excluded from tab order, reachable only by click or the chip's own keyboard remove).
- Focus management: one-shot `PendingChipFocus`/`PendingFieldFocus` flags are consumed in `OnAfterRenderAsync` on the relevant part, calling `ElementReference.FocusAsync()` so focus follows the highlight or falls back to the field after a chip is removed or navigation crosses back into the field.

## WPF strategy

Tier B: custom lookless control. No native WPF control models "type-to-create-chips" input; this needs a custom `Control` (or `ItemsControl` wrapping a `TextBox` + chip `ItemsControl`) combining a `TextBox`-derived text-entry part with a chip collection, similar in spirit to a `ListBox` with an embedded editable slot. `AutomationPeer` mapping is ambiguous since the source itself wires no ARIA roles (would need a fresh accessibility design, e.g. `EditAutomationPeer` for the field plus custom peers exposing chip removal as an invoke pattern). The roving-tabindex-on-chip model maps to WPF's `KeyboardNavigation.TabIndex`/manual focus management (`Keyboard.Focus`) mirroring the one-shot `PendingChipFocus`/`PendingFieldFocus` pattern verbatim as C# state. Commit-on-delimiter (Enter/Tab/comma/space) logic, transform/validate/duplicate/max rules, and the Backspace-then-remove two-step all port as pure C# onto `PreviewKeyDown`/`TextChanged` handlers with no framework-specific rewrite needed.

## Open questions

- No ARIA/role wiring exists in the source to translate to `AutomationPeer`s; the WPF port needs a fresh accessibility design for this component rather than a 1:1 mapping.
- Should `NaviusTagRemove`'s default `aria-label` pattern become a WPF `AutomationProperties.Name` binding, and is English-only text acceptable long-term.
- Paste-splitting on comma/space (`FindCharDelimiter`) only inspects `@oninput`'s resulting text; confirm the WPF `TextBox` equivalent (`TextChanged` after paste) produces the same split points.

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/TagInput/TagInputEngine.cs` (pure commit/split/
remove/highlight math + `TagDelimiter` + `TagCommitStatus`), `TagChipVm.cs`, `NaviusTagInput.cs`
(Tier B lookless control + UIA peer), `Themes/TagInput.xaml`,
`tests/Navius.Wpf.Tests/TagInputTests.cs`, `apps/Navius.Wpf.Gallery/Pages/TagInputPage.xaml(.cs)`.

**Parts folding**: the web's 5 parts collapse into one Control with two template parts
(`PART_Input` TextBox, `PART_Chips` ItemsControl of `TagChipVm`), the NumberField folding
minimalism. `NaviusTagInputList` is layout-only in the contract (a WrapPanel here);
`NaviusTag`/`NaviusTagRemove` became a chip VM + DataTemplate (the Combobox chip idiom); the chip
visual language echoes `Themes/Combobox.xaml`'s DefaultChip but is owned by this family's own
dictionary.

**Commit pipeline**: ported verbatim into pure `TagInputEngine.TryCommit` with the web's exact
rule order (trim -> transform -> empty-silent -> duplicate -> max -> validate), including the
duplicate-before-max precedence, which tests pin. Char-delimiter splitting
(`FindCharDelimiter`/`Split`, comma before space, remainder stays in the field) ports the web's
`@oninput` detection onto `TextChanged`, so a paste containing delimiters splits at the same
points (third open question resolved: `TextChanged` fires once with the full pasted text, and
`Split` walks all completed segments in one pass).

**Keyboard**: field Enter/Tab (per `Delimiters`) commit; Backspace on an empty field is the
contract's two-step (first press highlights the last chip, second removes it); ArrowLeft on an
empty field enters chip navigation. Chip keys (ArrowLeft/Right/Home/End, Delete/Backspace) live on
the chips host and use `HighlightedIndex`. Unlike the Combobox's virtual highlight, the chip
highlight IS a real focus target: the roving-tabindex model maps to focusing the highlighted
chip's container, and the one-shot `PendingChipFocus`/`PendingFieldFocus` dance collapsed into
synchronous `Focus()` calls (no re-render round-trip exists in WPF).

**Accessibility (first + second open questions resolved)**: fresh minimal design per the APG,
since the source wires no ARIA. The field is a native TextBox (Edit peer for free). The remove
button's default `aria-label="Remove {value}"` became an `AutomationProperties.Name` binding to
the VM's `RemoveName` (English-only for now, same as the web). The root's peer additionally
exposes the committed tags over a read-only ValuePattern (comma-joined), following the
NaviusSelect peer / M3-gate precedent that value living in template text must be readable over
UIA.

**Mapping notes**: `Value` is a two-way `IReadOnlyList<string>` DP (immutable snapshots, the
Combobox `Values` convention) with CLR events `TagAdded`/`TagRemoved`/`TagRejected`/`ValueChanged`
for OnAdd/OnRemove/OnInvalid/ValueChanged. `DefaultValue` seeds once on `Loaded` when `Value` is
unset. `Disabled` maps to native `IsEnabled`. `data-empty` maps to the read-only `IsTagListEmpty`
DP (placeholder + chips-visibility triggers); `data-highlighted` to the VM's `IsHighlighted`.
The public state machine (`CommitText`/`RemoveTagAt`/`RemoveHighlighted`/`Highlight`) is
template-independent, so the whole contract is unit-testable headless.
