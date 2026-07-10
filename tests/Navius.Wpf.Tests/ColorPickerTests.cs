using System.Windows;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class ColorPickerTests
{
    static ColorPickerTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    // --- ColorMath: pure HSV/RGB/hex/rgb/hsl conversion + parse/format ---

    [Fact]
    public void ColorMath_HsvToRgb_PureRedAtZeroDegrees()
    {
        var (r, g, b) = ColorMath.HsvToRgb(0, 1, 1);

        Assert.Equal((255, 0, 0), (r, g, b));
    }

    [Fact]
    public void ColorMath_HsvToRgb_ZeroSaturationIsGray()
    {
        var (r, g, b) = ColorMath.HsvToRgb(200, 0, 0.5);

        Assert.Equal(r, g);
        Assert.Equal(g, b);
    }

    [Fact]
    public void ColorMath_RgbToHsv_RoundTripsPureRed()
    {
        var (h, s, v) = ColorMath.RgbToHsv(255, 0, 0);

        Assert.Equal(0, h);
        Assert.Equal(1, s);
        Assert.Equal(1, v);
    }

    [Fact]
    public void ColorMath_ToHex_FormatsUppercaseWithoutAlphaByDefault()
    {
        Assert.Equal("#FF0000", ColorMath.ToHex(255, 0, 0));
    }

    [Fact]
    public void ColorMath_ToHex_IncludesAlphaWhenProvided()
    {
        Assert.Equal("#FF000080", ColorMath.ToHex(255, 0, 0, 0x80));
    }

    [Fact]
    public void ColorMath_Format_Hex()
    {
        Assert.Equal("#FF0000", ColorMath.Format(0, 1, 1, 1, "hex"));
    }

    [Fact]
    public void ColorMath_Format_Rgb()
    {
        Assert.Equal("rgb(255, 0, 0)", ColorMath.Format(0, 1, 1, 1, "rgb"));
    }

    [Fact]
    public void ColorMath_Format_Rgba()
    {
        Assert.Equal("rgba(255, 0, 0, 0.5)", ColorMath.Format(0, 1, 1, 0.5, "rgba"));
    }

    [Fact]
    public void ColorMath_Format_Hsl()
    {
        Assert.Equal("hsl(0, 100%, 50%)", ColorMath.Format(0, 1, 1, 1, "hsl"));
    }

    [Theory]
    [InlineData("#F00")]
    [InlineData("#FF0000")]
    [InlineData("#FF0000FF")]
    public void ColorMath_TryParse_HexVariantsAllParseToPureRed(string hex)
    {
        var ok = ColorMath.TryParse(hex, out var h, out var s, out var v, out var a);

        Assert.True(ok);
        Assert.Equal(0, h);
        Assert.Equal(1, s);
        Assert.Equal(1, v);
        Assert.Equal(1, a);
    }

    [Fact]
    public void ColorMath_TryParse_HexWithAlphaExtractsAlpha()
    {
        var ok = ColorMath.TryParse("#FF000080", out _, out _, out _, out var a);

        Assert.True(ok);
        Assert.Equal(128 / 255.0, a, precision: 3);
    }

    [Fact]
    public void ColorMath_TryParse_RgbFunction()
    {
        var ok = ColorMath.TryParse("rgb(255, 0, 0)", out var h, out var s, out var v, out var a);

        Assert.True(ok);
        Assert.Equal(0, h);
        Assert.Equal(1, s);
        Assert.Equal(1, v);
        Assert.Equal(1, a);
    }

    [Fact]
    public void ColorMath_TryParse_RgbaFunctionExtractsAlpha()
    {
        var ok = ColorMath.TryParse("rgba(255, 0, 0, 0.5)", out _, out _, out _, out var a);

        Assert.True(ok);
        Assert.Equal(0.5, a);
    }

    [Fact]
    public void ColorMath_TryParse_HslFunction()
    {
        var ok = ColorMath.TryParse("hsl(0, 100%, 50%)", out var h, out var s, out var v, out _);

        Assert.True(ok);
        Assert.Equal(0, h);
        Assert.Equal(1, s);
        Assert.Equal(1, v);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#ZZZZZZ")]
    public void ColorMath_TryParse_InvalidReturnsFalse(string value)
    {
        Assert.False(ColorMath.TryParse(value, out _, out _, out _, out _));
    }

    // --- ColorPickerSteps: pure keyboard step math ---

    [Fact]
    public void Steps_Saturation_SmallStepIsOneHundredth()
    {
        Assert.Equal(0.51, ColorPickerSteps.StepSaturation(0.5, increase: true, shift: false), precision: 5);
    }

    [Fact]
    public void Steps_Saturation_ShiftStepIsOneTenth()
    {
        Assert.Equal(0.6, ColorPickerSteps.StepSaturation(0.5, increase: true, shift: true), precision: 5);
    }

    [Fact]
    public void Steps_Saturation_ClampsAtBounds()
    {
        Assert.Equal(0, ColorPickerSteps.StepSaturation(0, increase: false, shift: false));
        Assert.Equal(1, ColorPickerSteps.StepSaturation(1, increase: true, shift: false));
    }

    [Fact]
    public void Steps_Brightness_LargeStepIsOneTenth()
    {
        Assert.Equal(0.6, ColorPickerSteps.StepBrightnessLarge(0.5, increase: true), precision: 5);
    }

    [Fact]
    public void Steps_Hue_WrapsPastThreeSixty()
    {
        Assert.Equal(0, ColorPickerSteps.StepHue(359, increase: true, shift: false), precision: 5);
    }

    [Fact]
    public void Steps_Hue_WrapsBelowZero()
    {
        Assert.Equal(359, ColorPickerSteps.StepHue(0, increase: false, shift: false), precision: 5);
    }

    [Fact]
    public void Steps_Hue_LargeStepIsTenDegrees()
    {
        Assert.Equal(20, ColorPickerSteps.StepHueLarge(10, increase: true), precision: 5);
    }

    [Fact]
    public void Steps_Alpha_ClampsAtBounds()
    {
        Assert.Equal(0, ColorPickerSteps.StepAlpha(0, increase: false, shift: false));
        Assert.Equal(1, ColorPickerSteps.StepAlpha(1, increase: true, shift: false));
    }

    // --- NaviusColorPicker: control-level defaults, coercion, and model<->Value sync ---

    [StaFact]
    public void DefaultState_IsWhiteOpaqueHex()
    {
        var picker = new NaviusColorPicker();

        Assert.Equal(0, picker.Hue);
        Assert.Equal(0, picker.Saturation);
        Assert.Equal(1, picker.Brightness);
        Assert.Equal(1, picker.Alpha);
        Assert.Equal("hex", picker.Format);
        Assert.False(picker.AlphaEnabled);
        Assert.False(picker.ReadOnly);
        Assert.Equal("#FFFFFF", picker.HexValue);
    }

    [StaFact]
    public void Hue_CoercesIntoZeroToThreeSixtyRange()
    {
        var picker = new NaviusColorPicker { Hue = 400 };

        Assert.Equal(40, picker.Hue);
    }

    [StaFact]
    public void Hue_CoercesNegativeIntoRange()
    {
        var picker = new NaviusColorPicker { Hue = -10 };

        Assert.Equal(350, picker.Hue);
    }

    [StaFact]
    public void Saturation_CoercesIntoUnitRange()
    {
        var picker = new NaviusColorPicker { Saturation = 1.5 };

        Assert.Equal(1, picker.Saturation);
    }

    [StaFact]
    public void SettingModelUpdatesValueViaFormat()
    {
        var picker = new NaviusColorPicker { Hue = 0, Saturation = 1, Brightness = 1 };

        Assert.Equal("#FF0000", picker.Value);
    }

    [StaFact]
    public void SettingModelRaisesValueChanged()
    {
        var picker = new NaviusColorPicker();
        string? observed = null;
        picker.ValueChanged += (_, e) => observed = e.NewValue;

        picker.Hue = 120;
        picker.Saturation = 1;

        Assert.Equal("#00FF00", observed);
    }

    [StaFact]
    public void SettingValue_ParsesIntoModel()
    {
        var picker = new NaviusColorPicker { Value = "#0000FF" };

        Assert.Equal(240, picker.Hue);
        Assert.Equal(1, picker.Saturation);
        Assert.Equal(1, picker.Brightness);
    }

    [StaFact]
    public void SettingValue_IgnoresAlphaWhenAlphaDisabled()
    {
        var picker = new NaviusColorPicker { AlphaEnabled = false, Value = "#FF000080" };

        Assert.Equal(1, picker.Alpha);
    }

    [StaFact]
    public void SettingValue_AppliesAlphaWhenAlphaEnabled()
    {
        var picker = new NaviusColorPicker { AlphaEnabled = true, Value = "#FF000080" };

        Assert.Equal(128 / 255.0, picker.Alpha, precision: 3);
    }

    [StaFact]
    public void Format_Rgba_ReflectsInValue()
    {
        var picker = new NaviusColorPicker { Format = "rgba", AlphaEnabled = true, Alpha = 0.5, Hue = 0, Saturation = 1, Brightness = 1 };

        Assert.Equal("rgba(255, 0, 0, 0.5)", picker.Value);
    }

    // --- Automation peer ---

    [StaFact]
    public void AutomationPeer_ValueIsReadOnlyHex()
    {
        var picker = new NaviusColorPicker { Hue = 0, Saturation = 1, Brightness = 1 };
        var peer = new NaviusColorPickerAutomationPeer(picker);

        Assert.True(peer.IsReadOnly);
        Assert.Equal("#FF0000", peer.Value);
    }

    [StaFact]
    public void AutomationPeer_SetValueThrows()
    {
        var picker = new NaviusColorPicker();
        var peer = new NaviusColorPickerAutomationPeer(picker);

        Assert.Throws<InvalidOperationException>(() => peer.SetValue("#000000"));
    }

    [StaFact]
    public void AutomationPeer_GetPattern_SurfacesValuePattern()
    {
        // Regression (M6 audit): implementing IValueProvider alone does not surface it over UIA;
        // GetPattern must be overridden or the base implementation always returns null.
        var picker = new NaviusColorPicker();
        var peer = new NaviusColorPickerAutomationPeer(picker);

        var pattern = peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Value);

        Assert.Same(peer, pattern);
    }

    // --- Hue coercion: 0 and 360 are the same color but distinct thumb/model positions ---

    [StaFact]
    public void Hue_SettingExactly360_IsPreservedNotWrappedToZero()
    {
        // Regression (M6 audit): CoerceHue used to wrap 360 -> 0 unconditionally, making the End
        // key (Hue = 360) indistinguishable from Home (Hue = 0) and snapping the thumb to the
        // wrong edge of the track.
        var picker = new NaviusColorPicker { Hue = 360 };

        Assert.Equal(360.0, picker.Hue);
    }

    [StaFact]
    public void Hue_OtherOutOfRangeValues_StillWrapViaModulo()
    {
        var picker = new NaviusColorPicker { Hue = 400 };

        Assert.Equal(40.0, picker.Hue);
    }

    [StaFact]
    public void Hue_At360_HexValueMatchesHueAtZero()
    {
        // 0 and 360 are the same color: ColorMath.HsvToRgb re-normalizes internally.
        var atZero = new NaviusColorPicker { Hue = 0, Saturation = 1, Brightness = 1 };
        var at360 = new NaviusColorPicker { Hue = 360, Saturation = 1, Brightness = 1 };

        Assert.Equal(atZero.HexValue, at360.HexValue);
    }

    // --- AriaLabel: Area/Field/Swatches accessible names (previously hardcoded in the theme) ---

    [StaFact]
    public void AriaLabels_DefaultToContractStrings()
    {
        var picker = new NaviusColorPicker();

        Assert.Equal("Color", picker.AreaAriaLabel);
        Assert.Equal("Hex color", picker.FieldAriaLabel);
        Assert.Equal("Swatches", picker.SwatchesAriaLabel);
    }

    [StaFact]
    public void AriaLabels_AreConsumerOverridable()
    {
        var picker = new NaviusColorPicker
        {
            AreaAriaLabel = "Saturation and brightness",
            FieldAriaLabel = "Hex code",
            SwatchesAriaLabel = "Presets",
        };

        Assert.Equal("Saturation and brightness", picker.AreaAriaLabel);
        Assert.Equal("Hex code", picker.FieldAriaLabel);
        Assert.Equal("Presets", picker.SwatchesAriaLabel);
    }
}
