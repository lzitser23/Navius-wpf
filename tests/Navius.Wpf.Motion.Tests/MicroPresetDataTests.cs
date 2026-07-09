namespace Navius.Wpf.Motion.Tests;

/// <summary>
/// Ported from the pure-data assertions of tests/Navius.Motion.Tests/MicroPresetTests.cs
/// in the web repo (the table itself, <see cref="MicroPresets"/>). The web test file also
/// covers Motion.Shake()/MotionPrograms.Micro, which are Blazor/WAAPI-runtime specific
/// and are not ported here (see task scope: only the pure preset data crosses over).
/// </summary>
public class MicroPresetDataTests
{
    [Fact]
    public void Table_is_ordered_and_names_derive_the_class_and_keyframes()
    {
        Assert.Equal(4, MicroPresets.All.Count);
        Assert.Equal(new[] { "shake", "pulse", "shimmer", "focus-glow" },
            MicroPresets.All.Select(p => p.Name).ToArray());

        Assert.Equal("motion-shake", MicroPresets.Shake.Class);
        Assert.Equal("navius-shake", MicroPresets.Shake.KeyframesName);
        Assert.Equal("navius-pulse-reduced", MicroPresets.Pulse.ReducedKeyframesName);
    }

    [Fact]
    public void Loop_and_reduced_behaviour_match_each_preset_intent()
    {
        // One-shot, transform-only: collapses to nothing under reduced motion.
        Assert.False(MicroPresets.Shake.Loop);
        Assert.Equal(MicroReduce.Collapse, MicroPresets.Shake.Reduce);

        // Looping status beat: keeps the opacity keyframes under reduced motion.
        Assert.True(MicroPresets.Pulse.Loop);
        Assert.Equal(MicroReduce.OpacityOnly, MicroPresets.Pulse.Reduce);

        // Ambient loops that rest under reduced motion.
        Assert.True(MicroPresets.Shimmer.Loop);
        Assert.Equal(MicroReduce.Collapse, MicroPresets.Shimmer.Reduce);
        Assert.True(MicroPresets.FocusGlow.Loop);
        Assert.Equal(MicroReduce.Collapse, MicroPresets.FocusGlow.Reduce);
    }
}
