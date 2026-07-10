# ADR-0006: M6 RTL + multi-monitor DPI hardening

Status: accepted

## Context

Two independent hardening tasks for M6: (1) `NaviusAnchoredPopup` positioned every popup against
`SystemParameters.WorkArea`, the primary monitor's work area, so placement was wrong on any
non-primary monitor; (2) an RTL audit across the catalog for the "escapes automatic mirroring"
bug class (`RenderTransform`/`Clip`/`Canvas` math and glyphs that WPF's own `FlowDirection`
mirroring does not reach).

## Decision: multi-monitor + DPI

Added `Positioning/MonitorWorkArea.cs`: `TryGetWorkAreaDeviceUnits(Point)` P/Invokes
`MonitorFromPoint` (`MONITOR_DEFAULTTONEAREST`) + `GetMonitorInfo` to get the containing
monitor's work area in device pixels, and the pure `ResolveWorkArea(Rect? deviceUnits, dpiScaleX,
dpiScaleY, fallbackDips)` converts to DIPs or returns the caller's fallback when the P/Invoke
lookup yields null. `NaviusAnchoredPopup.UpdatePlacement()` now resolves the anchor's center in
device pixels (`Anchor.PointToScreen`) and passes it through both, falling back to
`SystemParameters.WorkArea` exactly as before if the monitor lookup fails. `PlacementMath` is
unchanged (still pure, still takes `workArea` as a parameter); only the acquisition changed, per
the task's constraint.

`apps/Navius.Wpf.Gallery/app.manifest` declares `dpiAwareness=PerMonitorV2` (with the pre-PMv2
`dpiAware=true/pm` fallback for older Windows), wired via `<ApplicationManifest>` in the Gallery's
csproj. Without this the app runs system- or per-monitor(v1)-DPI-aware, and `VisualTreeHelper
.GetDpi` (which the popup's DIP conversion already depends on) does not reflect the true
per-monitor scale on a live DPI change.

Tests: `MonitorWorkAreaTests.cs` covers `ResolveWorkArea` directly (the pure, P/Invoke-free seam)
including the null-fallback path, plus one live-Win32 sanity check that
`TryGetWorkAreaDeviceUnits` resolves a non-empty work area for `(0,0)`.

## Decision: RTL audit findings

**Real bug found and fixed** - `DateInput`/`TimeInput` segment order was mirroring under RTL when
it should not. `Themes/DateInput.xaml` and `Themes/TimeInput.xaml`'s `PART_Segments` `StackPanel`
(built in code with segments and literal separators in a fixed reading order) had no local
`FlowDirection`, so it inherited the control's own `FlowDirection` with no mirror boundary between
them. Verified with a pixel-rendered `RenderTargetBitmap` diagnostic: the four-digit year
segment's ink cluster moved from the end of the row to the start under
`FlowDirection=RightToLeft` on the unpinned template. Fixed by pinning
`FlowDirection="LeftToRight"` directly on `PART_Segments` in both files, leaving the surrounding
`Border`/control free to carry the ambient `FlowDirection` that `NaviusDateInput
.OnSegmentPreviewKeyDown` / `NaviusTimeInput`'s equivalent already read correctly for arrow-key
direction. Re-verified with the same pixel diagnostic: segment order and ink-cluster layout are
now byte-for-byte identical between LTR and RTL. Regression tests added in `DateInputTests.cs`
and `TimeInputTests.cs` (`PartSegments_FlowDirectionPinnedToLeftToRight_...`,
`PartSegments_ChildOrder_UnaffectedByFlowDirection`).

**False-flag corrected, no code change** - `docs/parity/rating.md` claimed the half-fill `Clip`
(`Themes/Rating.xaml`'s `Fill` `Path`, `RectangleGeometry Rect="0,0,12,24"`) was "not
RTL-mirrored". Pixel-rendered verification shows this was wrong: nothing in `Themes/Rating.xaml`
opts any descendant out with its own local `FlowDirection`, so WPF's automatic mirroring (applied
once, as a whole, at whatever element the `FlowDirection` value is actually set on) already
reflects the `Clip` along with everything else the item renders. The doc section was corrected in
place, and a regression test locks in the correct behavior:
`RatingTests.HalfFill_ClipMirrorsUnderRtl_SolidInkOnOppositeSideFromLtr`.

**Swept, found already correct (no change)** - `Switch`'s thumb `TranslateTransform` (`Themes
/Switch.xaml`) and raw asymmetric `Path` glyphs (e.g. `Breadcrumb.xaml`'s separator chevron,
`Pagination.xaml`'s prev/next chevrons) all mirror correctly automatically, for the same reason
as Rating's clip: nothing locally overrides `FlowDirection` on these elements or their ancestors,
so the platform's whole-subtree mirror already reflects `RenderTransform`-driven positions and
`Path` `Data` the same way it reflects everything else. Verified for Switch and a synthetic
chevron `Path` via the same pixel-rendered diagnostic technique. `Slider`/`Progress`/`Meter` fill
direction and `Tabs`/`Accordion` chevrons were not independently re-audited pixel-by-pixel this
wave (see Residuals) but follow the same "no local `FlowDirection` override anywhere" pattern, so
are expected to already be correct on the same evidence.

**Confirmed already correct per the parity docs** - `DateInput`/`TimeInput`'s arrow-key mirroring
(`FlowDirection == FlowDirection.RightToLeft` checks in `OnSegmentPreviewKeyDown`) and `Rating`'s
keyboard mirroring (`HandleKey`) were already correct; not touched.

`apps/Navius.Wpf.Gallery/Pages/RtlPage.xaml(.cs)` added (self-contained, no nav wiring,
`AutomationId="RtlDemo"` on the `FlowDirection=RightToLeft` panel) hosting Slider, Progress,
Rating, DateInput, Tabs, and Breadcrumb for manual inspection.

## Residuals

- The pixel-rendered verification technique (render a control to a `RenderTargetBitmap` under
  both `FlowDirection` values and diff the ink) was applied to Rating, Switch, DateInput, and a
  synthetic chevron `Path`, but not exhaustively to every control the task's bug-class list named
  (`Slider`/`Progress`/`Meter` fill direction, `Tabs`/`Accordion` chevrons, `Sortable`). Given the
  consistent "no local `FlowDirection` override anywhere in this codebase" finding across every
  control actually checked, a full per-control pixel sweep is very likely to turn up nothing
  further, but it was not run for every family this wave.
- `NaviusOneTimePasswordField`, `MaskedInput`, `CurrencyInput`, and `NumberField` are other
  segment/digit-oriented controls that plausibly share DateInput/TimeInput's "fixed reading-order
  layout, not bidi-mirrored" requirement. Not in this wave's explicit scope (only
  DateInput/TimeInput were named) and not touched, to avoid unrequested edits to families owned by
  the concurrent parity auditors. Worth a follow-up audit.
- No live multi-monitor hardware was available to visually confirm `NaviusAnchoredPopup`'s
  placement on a real secondary monitor with a different DPI scale; verification is at the unit
  level (`MonitorWorkAreaTests`) plus a real (single-monitor) `TryGetWorkAreaDeviceUnits` Win32
  call.
