# MessageScroller

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusMessageScrollerProvider | none (CascadingValue only) | Headless root: owns scroll options, cascades `MessageScrollerContext` (works anywhere inside it, including outside the scroller frame) |
| NaviusMessageScroller | `<div>` | The scroller frame: positioning parent for the scroll button; the engine mirrors `data-scrollable`/`data-autoscrolling` onto it (per doc comment) |
| NaviusMessageScrollerViewport | `<div role="region">` | The scroll container; hands its element to the JS engine (`createMessageScroller`) which owns the scroll hot path |
| NaviusMessageScrollerContent | `<div role="log">` + a trailing spacer `<div>` | The transcript live region; direct children must be `NaviusMessageScrollerItem` |
| NaviusMessageScrollerItem | `<div>` | One transcript row boundary (message, marker, typing indicator, separator, or load-earlier row) |
| NaviusMessageScrollerButton | `<button type="button">` | Scroll-to-start/end control; context-aware active/inert state |

## Parameters

### NaviusMessageScrollerProvider

| Name | Type | Default | Notes |
|---|---|---|---|
| AutoScroll | bool | false | Follow new content only while already at the live edge; wheel/touch/keyboard scroll, scrollbar drags, and explicit jumps release follow; the scroll button or `ScrollToEndAsync` re-engages it |
| DefaultScrollPosition | string | "end" | "start", "end", or "last-anchor" (falls back to "end" when the turn fits the viewport or no anchor exists); applied once on the first non-empty render |
| ScrollEdgeThreshold | double | 8 | Distance in px from either edge that still counts as being at it; drives scrollable-edge state + button visibility |
| ScrollMargin | double | 0 | Margin in px applied to the aligned edge for scroll targets and the visibility reading line |
| ScrollPreviousItemPeek | double | 64 | Extra margin added to `ScrollMargin` when a newly appended anchor row is positioned, so part of the previous item stays visible above it |
| ChildContent | RenderFragment? | null | |

### NaviusMessageScroller

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMessageScrollerViewport

| Name | Type | Default | Notes |
|---|---|---|---|
| PreserveScrollOnPrepend | bool | true | Keep the first visible row stable when older rows are prepended |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes (can override `role`/`aria-label`/`tabindex`) |

### NaviusMessageScrollerContent

| Name | Type | Default | Notes |
|---|---|---|---|
| SpacerClass | string? | null | CSS class for the internal trailing spacer element that makes room for anchored rows |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes (can carry `aria-busy` while a turn streams) |

### NaviusMessageScrollerItem

| Name | Type | Default | Notes |
|---|---|---|---|
| MessageId | string? | null | Stable row id used by jump-to-message, visibility tracking, and prepend preservation |
| ScrollAnchor | bool | false | Marks the row as a turn boundary that anchors newly appended turns |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

### NaviusMessageScrollerButton

(inherits `MessageScrollerPart`)

| Name | Type | Default | Notes |
|---|---|---|---|
| Direction | string | "end" | Which edge the button scrolls toward: "end" or "start" |
| Behavior | string | "smooth" | Scroll behavior for the jump: "smooth" or "auto" |
| ChildContent | RenderFragment? | null | Consumer supplies the label/icon content |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes |

## Events

No part in this family declares an `EventCallback<T>` `[Parameter]`. State is instead surfaced through `MessageScrollerContext`:

| Mechanism | Signature | Fires when |
|---|---|---|
| `MessageScrollerContext.Changed` | `event Func<Task>?` (internal; parts subscribe via `MessageScrollerPart.OnInitialized`) | `ScrollableStart`/`ScrollableEnd` change (driven by the engine's `OnScrollableChange` JS callback) |
| `MessageScrollerContext.SubscribeVisibilityAsync(Func<Task> handler)` / `UnsubscribeVisibilityAsync` | `Func<Task>` handler, not a Blazor `EventCallback` | App code subscribes to be notified when `CurrentAnchorId`/`VisibleMessageIds` change; the first subscriber starts engine-side visibility tracking, the last one stops it |
| `NaviusMessageScrollerViewport.OnScrollableChange` | `[JSInvokable] Task OnScrollableChange(bool start, bool end)` | Invoked FROM JS by the engine whenever the scrollable-edge state changes |
| `NaviusMessageScrollerViewport.OnVisibilityChange` | `[JSInvokable] Task OnVisibilityChange(string? currentAnchorId, string[] visibleMessageIds)` | Invoked FROM JS while visibility tracking runs |

Programmatic scroll commands (not events, but the app-facing API surface): `MessageScrollerContext.ScrollToMessageAsync(string messageId, MessageScrollerScrollOptions? options)`, `ScrollToStartAsync(...)`, `ScrollToEndAsync(...)`: all `Task<bool>`, queued (`_pendingTarget`) if called before the viewport/engine has mounted.

## State + data attributes

| Attribute / class | Where | Meaning |
|---|---|---|
| `data-navius-messagescroller` | Frame (`NaviusMessageScroller`) | Marker; also, per the component's doc comment, the mirror target for the JS engine's `data-scrollable`/`data-autoscrolling` attributes (engine-written, not visible as explicit C# in this folder) |
| `data-navius-messagescroller-viewport` | Viewport | Marker |
| `data-navius-messagescroller-content` | Content | Marker |
| `data-navius-messagescroller-spacer` | Content's trailing spacer div | Marker; engine-sized to make room for anchored rows |
| `data-navius-messagescroller-item` | Item | Marker |
| `data-message-id` | Item | Reflects `MessageId` |
| `data-scroll-anchor` | Item | `"true"`/`"false"` reflecting `ScrollAnchor` |
| `data-navius-messagescroller-button` | Button | Marker |
| `data-direction` | Button | Reflects `Direction` ("start"/"end") |
| `data-active` | Button | `"true"`/`"false"`; `true` only while the viewport can still scroll in that direction (`ScrollableStart`/`ScrollableEnd`) |
| `tabindex="-1"` + `inert` | Button, when inactive | Removes the button from the tab order and interaction/AT tree without visually disabling it |

Internal (non-DOM) state on `MessageScrollerContext`: `ScrollableStart` / `ScrollableEnd` (bool), `CurrentAnchorId` (string?, last anchor row at/above the reading line; only updates while a visibility subscriber listens), `VisibleMessageIds` (IReadOnlyList\<string\>, document order; only updates while a visibility subscriber listens), `Options` (effective `MessageScrollerOptions` = provider options + the viewport's `PreserveScrollOnPrepend` flag), a pending-target queue for `ScrollToMessageAsync` calls made before the engine attaches, and a visibility-subscriber reference count that gates engine-side tracking.

## Keyboard

No custom keyboard event handlers (`@onkeydown`/`OnKeyDown`) exist anywhere in this component folder, and no message-scroller e2e test in `tests/e2e/specs/chat.spec.ts` exercises keyboard interaction.

| Key | Behavior |
|---|---|
| (none custom) | `NaviusMessageScrollerViewport` is a native-focusable (`tabindex="0"`), native-scrollable `<div>`; arrow keys / Page Up / Page Down / Home / End / Space scroll via the browser's built-in scrollable-region behavior, unmediated by Navius code |
| Enter / Space (on Button) | Native `<button>` activation (browser default), not a custom key handler; `@onclick="OnClickAsync"` fires either way |

## Accessibility

- `role="region"` + `aria-label="Messages"` + `tabindex="0"` on Viewport (overridable via the attribute seam).
- `role="log"` + `aria-relevant="additions"` on Content: a live region so appended rows announce without narrating every streamed token; the consumer can pass `aria-busy` through `Attributes` while a turn streams to hold announcements until the row completes.
- `aria-hidden="true"` on the trailing spacer.
- No ARIA role is applied to `NaviusMessageScrollerItem` or the frame `NaviusMessageScroller` div.
- Button inactive state uses `tabindex="-1"` + HTML `inert` (not `disabled`/`aria-disabled`) so it is excluded from the focus order and interaction/AT tree while remaining visually unchanged.
- No `FocusAsync` calls or explicit focus-management logic exist anywhere in this family's C#; the Viewport is made focusable but nothing programmatically moves focus into or out of it.

## WPF strategy

Tier B (custom lookless control).

Per its own doc comment this is "a Navius extra... no Base UI counterpart" and there is no native WPF control that already implements anchored-turn scroll positioning, follow-the-live-edge auto-scroll, or intersection-based row visibility tracking. Build it as a lookless `Control`/`ItemsControl` wrapping a `ScrollViewer` (or a virtualizing panel), with a C#-side reimplementation of the scroll-anchoring, edge-detection, and visibility-tracking math currently done by the JS engine (`createMessageScroller`, in `Interop/NaviusJsInterop.cs`, outside this component folder). Map `role="log"`/`aria-relevant="additions"` toward `AutomationProperties.LiveSetting="Polite"` on the items host, and `role="region"`+`aria-label` toward a `Pane`/custom `AutomationPeer` with a matching `Name`. Things that will not translate cleanly: the entire JS scroll-hot-path engine (anchored positioning, follow-disengage-on-manual-scroll, `IntersectionObserver`-based visibility, prepend preservation) needs a full from-scratch WPF reimplementation; the `data-scrollable`/`data-autoscrolling` CSS-hook mirroring becomes `VisualStateManager` states/triggers instead; and HTML `inert` (visually-enabled but focus/hit-test-excluded) has no single WPF property: it needs `Focusable="False"` + `IsHitTestVisible="False"` rather than `IsEnabled="False"` (which would also restyle the button).

## Open questions

- The actual scroll-anchoring, follow/auto-scroll-disengage, and visibility-tracking behavior lives in the JS `createMessageScroller` engine (`Interop/NaviusJsInterop.cs`), outside this component folder's source of truth: a faithful WPF port requires reading that JS/interop layer, not just the Blazor wrapper components documented here.
- No keyboard interaction is implemented in this family; the Viewport's scroll keys and the Button's Enter/Space activation are entirely native-browser behavior. WPF's `ScrollViewer` default keyboard handling (arrow/PageUp/PageDown/Home/End increments) will differ from a browser's native scrollable-`div` defaults: decide whether exact increment parity matters.
- No ARIA role is applied to `NaviusMessageScrollerItem` (no `role="listitem"`/`"article"`); a WPF `ItemsControl` would naturally expose `ListItem` automation peers for its containers, a stronger AT contract than the current markup provides. Confirm whether that stronger contract is a deliberate parity target or should be avoided to match web-side minimalism.
- `DefaultScrollPosition="last-anchor"`'s exact fallback rule ("falling back to end when the turn fits the viewport or no anchor exists") is engine logic not visible in this folder and needs to be confirmed against the JS source before porting.
- The lazy "first subscriber starts visibility tracking, last unsubscribe stops it" pattern (`SubscribeVisibilityAsync`/`UnsubscribeVisibilityAsync`) is a reference-counted `Func<Task>` multicast, not a Blazor `EventCallback`: decide the idiomatic WPF equivalent (a plain CLR event vs. a weak-event pattern to avoid leaks from long-lived subscribers).

## WPF implementation notes

Shipped as `Controls/MessageScroller/` (`MessageScrollerEngine` + `NaviusMessageScroller`) with `Themes/MessageScroller.xaml` (not merged into `Generic.xaml`; consumers merge the dictionary themselves, Toast precedent). Tests in `tests/Navius.Wpf.Tests/MessageScrollerTests.cs`; demo page `Pages/MessageScrollerPage.xaml` (AutomationId `MessageScrollerDemo`).

### Shape

- The web family's six parts collapse into one lookless `ItemsControl` (`NaviusMessageScroller`) templated as `PART_ScrollViewer` (the viewport, `CanContentScroll="False"` because the engine's math is pixel-based) + `ItemsPresenter` (the content) + `PART_JumpToLatestButton`. There is no Provider/Context cascade: WPF's DP system covers the option-flow role, and there is no cross-frame scroll-command surface to justify a separate headless root.
- The behavior engine is a pure, WPF-free class (`MessageScrollerEngine`): a state machine over `(scrollOffset, viewportHeight, extentHeight)` snapshots plus intent events, so every contract rule is testable headlessly with plain `[Fact]`s. The control classifies `INotifyCollectionChanged` events by index (add at end = append/new content; insert at index 0 with items already present = history prepend) and routes real `ScrollChanged` geometry into the engine.

### Behavior parity (vs createMessageScroller)

- Live-edge follow (`AutoScroll`, default false), `ScrollEdgeThreshold` (default 8) edge hysteresis, disengage on reader intent, re-engage when the reader lands back within the threshold, one-time (non-engaging) jump when `AutoScroll` is off, and the queued jump-before-content all match the JS engine's `following`/`atEnd`/`scrollToEnd`/`pendingTarget` semantics.
- Reader intent is inferred from `ScrollChanged` events with a nonzero `VerticalChange` that the control did not itself initiate (an `_applyingEngineOffset` reentrancy flag marks engine-driven `ScrollToVerticalOffset` calls), rather than from wheel/touch/key/pointerdown listeners: WPF has no need to distinguish input modality, only "was this scroll mine".
- Prepend preservation shifts the offset by exactly the extent growth (extent-delta model) instead of re-locating a captured reference row (the JS `captureRef`/`restoreRef` element model). Equivalent whenever prepended rows are inserted strictly above the viewport, which is the classified case; mid-list mutations are out of contract and fall back to a plain clamp.

### Deliberate deltas (recorded)

- Not ported: anchored-turn positioning (`ScrollAnchor`/`data-scroll-anchor`, `ScrollPreviousItemPeek`, the trailing spacer), `DefaultScrollPosition` (with `AutoScroll` on, the first layout pass sticks to the end anyway because the engine starts following; with it off the view opens at the natural top, and consumers call `JumpToBottom()` after seeding to open at the end), `ScrollMargin`, `ScrollToMessageAsync`/start-direction button, and IntersectionObserver visibility tracking (`CurrentAnchorId`/`VisibleMessageIds`). These are chat-app affordances with no consumer in this repo yet; the parity doc's open questions on `last-anchor` and the visibility-subscriber pattern stay open.
- New in WPF, not in the web contract: `NewMessageCount` (read-only DP), the unseen-messages count accumulated while disengaged, driving the JumpToLatest badge; the web scroll button is edge-driven (`data-active` from `ScrollableEnd`) and count-free. Chosen because the WPF part doubles as the new-message notifier (see a11y below).
- The JumpToLatest button appears only while disengaged with unseen content (`Visibility` toggled by the control); the web button is visible-but-inert (`tabindex="-1"` + `inert`) whenever the edge state disables it. WPF has no `inert` equivalent, and per APG a control that cannot do anything should not be in the accessibility tree at all: `Collapsed` is the honest native mapping.

### A11y (APG/native-WPF tiebreaks over the web contract)

- `role="log"` + `aria-relevant="additions"`: `AutomationProperties.LiveSetting = Polite` set in the constructor, plus `RaiseNotificationEvent` ("N new messages", `CurrentThenMostRecent`) raised only when messages arrive while disengaged (Toast precedent, including the Win10-1709+/no-AT fallback note). A following reader is watching the transcript move; announcing every appended row would be noise the web's own docs warn about (`aria-busy` seam).
- The web Viewport's `role="region"` + `aria-label="Messages"` + `tabindex="0"`: the inner `ScrollViewer` is natively focusable and keyboard-scrollable; consumers set `AutomationProperties.Name` on the control (the demo page does). No custom keyboard handling was added, matching the web family's zero custom key handlers; WPF `ScrollViewer` increments differ from browser-native scrollable-div increments, accepted per the open question.
- Items: the web applies no ARIA role to rows; the WPF `ItemsControl`'s default `ContentPresenter` containers likewise surface no interactive control type, so the AT contract stays minimal without extra suppression work.

## M6 audit (2026-07-09)

Adversarial re-verification against the C#/XAML at file:line. No confirmed disparities found; the implementation notes above match the code.

CONFIRMED (fixed): none.

Verified true (spot checks):
- No false keyboard claims. There are zero custom key handlers in `NaviusMessageScroller.cs`; the auto-scroll-pause-on-reader-intent behavior is real (`MessageScrollerEngine.OnUserScrolled` recomputes follow from position, engine.cs:118). Scroll keys are left to the native `ScrollViewer` per the contract.
- `NewMessageCount`/`IsFollowing` are read-only DPs (NaviusMessageScroller.cs:42-52); `AutoScroll` default `false` and `ScrollEdgeThreshold` default `8` match the contract (lines 34-40) and are covered by `Defaults_MatchTheContract`.
- Live-region a11y: `AutomationProperties.SetLiveSetting(this, Polite)` in the constructor (line 72) plus `RaiseNotificationEvent` in `AnnounceNewMessages` (lines 261-277), guarded by `IsLoaded`.
- `Themes/MessageScroller.xaml` uses `DynamicResource` for every brush/radius token; all referenced keys (`Navius.Primary`, `Navius.PrimaryForeground`, `Navius.Border`, `Navius.Radius.Control`, `Navius.Background`, `Navius.Foreground`) exist in both `Tokens.Light.xaml` and `Tokens.Dark.xaml`.

PLAUSIBLE (residual, unfixed):
- The `JumpToLatest` button pulls its style via `Style="{StaticResource Navius.MessageScroller.JumpToLatestButtonStyle}"` (MessageScroller.xaml:64). This is a `StaticResource` to a `Style` defined earlier in the same dictionary, not a color/brush token, and the brushes inside that style are `DynamicResource`, so runtime theme switching is unaffected. Acceptable, but the only `StaticResource` in the four audited themes.
- The native `ScrollViewer`'s keyboard-scroll increments differ from a browser's native scrollable-`div` increments; already recorded as an accepted open question, not a regression.
