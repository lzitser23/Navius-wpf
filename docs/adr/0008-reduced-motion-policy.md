# ADR-0008: Native reduced-motion execution policy

Status: accepted

## Context

`Navius.Wpf.Motion` exposed two WPF execution tiers, `SpringTicker` and
`SpringKeyframeBaker`, but both always emitted the full spring. The package's micro presets
carried `MicroReduce.Collapse` and `MicroReduce.OpacityOnly` as web-oriented data without a WPF
resolution path. Consumers therefore had to read Windows settings and reinterpret preset policy
themselves.

The styled layer already guards its own animations through one internal preference seam. The
Motion package must remain standalone, so moving that helper into Primitives or making Motion and
Ui depend on one another would invert the existing package boundaries.

## Decision

Add an immutable, injectable `MotionPolicy` to `Navius.Wpf.Motion`.

- `MotionPolicy.System` reads `SystemParameters.ClientAreaAnimation` each time it is evaluated.
- A public constructor accepts `Func<bool>`, giving tests and applications a deterministic or
  app-specific preference without mutating process-wide state.
- `SpringTicker` accepts an optional policy. When motion is disabled it reports the target
  synchronously, has zero velocity, and never attaches `CompositionTarget.Rendering`. If the
  preference changes during a live run, the next step completes and detaches it.
- `SpringKeyframeBaker.Bake` accepts an optional policy. Reduced motion returns one discrete target
  keyframe at time zero.
- `MotionPolicy.Resolve(MicroPreset)` preserves the full preset when enabled, returns no frames for
  `Collapse`, and strips every non-opacity property for `OpacityOnly`.

Ui keeps its internal reduced-motion seam because vendorable styled controls cannot acquire a
package dependency on Motion without breaking source-closure distribution. Both seams read the
same Windows setting and expose injectable providers at their respective execution boundaries.

## Carousel consequence

Carousel's container style has two explicit paths. With animation enabled, the outgoing slide
fades for 150ms and then becomes `Collapsed`; with reduced motion, selection swaps visibility
immediately. `Collapsed` is required, rather than opacity and hit-testing alone, because inactive
descendants must leave the UI Automation Control and Content views.

## Consequences

- Existing calls remain source compatible because policy parameters are optional.
- Reduced motion changes the callback contract intentionally: a reduced ticker reports the final
  value once instead of reporting the origin and starting a render loop.
- Preference evaluation is live, but no global event subscription is required.
- Tests cover enabled, disabled, and mid-run preference changes without changing machine settings.
