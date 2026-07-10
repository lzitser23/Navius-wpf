using System.Windows;

namespace Navius.Wpf.Ui.Internal;

/// <summary>
/// Shared reduced-motion guard for the looping animations in this project (Skeleton's shimmer
/// pulse, Spinner's rotation). Both must respect the OS "reduce animations" preference
/// (SystemParameters.ClientAreaAnimation) instead of looping forever unconditionally. Factored
/// into one seam, rather than each control reading SystemParameters directly, so unit tests can
/// flip the preference without touching real OS settings.
/// </summary>
public static class ReducedMotion
{
    private static Func<bool>? _animationsEnabledOverride;

    /// <summary>True when looping animations should run: SystemParameters.ClientAreaAnimation, unless overridden for tests.</summary>
    public static bool AnimationsEnabled => (_animationsEnabledOverride ?? DefaultCheck)();

    private static bool DefaultCheck() => SystemParameters.ClientAreaAnimation;

    /// <summary>Test-only seam: replace the check. Pass null to restore the real SystemParameters read.</summary>
    public static void SetTestOverride(Func<bool>? animationsEnabled) => _animationsEnabledOverride = animationsEnabled;
}
