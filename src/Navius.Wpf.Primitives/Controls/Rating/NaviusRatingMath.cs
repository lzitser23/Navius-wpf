namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure, unit-testable step/clamp/select math for <see cref="NaviusRating"/>, factored out of the
/// control so it is testable without an STA Application (mirrors NaviusSliderKeyboard's factoring
/// in the Slider family).
/// </summary>
public static class NaviusRatingMath
{
    public static decimal Step(bool allowHalf) => allowHalf ? 0.5m : 1m;

    /// <summary>ArrowUp/ArrowRight (or mirrored under rtl): increase by Step, clamped to Max.</summary>
    public static decimal StepUp(decimal? current, bool allowHalf, int max)
    {
        var next = (current ?? 0m) + Step(allowHalf);
        return Math.Min(next, max);
    }

    /// <summary>
    /// ArrowDown/ArrowLeft (or mirrored): decrease by Step; clears to null (unrated) when the
    /// result would fall below Step and AllowClear is set, otherwise floors at Step.
    /// </summary>
    public static decimal? StepDown(decimal? current, bool allowHalf, bool allowClear)
    {
        var step = Step(allowHalf);
        var next = (current ?? 0m) - step;

        if (next < step)
        {
            return allowClear ? null : step;
        }

        return next;
    }

    /// <summary>Digit 1-9: jump directly to that value, clamped to Max.</summary>
    public static decimal Digit(int digit, int max) => Math.Min(digit, max);

    /// <summary>The 1-based star index that should hold focus: ceiling of value, clamped to [1, max].</summary>
    public static int FocusIndex(decimal? value, int max) =>
        value is null || value <= 0m ? 1 : Math.Clamp((int)Math.Ceiling(value.Value), 1, max);

    /// <summary>Click/select: re-selecting the current value clears it when AllowClear, otherwise selects it.</summary>
    public static decimal? Select(decimal candidate, decimal? current, bool allowClear) =>
        current == candidate && allowClear ? null : candidate;
}
