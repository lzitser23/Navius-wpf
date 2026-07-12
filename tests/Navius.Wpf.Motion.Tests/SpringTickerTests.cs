namespace Navius.Wpf.Motion.Tests;

/// <summary>
/// Drives <see cref="SpringTicker"/> via its internal <c>Step(elapsedSeconds)</c> method
/// (see <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/> in
/// src/Navius.Wpf.Motion/Properties/AssemblyInfo.cs) so these tests never need a real
/// CompositionTarget.Rendering loop to pump frames.
/// </summary>
public class SpringTickerTests
{
    private static readonly MotionPolicy FullMotion = new(() => true);

    [StaFact]
    public void Reduced_motion_completes_synchronously_without_starting_a_render_loop()
    {
        var values = new List<double>();

        using var ticker = new SpringTicker(
            Spring.Default,
            from: 0,
            to: 10,
            values.Add,
            new MotionPolicy(() => false));

        Assert.False(ticker.IsRunning);
        Assert.True(ticker.IsAtRest);
        Assert.Equal(10, ticker.Value);
        Assert.Equal(0, ticker.Velocity);
        Assert.Equal([10d], values);
    }

    [StaFact]
    public void Preference_switch_to_reduced_motion_completes_the_active_run()
    {
        var enabled = true;
        var values = new List<double>();
        using var ticker = new SpringTicker(
            Spring.Default,
            from: 0,
            to: 10,
            values.Add,
            new MotionPolicy(() => enabled));

        ticker.Step(0.05);
        enabled = false;
        ticker.Step(0.05);

        Assert.False(ticker.IsRunning);
        Assert.True(ticker.IsAtRest);
        Assert.Equal(10, ticker.Value);
        Assert.Equal(10, values[^1]);
    }

    [StaFact]
    public void Converges_to_the_target_as_steps_accumulate()
    {
        var values = new List<double>();
        using var ticker = new SpringTicker(Spring.Snappy, 0, 1, values.Add, FullMotion);

        // Drive well past the settle duration in small, evenly spaced steps.
        var solver = new SpringSolver(Spring.Snappy, 0, 1);
        var frame = 1.0 / 60.0;
        for (var t = 0.0; t < solver.SettleDuration + 1.0; t += frame)
        {
            ticker.Step(frame);
        }

        Assert.True(ticker.IsAtRest);
        Assert.Equal(1, ticker.Value, 1e-2);
        Assert.True(double.IsFinite(ticker.Value));
        Assert.True(double.IsFinite(ticker.Velocity));
        Assert.True(values.Count > 1);
        Assert.Equal(ticker.Value, values[^1]);
    }

    [StaFact]
    public void Retarget_keeps_the_value_continuous()
    {
        using var ticker = new SpringTicker(Spring.Default, 0, 1, _ => { }, FullMotion);

        // Run partway through the first solve, not to completion.
        ticker.Step(0.05);
        var valueBeforeRetarget = ticker.Value;

        ticker.Retarget(2);

        // Retargeting recomputes the solve's origin from the current value; before any
        // further Step() call, the reported value must not have jumped.
        Assert.Equal(valueBeforeRetarget, ticker.Value, 1e-12);
        Assert.Equal(2, ticker.Target);
    }

    [StaFact]
    public void Retarget_carries_velocity_into_the_first_post_retarget_step()
    {
        using var ticker = new SpringTicker(Spring.Default, 0, 1, _ => { }, FullMotion);

        // Run partway through so the ticker has picked up nonzero velocity.
        ticker.Step(0.05);
        var velocityAtRetarget = ticker.Velocity;
        Assert.NotEqual(0, velocityAtRetarget);

        ticker.Retarget(2);
        var valueAtRetarget = ticker.Value;

        // A tiny post-retarget step: over a small enough interval, the analytic
        // velocity term dominates the spring's restoring-force term, so the value
        // should move in the direction the carried velocity implies.
        ticker.Step(0.001);

        var delta = ticker.Value - valueAtRetarget;
        Assert.Equal(Math.Sign(velocityAtRetarget), Math.Sign(delta));
    }

    [StaFact]
    public void Stop_detaches_the_rendering_hook_and_freezes_the_value()
    {
        using var ticker = new SpringTicker(Spring.Default, 0, 1, _ => { }, FullMotion);
        ticker.Step(0.05);
        var valueBeforeStop = ticker.Value;

        ticker.Stop();

        Assert.False(ticker.IsRunning);
        Assert.Equal(valueBeforeStop, ticker.Value);
    }
}
