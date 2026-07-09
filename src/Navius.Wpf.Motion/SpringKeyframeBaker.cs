using System.Windows.Media.Animation;

namespace Navius.Wpf.Motion;

/// <summary>
/// Bakes a solved <see cref="Spring"/> run into a WPF
/// <see cref="DoubleAnimationUsingKeyFrames"/> for the zero-interruption timeline tier
/// (storyboards, triggers). For interruptible runtime motion (retargeting mid-flight)
/// use <see cref="SpringTicker"/> instead.
/// </summary>
public static class SpringKeyframeBaker
{
    // Sampling bounds in seconds: dense while the spring is moving fast (as low as
    // ~4ms between samples, finer than a 240Hz frame), sparse once it settles into the
    // tail (up to ~66ms, a bit slower than a 15fps frame). The step for a given sample
    // is interpolated between the two based on how fast the spring is moving there
    // relative to its peak speed over the run.
    private const double MinStepSeconds = 1.0 / 240.0;
    private const double MaxStepSeconds = 1.0 / 15.0;

    // Samples taken across the run to find the peak speed used to scale the adaptive
    // step. Only used to size steps, not part of the emitted keyframes.
    private const int VelocityProfileSamples = 200;

    /// <summary>
    /// Bake <paramref name="spring"/> running from <paramref name="from"/> to
    /// <paramref name="to"/> into a keyframe animation. The first keyframe is at t = 0
    /// with value <paramref name="from"/>; the last keyframe lands exactly at
    /// <paramref name="to"/> at the solver's settle duration. Sample spacing is adaptive:
    /// dense while the spring is moving fast, sparse in the settled tail.
    /// </summary>
    public static DoubleAnimationUsingKeyFrames Bake(Spring spring, double from, double to, double initialVelocity = 0)
    {
        var effectiveSpring = spring.WithInitialVelocity(initialVelocity);
        var solver = new SpringSolver(effectiveSpring, from, to);
        var duration = solver.SettleDuration;
        if (duration <= 0)
        {
            duration = MinStepSeconds;
        }

        var peakSpeed = 0.0;
        for (var i = 0; i <= VelocityProfileSamples; i++)
        {
            var t = duration * i / VelocityProfileSamples;
            peakSpeed = Math.Max(peakSpeed, Math.Abs(solver.Velocity(t)));
        }
        if (peakSpeed <= 0)
        {
            // No motion at all (e.g. from == to and no initial velocity): peakSpeed
            // stays 0, so every step below already collapses to MaxStepSeconds, which
            // is fine, but guard the division anyway for clarity.
            peakSpeed = 1;
        }

        var animation = new DoubleAnimationUsingKeyFrames();
        var time = 0.0;
        while (true)
        {
            double value;
            if (time <= 0)
            {
                value = from;
            }
            else if (time >= duration)
            {
                value = to;
            }
            else
            {
                value = solver.Position(time);
            }

            animation.KeyFrames.Add(new LinearDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(time))));

            if (time >= duration)
            {
                break;
            }

            var speedRatio = Math.Clamp(Math.Abs(solver.Velocity(time)) / peakSpeed, 0, 1);
            var step = MinStepSeconds + (MaxStepSeconds - MinStepSeconds) * (1 - speedRatio);
            time = Math.Min(time + step, duration);
        }

        return animation;
    }
}
