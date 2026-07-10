namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure keyboard step math backing NaviusColorPicker's Area/Hue/Alpha tracks (see
/// docs/parity/color-picker.md "Keyboard"). Kept separate from NaviusColorPicker so the
/// arrow/page/home/end increments are unit-testable without a live Control.
///
/// Deviation: the contract tracks a separate "_atMax" flag so End on the hue slider displays
/// 360 even though the underlying model wraps to 0; this port wraps Hue uniformly via modulo
/// (StepHue(359, increase: true, shift: false) == 0) and does not special-case the display-only
/// 360 endpoint. Home/End still set the model to exactly 0/360.
/// </summary>
public static class ColorPickerSteps
{
    public static double StepSaturation(double s, bool increase, bool shift) =>
        Clamp01(s + Sign(increase) * (shift ? 0.1 : 0.01));

    public static double StepBrightness(double v, bool increase, bool shift) =>
        Clamp01(v + Sign(increase) * (shift ? 0.1 : 0.01));

    public static double StepBrightnessLarge(double v, bool increase) => Clamp01(v + Sign(increase) * 0.1);

    public static double StepHue(double h, bool increase, bool shift) => Wrap360(h + Sign(increase) * (shift ? 10 : 1));

    public static double StepHueLarge(double h, bool increase) => Wrap360(h + Sign(increase) * 10);

    public static double StepAlpha(double a, bool increase, bool shift) =>
        Clamp01(a + Sign(increase) * (shift ? 0.1 : 0.01));

    public static double StepAlphaLarge(double a, bool increase) => Clamp01(a + Sign(increase) * 0.1);

    private static int Sign(bool increase) => increase ? 1 : -1;

    private static double Wrap360(double h) => ((h % 360) + 360) % 360;

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}
