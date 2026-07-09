using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.DateInput;
using Navius.Wpf.Primitives.Controls.Internal;
using Navius.Wpf.Primitives.Controls.TimeInput;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class TimeInputTests
{
    static TimeInputTests()
    {
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

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created, not a static field initializer: HwndSource construction requires an STA
    // thread, and this class also has plain [Fact] engine tests that can run on a non-STA thread
    // (see DateInputTests.TestSource for the full rationale).
    private static PresentationSource? _testSource;

    private static PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusTimeInputTests", IntPtr.Zero);

    private static void SendKey(UIElement element, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        element.RaiseEvent(args);
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DateInput.xaml"),
        });
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/TimeInput.xaml"),
        });

        return scope;
    }

    private static NaviusTimeInput CreateApplied(Action<NaviusTimeInput>? configure = null)
    {
        var input = new NaviusTimeInput { Culture = CultureInfo.InvariantCulture };
        configure?.Invoke(input);

        var scope = CreateThemedScope();
        input.Resources = scope;
        input.Style = (Style)scope[typeof(NaviusTimeInput)];
        Assert.True(input.ApplyTemplate());

        return input;
    }

    private static NaviusFieldSegment[] GetCells(NaviusTimeInput input)
    {
        var panel = (Panel)input.Template.FindName("PART_Segments", input);
        return panel.Children.OfType<NaviusFieldSegment>().ToArray();
    }

    // ---- Pure engine: BuildTimeLayout ---------------------------------------------------------

    [Fact]
    public void BuildTimeLayout_TwelveHour_IncludesDayPeriod()
    {
        var layout = SegmentLayoutBuilder.BuildTimeLayout(CultureInfo.InvariantCulture, "minute", hourCycle: 12);

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.Contains(SegmentUnit.Hour, units);
        Assert.Contains(SegmentUnit.Minute, units);
        Assert.Contains(SegmentUnit.DayPeriod, units);
        Assert.DoesNotContain(SegmentUnit.Second, units);
    }

    [Fact]
    public void BuildTimeLayout_TwentyFourHour_OmitsDayPeriod()
    {
        var layout = SegmentLayoutBuilder.BuildTimeLayout(CultureInfo.InvariantCulture, "minute", hourCycle: 24);

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.DoesNotContain(SegmentUnit.DayPeriod, units);
    }

    [Fact]
    public void BuildTimeLayout_SecondGranularity_IncludesSecond()
    {
        var layout = SegmentLayoutBuilder.BuildTimeLayout(CultureInfo.InvariantCulture, "second", hourCycle: 24);

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.Equal(new[] { SegmentUnit.Hour, SegmentUnit.Minute, SegmentUnit.Second }, units);
    }

    [Fact]
    public void BuildTimeLayout_HourGranularity_OnlyHour()
    {
        var layout = SegmentLayoutBuilder.BuildTimeLayout(CultureInfo.InvariantCulture, "hour", hourCycle: 24);

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.Equal(new[] { SegmentUnit.Hour }, units);
    }

    // ---- Pure engine: TimeSegmentComposer ------------------------------------------------------

    [Fact]
    public void TimeCompose_NullWhenHourUnfilled()
    {
        var hour = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 24, 1, 1);
        var minute = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Minute, 24, 1, 1);
        minute.Value = 30;

        Assert.Null(TimeSegmentComposer.Compose(hour, minute, null, null, hourCycle: 24));
    }

    [Fact]
    public void TimeCompose_TwelveHour_MidnightAndNoonRoundtrip()
    {
        var hour = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 12, 1, 1);
        var minute = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Minute, 12, 1, 1);
        minute.Value = 0;
        var dayPeriod = SegmentLayoutBuilder.CreateSegment(SegmentUnit.DayPeriod, 12, 1, 1);

        hour.Value = 12;
        dayPeriod.Value = 0; // 12 AM -> hour 0
        Assert.Equal(new TimeOnly(0, 0), TimeSegmentComposer.Compose(hour, minute, null, dayPeriod, 12));

        dayPeriod.Value = 1; // 12 PM -> hour 12
        Assert.Equal(new TimeOnly(12, 0), TimeSegmentComposer.Compose(hour, minute, null, dayPeriod, 12));

        hour.Value = 1;
        dayPeriod.Value = 1; // 1 PM -> hour 13
        Assert.Equal(new TimeOnly(13, 0), TimeSegmentComposer.Compose(hour, minute, null, dayPeriod, 12));
    }

    [Fact]
    public void TimeCompose_TwentyFourHour_IgnoresDayPeriod()
    {
        var hour = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 24, 1, 1);
        hour.Value = 14;
        var minute = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Minute, 24, 1, 1);
        minute.Value = 45;

        Assert.Equal(new TimeOnly(14, 45), TimeSegmentComposer.Compose(hour, minute, null, null, 24));
    }

    [Fact]
    public void TimeCompose_AbsentMinuteSecond_DefaultToZero()
    {
        var hour = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 24, 1, 1);
        hour.Value = 9;

        Assert.Equal(new TimeOnly(9, 0, 0), TimeSegmentComposer.Compose(hour, null, null, null, 24));
    }

    // ---- StaFact: NaviusTimeInput control wiring -----------------------------------------------

    [StaFact]
    public void DefaultState_IsEmptyWithContractDefaults()
    {
        var input = new NaviusTimeInput();

        Assert.Null(input.Value);
        Assert.Equal("minute", input.Granularity);
        Assert.Equal(1, input.MinuteStep);
        Assert.Equal(1, input.SecondStep);
    }

    [StaFact]
    public void ApplyTemplate_TwelveHourMinuteGranularity_BuildsHourMinuteDayPeriodCells()
    {
        var input = CreateApplied(i => i.HourCycle = 12);

        var cells = GetCells(input);

        Assert.Equal(new[] { SegmentUnit.Hour, SegmentUnit.Minute, SegmentUnit.DayPeriod }, cells.Select(c => c.Unit));
    }

    [StaFact]
    public void ApplyTemplate_TwentyFourHour_BuildsHourMinuteOnly()
    {
        var input = CreateApplied(i => i.HourCycle = 24);

        var cells = GetCells(input);

        Assert.Equal(new[] { SegmentUnit.Hour, SegmentUnit.Minute }, cells.Select(c => c.Unit));
    }

    [StaFact]
    public void SettingValue_SeedsSegments_TwelveHour()
    {
        var input = CreateApplied(i => i.HourCycle = 12);

        input.Value = new TimeOnly(13, 30); // 1:30 PM

        var cells = GetCells(input);
        Assert.Equal(1, cells[0].ValueNow); // hour
        Assert.Equal(30, cells[1].ValueNow); // minute
        Assert.Equal(1, cells[2].ValueNow); // dayPeriod = PM
    }

    [StaFact]
    public void TypingDigits_ComposesValue_TwentyFourHour()
    {
        var input = CreateApplied(i => i.HourCycle = 24);
        var cells = GetCells(input);
        TimeOnly? observed = null;
        input.ValueChanged += (_, e) => observed = e.NewValue;

        SendKey(cells[0], Key.D1);
        SendKey(cells[0], Key.D4); // hour "14" -> advances (24h MaxDigits=2)
        SendKey(cells[1], Key.D4);
        SendKey(cells[1], Key.D5); // minute "45"

        Assert.Equal(new TimeOnly(14, 45), input.Value);
        Assert.Equal(new TimeOnly(14, 45), observed);
    }

    [StaFact]
    public void DayPeriodSegment_LetterKeys_SetAmPm()
    {
        var input = CreateApplied(i => { i.HourCycle = 12; i.Value = new TimeOnly(9, 0); });
        var cells = GetCells(input);
        var dayPeriodCell = cells.Single(c => c.Unit == SegmentUnit.DayPeriod);

        SendKey(dayPeriodCell, Key.P);

        Assert.Equal(new TimeOnly(21, 0), input.Value); // 9 AM -> 9 PM
    }

    [StaFact]
    public void ReadOnly_BlocksAllSegmentKeys()
    {
        var input = CreateApplied(i => { i.Value = new TimeOnly(9, 30); i.ReadOnly = true; i.HourCycle = 24; });
        var cells = GetCells(input);

        SendKey(cells[0], Key.Up);

        Assert.Equal(new TimeOnly(9, 30), input.Value);
    }

    [StaFact]
    public void MinuteStep_DrivesArrowIncrement()
    {
        var input = CreateApplied(i => { i.HourCycle = 24; i.MinuteStep = 15; i.Value = new TimeOnly(9, 0); });
        var cells = GetCells(input);
        var minuteCell = cells.Single(c => c.Unit == SegmentUnit.Minute);

        SendKey(minuteCell, Key.Up);

        Assert.Equal(new TimeOnly(9, 15), input.Value);
    }

    // ---- Automation peers -----------------------------------------------------------------------

    [StaFact]
    public void RootAutomationPeer_ReportsGroup_AndValuePatternSurfacesTime()
    {
        var input = new NaviusTimeInput { Value = new TimeOnly(9, 30, 15) };
        var peer = new NaviusTimeInputAutomationPeer(input);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        var valuePattern = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;
        Assert.Equal("09:30:15", valuePattern.Value);
    }
}
