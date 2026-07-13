using System.Windows.Media;

namespace Navius.Wpf.Motion;

/// <summary>
/// An interruptible runtime spring driven by <see cref="CompositionTarget.Rendering"/>.
/// Unlike <see cref="SpringKeyframeBaker"/>, a ticker can be retargeted mid-flight: the
/// current position and velocity carry into the new solve, matching the web executor's
/// interruption semantics (navius-motion.js). Construct it with the starting spring,
/// origin, and target; it starts running immediately and calls <paramref name="onValue"/>
/// once per frame (plus once synchronously at construction) with the current position.
/// Call <see cref="Stop"/> or <see cref="Dispose"/> to detach the rendering hook. The optional
/// <see cref="MotionPolicy"/> defaults to Windows' animation preference; when motion is disabled,
/// the ticker reports its target synchronously and never attaches the rendering hook.
/// </summary>
public sealed class SpringTicker : IDisposable
{
    private readonly Spring _springTemplate;
    private readonly Action<double> _onValue;
    private readonly MotionPolicy _motionPolicy;
    private SpringSolver _solver;
    private double _elapsed;
    private double _value;
    private double _velocity;
    private bool _isRunning;
    private bool _disposed;
    private TimeSpan? _lastRenderingTime;

    /// <summary>
    /// Create a ticker running <paramref name="spring"/> from <paramref name="from"/> to
    /// <paramref name="to"/>. It starts immediately unless <paramref name="motionPolicy"/>
    /// disables motion, in which case it completes synchronously at the target.
    /// </summary>
    public SpringTicker(
        Spring spring,
        double from,
        double to,
        Action<double> onValue,
        MotionPolicy? motionPolicy = null)
    {
        _springTemplate = spring;
        _onValue = onValue;
        _motionPolicy = motionPolicy ?? MotionPolicy.System;
        _solver = new SpringSolver(spring, from, to);
        _value = from;
        _velocity = spring.InitialVelocity;

        if (!_motionPolicy.AnimationsEnabled)
        {
            CompleteAtTarget(notify: true);
            return;
        }

        _onValue(_value);

        _isRunning = true;
        CompositionTarget.Rendering += OnRendering;
    }

    /// <summary>The current (last computed) value.</summary>
    public double Value => _value;

    /// <summary>The current (last computed) velocity, in value units per second.</summary>
    public double Velocity => _velocity;

    /// <summary>The value this run is solving toward.</summary>
    public double Target => _solver.Target;

    /// <summary>True once the current run has settled (see <see cref="SpringSolver.IsAtRest"/>).</summary>
    public bool IsAtRest => _solver.IsAtRest(_value, _velocity);

    /// <summary>True while the rendering hook is attached.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Retarget to <paramref name="newTarget"/>, carrying the current position and
    /// velocity into a new solve (velocity-carrying retarget): motion stays continuous,
    /// no snap back to rest first.
    /// </summary>
    public void Retarget(double newTarget)
    {
        var spring = _springTemplate.WithInitialVelocity(_velocity);
        _solver = new SpringSolver(spring, _value, newTarget);
        _elapsed = 0;
        _lastRenderingTime = null;

        if (!_motionPolicy.AnimationsEnabled)
        {
            CompleteAtTarget(notify: true);
        }
    }

    /// <summary>Detach the rendering hook. Safe to call multiple times.</summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }
        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    /// <summary>Stop the ticker and release the rendering hook. Safe to call multiple times.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (e is RenderingEventArgs args)
        {
            if (_lastRenderingTime is { } last)
            {
                var elapsedSeconds = (args.RenderingTime - last).TotalSeconds;
                if (elapsedSeconds > 0)
                {
                    Step(elapsedSeconds);
                }
            }
            _lastRenderingTime = args.RenderingTime;
        }

        if (IsAtRest)
        {
            Stop();
        }
    }

    /// <summary>
    /// Advance the current solve by <paramref name="elapsedSeconds"/> and report the new
    /// value. Pure aside from updating this instance's state and invoking the callback,
    /// so tests can drive a ticker deterministically without a real rendering loop.
    /// </summary>
    internal void Step(double elapsedSeconds)
    {
        if (!_motionPolicy.AnimationsEnabled)
        {
            CompleteAtTarget(notify: false);
            return;
        }

        _elapsed += elapsedSeconds;
        _value = _solver.Position(_elapsed);
        _velocity = _solver.Velocity(_elapsed);
        _onValue(_value);
    }

    private void CompleteAtTarget(bool notify)
    {
        var changed = _value != _solver.Target || _velocity != 0;
        _value = _solver.Target;
        _velocity = 0;
        _elapsed = 0;
        _lastRenderingTime = null;
        if (notify || changed)
        {
            _onValue(_value);
        }
        Stop();
    }
}
