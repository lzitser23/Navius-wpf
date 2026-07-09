# Avatar

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusAvatar` | `<span>` | Root; cascades `AvatarContext` (load status) |
| `NaviusAvatarFallback` | `<span>` (conditional on `Status != Loaded` and delay elapsed) | Content shown while idle/loading/error, or before `DelayMs` elapses |
| `NaviusAvatarImage` | `<img>` (conditional on `Status != Error`) | The avatar image; drives the load-status state machine via native `onload`/`onerror` |

## Parameters

### NaviusAvatar

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | Captured unmatched attributes |

### NaviusAvatarFallback

| Name | Type | Default | Notes |
|---|---|---|---|
| `DelayMs` | `int` | `0` (implicit) | If `<= 0`, renders immediately; otherwise waits `DelayMs` before it is allowed to render |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | |

### NaviusAvatarImage

| Name | Type | Default | Notes |
|---|---|---|---|
| `Src` | `string?` | none | A (re)assigned non-empty value transitions status to `Loading` until native `load`/`error` resolves it |
| `OnLoadingStatusChange` | `EventCallback<string>` | none | Fires on every status transition with `'idle'`\|`'loading'`\|`'loaded'`\|`'error'` |
| `Attributes` | `IDictionary<string, object>?` | none | |

## Events

| Part | Event | Type |
|---|---|---|
| `NaviusAvatarImage` | `OnLoadingStatusChange` | `EventCallback<string>` |

## State + data attributes

`AvatarContext`:

- `Status`: `AvatarLoadStatus` enum { `Idle`, `Loading`, `Loaded`, `Error` }, default `Idle`.
- `ToStatusString(AvatarLoadStatus)`: maps the enum to the spec's lowercase string (`'idle'`/`'loading'`/`'loaded'`/`'error'`) used by `OnLoadingStatusChange`.
- Transitions: `RequestLoadingAsync()`, `RequestLoadedAsync()`, `RequestErrorAsync()` (internally routed from `NaviusAvatarImage`'s `Src` change and its `onload`/`onerror` handlers).
- `Changed` event (re-renders subscribed parts); `StatusChanged` event (fires `OnLoadingStatusChange` on the owning Image).

Rendered `data-*` markers (no `data-state`/`data-status` attribute is rendered; status is only consumed internally to gate conditional rendering):

| Attribute | Where |
|---|---|
| `data-navius-avatar` | Root |
| `data-navius-avatar-fallback` | Fallback |
| `data-navius-avatar-image` | Image |

`NaviusAvatarImage` also sets an inline `style="visibility:hidden;"` while `Status != Loaded` (kept in the DOM so `load`/`error` events fire, but hidden until actually loaded).

## Keyboard

No keyboard interaction implemented in this family.

## Accessibility

No ARIA roles or `aria-*` attributes are wired in any Avatar part's markup. No explicit focus management is implemented.

## WPF strategy

Tier B (custom lookless control)

Build a lookless `Control` (e.g. `NaviusAvatar`) whose `ControlTemplate` overlays an `Image` and a fallback `ContentPresenter`, switching visibility from an internal `AvatarLoadStatus`-equivalent dependency property, mirroring how `AvatarContext.Status` drives the two conditional Blazor parts; a plain WPF `Image` is not sufficient on its own because the family's actual behavior is the fallback/idle/loading/error state machine layered on top of it, not the image element itself. Map the composite to a `FrameworkElementAutomationPeer` (there is no ARIA role in the source to preserve, so no specific UIA control-type mapping is mandated by parity: plain `ContentControl`-level automation is sufficient). The two features that will NOT translate cleanly: (1) WPF's `Image`/`BitmapImage` loads asynchronously and fails via the `ImageFailed` routed event rather than the HTML `<img onerror>` pattern used here, so the `Loading`→`Loaded`/`Error` transition needs to be wired off `BitmapImage.DownloadCompleted`/`DownloadFailed` (or `ImageFailed`) instead of native `onload`/`onerror`; (2) the `DelayMs` fallback-suppression timer (`Task.Delay` + `CancellationTokenSource`) is a straightforward `DispatcherTimer` port but has no WPF built-in equivalent to lean on.

## Open questions

- Whether `DelayMs`'s cancellation-on-dispose semantics (via `CancellationTokenSource`) need to also cancel/restart if `DelayMs` itself changes after initialization; the Blazor source only starts the delay once in `OnInitialized` and never reacts to a later `DelayMs` parameter change.
- Whether the WPF port should expose `Status` as a public bindable property (useful for consumer styling/triggers) even though the Blazor `AvatarContext.Status` setter is internal and not exposed as a public parameter on `NaviusAvatar`.
- No `data-state`/status attribute is rendered anywhere in this family, unlike most other Navius primitives; confirm this is intentional (vs. a gap) before deciding whether the WPF version should add one for template-trigger convenience without breaking parity intent.
