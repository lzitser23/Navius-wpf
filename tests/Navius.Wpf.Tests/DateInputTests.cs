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
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class DateInputTests : IDisposable
{
    static DateInputTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
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

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown,
    // style 0 = no WS_VISIBLE bit) is the lightest real one available headlessly (same trick as
    // SelectTests.TestSource). Lazily created (not a static field initializer) because HwndSource
    // construction requires an STA thread: this class also has plain [Fact] engine tests that
    // xunit can run on a non-STA thread pool thread, and a static field initializer would run on
    // whichever thread first touches the type. Instance-level (not static) and disposed via
    // IDisposable.Dispose() so it never outlives the STA thread of the test that created it.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusDateInputTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    private void SendKey(UIElement element, Key key)
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

        return scope;
    }

    private static NaviusDateInput CreateApplied(Action<NaviusDateInput>? configure = null)
    {
        var input = new NaviusDateInput { Culture = CultureInfo.InvariantCulture };
        configure?.Invoke(input);

        var scope = CreateThemedScope();
        input.Resources = scope;
        input.Style = (Style)scope[typeof(NaviusDateInput)];
        Assert.True(input.ApplyTemplate());

        return input;
    }

    private static NaviusFieldSegment[] GetCells(NaviusDateInput input)
    {
        var panel = (Panel)input.Template.FindName("PART_Segments", input);
        return panel.Children.OfType<NaviusFieldSegment>().ToArray();
    }

    // ---- Pure engine: SegmentMath -----------------------------------------------------------

    [Fact]
    public void SegmentMath_Wrap_WrapsAtBothEnds()
    {
        Assert.Equal(1, SegmentMath.Wrap(13, 1, 12)); // past max wraps to min
        Assert.Equal(12, SegmentMath.Wrap(0, 1, 12)); // below min wraps to max
        Assert.Equal(6, SegmentMath.Wrap(6, 1, 12));
    }

    [Fact]
    public void SegmentMath_ArrowUp_FromEmpty_LandsOnPlaceholderBasis()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.ArrowUp, 0, placeholderBasis: 7, rtl: false);

        Assert.True(result.Changed);
        Assert.Equal(7, segment.Value);
    }

    [Fact]
    public void SegmentMath_ArrowUp_WrapsAtMax()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);
        segment.Value = 12;

        SegmentMath.HandleKey(segment, SegmentKey.ArrowUp, 0, placeholderBasis: 1, rtl: false);

        Assert.Equal(1, segment.Value);
    }

    [Fact]
    public void SegmentMath_ArrowDown_WrapsAtMin()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);
        segment.Value = 1;

        SegmentMath.HandleKey(segment, SegmentKey.ArrowDown, 0, placeholderBasis: 1, rtl: false);

        Assert.Equal(12, segment.Value);
    }

    [Fact]
    public void SegmentMath_PageUpDown_UsesPageStep()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1);
        segment.Value = 2020;

        SegmentMath.HandleKey(segment, SegmentKey.PageUp, 0, 2020, rtl: false);
        Assert.Equal(2025, segment.Value); // year PageStep = 5

        SegmentMath.HandleKey(segment, SegmentKey.PageDown, 0, 2020, rtl: false);
        Assert.Equal(2020, segment.Value);
    }

    [Fact]
    public void SegmentMath_HomeEnd_JumpToBounds()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        segment.Value = 15;

        SegmentMath.HandleKey(segment, SegmentKey.Home, 0, 15, rtl: false);
        Assert.Equal(1, segment.Value);

        SegmentMath.HandleKey(segment, SegmentKey.End, 0, 15, rtl: false);
        Assert.Equal(31, segment.Value);
    }

    [Fact]
    public void SegmentMath_BackspaceAndDelete_ClearToUnfilled()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        segment.Value = 15;

        var result = SegmentMath.HandleKey(segment, SegmentKey.Backspace, 0, 1, rtl: false);

        Assert.True(result.Changed);
        Assert.False(segment.Filled);
    }

    [Fact]
    public void SegmentMath_Backspace_OnAlreadyEmpty_ReportsNoChange()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.Backspace, 0, 1, rtl: false);

        Assert.False(result.Changed);
        Assert.True(result.Handled);
    }

    [Theory]
    [InlineData(false, SegmentFocusMove.Previous)]
    [InlineData(true, SegmentFocusMove.Next)]
    public void SegmentMath_ArrowLeft_FlipsWithRtl(bool rtl, SegmentFocusMove expected)
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.ArrowLeft, 0, 1, rtl);

        Assert.Equal(expected, result.Focus);
    }

    [Fact]
    public void SegmentMath_Digit_AccumulatesUntilMaxDigits()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1); // MaxDigits = 4

        var r1 = SegmentMath.HandleKey(segment, SegmentKey.Digit, 2, 0, rtl: false);
        Assert.Equal(2, segment.Value);
        Assert.Equal(SegmentFocusMove.None, r1.Focus);

        SegmentMath.HandleKey(segment, SegmentKey.Digit, 0, 0, rtl: false);
        SegmentMath.HandleKey(segment, SegmentKey.Digit, 2, 0, rtl: false);
        var r4 = SegmentMath.HandleKey(segment, SegmentKey.Digit, 6, 0, rtl: false);

        Assert.Equal(2026, segment.Value);
        Assert.Equal(SegmentFocusMove.Next, r4.Focus); // 4th digit hits MaxDigits -> auto-advance
    }

    [Fact]
    public void SegmentMath_Digit_AutoAdvances_WhenNoSecondDigitCouldFit()
    {
        // Day: typing "4" alone -> 4*10=40 > Max(31), so it must auto-advance immediately.
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.Digit, 4, 0, rtl: false);

        Assert.Equal(4, segment.Value);
        Assert.Equal(SegmentFocusMove.Next, result.Focus);
    }

    [Fact]
    public void SegmentMath_Digit_WaitsForSecondDigit_WhenBothCouldFit()
    {
        // Month: typing "1" -> 1*10=10 <= Max(12), so it waits for a possible second digit.
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.Digit, 1, 0, rtl: false);

        Assert.Equal(1, segment.Value);
        Assert.Equal(SegmentFocusMove.None, result.Focus);
    }

    [Fact]
    public void SegmentMath_Digit_ExceedingMax_RestartsBufferAtSingleDigit()
    {
        // Month: "1" then "9" -> "19" > 12, restarts the buffer at "9" alone.
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);
        SegmentMath.HandleKey(segment, SegmentKey.Digit, 1, 0, rtl: false);

        var result = SegmentMath.HandleKey(segment, SegmentKey.Digit, 9, 0, rtl: false);

        Assert.Equal(9, segment.Value);
        Assert.Equal(SegmentFocusMove.Next, result.Focus); // 9*10=90 > 12 -> advances
    }

    [Fact]
    public void SegmentMath_DayPeriod_ArrowsToggleAmPm_AndLettersJump()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.DayPeriod, 12, 1, 1);

        SegmentMath.HandleKey(segment, SegmentKey.LetterP, 0, 0, rtl: false);
        Assert.Equal(1, segment.Value);

        SegmentMath.HandleKey(segment, SegmentKey.LetterA, 0, 0, rtl: false);
        Assert.Equal(0, segment.Value);

        SegmentMath.HandleKey(segment, SegmentKey.ArrowUp, 0, 0, rtl: false);
        Assert.Equal(1, segment.Value);
    }

    [Fact]
    public void SegmentMath_DayPeriod_IgnoresDigits()
    {
        var segment = SegmentLayoutBuilder.CreateSegment(SegmentUnit.DayPeriod, 12, 1, 1);

        var result = SegmentMath.HandleKey(segment, SegmentKey.Digit, 1, 0, rtl: false);

        Assert.False(result.Handled);
        Assert.Null(segment.Value);
    }

    // ---- Pure engine: SegmentLayoutBuilder ---------------------------------------------------

    [Fact]
    public void BuildDateLayout_InvariantCulture_OrdersMonthDayYearWithSlashes()
    {
        var layout = SegmentLayoutBuilder.BuildDateLayout(CultureInfo.InvariantCulture, "day");

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.Equal(new[] { SegmentUnit.Month, SegmentUnit.Day, SegmentUnit.Year }, units);

        var literals = layout.Where(i => i.Kind == SegmentLayoutKind.Literal).Select(i => i.Literal).ToArray();
        Assert.All(literals, l => Assert.Equal("/", l));
    }

    [Fact]
    public void BuildDateLayout_MonthGranularity_DropsDayAndItsSeparator()
    {
        var layout = SegmentLayoutBuilder.BuildDateLayout(CultureInfo.InvariantCulture, "month");

        var units = layout.Where(i => i.Kind == SegmentLayoutKind.Editable).Select(i => i.Unit).ToArray();
        Assert.Equal(new[] { SegmentUnit.Month, SegmentUnit.Year }, units);
        Assert.Equal(1, layout.Count(i => i.Kind == SegmentLayoutKind.Literal)); // one separator remains between Month and Year
    }

    [Fact]
    public void BuildDateLayout_YearGranularity_OnlyYear()
    {
        var layout = SegmentLayoutBuilder.BuildDateLayout(CultureInfo.InvariantCulture, "year");

        Assert.Single(layout);
        Assert.Equal(SegmentUnit.Year, layout[0].Unit);
    }

    [Fact]
    public void ResolveHourCycle_ExplicitValueWins()
    {
        Assert.Equal(12, SegmentLayoutBuilder.ResolveHourCycle(CultureInfo.InvariantCulture, 12));
        Assert.Equal(24, SegmentLayoutBuilder.ResolveHourCycle(CultureInfo.InvariantCulture, 24));
    }

    [Fact]
    public void ResolveHourCycle_SniffsCulturePattern_WhenUnset()
    {
        var invariant = SegmentLayoutBuilder.ResolveHourCycle(CultureInfo.InvariantCulture, null);
        Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortTimePattern.Contains('H') ? 24 : 12, invariant);
    }

    [Fact]
    public void CreateSegment_Bounds_MatchContractPerUnit()
    {
        Assert.Equal((1, 9999, 4), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1)));
        Assert.Equal((1, 12, 2), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1)));
        Assert.Equal((1, 31, 2), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1)));
        Assert.Equal((0, 23, 2), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 24, 1, 1)));
        Assert.Equal((1, 12, 2), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.Hour, 12, 1, 1)));
        Assert.Equal((0, 1, 1), Bounds(SegmentLayoutBuilder.CreateSegment(SegmentUnit.DayPeriod, 12, 1, 1)));
    }

    private static (int Min, int Max, int MaxDigits) Bounds(DateTimeSegment s) => (s.Min, s.Max, s.MaxDigits);

    // ---- Pure engine: DateSegmentComposer ----------------------------------------------------

    [Fact]
    public void Compose_NullWhenAnyPresentSegmentUnfilled()
    {
        var year = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1);
        year.Value = 2026;
        var month = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1); // unfilled
        var day = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        day.Value = 9;

        Assert.Null(DateSegmentComposer.Compose(year, month, day));
    }

    [Fact]
    public void Compose_ClampsDayToMonthMax()
    {
        var year = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1);
        year.Value = 2026;
        var month = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);
        month.Value = 2; // February
        var day = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        day.Value = 30; // out of range for February

        var composed = DateSegmentComposer.Compose(year, month, day);

        Assert.Equal(new DateOnly(2026, 2, 28), composed);
    }

    [Fact]
    public void Compose_MonthGranularity_DefaultsAbsentDayToOne()
    {
        var year = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1);
        year.Value = 2026;
        var month = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Month, 24, 1, 1);
        month.Value = 7;

        var composed = DateSegmentComposer.Compose(year, month, day: null);

        Assert.Equal(new DateOnly(2026, 7, 1), composed);
    }

    [Fact]
    public void RecomputeDayMax_ClampsDayValueDown_WhenMonthShrinksMax()
    {
        var day = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        day.Value = 31;

        DateSegmentComposer.RecomputeDayMax(day, year: 2026, month: 2);

        Assert.Equal(28, day.Max);
        Assert.Equal(28, day.Value);
    }

    // ---- Pure engine: SegmentFormat -----------------------------------------------------------

    [Fact]
    public void FormatValue_UnfilledSegment_ShowsPlaceholderToken()
    {
        var day = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);

        Assert.Equal("dd", SegmentFormat.FormatValue(day, forceLeadingZeros: false));
    }

    [Fact]
    public void FormatValue_ForceLeadingZeros_PadsPerUnit()
    {
        var day = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Day, 24, 1, 1);
        day.Value = 5;
        var year = SegmentLayoutBuilder.CreateSegment(SegmentUnit.Year, 24, 1, 1);
        year.Value = 26;

        Assert.Equal("05", SegmentFormat.FormatValue(day, forceLeadingZeros: true));
        Assert.Equal("0026", SegmentFormat.FormatValue(year, forceLeadingZeros: true));
        Assert.Equal("5", SegmentFormat.FormatValue(day, forceLeadingZeros: false));
    }

    // ---- StaFact: NaviusDateInput control wiring ---------------------------------------------

    [StaFact]
    public void DefaultState_IsEmptyWithContractDefaults()
    {
        var input = new NaviusDateInput();

        Assert.Null(input.Value);
        Assert.Equal("day", input.Granularity);
        Assert.False(input.ReadOnly);
        Assert.False(input.IsFilled);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_BuildsOneCellPerEditableSegment()
    {
        var input = CreateApplied();

        var cells = GetCells(input);

        Assert.Equal(3, cells.Length); // month, day, year
        Assert.Equal(SegmentUnit.Month, cells[0].Unit);
        Assert.Equal(SegmentUnit.Day, cells[1].Unit);
        Assert.Equal(SegmentUnit.Year, cells[2].Unit);
        Assert.All(cells, c => Assert.True(c.IsPlaceholder));
    }

    [StaFact]
    public void SettingValue_SeedsAllSegments()
    {
        var input = CreateApplied();

        input.Value = new DateOnly(2026, 7, 9);

        var cells = GetCells(input);
        Assert.Equal(7, cells[0].ValueNow);
        Assert.Equal(9, cells[1].ValueNow);
        Assert.Equal(2026, cells[2].ValueNow);
        Assert.True(input.IsFilled);
    }

    [StaFact]
    public void TypingTwoDigits_ComposesDay_AndAdvancesFocus()
    {
        var input = CreateApplied();
        var cells = GetCells(input);
        DateOnly? observed = null;
        input.ValueChanged += (_, e) => observed = e.NewValue;

        SendKey(cells[0], Key.D0); // month "0" -> waits (0*10=0 <= 12)
        SendKey(cells[0], Key.D7); // month "07" -> hits MaxDigits, advances to Day
        SendKey(cells[1], Key.D0);
        SendKey(cells[1], Key.D9); // day "09" -> advances to Year
        SendKey(cells[2], Key.D2);
        SendKey(cells[2], Key.D0);
        SendKey(cells[2], Key.D2);
        SendKey(cells[2], Key.D6); // year "2026" -> complete

        Assert.Equal(new DateOnly(2026, 7, 9), input.Value);
        Assert.Equal(new DateOnly(2026, 7, 9), observed);
    }

    [StaFact]
    public void ArrowUp_OnEmptySegment_RevealsPlaceholderBasis()
    {
        var input = CreateApplied(i => i.PlaceholderValue = new DateOnly(2030, 3, 15));
        var cells = GetCells(input);

        SendKey(cells[0], Key.Up); // month

        Assert.Equal(3, cells[0].ValueNow);
    }

    [StaFact]
    public void ArrowLeft_MovesFocusToPreviousSegment()
    {
        var input = CreateApplied();
        var cells = GetCells(input);

        SendKey(cells[1], Key.Left);

        Assert.True(cells[0].IsFocused);
    }

    [StaFact]
    public void ArrowRight_MovesFocusToNextSegment()
    {
        var input = CreateApplied();
        var cells = GetCells(input);

        SendKey(cells[0], Key.Right);

        Assert.True(cells[1].IsFocused);
    }

    [StaFact]
    public void Backspace_ClearsSegment_AndUpdatesValue()
    {
        var input = CreateApplied(i => i.Value = new DateOnly(2026, 7, 9));
        var cells = GetCells(input);

        SendKey(cells[1], Key.Back); // clear day

        Assert.Null(input.Value); // no longer fully composed
        Assert.True(cells[1].IsPlaceholder);
    }

    [StaFact]
    public void ReadOnly_BlocksAllSegmentKeys()
    {
        var input = CreateApplied(i => { i.Value = new DateOnly(2026, 7, 9); i.ReadOnly = true; });
        var cells = GetCells(input);

        SendKey(cells[0], Key.Up);

        Assert.Equal(new DateOnly(2026, 7, 9), input.Value); // unchanged
    }

    [StaFact]
    public void MonthChange_RecomputesDayMax_AndClampsDay()
    {
        var input = CreateApplied(i => i.Value = new DateOnly(2026, 1, 31));
        var cells = GetCells(input);

        SendKey(cells[0], Key.D0);
        SendKey(cells[0], Key.D2); // month -> 02 (February)

        Assert.Equal(28, cells[1].ValueNow); // day clamped from 31
        Assert.Equal(new DateOnly(2026, 2, 28), input.Value);
    }

    [StaFact]
    public void OutOfRange_SetsIsOutOfRangeAndInvalidState()
    {
        var input = CreateApplied(i =>
        {
            i.MinValue = new DateOnly(2026, 1, 1);
            i.MaxValue = new DateOnly(2026, 12, 31);
            i.Value = new DateOnly(2027, 1, 1);
        });

        Assert.True(input.IsOutOfRange);
        Assert.True(input.IsInvalidState);
    }

    [StaFact]
    public void Required_EmptyValue_SetsIsInvalidState()
    {
        // Regression (M6 audit): Required was a dead DP -- it never fed into IsInvalidState, so a
        // required-but-empty date input never reported invalid, contradicting the contract's
        // "Required drives Field validity (ValueMissing) when composing is incomplete."
        var input = CreateApplied(i => i.Required = true);

        Assert.True(input.IsInvalidState);
    }

    [StaFact]
    public void Required_False_EmptyValue_DoesNotSetIsInvalidState()
    {
        var input = CreateApplied();

        Assert.False(input.IsInvalidState);
    }

    [StaFact]
    public void Required_ComposedValue_ClearsIsInvalidState()
    {
        var input = CreateApplied(i =>
        {
            i.Required = true;
            i.Value = new DateOnly(2026, 7, 9);
        });

        Assert.False(input.IsInvalidState);
    }

    [StaFact]
    public void FocusFirstSegment_FocusesCellZero()
    {
        var input = CreateApplied();
        var cells = GetCells(input);

        input.FocusFirstSegment();

        Assert.True(cells[0].IsFocused);
    }

    // ---- Automation peers ---------------------------------------------------------------------

    [StaFact]
    public void RootAutomationPeer_ReportsGroup_AndValuePatternSurfacesIsoDate()
    {
        var input = new NaviusDateInput { Value = new DateOnly(2026, 7, 9) };
        var peer = new NaviusDateInputAutomationPeer(input);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        var valuePattern = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;
        Assert.Equal("2026-07-09", valuePattern.Value);
        Assert.True(valuePattern.IsReadOnly);
    }

    [StaFact]
    public void SegmentAutomationPeer_ReportsSpinner_WithRangeValues()
    {
        var segment = new NaviusFieldSegment { ValueNow = 7, Minimum = 1, Maximum = 12 };
        var peer = new NaviusFieldSegmentAutomationPeer(segment);

        Assert.Equal(AutomationControlType.Spinner, peer.GetAutomationControlType());
        Assert.Equal(7, peer.Value);
        Assert.Equal(1, peer.Minimum);
        Assert.Equal(12, peer.Maximum);
    }

    [StaFact]
    public void SegmentAutomationPeer_SetValue_RaisesValueRequested()
    {
        var segment = new NaviusFieldSegment { Minimum = 1, Maximum = 12 };
        var peer = new NaviusFieldSegmentAutomationPeer(segment);
        int? requested = null;
        segment.ValueRequested += (_, v) => requested = v;

        peer.SetValue(5);

        Assert.Equal(5, requested);
    }

    // ---- RTL: segment order must not mirror (only arrow-key navigation does) ----------------

    [StaFact]
    public void PartSegments_FlowDirectionPinnedToLeftToRight_RegardlessOfControlFlowDirection()
    {
        // Year/month/day segment order (and the literal separators between them) is a fixed
        // reading-order layout, not a bidi-mirrored one -- only NaviusDateInput.OnSegmentPreviewKeyDown's
        // arrow-key handling is RTL-aware (see docs/adr/0006-rtl-dpi-hardening.md). Without pinning
        // PART_Segments' own FlowDirection, WPF would auto-mirror the whole segment row (verified via
        // pixel-rendered RenderTargetBitmap diagnostics during the M6 RTL wave: the year segment's ink
        // cluster moved from the end of the row to the start under FlowDirection=RightToLeft on the
        // unpinned template).
        var input = CreateApplied(i => i.FlowDirection = FlowDirection.RightToLeft);

        var panel = (Panel)input.Template.FindName("PART_Segments", input);

        Assert.Equal(FlowDirection.LeftToRight, panel.FlowDirection);
    }

    [StaFact]
    public void PartSegments_ChildOrder_UnaffectedByFlowDirection()
    {
        var ltr = CreateApplied(i => { i.FlowDirection = FlowDirection.LeftToRight; i.Value = new DateOnly(2026, 7, 9); });
        var rtl = CreateApplied(i => { i.FlowDirection = FlowDirection.RightToLeft; i.Value = new DateOnly(2026, 7, 9); });

        var ltrUnits = GetCells(ltr).Select(c => c.Unit).ToArray();
        var rtlUnits = GetCells(rtl).Select(c => c.Unit).ToArray();

        Assert.Equal(ltrUnits, rtlUnits);
    }
}
