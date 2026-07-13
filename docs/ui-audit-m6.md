# M6 Styled-Layer Parity Audit (Navius.Wpf.Ui)

Date: 2026-07-09

Scope: all 20 styled items in `src/Navius.Wpf.Ui/` (18 folder-based controls plus the two
XAML-only theme files `Themes/Table.xaml` and `Themes/Typography.xaml`). Each item was checked
adversarially against four disciplines: token sourcing via `DynamicResource`, template-part-name
agreement between C# and the matching `Themes/<Name>.xaml`, real runtime reduced-motion guards on
every animation, and the "TwoWay binding to a get-only CLR property" bug class already fixed once in
Pagination by a sibling audit.

This is an audit report, not an architectural decision, so it does not use the `docs/adr/000X-*.md`
ADR format (those are all decision records); it lives at `docs/ui-audit-m6.md` instead.

## Summary

- Confirmed issues found and fixed: 1.
  - Sidebar: `AnimateWidth` ran its collapse/expand width animation with no reduced-motion guard
    (`src/Navius.Wpf.Ui/Sidebar/NaviusSidebar.cs:136`). Fixed by short-circuiting to an instant width
    set via the existing `ReducedMotion.AnimationsEnabled` seam.
- Pagination bug-class verdict: the sibling fix is confirmed real (`Themes/Pagination.xaml:65-72`
  binds `IsChecked` `Mode="OneWay"` because `PaginationPageToken.Page` is get-only). No other
  instance of the same bug class exists across the 20 items.
- Token discipline: clean across all 20. Every Brush/Color/Thickness/CornerRadius is sourced via
  `DynamicResource`; the only color literals are idiomatic `Background="Transparent"`. No shadow
  effects, no hardcoded hex colors. `StaticResource` appears only for styles, converters, and
  templates (which do not need live theme swapping), never for a color/brush token.
- Template parts: all four `GetTemplateChild`/`[TemplatePart]` lookups resolve to a real `x:Name`.

## Per-item findings

### Alert (`Themes/Alert.xaml`, NaviusAlert/Title/Description)
Clean. Tokens via `DynamicResource`, no template parts looked up in C#, no animations, no TwoWay
bindings.

### Badge (`Themes/Badge.xaml`, NaviusBadge)
Clean. Variant triggers set only token-backed brushes. No parts, animations, or bindings at risk.

### Breadcrumb (`Themes/Breadcrumb.xaml`, NaviusBreadcrumb/Item/Separator)
Clean. All foreground/ring brushes via `DynamicResource` (`Breadcrumb.xaml:31,44,53,60`). No C#
template-part lookup, no animation, no TwoWay-to-readonly binding.

### ButtonGroup (`Themes/ButtonGroup.xaml`, NaviusButtonGroup/Item, RoundedClipConverter)
Clean. The `RoundedClipConverter` MultiBinding (`ButtonGroup.xaml:25`) is a geometry converter
binding, inherently OneWay. No parts or animations.

### Card (`Themes/Card.xaml`, NaviusCard + subparts)
Clean. Token-only surfaces, no parts, no animations, no bindings at risk.

### Carousel (`Themes/Carousel.xaml`, NaviusCarousel, CarouselEngine, CarouselDotConverter)
Token discipline clean (nav-button and dot brushes via `DynamicResource`; `Background="Transparent"`
on the dot button is idiomatic). Bindings: `SelectedIndex`/`AlternationIndex` feed a `DataTrigger`
MultiBinding and a `CommandParameter` (`Carousel.xaml:110,130,131`), all inherently OneWay, so no
pagination-class bug.
Follow-up completed: `NaviusCarousel.ShouldAnimate` snapshots the shared reduced-motion seam and
the container style now has separate animated and immediate trigger paths. Animated selection
keeps the outgoing slide visible only for its 150ms fade, then falls back to
`Visibility.Collapsed`; reduced-motion selection swaps visibility immediately. Collapsing the
inactive container also removes its descendants from the UIA Control/Content tree, covered by a
real Gallery/FlaUI regression test.

### CommandPalette (`Themes/CommandPalette.xaml`, NaviusCommandPalette + engine/item)
Clean. Declares `PART_Input` and `PART_List`; C# looks up `PART_Input`
(`NaviusCommandPalette.cs:115`) and both `x:Name`s exist (`CommandPalette.xaml:64,93`). `PART_List`
is declared for designers but not retrieved, which is valid. No animation owned here (the enter/exit
fade lives in the `NaviusOverlaySurfaceBase` primitive, out of scope). No TwoWay-to-readonly binding.

### Empty (`Themes/Empty.xaml`, NaviusEmpty + subparts)
Clean. Variant triggers use token brushes only. No parts, animations, or risky bindings.

### InputGroup (`Themes/InputGroup.xaml`, NaviusInputGroup)
Clean, and specifically cleared of the pagination bug class. `PART_Input` lookup
(`NaviusInputGroup.cs:81`) matches `x:Name="PART_Input"` (`InputGroup.xaml:44`). The inner TextBox
`Text="{Binding Text ... Mode=TwoWay ...}"` (`InputGroup.xaml:53`) targets
`NaviusInputGroup.Text`, which is a read-write dependency property with a real setter
(`NaviusInputGroup.cs:18-20,43-47`), so TwoWay is valid here, not the get-only bug.

### Item (`Themes/Item.xaml`, NaviusItem + subparts)
Clean. Variant/size triggers use token brushes; `Background`/`BorderBrush="Transparent"` defaults are
idiomatic. No parts, animations, or risky bindings.

### Kbd (`Themes/Kbd.xaml`, NaviusKbd/Group)
Clean. Muted token brushes only; `FontFamily="Consolas"` is a font, not a color. No parts or
animations.

### Pagination (`Themes/Pagination.xaml`, NaviusPagination + engine/token/converter)
Pagination bug-class fix VERIFIED REAL. `PaginationPageToken.Page` is get-only
(`PaginationPageToken.cs:13`); the `NaviusToggleGroupItem.IsChecked` MultiBinding is pinned
`Mode="OneWay"` with each child `Mode="OneWay"` and an explanatory comment
(`Pagination.xaml:65-72`), which is exactly the sibling fix. Token discipline clean, nav-button
chrome all `DynamicResource`. No template-part lookups in C#.

### Resizable (`Themes/Resizable.xaml`, NaviusResizablePanelGroup)
Clean. Splitter hover/drag triggers use `Navius.Ring` via `DynamicResource`
(`Resizable.xaml:27,31,50,54`); the vertical style is `BasedOn` the horizontal (a style
`StaticResource`, correct). No parts, animations, or risky bindings.

### Sidebar (`Themes/Sidebar.xaml`, NaviusSidebar + item/section/navigation)
CONFIRMED issue, FIXED. `NaviusSidebar.AnimateWidth` (`NaviusSidebar.cs:136`) began a 150ms width
`DoubleAnimation` on collapse/expand unconditionally, with only a design-time (`PresentationSource`
null) synchronous fallback and no reduced-motion guard, unlike the sibling looping animations in
Skeleton/Spinner which gate on `ReducedMotion.AnimationsEnabled`. Fixed by adding a
`!ReducedMotion.AnimationsEnabled` short-circuit that sets the target width instantly
(`NaviusSidebar.cs`, new guard just above the design-time branch) plus
`using Navius.Wpf.Ui.Internal;`. Template part `PART_Root` lookup (`NaviusSidebar.cs:101`) matches
`x:Name="PART_Root"` (`Sidebar.xaml:19`). Token discipline clean.
Testing note: the change is not independently exercisable by the xUnit harness because unit tests run
with no live `PresentationSource`, so the existing design-time branch already resolves width
synchronously there; the reduced-motion branch and the design-time branch are behaviourally identical
under test. No new test added for this reason.

### Skeleton (`Themes/Skeleton.xaml`, NaviusSkeleton)
Clean and exemplary. Shimmer pulse storyboard is gated by the `ShouldAnimate` trigger
(`Skeleton.xaml:22`), whose value is snapshotted from `ReducedMotion.AnimationsEnabled` at
construction (`NaviusSkeleton.cs:28`). Tokens via `DynamicResource`. No risky bindings.

### Spinner (`Themes/Spinner.xaml`, NaviusSpinner)
Clean and exemplary. Rotation storyboard gated by `ShouldAnimate` (`Spinner.xaml:34`), snapshotted
from `ReducedMotion.AnimationsEnabled` (`NaviusSpinner.cs:28`). Foreground via `DynamicResource`.

### SplitButton (`Themes/SplitButton.xaml`, NaviusSplitButton)
Clean. Declares `PART_Primary` and `PART_Chevron`; C# retrieves `PART_Primary`
(`NaviusSplitButton.cs:90`) and both `x:Name`s exist (`SplitButton.xaml:113,125`). `PART_Chevron` is
consumed declaratively (re-skinned in XAML) and correctly not retrieved in C#. Primary/chevron styles
use token brushes. No animations, no risky bindings.

### Timeline (`Themes/Timeline.xaml`, NaviusTimeline + subparts)
Clean. All variant brushes (`Primary`, `Secondary`, `Destructive`, `Muted` and their foregrounds) via
`DynamicResource` (`Timeline.xaml:47-82`). No parts, animations, or risky bindings.

### Table (`Themes/Table.xaml`, XAML-only, styles native ListView/GridView)
Clean against the token/design discipline. Row, header, and cell brushes all `DynamicResource`
(`Table.xaml:27-44,66-88`); `Background="Transparent"` on the cell is idiomatic. No animations, no
template-part C# (pure styling), no TwoWay-to-readonly binding. The `StaticResource` references
(`Table.xaml:10,11,14`) point at the styles defined in the same dictionary, which is correct.

### Typography (`Themes/Typography.xaml`, XAML-only, TextBlock styles)
Clean against the token/design discipline. Every `Foreground`/`Background` via `DynamicResource`
(`Typography.xaml:22,29,36,43,50,55,60,61,68`); `FontFamily="Consolas"` and numeric font sizes are
typography, not color tokens. No animations or bindings. `StaticResource` references
(`Typography.xaml:12,13`) are demo-block style lookups within the file, correct.

## Build and test status

Verified green after the transient breakage cleared. At audit time,
`dotnet build src/Navius.Wpf.Ui/Navius.Wpf.Ui.csproj` failed on an unrelated in-flight file from
the parallel theming agent (`src/Navius.Wpf.Primitives/Themes/Tokens.HighContrast.xaml`, an
illegal `--` inside an XML comment, MC3000). That agent corrected its file; on re-verification the
same day both `Navius.Wpf.Primitives` and `Navius.Wpf.Ui` build with 0 errors and the
`Ui`-filtered test run passes 118/118.

The one C# change in this audit (`NaviusSidebar.cs`) is a two-part edit: a `using Navius.Wpf.Ui.Internal;`
import and a guard clause calling `ReducedMotion.AnimationsEnabled`, both matching verbatim the
pattern in `NaviusSkeleton.cs` and `NaviusSpinner.cs`; it compiles and its layer's tests pass as
part of the green run above.
