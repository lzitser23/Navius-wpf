using System.Runtime.CompilerServices;

// Exposes the internal series-mapping/palette-reapplication logic to the test project so it
// can be unit-tested without a live chart control.
[assembly: InternalsVisibleTo("Navius.Wpf.Charts.Tests")]
