# OneTimePasswordField

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusOneTimePasswordField (Root) | `<div role="group" data-navius-otp>` | Owns the authoritative per-character buffer; cascades `OneTimePasswordFieldContext`; auto-renders `Length` cells (or accepts custom `ChildContent`) plus an implicit hidden input when `Name` is set and no custom content is used |
| NaviusOneTimePasswordFieldInput | `<input data-navius-otp-input>` | A single-character cell; one per slot (`Index`) |
| NaviusOneTimePasswordFieldHiddenInput | `<input type="hidden" data-navius-otp-hidden>` | Carries the concatenated aggregate value for real form submission |

## Parameters

**NaviusOneTimePasswordField (Root)**

| Name | Type | Default | Notes |
|---|---|---|---|
| Length | `int` | 6 | Number of single-character cells; also the max value length |
| Value | `string?` | null | Controlled aggregate value; use with `ValueChanged` |
| DefaultValue | `string?` | null | Uncontrolled initial aggregate value |
| Disabled | `bool` | false | Disables every cell |
| ReadOnly | `bool` | false | Marks every cell read-only |
| InputMode | `string` | "numeric" | `inputmode` applied to each cell |
| ValidationType | `string` | "numeric" | `"numeric"`, `"alpha"`, or `"alphanumeric"`; input outside the class is rejected |
| SanitizeValue | `Func<string, string>?` | null | Optional transform applied to each character/pasted value after `ValidationType` filtering |
| Type | `string` | "text" | `"text"` or `"password"` (masks glyphs) |
| Orientation | `string` | "vertical" | `"vertical"` or `"horizontal"`; determines which arrow keys navigate |
| Placeholder | `string?` | null | Applied to empty cells |
| AutoFocus | `bool` | false | Focuses the first cell on mount |
| Name | `string?` | null | Form field name for the hidden input carrying the aggregate value |
| Form | `string?` | null | Id of a form to associate the hidden input with, when outside that form |
| AutoSubmit | `bool` | false | Submits the owning form once every cell is filled |
| ChildContent | `RenderFragment?` | null | Custom cell markup; disables auto-rendering of cells and the implicit hidden input |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusOneTimePasswordFieldInput**

| Name | Type | Default | Notes |
|---|---|---|---|
| Index | `int` | 0 | Zero-based slot this cell occupies |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusOneTimePasswordFieldHiddenInput**

| Name | Type | Default | Notes |
|---|---|---|---|
| Name | `string?` | null | Form field name submitted with the owning form |
| Form | `string?` | null | Id of an associated form element when outside that form |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

## Events

| Part | Event | Type |
|---|---|---|
| NaviusOneTimePasswordField (Root) | ValueChanged | `EventCallback<string>` |
| NaviusOneTimePasswordField (Root) | OnComplete | `EventCallback<string>`, raised when every cell is occupied |
| NaviusOneTimePasswordField (Root) | OnAutoSubmit | `EventCallback<string>`, raised alongside `AutoSubmit` firing (after `OnComplete`) |

Cell and hidden-input parts have no public `EventCallback` parameters; they call back into `OneTimePasswordFieldContext` methods (`SetCharAsync`, `PasteAsync`, `KeyAsync`, `SubmitAsync`, `FocusAsync`) wired by the root.

## State + data attributes

Root div: `role="group"`, `data-navius-otp`, `data-orientation`, `data-disabled`.

Cell input: `type` (`Context.Type`), `inputmode`, `autocomplete="one-time-code"`, `maxlength` (`Context.Length` when `Index == 0`, otherwise `1`), `value` = `Context.CharAt(Index)`, `placeholder`, `aria-label="Character N of M"`, `disabled`, `readonly`, `data-navius-otp-input`, `data-index`, `data-filled` (present when the cell holds a character), `data-orientation`, `data-disabled`, `data-readonly`.

Hidden input: `type="hidden"`, `name`, `value` = `Context.Value` (aggregate), `form`, `data-navius-otp-hidden`.

Public state on `OneTimePasswordFieldContext`: `Length`, `Disabled`, `ReadOnly`, `InputMode`, `Type`, `Orientation`, `Placeholder`, `Value` (dense aggregate string, no interior gaps), `CharAt(index)`.

Internally the root keeps a positional `char?[]` buffer; interior gaps are represented as spaces in the aggregate string round-trip so slot indices survive `Value`/`SetValueInternalAsync`.

## Keyboard

Handled in `NaviusOneTimePasswordFieldInput.OnKeyDownAsync`, dispatched to the root via `KeyKind`.

| Key | Behavior |
|---|---|
| Any character | Writes the cell (last char of raw input wins) and advances focus to the next cell; a keystroke that fails `ValidationType`/`SanitizeValue` is rejected and reverted, leaving the buffer untouched |
| Backspace | Non-empty focused cell: clears it, shifts remainder back, focus retreats one cell. Empty focused cell: clears the previous cell instead, focus retreats one cell |
| Cmd/Ctrl+Backspace | Clears the whole field and focuses the first cell |
| Delete | Clears the focused character and shifts the remaining characters back one slot |
| ArrowUp / ArrowDown | Previous/next cell when `Orientation="vertical"` (default) |
| ArrowLeft / ArrowRight | Previous/next cell when `Orientation="horizontal"` |
| Home | Focus first cell |
| End | Focus last cell |
| Enter | Requests submission of the closest enclosing form (via JS interop) |
| Paste (multi-char input event) | Replaces the entire field with the sanitized pasted text starting at slot 0 (regardless of which cell was focused), then focuses the last filled cell (or `Length - 1`) |

## Accessibility

Root carries `role="group"` to label the cluster of single-character inputs as one control. Each cell has a dynamically generated `aria-label="Character {Index + 1} of {Length}"`. Cells set `autocomplete="one-time-code"` so mobile keyboards/SMS autofill can target the group. Focus is managed entirely programmatically via `ElementReference.FocusAsync()` calls from the root (advance on type, retreat on backspace, arrow navigation, Home/End, paste landing): there is no roving-tabindex pattern; natural DOM tab order plus explicit focus calls handle movement.

## WPF strategy

Tier B (custom lookless control). No native WPF OTP control exists; model as a lookless `Control`/`ItemsControl` hosting `Length` `TextBox` cells inside a template, or a custom `Control` that owns an internal collection of textbox-like parts bound to dependency properties mirroring `Length`/`Value`/`Disabled`/`ReadOnly`/`Type`/`Orientation`. AutomationPeer: root as `AutomationControlType.Group` (`GroupPattern`), cells as `AutomationControlType.Edit` with `AutomationProperties.Name` bound to the same "Character N of M" text. All focus advancement (type/backspace/delete/arrows/Home/End/paste-landing) must be re-implemented manually via `Keyboard.Focus()`/`UIElement.Focus()` in `PreviewKeyDown`, since WPF has no equivalent to HTML `autocomplete="one-time-code"` or native SMS/mobile autofill: that affordance will not translate. The `AutoSubmit`/`RequestSubmitAsync` behavior (JS interop submitting the closest HTML `<form>`) has no WPF form equivalent and needs a different mapping (see open questions).

## Open questions

- Cell 0's `maxlength` is set to `Context.Length` rather than `1` (every other cell is `1`). This looks like it exists to let a raw browser paste/autofill event land fully in the first cell so the component's `oninput` length-based routing (`raw.Length > 1` → paste) can detect it. Confirm whether this is intentional before deciding how WPF (which has no native paste-into-maxlength-1-field constraint) should replicate multi-char paste detection.
- No IME/composition-event handling is visible in the keydown/input handlers; is CJK/IME composition input in scope for the WPF port?
- `ValidationType` classification uses `char.IsLetter`/`char.IsDigit` (culture-aware in .NET); confirm the WPF port should use the same semantics rather than ASCII-only matching.
- `AutoSubmit` submits the closest HTML form via JS interop; WPF has no forms, so this needs a product decision (e.g. raise `OnAutoSubmit` only, or invoke a bound `ICommand`).
- `Densify` uses `' '` (space) as the internal sentinel for an interior gap when round-tripping the aggregate string through `Value`; if `ValidationType`/`SanitizeValue` in a future config ever admitted spaces as valid characters this encoding would be ambiguous. Worth flagging for the WPF value-buffer design even though today's validation classes (numeric/alpha/alphanumeric) never admit spaces.

## WPF port notes (implemented 2026-07-09)

Shipped as `Controls/OneTimePasswordField/`: `NaviusOneTimePasswordField` (lookless Control whose template exposes a `PART_Cells` panel; the control builds `Length` cells in code, following the RadioGroup/CheckboxGroup owns-its-parts precedent) plus `NaviusOneTimePasswordFieldInput` (thin themable TextBox cell) and a pure, WPF-free `OneTimePasswordBuffer` implementing the whole keyboard table (SetChar advance, Backspace shift-back/retreat, Delete shift-back, Ctrl+Backspace clear-all, paste-from-slot-0), unit-tested without an STA thread.

Contract deltas, recorded per the open questions above:

- Cell 0's `maxlength=Length` quirk does not port: WPF surfaces paste through the separate `DataObject.Pasting` event, fully decoupled from typed `PreviewTextInput`, so every cell uses a uniform `MaxLength=1` and multi-char paste detection needs no length heuristic.
- `NaviusOneTimePasswordFieldHiddenInput` and the `Name`/`Form` parameters drop entirely per `docs/adr/0001-web-form-participation-params.md` (no HTML form submission in WPF).
- `AutoSubmit`: Enter always raises a bubbling `SubmitRequested` routed event (the WPF analog of "submit the closest enclosing form"; any ancestor can subscribe), and with `AutoSubmit=true` a separate `AutoSubmitted` event fires once every cell fills, immediately after `Complete`. Nothing is invoked directly.
- `autocomplete="one-time-code"` / SMS autofill has no WPF analog and is dropped, as predicted by the extraction.
- `ValidationType` keeps the culture-aware `char.IsDigit`/`char.IsLetter`/`char.IsLetterOrDigit` semantics of the source (open question resolved: same semantics, not ASCII-only).
- IME/composition input is not specially handled, matching the web source, which also has none.
- Accessibility: root automation peer reports `Group`; each cell carries `AutomationProperties.Name = "Character N of M"`. `Type="password"` masks the cell glyph with a bullet while the logical character stays in the buffer.
- The space-sentinel `Densify` encoding is preserved exactly (interior gaps round-trip as spaces through `Value`), keeping slot indices stable.

## M6 audit (2026-07-09)

Adversarial re-verification against the actual C#/XAML. The buffer mechanics and key routing held
up (this control correctly uses `PreviewKeyDown`, unlike NumberField); one undocumented parameter
drop and two minor theme gaps found.

CONFIRMED correct (no fix needed):
- Keyboard routing: cell handlers are wired on `PreviewKeyDown`/`PreviewTextInput`
  (`RebuildCells`, lines 266-268), the tunneling phase that fires before the hosted `TextBox`
  consumes navigation keys. Every keyboard-table row traces to `OnCellPreviewKeyDown`
  (lines 285-354): Ctrl+Back before plain Back, Delete, Up/Down gated on vertical, Left/Right gated
  on horizontal, Home->0, End->Length-1, Enter->`SubmitRequested`.
- Paste distribution: `OneTimePasswordBuffer.Paste` fills from slot 0 regardless of the focused
  cell and lands focus on `Math.Max(count-1, 0)` (last filled), no off-by-one; pinned by
  `Paste_ReplacesFromSlotZero_*` and `Paste_TruncatesToLength_*`.
- Aggregate value: `Value` is the concatenated `ToValue(buffer)` string (space-padded for interior
  gaps), not per-slot only; `ApplyBufferChange` syncs it via `SetCurrentValue` and edge-triggers
  `Complete`/`AutoSubmitted` (`!wasComplete && isComplete`), matching the contract.
- Theme: all `DynamicResource`; keys `Navius.Background/Foreground/Input/Ring` and
  `Navius.Radius.Control` exist in both token dictionaries.

CONFIRMED disparity (doc fixed here):
- The parameter table lists `DefaultValue` (uncontrolled initial aggregate value), but no
  `DefaultValue` DP exists on `NaviusOneTimePasswordField` (grep-confirmed). Dropped silently and
  undocumented. Recorded now: `DefaultValue` is NOT ported for OTP; a consumer sets `Value`.

PLAUSIBLE (unfixed, low severity, cosmetic parity gaps in `Themes/OneTimePasswordField.xaml`):
- `PART_Cells` is a hardcoded `Orientation="Horizontal"` StackPanel with no trigger keyed off the
  control's `Orientation` property, so `Orientation="vertical"` (the default) still renders cells in
  a horizontal row while Up/Down are the navigation keys. This is parity-faithful to the web
  contract's `data-orientation` (which drives keys, not necessarily the visual axis) but is a UX
  oddity worth a product decision.
- The `data-filled` trigger (line 33) keys on `Text=""` and sets the border to `Navius.Input`, the
  same brush as the default, so there is no visible filled-vs-empty distinction; the `data-filled`
  affordance is effectively absent visually.
