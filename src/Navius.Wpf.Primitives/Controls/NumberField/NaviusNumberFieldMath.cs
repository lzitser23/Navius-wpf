using System.Globalization;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure, unit-testable clamp/step/format/parse math for <see cref="NaviusNumberField"/>, factored
/// out of the control so it is testable without an STA Application (mirrors NaviusSliderKeyboard's
/// factoring in the Slider family).
/// </summary>
public static class NaviusNumberFieldMath
{
    /// <summary>Step &lt;= 0 is coerced to 1, per the contract's context-sync rule.</summary>
    public static double CoerceStep(double step) => step <= 0 ? 1 : step;

    public static double Clamp(double value, double? min, double? max)
    {
        if (min is not null && value < min.Value)
        {
            value = min.Value;
        }

        if (max is not null && value > max.Value)
        {
            value = max.Value;
        }

        return value;
    }

    /// <summary>Adds delta to the current value (treated as 0 when null), clamped to [Min, Max].</summary>
    public static double Step(double? current, double delta, double? min, double? max) =>
        Clamp((current ?? 0) + delta, min, max);

    public static bool CanIncrement(double? value, double? max, bool disabled, bool readOnly) =>
        !disabled && !readOnly && (max is null || (value ?? 0) < max.Value);

    public static bool CanDecrement(double? value, double? min, bool disabled, bool readOnly) =>
        !disabled && !readOnly && (min is null || (value ?? 0) > min.Value);

    /// <summary>Formats with InvariantCulture, matching the contract's explicit culture choice.</summary>
    public static string Format(double? value, string? format) =>
        value is null ? string.Empty : value.Value.ToString(format ?? "G", CultureInfo.InvariantCulture);

    /// <summary>Parses with InvariantCulture + NumberStyles.Float, matching the contract's SetTextAsync.</summary>
    public static bool TryParse(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
