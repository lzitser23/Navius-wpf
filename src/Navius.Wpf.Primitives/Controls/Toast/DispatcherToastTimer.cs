using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Production <see cref="IToastTimer"/>: wraps a <see cref="DispatcherTimer"/>. DispatcherTimer
/// has no native pause, so Pause/Resume stop it and track the remaining time with a Stopwatch,
/// meaning a resumed toast counts down the time it had left rather than restarting its whole
/// window (contract: "Auto-close timer pauses on hover / focus-within / window-blur").
/// </summary>
public sealed class DispatcherToastTimer : IToastTimer
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private Action? _onElapsed;
    private TimeSpan _remaining;
    private bool _running;

    public DispatcherToastTimer(Dispatcher? dispatcher = null)
    {
        _timer = dispatcher is null ? new DispatcherTimer() : new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
        _timer.Tick += OnTick;
    }

    public void Start(TimeSpan duration, Action onElapsed)
    {
        ArgumentNullException.ThrowIfNull(onElapsed);
        Stop();

        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        _onElapsed = onElapsed;
        _remaining = duration;
        Restart();
    }

    public void Pause()
    {
        if (!_running)
        {
            return;
        }

        _timer.Stop();
        _stopwatch.Stop();
        _remaining -= _stopwatch.Elapsed;
        if (_remaining < TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
        }

        _running = false;
    }

    public void Resume()
    {
        if (_running || _onElapsed is null || _remaining <= TimeSpan.Zero)
        {
            return;
        }

        Restart();
    }

    public void Stop()
    {
        _timer.Stop();
        _stopwatch.Reset();
        _onElapsed = null;
        _running = false;
    }

    public void Dispose() => Stop();

    private void Restart()
    {
        _timer.Interval = _remaining;
        _stopwatch.Restart();
        _timer.Start();
        _running = true;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        _running = false;
        var callback = _onElapsed;
        _onElapsed = null;
        callback?.Invoke();
    }
}
