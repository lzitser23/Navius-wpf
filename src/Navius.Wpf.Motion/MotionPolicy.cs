using System.Windows;

namespace Navius.Wpf.Motion;

/// <summary>
/// Resolves whether motion is allowed for one execution path. <see cref="System"/> follows
/// Windows' client-area animation preference; callers and tests can inject a deterministic
/// provider without changing the machine setting.
/// </summary>
public sealed class MotionPolicy
{
    private readonly Func<bool> _animationsEnabled;

    /// <summary>The default policy, backed by <see cref="SystemParameters.ClientAreaAnimation"/>.</summary>
    public static MotionPolicy System { get; } = new(() => SystemParameters.ClientAreaAnimation);

    /// <summary>Create a policy backed by a live preference provider.</summary>
    public MotionPolicy(Func<bool> animationsEnabled)
    {
        ArgumentNullException.ThrowIfNull(animationsEnabled);
        _animationsEnabled = animationsEnabled;
    }

    /// <summary>Whether spatial and decorative animation is currently allowed.</summary>
    public bool AnimationsEnabled => _animationsEnabled();

    /// <summary>
    /// Resolve a micro preset for the current preference. Full motion preserves the preset;
    /// reduced motion either stops it or keeps only its opacity beat according to
    /// <see cref="MicroPreset.Reduce"/>.
    /// </summary>
    public MicroPresetPlayback Resolve(MicroPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        if (AnimationsEnabled)
        {
            return new MicroPresetPlayback(true, preset.Keyframes);
        }

        if (preset.Reduce == MicroReduce.Collapse)
        {
            return new MicroPresetPlayback(false, Array.Empty<MicroFrame>());
        }

        var opacityFrames = preset.Keyframes
            .Where(frame => frame.Opacity is not null)
            .Select(frame => new MicroFrame(frame.Offset, Opacity: frame.Opacity))
            .ToArray();
        return new MicroPresetPlayback(opacityFrames.Length > 1, opacityFrames);
    }
}

/// <summary>The micro-preset frames that remain after applying a <see cref="MotionPolicy"/>.</summary>
public sealed record MicroPresetPlayback(bool ShouldAnimate, IReadOnlyList<MicroFrame> Keyframes);
