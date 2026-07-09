using System;
using System.Collections.Generic;

namespace Navius.Wpf.Primitives.Controls.Internal;

/// <summary>One selectable row in a NaviusTimePicker column listbox (contract's NaviusTimePickerOption, computed by the owning column).</summary>
public readonly record struct TimePickerOption(int Value, string Text);

/// <summary>
/// Pure, WPF-free option-list generation for NaviusTimePicker's hour/minute/second/day-period
/// columns (contract's "auto-generates its options"), factored out so it is directly
/// unit-testable without an STA Application, same rationale as SegmentEngine.cs.
/// </summary>
public static class TimePickerOptionBuilder
{
    public static IReadOnlyList<TimePickerOption> Hours(int hourCycle)
    {
        var options = new List<TimePickerOption>();
        if (hourCycle == 24)
        {
            for (var h = 0; h <= 23; h++)
            {
                options.Add(new TimePickerOption(h, h.ToString("D2")));
            }
        }
        else
        {
            for (var h = 1; h <= 12; h++)
            {
                options.Add(new TimePickerOption(h, h.ToString("D2")));
            }
        }

        return options;
    }

    public static IReadOnlyList<TimePickerOption> Minutes(int step) => StepRange(step);

    public static IReadOnlyList<TimePickerOption> Seconds(int step) => StepRange(step);

    public static IReadOnlyList<TimePickerOption> DayPeriods() => new[]
    {
        new TimePickerOption(0, "AM"),
        new TimePickerOption(1, "PM"),
    };

    private static IReadOnlyList<TimePickerOption> StepRange(int step)
    {
        step = Math.Max(step, 1);
        var options = new List<TimePickerOption>();
        for (var v = 0; v <= 59; v += step)
        {
            options.Add(new TimePickerOption(v, v.ToString("D2")));
        }

        return options;
    }
}
