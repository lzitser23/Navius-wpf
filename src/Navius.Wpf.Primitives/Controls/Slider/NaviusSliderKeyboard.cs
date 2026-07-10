using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure keyboard-to-value decision logic for <see cref="NaviusSlider"/>, split out from the
/// control so the parity contract's keyboard table (Arrow/Shift+Arrow/PageUp/PageDown/Home/End)
/// can be unit tested directly instead of through simulated WPF KeyEventArgs.
/// </summary>
public static class NaviusSliderKeyboard
{
    /// <summary>
    /// Computes the effective large step per the contract heuristic: the explicit LargeStep when
    /// set (&gt; 0), otherwise max(step, 10% of the range snapped to step).
    /// </summary>
    public static double ComputeEffectiveLargeStep(double largeStep, double step, double minimum, double maximum)
    {
        if (largeStep > 0)
        {
            return largeStep;
        }

        var effectiveStep = step > 0 ? step : 1;
        var tenPercent = (maximum - minimum) * 0.1;
        var snapped = Math.Round(tenPercent / effectiveStep) * effectiveStep;
        return Math.Max(effectiveStep, snapped);
    }

    /// <summary>
    /// Resolves a key press to a target value, clamped to [minimum, maximum]. Returns false for
    /// keys outside the contract's table, leaving <paramref name="targetValue"/> unchanged.
    /// Arrow direction flips under <paramref name="isDirectionReversed"/> (RTL/Inverted); Page/Home/End
    /// do not, per the contract.
    /// </summary>
    public static bool TryGetTargetValue(
        Key key,
        bool shiftPressed,
        bool isDirectionReversed,
        double value,
        double minimum,
        double maximum,
        double step,
        double effectiveLargeStep,
        out double targetValue)
    {
        var small = step > 0 ? step : 1;
        var large = effectiveLargeStep > 0 ? effectiveLargeStep : small;

        switch (key)
        {
            case Key.Right:
            case Key.Up:
                targetValue = value + (isDirectionReversed ? -1 : 1) * (shiftPressed ? large : small);
                break;
            case Key.Left:
            case Key.Down:
                targetValue = value + (isDirectionReversed ? 1 : -1) * (shiftPressed ? large : small);
                break;
            case Key.PageUp:
                targetValue = value + large;
                break;
            case Key.PageDown:
                targetValue = value - large;
                break;
            case Key.Home:
                targetValue = minimum;
                break;
            case Key.End:
                targetValue = maximum;
                break;
            default:
                targetValue = value;
                return false;
        }

        targetValue = Math.Clamp(targetValue, minimum, maximum);
        return true;
    }
}
