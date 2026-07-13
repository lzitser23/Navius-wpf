# Navius.Wpf.Motion

Standalone spring motion engine for WPF, sharing its closed-form solver with the web
Navius Motion package: bake springs to `DoubleAnimationUsingKeyFrames` for
zero-interruption playback, or drive values live with a retargetable `SpringTicker`,
plus named enter/exit and micro-interaction presets. `MotionPolicy.System` follows
`SystemParameters.ClientAreaAnimation`; both executors complete at the target without
attaching or emitting spatial motion when animations are disabled. Inject a policy backed by
your own `Func<bool>` for deterministic tests or app-specific preference handling.

`MotionPolicy.Resolve(microPreset)` applies each preset's `MicroReduce` contract: collapsed
presets stop, while `OpacityOnly` presets retain only their opacity frames. The package remains
standalone and has no reference to the primitives.

Documentation: https://wpf.naviusui.dev/guides/motion/
