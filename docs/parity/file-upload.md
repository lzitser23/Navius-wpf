# FileUpload

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusFileUpload | `div[data-navius-file-upload]` (+ hidden live-region `div`) | Root. Owns the file list (controlled via `Files`/`FilesChanged` or uncontrolled), validates selections against `Accept`/`MaxSize`/`MaxFiles`, wires the JS drag/drop relay, and cascades `FileUploadContext`. |
| NaviusFileUploadInput | `InputFile` (Blazor) → hidden `input[type=file][data-navius-file-upload-input]` | The real, visually-hidden native file input; the a11y + form source of truth. Registers its element on the context so the root can relay drops into it and open the OS dialog. |
| NaviusFileUploadTrigger | `button[data-navius-file-upload-trigger]` | Plain button that opens the OS file dialog by relaying to the hidden input. |
| NaviusFileUploadDropzone | `div[role=button][data-navius-file-upload-dropzone]` | A focusable drop target; click or Enter/Space opens the file dialog. `data-dragging` is Blazor-owned, mirrored from the engine's drag-over notifications. |
| NaviusFileUploadList | `ul[role=list][data-navius-file-upload-list]` | The selected-files list; auto-renders one `NaviusFileUploadItem` per file via `ItemTemplate`, or takes explicit children. |
| NaviusFileUploadItem | `li[role=listitem][data-navius-file-upload-item]` | One file row; cascades a `FileUploadItemContext` so its name/size/delete children know which file they describe. |
| NaviusFileUploadItemName | `span[data-navius-file-upload-item-name]` | The file name (defaults to `Item.File.Name`; overridable via `ChildContent`). |
| NaviusFileUploadItemSize | `span[data-navius-file-upload-item-size]` | The human-readable file size (e.g. "1.4 KB") via `FileUploadContext.FormatSize`. |
| NaviusFileUploadItemDelete | `button[data-navius-file-upload-item-delete]` | Per-file remove button (`aria-label="Remove {name}"`). |
| NaviusFileUploadClear | `button[data-navius-file-upload-clear]` | Clears all selected files; disabled when the list is already empty. |

`FileUploadPart` (abstract `ComponentBase`) is the shared base for parts that must re-render on file-list/drag-state change; it is not itself rendered, so it has no row above. `NaviusFileRejection` is a `record` (not a rendered component).

## Parameters

### NaviusFileUpload

| Name | Type | Default | Notes |
|---|---|---|---|
| Files | IReadOnlyList<IBrowserFile>? | `null` | Controlled file list; use `@bind-Files`. Presence (not value) of this parameter in `SetParametersAsync` marks the component controlled. |
| FilesChanged | EventCallback<IReadOnlyList<IBrowserFile>> | n/a | Paired with `Files`. |
| Accept | string? | `null` | Comma-separated MIME types / extensions (native `accept`). |
| Multiple | bool | `false` | Allows more than one file; also gates `MaxFiles` enforcement and dedup logic. |
| MaxFiles | int? | `null` | Cap on total file count (`null` = unlimited); only enforced when `Multiple` is true. |
| MaxSize | long | `0` | Per-file byte ceiling (`0` = unlimited). |
| Directory | bool | `false` | Accept a whole directory (native `webkitdirectory`). |
| Capture | string? | `null` | Native `capture` hint (`"user"` / `"environment"`). |
| Disabled | bool | `false` | Disables the root, the hidden input, and short-circuits all context actions. |
| Name | string? | `null` | Form field name applied to the real file input. |
| OnAccepted | EventCallback<IReadOnlyList<IBrowserFile>> | n/a | See Events. |
| OnRejected | EventCallback<IReadOnlyList<NaviusFileRejection>> | n/a | See Events. |
| ChildContent | RenderFragment? | `null` | Root body. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the root `<div>`. |

### NaviusFileUploadInput

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the underlying `InputFile`, minus `style` which is merged with a screen-reader-only inline style string. |

### NaviusFileUploadTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Button content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<button>`. |

### NaviusFileUploadDropzone

| Name | Type | Default | Notes |
|---|---|---|---|
| AriaLabel | string? | `"Drop files here or press Enter to browse"` | Accessible name for the drop target. |
| ChildContent | RenderFragment? | `null` | Dropzone content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<div>`. |

### NaviusFileUploadList

| Name | Type | Default | Notes |
|---|---|---|---|
| ItemTemplate | RenderFragment<IBrowserFile>? | `null` | Renders the inside of each item row for a file; auto-wraps it in a `NaviusFileUploadItem`. When set, takes precedence over `ChildContent`. |
| ChildContent | RenderFragment? | `null` | Explicit children, used only when `ItemTemplate` is null. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<ul>`. |

### NaviusFileUploadItem

| Name | Type | Default | Notes |
|---|---|---|---|
| File | IBrowserFile | `default!` | The file this row describes. Not marked `EditorRequired` in code, but has no usable default. |
| Invalid | bool | `false` | Marks this row invalid (`data-invalid`), e.g. a failed upload. |
| ChildContent | RenderFragment? | `null` | Row content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<li>`. |

### NaviusFileUploadItemName

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Overrides the default file-name text. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<span>`. |

### NaviusFileUploadItemSize

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<span>`. No `ChildContent` parameter; text is always `FileUploadContext.FormatSize(Item.File.Size)`. |

### NaviusFileUploadItemDelete

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Button content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<button>`. |

### NaviusFileUploadClear

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Button content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<button>`. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusFileUpload | FilesChanged | `EventCallback<IReadOnlyList<IBrowserFile>>` | In `ApplyFilesAsync`, only when controlled (`Files` was set), with the next file list, after any add/remove/clear operation. |
| NaviusFileUpload | OnAccepted | `EventCallback<IReadOnlyList<IBrowserFile>>` | In `HandleFilesAsync`, when a new selection produces at least one accepted file (`accepted.Count > 0`), with the accepted files (duplicates excluded). |
| NaviusFileUpload | OnRejected | `EventCallback<IReadOnlyList<NaviusFileRejection>>` | In `HandleFilesAsync`, when a new selection produces at least one rejection (`rejected.Count > 0`), with each rejected file paired to a `FileRejectionReason` (`TooLarge`, `TooMany`, `WrongType`). |

Internal (non-parameter) wiring: `NaviusFileUploadInput`'s `InputFile.OnChange` calls `Context.HandleFilesAsync`; `NaviusFileUploadTrigger`/`NaviusFileUploadDropzone` `@onclick` call `Context.OpenAsync()`; `NaviusFileUploadItemDelete` `@onclick` calls `Item.RemoveAsync`; `NaviusFileUploadClear` `@onclick` calls `Context.ClearAsync`. `NaviusFileUpload.OnDraggingChange(bool)` is a `[JSInvokable]` called by the JS drag/drop engine, not a `[Parameter]`.

## State + data attributes

| Attribute | Element | Set when |
|---|---|---|
| `data-navius-file-upload` | NaviusFileUpload root `<div>` | always |
| `data-disabled` | NaviusFileUpload root `<div>` | `Disabled` true |
| `data-invalid` | NaviusFileUpload root `<div>` | most recent selection produced at least one rejection (`_invalid`) |
| `data-dragging` | NaviusFileUpload root `<div>` | `Context.IsDragging` true (mirrored from the JS engine's `OnDraggingChange`) |
| `role="status"` / `aria-live="polite"` | hidden status `<div data-navius-file-upload-status>` | always; visually hidden, contains `Context.StatusMessage` |
| `data-navius-file-upload-input` | NaviusFileUploadInput's `<input type=file>` | always; input itself is visually hidden via inline screen-reader-only CSS |
| `data-navius-file-upload-trigger` / `data-disabled` | NaviusFileUploadTrigger `<button>` | `data-disabled` when `Context.Disabled` |
| `data-navius-file-upload-dropzone` / `data-dragging` / `data-disabled` | NaviusFileUploadDropzone `<div>` | `data-dragging` when `Context.IsDragging`; `data-disabled` when `Context.Disabled` |
| `data-navius-file-upload-list` | NaviusFileUploadList `<ul>` | always |
| `data-navius-file-upload-item` / `data-invalid` | NaviusFileUploadItem `<li>` | `data-invalid` when the item's own `Invalid` parameter is true |
| `data-navius-file-upload-item-name` | NaviusFileUploadItemName `<span>` | always |
| `data-navius-file-upload-item-size` | NaviusFileUploadItemSize `<span>` | always |
| `data-navius-file-upload-item-delete` | NaviusFileUploadItemDelete `<button>` | always |
| `data-navius-file-upload-clear` / `data-disabled` | NaviusFileUploadClear `<button>` | `data-disabled` when `Context.Disabled || Context.Files.Count == 0` |

Internal (non-DOM) state: `FileUploadContext` holds `Files`, `IsDragging`, `Invalid` ("true after the most recent selection produced at least one rejection"), `Disabled`, `Accept`, `Multiple`, `Directory`, `Capture`, `Name`, `StatusMessage` (polite live-region text, e.g. `"2 files added. 1 file rejected"` built by `BuildStatus`), and `InputElement` (registered by `NaviusFileUploadInput` for the engine's drop relay + dialog open). `FileUploadItemContext` holds `File` and `Invalid` for one row.

## Keyboard

| Key | Behavior |
|---|---|
| Enter / Space on `NaviusFileUploadDropzone` (`role=button`, focused) | Explicit `OnKeyDownAsync` handler checks `e.Key == "Enter" \|\| e.Key == " "` and calls `Context.OpenAsync()` (opens the OS file dialog); no-op when `Context.Disabled`. |
| Tab | `NaviusFileUploadDropzone` sets `tabindex="0"` (or `"-1"` when disabled), making it keyboard-focusable via standard browser tab order; this is a static attribute, not custom key handling. |
| Enter / Space on `NaviusFileUploadTrigger`, `NaviusFileUploadItemDelete`, `NaviusFileUploadClear` | No explicit key handler in code; these are native `<button>` elements, so activation relies on native browser button keyboard behavior (per the code comment "native button keyboard" on the Trigger). |
| (any) on the hidden `<input type=file>` (`NaviusFileUploadInput`) | No explicit key handler; the code comment states "native keyboard, the OS file dialog and screen-reader announcements all come for free" from the real `<input type=file>` element. |

No `tests/e2e` files exist for this family (confirmed by glob search) to cross-check.

## Accessibility

- `NaviusFileUploadInput` renders the real, hidden `<input type="file">` (Blazor's `InputFile`) and is described in code as "the a11y + form source of truth"; it is visually hidden via an inline screen-reader-only style (`position:absolute;width:1px;height:1px;...clip:rect(0 0 0 0)`), not `display:none`, so it remains in the accessibility tree and reachable by AT/tests. `multiple`, `disabled`, `accept`, `name`, `webkitdirectory`, `capture` are all conditionally wired from `FileUploadContext`.
- `NaviusFileUploadDropzone` renders `role="button"`, `tabindex` (`0`/`-1` by disabled state), `aria-disabled` (when disabled), and `aria-label` (defaults to `"Drop files here or press Enter to browse"`, overridable via `AriaLabel`). The code comment frames this as the react-aria `DropZone` pattern, noting "Enter/Space open the file dialog (the button path is the a11y path)."
- `NaviusFileUploadList` renders `role="list"`; `NaviusFileUploadItem` renders `role="listitem"`.
- `NaviusFileUploadItemDelete` renders a dynamic `aria-label="Remove {Item.File.Name}"`.
- The root's hidden status `<div role="status" aria-live="polite">` announces `StatusMessage` (added/rejected/removed counts) as a polite live region.
- No `FocusAsync`/programmatic focus calls exist anywhere in this family (unlike the Field family's `NaviusInput`). Opening the OS file dialog goes through JS interop (`_dropzone.ClickToOpenAsync()` on the real hidden input), not a C# focus call.

## WPF strategy

Tier B (custom lookless control). No single native WPF control maps to this family: the root needs a custom `Control` owning a `FileUploadContext`-equivalent (dependency properties or a view-model) for the file list, drag state, and validation results; `NaviusFileUploadList`/`Item`/`ItemName`/`ItemSize`/`ItemDelete` map onto a `ListBox`/`ItemsControl` with a `DataTemplate` (native `ListBoxItem` peers already satisfy the `role=list`/`listitem` ARIA pattern via `AutomationProperties`). What will NOT translate cleanly: (1) the entire "hidden real `<input type=file>` is the a11y + form source of truth, other parts relay clicks to it" design is DOM/browser-specific and has no WPF analog: the OS file dialog is instead `Microsoft.Win32.OpenFileDialog`, called directly rather than relayed through a hidden input; (2) the JS-interop drag/drop relay (`NaviusJsInterop.CreateFileDropzoneAsync`, which copies dropped files into the hidden input so Blazor's own `InputFile.OnChange` fires) should be replaced outright with WPF's native `AllowDrop`/`Drop`/`DataObject.GetFileDropList()` rather than ported; (3) the CSS-only screen-reader-only hiding trick for the native input has no purpose once there's no hidden-input DOM pattern to preserve.

## Open questions

- The whole "hidden native `<input type=file>` + relay from Trigger/Dropzone clicks + JS drag/drop copies files into that hidden input" architecture is a DOM-specific accessibility/testability pattern (comment: "tests drive it via setInputFiles"); the WPF port needs a from-scratch design (likely `OpenFileDialog` + native `Drop` events feeding the same `FileUploadContext`-equivalent) rather than a literal port, since there's no WPF concept of "a real native file input under the hood."
- `Multiple=false` semantics: a new accepted selection replaces `CurrentFiles` only when `accepted.Count > 0` (`next = ... : (accepted.Count > 0 ? new[] { accepted[^1] } : CurrentFiles)`), i.e. an all-rejected selection leaves the existing single file in place rather than clearing it. Confirm this "replace only if something was accepted" behavior is the intended port target.
- `MaxFiles` is enforced only when `Multiple` is true (`if (Multiple && MaxFiles.HasValue && running.Count >= MaxFiles.Value)`); a non-multiple upload has no explicit `TooMany` rejection path (it's implicitly capped at 1 via the replace logic above). Confirm this asymmetry should carry into the WPF contract.
- Duplicate detection (`Same`, matched by `Name` + `Size` only, not content hash) silently drops the duplicate: it's excluded from both `accepted` and `rejected`, so it's not reflected in the status message or either event. Decide whether the WPF port should keep this "silent no-op" behavior or surface a rejection reason for duplicates.
- `AcceptMatch` supports only extension (`.ext`), MIME wildcard (`type/*`), and exact MIME string match, evaluated in C# against `IBrowserFile.Name`/`ContentType` (browser-supplied). WPF's `OpenFileDialog.Filter` has different syntax and is evaluated by the OS dialog itself; the port needs to decide whether filtering happens once (dialog filter only) or twice (dialog filter + the same post-hoc `AcceptMatch`-style check, to keep `OnRejected`/`WrongType` reporting working for drag-dropped files that bypass the dialog).
- `MaxSize` is a per-file byte ceiling only; there is no aggregate/total-size limit anywhere in this family's code. Confirm no aggregate cap is expected in the WPF port.

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/FileUpload/FileUploadEngine.cs` (pure validation
core + `FileEntry` + `NaviusFileRejection`/`FileRejectionReason` + `FileSelectionResult`),
`IFilePicker.cs` (the injectable dialog seam + `OpenFileDialogPicker`), `NaviusFileUpload.cs`
(Tier B lookless control + UIA peer), `Themes/FileUpload.xaml`,
`tests/Navius.Wpf.Tests/FileUploadTests.cs`,
`apps/Navius.Wpf.Gallery/Pages/FileUploadPage.xaml(.cs)`.

**Architecture (first open question resolved)**: redesigned from scratch as the strategy
predicted. The hidden-`<input type=file>` relay is gone: the OS dialog sits behind an injectable
`IFilePicker` (defaulting to `Microsoft.Win32.OpenFileDialog`), so unit tests stub it and never
open a real dialog; the JS drag/drop relay became native WPF `AllowDrop` + `DragOver` validation +
`Drop` extracting `DataFormats.FileDrop` paths on the dropzone. `IBrowserFile` became the
`FileEntry` record (`Path`, `Name`, `Size`, `ContentType`), exposed as
`IReadOnlyList<FileEntry> Files`.

**Preserved web quirks (second/third/fourth open questions resolved, all kept for parity, pinned
by engine tests)**: non-multiple replaces the single file only when something was accepted (an
all-rejected selection leaves it in place); `MaxFiles` is enforced only when `Multiple` (a
non-multiple upload is implicitly capped at 1 via the replace rule, no `TooMany` path); duplicates
(same name + size, no content hash) are silently dropped, appearing in neither the accepted nor
the rejected lists nor the status line. The check order (accept -> size -> count -> duplicate) is
also pinned.

**Accept filter (fifth open question resolved)**: filtering happens twice, deliberately. The
dialog filter is built from the accept string's extension entries (MIME entries widen it to All
files, since `OpenFileDialog.Filter` cannot express them), and the engine's post-hoc
`AcceptMatch` (extension / `type/*` wildcard / exact MIME, the web's exact semantics) runs on
EVERY selection, dialog or drop, so `OnRejected`/`WrongType` reporting works uniformly. Since no
browser supplies MIME types, `FileEntry.FromPath` infers `ContentType` from a small built-in
extension map (`ContentTypeFor`); unknown extensions still match extension accept entries.

**MaxSize (sixth open question resolved)**: per-file ceiling only, no aggregate cap, matching the
web.

**Parts folding + a11y**: 10 parts fold into one Control with four template parts. The dropzone is
a real restyled Button, so the contract's `role="button"` + tab stop + Enter/Space-open-dialog all
come free from the native peer (Space and Enter both activate a focused WPF Button); its
accessible name is the `DropzoneText` DP (the contract's AriaLabel default, same wording).
Item rows are a DataTemplate over `FileEntry` (name + `SizeText` via the ported `FormatSize` +
delete button named "Remove {name}"). The hidden `role=status` live region became a visible muted
status TextBlock with `AutomationProperties.LiveSetting=Polite` (the Combobox status precedent),
carrying the ported `BuildStatus` wordings ("2 files added. 1 file rejected", "Removed x",
"Cleared all files"). The root peer exposes the selected file names over a read-only ValuePattern
(the Select-peer / M3-gate precedent). The `role=list`/`listitem` mapping onto ListBox peers was
not taken (an ItemsControl carries the rows); noted as the one a11y simplification.

**Dropped parameters**: `Directory` (`webkitdirectory`; `OpenFileDialog` cannot pick folders,
would need `OpenFolderDialog` and different semantics), `Capture` (mobile camera hint, no desktop
analog), and `Name` (HTML form field name) have no WPF equivalent and were omitted rather than
stubbed. `data-dragging`/`data-invalid` map to read-only `IsDragging`/`Invalid` DPs consumed by
template triggers; `Disabled` is native `IsEnabled`.

## M6 audit (2026-07-09)

Adversarial re-verification against the actual C#/XAML. This family held up: every checked claim
traced to real code.

CONFIRMED correct (no fix needed):
- Keyboard: the dropzone `PART_Dropzone` is a real `Button` (`Themes/FileUpload.xaml`), and both
  Space and Enter on a focused dropzone open the picker. Verified empirically by raising real
  KeyDown/KeyUp routed events on the dropzone button through the actual routing (Space and Enter
  each invoked the injected `IFilePicker` exactly once), so the "Space and Enter both activate a
  focused WPF Button" note in the WPF implementation notes is accurate, not aspirational.
- Engine quirks (`FileUploadEngine.Process`, lines 63-118): the four preserved web quirks
  (non-multiple replace-only-if-accepted, `MaxFiles` only when `Multiple`, silent duplicate drop by
  name+size, check order accept->size->count->duplicate) all match the prose and are pinned by
  `FileUploadEngineTests`.
- Peer: `NaviusFileUploadAutomationPeer` correctly overrides `GetPattern` to surface its read-only
  `ValuePattern` (line 415), so the file-name value is genuinely reachable over UIA (contrast the
  NumberField peer bug found in this same audit wave).
- Theme: every brush/radius reference is `DynamicResource`; all referenced token keys
  (`Navius.Card/Border/Background/Ring/Accent/Destructive/Foreground/MutedForeground`,
  `Navius.Radius.Small/Card`) exist in both `Tokens.Light.xaml` and `Tokens.Dark.xaml`.

PLAUSIBLE (unfixed, low severity):
- `Themes/FileUpload.xaml` line 120 uses one `StaticResource` (`Navius.FileUpload.DefaultItem`).
  This references a `DataTemplate` defined in the same dictionary, not a themeable Color/Brush, so
  it does not break runtime re-theming; flagged only because the family theme convention is
  otherwise all-DynamicResource.
