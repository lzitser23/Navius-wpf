using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Navius.Wpf.Primitives.Controls.Internal;

/// <summary>
/// Pure, WPF-free segment engine shared by NaviusDateInput and NaviusTimeInput (the web contracts'
/// shared Navius.Primitives.Common machinery: DateTimeSegment/SegmentMath/segment layout).
/// References only BCL types (System.Globalization, DateOnly/TimeOnly) so it is testable with
/// plain [Fact] and no STA Application, per docs/parity/date-input.md and time-input.md's WPF
/// strategy ("SegmentMath ... is pure C# with no Blazor dependency and ports essentially
/// unchanged").
/// </summary>
public enum SegmentUnit
{
    Year,
    Month,
    Day,
    Hour,
    Minute,
    Second,
    DayPeriod,
}

/// <summary>
/// WPF-free key abstraction for <see cref="SegmentMath.HandleKey"/>; callers (the NaviusDateInput/
/// NaviusTimeInput controls) map <c>System.Windows.Input.Key</c> onto this enum before calling in,
/// keeping the engine itself free of any WPF assembly reference.
/// </summary>
public enum SegmentKey
{
    None,
    ArrowUp,
    ArrowDown,
    PageUp,
    PageDown,
    Home,
    End,
    Backspace,
    Delete,
    ArrowLeft,
    ArrowRight,
    Digit,
    LetterA,
    LetterP,
}

public enum SegmentFocusMove
{
    None,
    Previous,
    Next,
}

/// <summary>Result of one <see cref="SegmentMath.HandleKey"/> call.</summary>
public readonly record struct SegmentKeyResult(bool Changed, SegmentFocusMove Focus, bool Handled)
{
    public static readonly SegmentKeyResult NoOp = new(false, SegmentFocusMove.None, false);
}

/// <summary>
/// Mutable per-segment state (contract's DateTimeSegment): a nullable typed value, the in-progress
/// type buffer, and the bounds/steps that drive <see cref="SegmentMath"/>. One instance per
/// editable segment in a layout; literal separators have no corresponding instance.
/// </summary>
public sealed class DateTimeSegment
{
    public DateTimeSegment(SegmentUnit unit, int min, int max, int arrowStep, int pageStep, int maxDigits)
    {
        Unit = unit;
        Min = min;
        Max = max;
        ArrowStep = arrowStep;
        PageStep = pageStep;
        MaxDigits = maxDigits;
    }

    public SegmentUnit Unit { get; }

    public int? Value { get; set; }

    public string TypeBuffer { get; set; } = string.Empty;

    public int Min { get; set; }

    public int Max { get; set; }

    public int ArrowStep { get; set; }

    public int PageStep { get; set; }

    public int MaxDigits { get; set; }

    public bool Filled => Value.HasValue;

    public DateTimeSegment Clone() =>
        new(Unit, Min, Max, ArrowStep, PageStep, MaxDigits) { Value = Value, TypeBuffer = TypeBuffer };
}

/// <summary>
/// Pure per-segment keyboard math (contract's SegmentMath.HandleKey), shared verbatim by
/// NaviusDateInput and NaviusTimeInput. Mutates <paramref name="segment"/> in place and reports
/// what the owning control should do next (recompose the value / move focus).
/// </summary>
public static class SegmentMath
{
    /// <summary>
    /// Handles one key against one segment. <paramref name="digit"/> is only consulted for
    /// <see cref="SegmentKey.Digit"/>. <paramref name="placeholderBasis"/> is the value an empty
    /// segment reveals on the first Arrow press (contract: "Empty segment lands on the placeholder
    /// basis ... rather than basis +/- step"). <paramref name="rtl"/> flips ArrowLeft/ArrowRight
    /// focus travel only; digit auto-advance always moves physically forward (contract's
    /// "ignoreDir: true").
    /// </summary>
    public static SegmentKeyResult HandleKey(DateTimeSegment segment, SegmentKey key, int digit, int placeholderBasis, bool rtl)
    {
        ArgumentNullException.ThrowIfNull(segment);

        switch (key)
        {
            case SegmentKey.ArrowUp:
                StepValue(segment, segment.ArrowStep, placeholderBasis);
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.ArrowDown:
                StepValue(segment, -segment.ArrowStep, placeholderBasis);
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.PageUp:
                StepValue(segment, segment.PageStep, placeholderBasis);
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.PageDown:
                StepValue(segment, -segment.PageStep, placeholderBasis);
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.Home:
                segment.Value = segment.Min;
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.End:
                segment.Value = segment.Max;
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.Backspace:
            case SegmentKey.Delete:
            {
                var wasFilled = segment.Filled;
                segment.Value = null;
                segment.TypeBuffer = string.Empty;
                return new SegmentKeyResult(wasFilled, SegmentFocusMove.None, true);
            }

            case SegmentKey.ArrowLeft:
                return new SegmentKeyResult(false, rtl ? SegmentFocusMove.Next : SegmentFocusMove.Previous, true);

            case SegmentKey.ArrowRight:
                return new SegmentKeyResult(false, rtl ? SegmentFocusMove.Previous : SegmentFocusMove.Next, true);

            case SegmentKey.LetterA when segment.Unit == SegmentUnit.DayPeriod:
                segment.TypeBuffer = string.Empty;
                if (segment.Value == 0)
                {
                    return SegmentKeyResult.NoOp with { Handled = true };
                }

                segment.Value = 0;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.LetterP when segment.Unit == SegmentUnit.DayPeriod:
                segment.TypeBuffer = string.Empty;
                if (segment.Value == 1)
                {
                    return SegmentKeyResult.NoOp with { Handled = true };
                }

                segment.Value = 1;
                return new SegmentKeyResult(true, SegmentFocusMove.None, true);

            case SegmentKey.Digit when segment.Unit == SegmentUnit.DayPeriod:
                return SegmentKeyResult.NoOp;

            case SegmentKey.Digit:
                return HandleDigit(segment, digit);

            default:
                return SegmentKeyResult.NoOp;
        }
    }

    /// <summary>Wraps value into [min, max] (contract's ArrowUp/Down wrap-at-bounds rule).</summary>
    public static int Wrap(int value, int min, int max)
    {
        var range = max - min + 1;
        if (range <= 0)
        {
            return min;
        }

        var offset = (value - min) % range;
        if (offset < 0)
        {
            offset += range;
        }

        return min + offset;
    }

    public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private static void StepValue(DateTimeSegment segment, int delta, int placeholderBasis)
    {
        if (segment.Value is null)
        {
            segment.Value = Clamp(placeholderBasis, segment.Min, segment.Max);
            return;
        }

        segment.Value = Wrap(segment.Value.Value + delta, segment.Min, segment.Max);
    }

    private static SegmentKeyResult HandleDigit(DateTimeSegment segment, int digit)
    {
        if (digit is < 0 or > 9)
        {
            return SegmentKeyResult.NoOp;
        }

        var candidateBuffer = segment.TypeBuffer + digit;
        if (!int.TryParse(candidateBuffer, NumberStyles.None, CultureInfo.InvariantCulture, out var candidateValue))
        {
            return SegmentKeyResult.NoOp;
        }

        if (candidateValue > segment.Max)
        {
            // A digit that would exceed Max restarts the buffer at that single digit.
            candidateBuffer = digit.ToString(CultureInfo.InvariantCulture);
            candidateValue = Math.Min(digit, segment.Max);
        }

        segment.TypeBuffer = candidateBuffer;
        segment.Value = Math.Max(candidateValue, segment.Min);

        var advance = candidateValue * 10 > segment.Max || segment.TypeBuffer.Length >= segment.MaxDigits;
        if (advance)
        {
            segment.TypeBuffer = string.Empty;
        }

        return new SegmentKeyResult(true, advance ? SegmentFocusMove.Next : SegmentFocusMove.None, true);
    }
}

public enum SegmentLayoutKind
{
    Editable,
    Literal,
}

/// <summary>One slot in a built layout: either an editable segment (Unit set, Literal empty) or a non-focusable separator (Literal set).</summary>
public readonly record struct SegmentLayoutItem(SegmentLayoutKind Kind, SegmentUnit Unit, string Literal);

/// <summary>
/// Builds culture-aware segment layouts from <see cref="DateTimeFormatInfo.ShortDatePattern"/> /
/// <see cref="DateTimeFormatInfo.ShortTimePattern"/> (contract: "culture-aware segment layout").
/// Ports .NET's own pattern strings directly, since (per time-input.md's WPF strategy)
/// <c>DateTimeFormatInfo</c> is available identically in WPF.
/// </summary>
public static class SegmentLayoutBuilder
{
    private const string UnitChars = "yMdhHmst";

    public static IReadOnlyList<SegmentLayoutItem> BuildDateLayout(CultureInfo culture, string granularity)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var allowed = granularity switch
        {
            "year" => new HashSet<SegmentUnit> { SegmentUnit.Year },
            "month" => new HashSet<SegmentUnit> { SegmentUnit.Year, SegmentUnit.Month },
            _ => new HashSet<SegmentUnit> { SegmentUnit.Year, SegmentUnit.Month, SegmentUnit.Day },
        };

        var items = new List<SegmentLayoutItem>();
        foreach (var (kind, text) in Tokenize(culture.DateTimeFormat.ShortDatePattern))
        {
            if (kind == '\0')
            {
                items.Add(new SegmentLayoutItem(SegmentLayoutKind.Literal, default, text));
                continue;
            }

            SegmentUnit? unit = kind switch
            {
                'y' => SegmentUnit.Year,
                'M' => SegmentUnit.Month,
                'd' => SegmentUnit.Day,
                _ => null,
            };

            if (unit is null || !allowed.Contains(unit.Value))
            {
                continue;
            }

            items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, unit.Value, string.Empty));
        }

        return TrimLiterals(items);
    }

    public static IReadOnlyList<SegmentLayoutItem> BuildTimeLayout(CultureInfo culture, string granularity, int hourCycle)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var allowed = granularity switch
        {
            "hour" => new HashSet<SegmentUnit> { SegmentUnit.Hour },
            "second" => new HashSet<SegmentUnit> { SegmentUnit.Hour, SegmentUnit.Minute, SegmentUnit.Second },
            _ => new HashSet<SegmentUnit> { SegmentUnit.Hour, SegmentUnit.Minute },
        };

        // LongTimePattern (not ShortTimePattern) is the base: .NET's ShortTimePattern never
        // includes seconds by definition (that omission is exactly what "short" means), so a
        // "second" granularity would otherwise have nowhere to source a Second token from --
        // LongTimePattern reliably does (e.g. InvariantCulture: "HH:mm:ss").
        var items = new List<SegmentLayoutItem>();
        var sawDayPeriod = false;
        foreach (var (kind, text) in Tokenize(culture.DateTimeFormat.LongTimePattern))
        {
            if (kind == '\0')
            {
                items.Add(new SegmentLayoutItem(SegmentLayoutKind.Literal, default, text));
                continue;
            }

            switch (kind)
            {
                case 'h':
                case 'H':
                    if (allowed.Contains(SegmentUnit.Hour))
                    {
                        items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, SegmentUnit.Hour, string.Empty));
                    }

                    break;
                case 'm':
                    if (allowed.Contains(SegmentUnit.Minute))
                    {
                        items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, SegmentUnit.Minute, string.Empty));
                    }

                    break;
                case 's':
                    if (allowed.Contains(SegmentUnit.Second))
                    {
                        items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, SegmentUnit.Second, string.Empty));
                    }

                    break;
                case 't':
                    sawDayPeriod = true;
                    if (hourCycle == 12)
                    {
                        items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, SegmentUnit.DayPeriod, string.Empty));
                    }

                    break;
                // 'y'/'M'/'d' should not appear in LongTimePattern; ignored defensively.
            }
        }

        // Many cultures (including InvariantCulture) have a 24-hour LongTimePattern with no 't'
        // token at all. An explicit HourCycle=12 request still needs a day-period segment to
        // unambiguously compose an hour value, so append one when the pattern didn't supply one.
        if (hourCycle == 12 && !sawDayPeriod)
        {
            items.Add(new SegmentLayoutItem(SegmentLayoutKind.Literal, default, " "));
            items.Add(new SegmentLayoutItem(SegmentLayoutKind.Editable, SegmentUnit.DayPeriod, string.Empty));
        }

        return TrimLiterals(items);
    }

    /// <summary>
    /// Resolves the effective hour cycle: an explicit 12/24 wins, otherwise sniffs the culture's
    /// ShortTimePattern for an 'H' token (contract: "HourCycle ... defaults to the culture's
    /// short-time pattern (H present =&gt; 24)").
    /// </summary>
    public static int ResolveHourCycle(CultureInfo culture, int? hourCycle)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (hourCycle is 12 or 24)
        {
            return hourCycle.Value;
        }

        return culture.DateTimeFormat.ShortTimePattern.Contains('H') ? 24 : 12;
    }

    public static DateTimeSegment CreateSegment(SegmentUnit unit, int hourCycle, int minuteStep, int secondStep) => unit switch
    {
        SegmentUnit.Year => new DateTimeSegment(unit, 1, 9999, 1, 5, 4),
        SegmentUnit.Month => new DateTimeSegment(unit, 1, 12, 1, 3, 2),
        SegmentUnit.Day => new DateTimeSegment(unit, 1, 31, 1, 7, 2),
        SegmentUnit.Hour when hourCycle == 24 => new DateTimeSegment(unit, 0, 23, 1, 3, 2),
        SegmentUnit.Hour => new DateTimeSegment(unit, 1, 12, 1, 3, 2),
        SegmentUnit.Minute => new DateTimeSegment(unit, 0, 59, Math.Max(minuteStep, 1), 15, 2),
        SegmentUnit.Second => new DateTimeSegment(unit, 0, 59, Math.Max(secondStep, 1), 15, 2),
        SegmentUnit.DayPeriod => new DateTimeSegment(unit, 0, 1, 1, 1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(unit)),
    };

    private static List<SegmentLayoutItem> TrimLiterals(List<SegmentLayoutItem> items)
    {
        var merged = new List<SegmentLayoutItem>();
        foreach (var item in items)
        {
            if (item.Kind == SegmentLayoutKind.Literal && merged.Count > 0 && merged[^1].Kind == SegmentLayoutKind.Literal)
            {
                merged[^1] = merged[^1] with { Literal = merged[^1].Literal + item.Literal };
                continue;
            }

            merged.Add(item);
        }

        while (merged.Count > 0 && merged[0].Kind == SegmentLayoutKind.Literal)
        {
            merged.RemoveAt(0);
        }

        while (merged.Count > 0 && merged[^1].Kind == SegmentLayoutKind.Literal)
        {
            merged.RemoveAt(merged.Count - 1);
        }

        return merged;
    }

    private static List<(char Kind, string Text)> Tokenize(string pattern)
    {
        var result = new List<(char, string)>();
        var i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (UnitChars.IndexOf(c) >= 0)
            {
                var j = i;
                while (j < pattern.Length && pattern[j] == c)
                {
                    j++;
                }

                result.Add((c, pattern[i..j]));
                i = j;
            }
            else
            {
                var j = i;
                while (j < pattern.Length && UnitChars.IndexOf(pattern[j]) < 0)
                {
                    j++;
                }

                result.Add(('\0', pattern[i..j]));
                i = j;
            }
        }

        return result;
    }
}

/// <summary>Composes/decomposes a DateOnly? from year/month/day DateTimeSegments (contract's Compose()).</summary>
public static class DateSegmentComposer
{
    /// <summary>
    /// Null unless every present segment (per granularity) is filled. Clamps day down to the
    /// month's max (contract: "Compose() silently clamps an out-of-range day ... via Math.Min").
    /// Missing year/month (narrower granularities) default to today's / January's.
    /// </summary>
    public static DateOnly? Compose(DateTimeSegment? year, DateTimeSegment? month, DateTimeSegment? day)
    {
        if (year is { Value: null } || month is { Value: null } || day is { Value: null })
        {
            return null;
        }

        var today = DateTime.Today;
        var y = SegmentMath.Clamp(year?.Value ?? today.Year, 1, 9999);
        var m = SegmentMath.Clamp(month?.Value ?? 1, 1, 12);
        var maxDay = DateTime.DaysInMonth(y, m);
        var d = Math.Min(day?.Value ?? 1, maxDay);

        return new DateOnly(y, m, d);
    }

    /// <summary>Recomputes the day segment's Max from the current year/month and clamps its Value down if needed (contract's RecomputeDayMax).</summary>
    public static void RecomputeDayMax(DateTimeSegment day, int? year, int? month)
    {
        ArgumentNullException.ThrowIfNull(day);

        var today = DateTime.Today;
        var y = SegmentMath.Clamp(year ?? today.Year, 1, 9999);
        var m = SegmentMath.Clamp(month ?? today.Month, 1, 12);
        var maxDay = DateTime.DaysInMonth(y, m);

        day.Max = maxDay;
        if (day.Value is { } v && v > maxDay)
        {
            day.Value = maxDay;
        }
    }
}

/// <summary>Pure segment text formatting shared by NaviusDateInput/NaviusTimeInput cell rendering.</summary>
public static class SegmentFormat
{
    /// <summary>Unit-shorthand token shown while a segment is unfilled (WPF's native masked-input placeholder idiom, in place of the web contract's literal "Empty" aria-valuetext -- see the family's "WPF implementation notes").</summary>
    public static string PlaceholderToken(SegmentUnit unit) => unit switch
    {
        SegmentUnit.Year => "yyyy",
        SegmentUnit.Month => "mm",
        SegmentUnit.Day => "dd",
        SegmentUnit.Hour => "hh",
        SegmentUnit.Minute => "mm",
        SegmentUnit.Second => "ss",
        SegmentUnit.DayPeriod => "AM",
        _ => "--",
    };

    /// <summary>Formats a segment's current value (or its placeholder token when unfilled). Year/Month/Day pad per forceLeadingZeros; DayPeriod always renders AM/PM.</summary>
    public static string FormatValue(DateTimeSegment segment, bool forceLeadingZeros)
    {
        ArgumentNullException.ThrowIfNull(segment);

        if (!segment.Filled)
        {
            return PlaceholderToken(segment.Unit);
        }

        if (segment.Unit == SegmentUnit.DayPeriod)
        {
            return segment.Value == 1 ? "PM" : "AM";
        }

        var digits = segment.Unit == SegmentUnit.Year ? 4 : 2;
        return forceLeadingZeros
            ? segment.Value!.Value.ToString("D" + digits, CultureInfo.InvariantCulture)
            : segment.Value!.Value.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>Composes a TimeOnly? from hour/minute/second/dayPeriod DateTimeSegments (contract's Compose()).</summary>
public static class TimeSegmentComposer
{
    /// <summary>Null unless every present segment is filled.</summary>
    public static TimeOnly? Compose(DateTimeSegment hour, DateTimeSegment? minute, DateTimeSegment? second, DateTimeSegment? dayPeriod, int hourCycle)
    {
        ArgumentNullException.ThrowIfNull(hour);

        if (!hour.Filled || minute is { Filled: false } || second is { Filled: false } || dayPeriod is { Filled: false })
        {
            return null;
        }

        var h = hour.Value!.Value;
        if (hourCycle == 12)
        {
            var pm = dayPeriod?.Value == 1;
            h %= 12;
            if (pm)
            {
                h += 12;
            }
        }

        var m = minute?.Value ?? 0;
        var s = second?.Value ?? 0;

        return new TimeOnly(SegmentMath.Clamp(h, 0, 23), SegmentMath.Clamp(m, 0, 59), SegmentMath.Clamp(s, 0, 59));
    }
}
