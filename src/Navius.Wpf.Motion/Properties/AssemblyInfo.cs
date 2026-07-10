using System.Runtime.CompilerServices;

// Exposes internal members (SpringTicker.Step) to the test project so tests can drive
// the ticker's per-frame update without a real CompositionTarget.Rendering loop.
[assembly: InternalsVisibleTo("Navius.Wpf.Motion.Tests")]
