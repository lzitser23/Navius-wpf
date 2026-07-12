using System.Windows.Media.Animation;

namespace Navius.Wpf.Motion.Tests;

public class SpringKeyframeBakerTests
{
    [StaFact]
    public void Reduced_motion_emits_only_the_final_value_at_zero()
    {
        var animation = SpringKeyframeBaker.Bake(
            Spring.Bouncy,
            from: 0,
            to: 100,
            motionPolicy: new MotionPolicy(() => false));

        var keyframe = Assert.Single(animation.KeyFrames.Cast<DoubleKeyFrame>());
        Assert.Equal(100, keyframe.Value);
        Assert.Equal(TimeSpan.Zero, keyframe.KeyTime.TimeSpan);
    }

    [StaFact]
    public void Keyframe_times_are_strictly_increasing()
    {
        var animation = SpringKeyframeBaker.Bake(Spring.Bouncy, 0, 1);
        var times = animation.KeyFrames.Cast<DoubleKeyFrame>().Select(k => k.KeyTime.TimeSpan).ToArray();

        Assert.True(times.Length >= 2);
        for (var i = 1; i < times.Length; i++)
        {
            Assert.True(times[i] > times[i - 1], $"time[{i}] ({times[i]}) must be after time[{i - 1}] ({times[i - 1]})");
        }
    }

    [StaFact]
    public void First_keyframe_is_at_zero_with_the_origin_value()
    {
        var animation = SpringKeyframeBaker.Bake(Spring.Default, 5, 20);
        var first = Assert.IsType<LinearDoubleKeyFrame>(animation.KeyFrames[0]);

        Assert.Equal(TimeSpan.Zero, first.KeyTime.TimeSpan);
        Assert.Equal(5, first.Value);
    }

    [StaFact]
    public void Last_keyframe_lands_exactly_on_the_target_at_the_settle_duration()
    {
        var animation = SpringKeyframeBaker.Bake(Spring.Snappy, 0, 250);
        var last = Assert.IsType<LinearDoubleKeyFrame>(animation.KeyFrames[^1]);

        Assert.Equal(250, last.Value);

        var solver = new SpringSolver(Spring.Snappy, 0, 250);
        Assert.Equal(solver.SettleDuration, last.KeyTime.TimeSpan.TotalSeconds, 1e-9);
    }

    [StaFact]
    public void Initial_velocity_parameter_feeds_the_solve()
    {
        // A kicked spring (nonzero initial velocity) still starts exactly at "from" and
        // still lands exactly on "to", it just gets there on a different curve.
        var kicked = SpringKeyframeBaker.Bake(Spring.Default, 0, 1, initialVelocity: 5);
        var atRest = SpringKeyframeBaker.Bake(Spring.Default, 0, 1, initialVelocity: 0);

        Assert.Equal(0d, Assert.IsType<LinearDoubleKeyFrame>(kicked.KeyFrames[0]).Value);
        Assert.Equal(1d, Assert.IsType<LinearDoubleKeyFrame>(kicked.KeyFrames[^1]).Value);
        Assert.NotEqual(
            Assert.IsType<LinearDoubleKeyFrame>(atRest.KeyFrames[^1]).KeyTime.TimeSpan,
            Assert.IsType<LinearDoubleKeyFrame>(kicked.KeyFrames[^1]).KeyTime.TimeSpan);
    }

    [StaFact]
    public void Sampling_is_adaptive_dense_near_peak_speed_sparse_in_the_tail()
    {
        // From rest (no initial velocity): speed starts and ends near zero and peaks
        // mid-run, so step size should vary noticeably across the run, bottoming out
        // near the dense bound somewhere and topping out near the sparse bound at the
        // tail rather than staying at one fixed rate throughout.
        var animation = SpringKeyframeBaker.Bake(Spring.Default, 0, 1);
        var times = animation.KeyFrames.Cast<DoubleKeyFrame>().Select(k => k.KeyTime.TimeSpan.TotalSeconds).ToArray();

        var gaps = new double[times.Length - 1];
        for (var i = 0; i < gaps.Length; i++)
        {
            gaps[i] = times[i + 1] - times[i];
        }

        // Step size is not fixed: it adapts across the run.
        Assert.True(gaps.Max() - gaps.Min() > 0.005);

        // Somewhere the spring is moving fast enough to be sampled near the dense bound.
        Assert.True(gaps.Min() < 1.0 / 60.0);

        // Somewhere (the settled tail) it is sampled near the sparse bound. Not
        // necessarily the very last gap: that one may be a short leftover clamped down
        // to land exactly on the settle duration.
        Assert.True(gaps.Max() > 1.0 / 20.0);
    }

    [StaFact]
    public void Handles_a_from_equal_to_target_run_without_hanging()
    {
        // No displacement and no initial velocity: the spring is already at rest.
        var animation = SpringKeyframeBaker.Bake(Spring.Default, 3, 3);

        Assert.True(animation.KeyFrames.Count >= 2);
        Assert.Equal(3, Assert.IsType<LinearDoubleKeyFrame>(animation.KeyFrames[0]).Value);
        Assert.Equal(3, Assert.IsType<LinearDoubleKeyFrame>(animation.KeyFrames[^1]).Value);
    }
}
