using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>Hue (degrees) -&gt; the pure hue Color (S=1, V=1), for the Area's base fill and the hue thumb. Template-only, not unit-tested (thin wrapper over ColorMath.HsvToRgb).</summary>
public sealed class HueToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hue = value is double d ? d : 0.0;
        var (r, g, b) = ColorMath.HsvToRgb(hue, 1, 1);
        return Color.FromRgb(r, g, b);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>[Hue, Saturation, Brightness, Alpha] -&gt; the resulting Color, for swatch previews and the alpha track's gradient end-stop.</summary>
public sealed class HsvaToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var h = values.Length > 0 && values[0] is double hd ? hd : 0.0;
        var s = values.Length > 1 && values[1] is double sd ? sd : 0.0;
        var v = values.Length > 2 && values[2] is double vd ? vd : 1.0;
        var a = values.Length > 3 && values[3] is double ad ? ad : 1.0;

        var (r, g, b) = ColorMath.HsvToRgb(h, s, v);
        return Color.FromArgb((byte)Math.Clamp(Math.Round(a * 255), 0, 255), r, g, b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Preset color string (hex/rgb/hsl) -&gt; Color, for auto-generated swatch items bound to Colors.</summary>
public sealed class ColorStringToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && ColorMath.TryParse(text, out var h, out var s, out var v, out var a))
        {
            var (r, g, b) = ColorMath.HsvToRgb(h, s, v);
            return Color.FromArgb((byte)Math.Clamp(Math.Round(a * 255), 0, 255), r, g, b);
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
