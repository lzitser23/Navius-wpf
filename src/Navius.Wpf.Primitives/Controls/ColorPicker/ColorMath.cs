using System.Globalization;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Pure HSV/RGB/hex/rgb/hsl conversion and parse/format engine backing NaviusColorPicker,
/// mirroring the contract's internal (non-rendering) ColorMath.cs -- no WPF dependency, fully
/// unit-testable (see docs/parity/color-picker.md "Parts": "Non-rendering shared-state/math
/// files"). H is degrees [0,360), S/V/A are fractions [0,1].
/// </summary>
public static class ColorMath
{
    public static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        s = Clamp01(s);
        v = Clamp01(v);

        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60 % 2) - 1));
        var m = v - c;

        var (r1, g1, b1) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return (ToByte(r1 + m), ToByte(g1 + m), ToByte(b1 + m));
    }

    public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        double h;
        if (delta == 0)
        {
            h = 0;
        }
        else if (max == rf)
        {
            h = 60 * (((gf - bf) / delta) % 6);
        }
        else if (max == gf)
        {
            h = 60 * (((bf - rf) / delta) + 2);
        }
        else
        {
            h = 60 * (((rf - gf) / delta) + 4);
        }

        if (h < 0)
        {
            h += 360;
        }

        var s = max == 0 ? 0 : delta / max;
        var v = max;

        return (h, s, v);
    }

    public static string ToHex(byte r, byte g, byte b, byte? a = null) =>
        a is null
            ? $"#{r:X2}{g:X2}{b:X2}"
            : $"#{r:X2}{g:X2}{b:X2}{a.Value:X2}";

    /// <summary>Formats HSVA as the requested output string: hex|rgb|rgba|hsl|hsla.</summary>
    public static string Format(double h, double s, double v, double a, string format)
    {
        var (r, g, b) = HsvToRgb(h, s, v);
        var alphaByte = ToByte(Clamp01(a));

        return format.ToLowerInvariant() switch
        {
            "rgb" => $"rgb({r}, {g}, {b})",
            "rgba" => $"rgba({r}, {g}, {b}, {FormatAlpha(a)})",
            "hsl" => FormatHsl(r, g, b, includeAlpha: false, a),
            "hsla" => FormatHsl(r, g, b, includeAlpha: true, a),
            _ => ToHex(r, g, b),
        };
    }

    /// <summary>Attempts to parse hex (#rgb/#rrggbb/#rrggbbaa), rgb()/rgba(), or hsl()/hsla() into HSVA. Returns false on failure.</summary>
    public static bool TryParse(string? value, out double h, out double s, out double v, out double a)
    {
        h = s = v = 0;
        a = 1;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();

        if (value.StartsWith('#'))
        {
            return TryParseHex(value, out h, out s, out v, out a);
        }

        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseRgbFunction(value, out h, out s, out v, out a);
        }

        if (value.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseHslFunction(value, out h, out s, out v, out a);
        }

        return false;
    }

    private static bool TryParseHex(string value, out double h, out double s, out double v, out double a)
    {
        h = s = v = 0;
        a = 1;

        var hex = value[1..];
        if (hex.Length is 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }

        if (hex.Length is not (6 or 8))
        {
            return false;
        }

        if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        if (hex.Length == 8)
        {
            if (!byte.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var alpha))
            {
                return false;
            }

            a = alpha / 255.0;
        }

        (h, s, v) = RgbToHsv(r, g, b);
        return true;
    }

    private static bool TryParseRgbFunction(string value, out double h, out double s, out double v, out double a)
    {
        h = s = v = 0;
        a = 1;

        var parts = ExtractParenParts(value);
        if (parts is null || parts.Length < 3)
        {
            return false;
        }

        if (!TryParseByte(parts[0], out var r) || !TryParseByte(parts[1], out var g) || !TryParseByte(parts[2], out var b))
        {
            return false;
        }

        if (parts.Length >= 4 && !TryParseUnit(parts[3], out a))
        {
            return false;
        }

        (h, s, v) = RgbToHsv(r, g, b);
        return true;
    }

    private static bool TryParseHslFunction(string value, out double h, out double s, out double v, out double a)
    {
        h = s = v = 0;
        a = 1;

        var parts = ExtractParenParts(value);
        if (parts is null || parts.Length < 3)
        {
            return false;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var hue))
        {
            return false;
        }

        if (!TryParsePercent(parts[1], out var sl) || !TryParsePercent(parts[2], out var l))
        {
            return false;
        }

        if (parts.Length >= 4 && !TryParseUnit(parts[3], out a))
        {
            return false;
        }

        var (r, g, b) = HslToRgb(hue, sl, l);
        (h, s, v) = RgbToHsv(r, g, b);
        return true;
    }

    private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        s = Clamp01(s);
        l = Clamp01(l);

        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60 % 2) - 1));
        var m = l - c / 2;

        var (r1, g1, b1) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return (ToByte(r1 + m), ToByte(g1 + m), ToByte(b1 + m));
    }

    private static string FormatHsl(byte r, byte g, byte b, bool includeAlpha, double a)
    {
        var (h, s, l) = RgbToHsl(r, g, b);
        var hue = Math.Round(h);
        var sat = Math.Round(s * 100);
        var light = Math.Round(l * 100);

        return includeAlpha
            ? $"hsla({hue}, {sat}%, {light}%, {FormatAlpha(a)})"
            : $"hsl({hue}, {sat}%, {light}%)";
    }

    private static (double H, double S, double L) RgbToHsl(byte r, byte g, byte b)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        var l = (max + min) / 2;

        if (delta == 0)
        {
            return (0, 0, l);
        }

        var s = delta / (1 - Math.Abs(2 * l - 1));

        double h;
        if (max == rf)
        {
            h = 60 * (((gf - bf) / delta) % 6);
        }
        else if (max == gf)
        {
            h = 60 * (((bf - rf) / delta) + 2);
        }
        else
        {
            h = 60 * (((rf - gf) / delta) + 4);
        }

        if (h < 0)
        {
            h += 360;
        }

        return (h, s, l);
    }

    private static string[]? ExtractParenParts(string value)
    {
        var open = value.IndexOf('(');
        var close = value.IndexOf(')');
        if (open < 0 || close < 0 || close < open)
        {
            return null;
        }

        return value[(open + 1)..close]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool TryParseByte(string text, out byte value)
    {
        value = 0;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return false;
        }

        value = ToByte(d / 255.0);
        return true;
    }

    private static bool TryParsePercent(string text, out double value)
    {
        value = 0;
        text = text.TrimEnd('%');
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return false;
        }

        value = Clamp01(d / 100.0);
        return true;
    }

    private static bool TryParseUnit(string text, out double value)
    {
        value = 1;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return false;
        }

        value = Clamp01(d);
        return true;
    }

    private static string FormatAlpha(double a) => Math.Round(Clamp01(a), 2).ToString(CultureInfo.InvariantCulture);

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

    private static byte ToByte(double fraction) => (byte)Math.Clamp(Math.Round(fraction * 255), 0, 255);
}
