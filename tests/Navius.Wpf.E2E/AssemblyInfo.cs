// Each e2e test launches its own Gallery instance and some use global keyboard
// input (FlaUI Keyboard.Type), which lands in whichever window is foreground.
// Parallel test classes would race two app instances; run everything serially.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
