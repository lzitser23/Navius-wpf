namespace Navius.Wpf.Motion.Tests;

public class MotionPolicyTests
{
    [Fact]
    public void Injected_preference_is_evaluated_when_read()
    {
        var enabled = true;
        var policy = new MotionPolicy(() => enabled);

        Assert.True(policy.AnimationsEnabled);

        enabled = false;

        Assert.False(policy.AnimationsEnabled);
    }

    [Fact]
    public void Collapse_micro_preset_resolves_to_no_animation()
    {
        var playback = new MotionPolicy(() => false).Resolve(MicroPresets.Shake);

        Assert.False(playback.ShouldAnimate);
        Assert.Empty(playback.Keyframes);
    }

    [Fact]
    public void Opacity_only_micro_preset_drops_spatial_properties()
    {
        var playback = new MotionPolicy(() => false).Resolve(MicroPresets.Pulse);

        Assert.True(playback.ShouldAnimate);
        Assert.Equal(3, playback.Keyframes.Count);
        Assert.All(playback.Keyframes, frame =>
        {
            Assert.NotNull(frame.Opacity);
            Assert.Null(frame.Transform);
            Assert.Null(frame.BoxShadow);
            Assert.Null(frame.BackgroundPosition);
        });
    }

    [Fact]
    public void Enabled_policy_preserves_the_full_micro_preset()
    {
        var playback = new MotionPolicy(() => true).Resolve(MicroPresets.Pulse);

        Assert.True(playback.ShouldAnimate);
        Assert.Same(MicroPresets.Pulse.Keyframes, playback.Keyframes);
    }
}
