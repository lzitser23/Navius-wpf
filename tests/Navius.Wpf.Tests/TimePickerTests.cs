using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;
using Navius.Wpf.Primitives.Controls.TimeInput;
using Navius.Wpf.Primitives.Controls.TimePicker;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class TimePickerTests
{
    static TimePickerTests()
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
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/TimePicker.xaml"),
        });

        return scope;
    }

    private static NaviusTimePicker CreateApplied(Action<NaviusTimePicker>? configure = null)
    {
        var picker = new NaviusTimePicker();
        configure?.Invoke(picker);

        var scope = CreateThemedScope();
        picker.Resources = scope;
        picker.Style = (Style)scope[typeof(NaviusTimePicker)];
        Assert.True(picker.ApplyTemplate());

        return picker;
    }

    private static ListBox Column(NaviusTimePicker picker, string partName) =>
        (ListBox)picker.Template.FindName(partName, picker);

    // ---- Pure engine: TimePickerOptionBuilder --------------------------------------------------

    [Fact]
    public void Hours_TwentyFourHour_ZeroToTwentyThree()
    {
        var hours = TimePickerOptionBuilder.Hours(24);

        Assert.Equal(24, hours.Count);
        Assert.Equal(0, hours[0].Value);
        Assert.Equal("00", hours[0].Text);
        Assert.Equal(23, hours[^1].Value);
    }

    [Fact]
    public void Hours_TwelveHour_OneToTwelve()
    {
        var hours = TimePickerOptionBuilder.Hours(12);

        Assert.Equal(12, hours.Count);
        Assert.Equal(1, hours[0].Value);
        Assert.Equal(12, hours[^1].Value);
    }

    [Fact]
    public void Minutes_RespectsStep()
    {
        var minutes = TimePickerOptionBuilder.Minutes(15);

        Assert.Equal(new[] { 0, 15, 30, 45 }, minutes.Select(m => m.Value));
    }

    [Fact]
    public void Minutes_StepCoercedToAtLeastOne()
    {
        var minutes = TimePickerOptionBuilder.Minutes(0);

        Assert.Equal(60, minutes.Count); // step 0 -> coerced to 1
    }

    [Fact]
    public void DayPeriods_AreAmAndPm()
    {
        var periods = TimePickerOptionBuilder.DayPeriods();

        Assert.Equal(new[] { (0, "AM"), (1, "PM") }, periods.Select(p => (p.Value, p.Text)));
    }

    // ---- StaFact: NaviusTimePicker control wiring ----------------------------------------------

    [StaFact]
    public void DefaultState_MatchesContractDefaults()
    {
        var picker = new NaviusTimePicker();

        Assert.Null(picker.Value);
        Assert.False(picker.IsOpen);
        Assert.Equal("minute", picker.Granularity);
        Assert.Equal(1, picker.MinuteStep);
        Assert.Equal(1, picker.SecondStep);
    }

    [StaFact]
    public void ApplyTemplate_MinuteGranularity_TwelveHour_ShowsHourMinuteDayPeriodColumns()
    {
        var picker = CreateApplied(p => p.HourCycle = 12);

        Assert.Equal(Visibility.Visible, Column(picker, "PART_HourColumn").Visibility);
        Assert.Equal(Visibility.Visible, Column(picker, "PART_MinuteColumn").Visibility);
        Assert.Equal(Visibility.Collapsed, Column(picker, "PART_SecondColumn").Visibility);
        Assert.Equal(Visibility.Visible, Column(picker, "PART_DayPeriodColumn").Visibility);
    }

    [StaFact]
    public void ApplyTemplate_TwentyFourHour_HidesDayPeriodColumn()
    {
        var picker = CreateApplied(p => p.HourCycle = 24);

        Assert.Equal(Visibility.Collapsed, Column(picker, "PART_DayPeriodColumn").Visibility);
    }

    [StaFact]
    public void ApplyTemplate_SecondGranularity_ShowsSecondColumn()
    {
        var picker = CreateApplied(p => p.Granularity = "second");

        Assert.Equal(Visibility.Visible, Column(picker, "PART_SecondColumn").Visibility);
    }

    [StaFact]
    public void SettingValue_SyncsColumnSelections()
    {
        var picker = CreateApplied(p => p.HourCycle = 12);

        picker.Value = new TimeOnly(13, 30); // 1:30 PM

        Assert.Equal(1, Column(picker, "PART_HourColumn").SelectedValue);
        Assert.Equal(30, Column(picker, "PART_MinuteColumn").SelectedValue);
        Assert.Equal(1, Column(picker, "PART_DayPeriodColumn").SelectedValue); // PM
    }

    [StaFact]
    public void SelectingColumnOptions_ComposesValue_TwentyFourHour()
    {
        var picker = CreateApplied(p => p.HourCycle = 24);
        var hourColumn = Column(picker, "PART_HourColumn");
        var minuteColumn = Column(picker, "PART_MinuteColumn");
        TimeOnly? observed = null;
        picker.ValueChanged += (_, e) => observed = e.NewValue;

        hourColumn.SelectedValue = 14;
        minuteColumn.SelectedValue = 45;

        Assert.Equal(new TimeOnly(14, 45), picker.Value);
        Assert.Equal(new TimeOnly(14, 45), observed);
    }

    [StaFact]
    public void SelectingColumnOptions_ComposesValue_TwelveHourPm()
    {
        var picker = CreateApplied(p => p.HourCycle = 12);
        var hourColumn = Column(picker, "PART_HourColumn");
        var minuteColumn = Column(picker, "PART_MinuteColumn");
        var dayPeriodColumn = Column(picker, "PART_DayPeriodColumn");

        hourColumn.SelectedValue = 1;
        minuteColumn.SelectedValue = 0;
        dayPeriodColumn.SelectedValue = 1; // PM

        Assert.Equal(new TimeOnly(13, 0), picker.Value);
    }

    [StaFact]
    public void MinuteStepChange_RebuildsMinuteColumnOptions()
    {
        var picker = CreateApplied(p => p.MinuteStep = 15);

        var minuteColumn = Column(picker, "PART_MinuteColumn");
        var values = ((System.Collections.Generic.IReadOnlyList<TimePickerOption>)minuteColumn.ItemsSource).Select(o => o.Value).ToArray();

        Assert.Equal(new[] { 0, 15, 30, 45 }, values);
    }

    [StaFact]
    public void PartInput_IsWiredToTheSameTimeInputFamily()
    {
        var picker = CreateApplied();

        var input = (NaviusTimeInput)picker.Template.FindName("PART_Input", picker);

        Assert.NotNull(input);
    }

    // ---- Automation peer ------------------------------------------------------------------------

    [StaFact]
    public void AutomationPeer_ReportsComboBox_ExpandCollapseAndValuePattern()
    {
        var picker = new NaviusTimePicker { Value = new TimeOnly(9, 30, 15) };
        var peer = new NaviusTimePickerAutomationPeer(picker);

        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());

        var valuePattern = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;
        Assert.Equal("09:30:15", valuePattern.Value);

        var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);

        expandCollapse.Expand();
        Assert.True(picker.IsOpen);

        expandCollapse.Collapse();
        Assert.False(picker.IsOpen);
    }
}
