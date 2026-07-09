using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.DatePicker;
using Navius.Wpf.Primitives.Controls.DateRangePicker;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class DateRangePickerTests
{
    static DateRangePickerTests()
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

    private static readonly MethodInfo HandlePreviewKeyDownMethod =
        typeof(NaviusDatePickerBase).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo OnPickCommittedMethod =
        typeof(NaviusDateRangePicker).GetMethod("OnPickCommitted", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown,
    // style 0 = no WS_VISIBLE bit) is the lightest real one available headlessly.
    private static readonly PresentationSource TestSource =
        new HwndSource(0, 0, 0, 0, 0, "NaviusDateRangePickerTests", IntPtr.Zero);

    private static readonly DateTime Jan5 = new(2026, 1, 5);
    private static readonly DateTime Jan9 = new(2026, 1, 9);
    private static readonly DateTime Jan20 = new(2026, 1, 20);

    private static void SimulateKey(NaviusDateRangePicker picker, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        HandlePreviewKeyDownMethod.Invoke(picker, new object[] { picker, args });
    }

    private static void CommitPick(NaviusDateRangePicker picker, DateTime day) =>
        OnPickCommittedMethod.Invoke(picker, new object[] { day });

    private static string Format(DateTime day) => day.ToString("d", CultureInfo.CurrentCulture);

    // ---- Pure commit engine (no WPF / STA needed) ---------------------------------------------

    [Fact]
    public void Commit_FirstPick_SetsStartOnly()
    {
        var next = DateRangeCommitEngine.Commit(NaviusDateRange.Empty, Jan9);

        Assert.Equal(new NaviusDateRange(Jan9, null), next);
        Assert.False(next.IsComplete);
    }

    [Fact]
    public void Commit_SecondPick_CompletesTheRange()
    {
        var next = DateRangeCommitEngine.Commit(new NaviusDateRange(Jan5, null), Jan9);

        Assert.Equal(new NaviusDateRange(Jan5, Jan9), next);
        Assert.True(next.IsComplete);
    }

    [Fact]
    public void Commit_SecondPickBeforeStart_SwapsToStayOrdered()
    {
        var next = DateRangeCommitEngine.Commit(new NaviusDateRange(Jan9, null), Jan5);

        Assert.Equal(new NaviusDateRange(Jan5, Jan9), next);
    }

    [Fact]
    public void Commit_SecondPickOnStartDay_MakesSingleDayRange()
    {
        var next = DateRangeCommitEngine.Commit(new NaviusDateRange(Jan9, null), Jan9);

        Assert.Equal(new NaviusDateRange(Jan9, Jan9), next);
        Assert.True(next.IsComplete);
    }

    [Fact]
    public void Commit_PickAfterCompleteRange_StartsFresh()
    {
        var next = DateRangeCommitEngine.Commit(new NaviusDateRange(Jan5, Jan9), Jan20);

        Assert.Equal(new NaviusDateRange(Jan20, null), next);
    }

    [Fact]
    public void RangeStates_ReportEmptyAndComplete()
    {
        Assert.True(NaviusDateRange.Empty.IsEmpty);
        Assert.False(NaviusDateRange.Empty.IsComplete);
        Assert.False(new NaviusDateRange(Jan5, null).IsEmpty);
        Assert.False(new NaviusDateRange(Jan5, null).IsComplete);
        Assert.True(new NaviusDateRange(Jan5, Jan9).IsComplete);
    }

    // ---- StaFact control wiring ----------------------------------------------------------------

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var picker = new NaviusDateRangePicker();

        Assert.False(picker.IsOpen);
        Assert.Equal(NaviusDateRange.Empty, picker.Value);
        Assert.False(picker.HasSelection);
        Assert.Equal(PlacementSide.Bottom, picker.Side);
        Assert.Equal(PlacementAlign.Start, picker.Align);
    }

    [StaFact]
    public void FirstPick_SetsStart_AndKeepsThePopupOpen()
    {
        var picker = new NaviusDateRangePicker();
        var raised = 0;
        picker.ValueChanged += (_, _) => raised++;
        picker.IsOpen = true;

        CommitPick(picker, Jan5);

        Assert.Equal(new NaviusDateRange(Jan5, null), picker.Value);
        Assert.True(picker.IsOpen); // waiting for the second pick
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void SecondPick_CompletesTheRange_AndCloses()
    {
        var picker = new NaviusDateRangePicker();
        var raised = 0;
        picker.ValueChanged += (_, _) => raised++;
        picker.IsOpen = true;

        CommitPick(picker, Jan9);
        CommitPick(picker, Jan5); // earlier than start: engine swaps

        Assert.Equal(new NaviusDateRange(Jan5, Jan9), picker.Value);
        Assert.False(picker.IsOpen);
        Assert.Equal(2, raised);
    }

    [StaFact]
    public void Escape_RevertsBothEndpoints_AndCloses()
    {
        var picker = new NaviusDateRangePicker { Value = new NaviusDateRange(Jan5, Jan9) };
        picker.IsOpen = true; // snapshots the open-time value

        CommitPick(picker, Jan20); // fresh start replaces the complete range
        Assert.Equal(new NaviusDateRange(Jan20, null), picker.Value);

        SimulateKey(picker, Key.Escape);

        Assert.Equal(new NaviusDateRange(Jan5, Jan9), picker.Value); // both endpoints reverted
        Assert.False(picker.IsOpen);
    }

    [StaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    [InlineData(Key.Down)]
    public void ClosedTrigger_OpenKeys_OpenThePopup(Key key)
    {
        var picker = new NaviusDateRangePicker();

        SimulateKey(picker, key);

        Assert.True(picker.IsOpen);
    }

    [StaFact]
    public void DisplayText_ShowsPlaceholder_PartialAndCompleteRange()
    {
        var picker = new NaviusDateRangePicker { Placeholder = "Pick dates" };

        Assert.Equal("Pick dates", picker.DisplayText);

        picker.Value = new NaviusDateRange(Jan5, null);
        Assert.Equal($"{Format(Jan5)} - ", picker.DisplayText);

        picker.Value = new NaviusDateRange(Jan5, Jan9);
        Assert.Equal($"{Format(Jan5)} - {Format(Jan9)}", picker.DisplayText);
        Assert.True(picker.HasSelection);
    }

    [StaFact]
    public void ValuePattern_SurfacesStartDashEnd()
    {
        var picker = new NaviusDateRangePicker { Placeholder = "Pick dates" };
        var peer = CreatePeer(picker);
        var value = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;

        Assert.True(value.IsReadOnly);
        Assert.Equal(string.Empty, value.Value); // empty while unset, not the placeholder

        picker.Value = new NaviusDateRange(Jan5, Jan9);

        Assert.Equal($"{Format(Jan5)} - {Format(Jan9)}", value.Value);
        Assert.Throws<InvalidOperationException>(() => value.SetValue("nope"));
    }

    [StaFact]
    public void ExpandCollapsePattern_TracksIsOpen()
    {
        var picker = new NaviusDateRangePicker();
        var peer = CreatePeer(picker);
        var expand = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;

        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);

        expand.Expand();
        Assert.True(picker.IsOpen);

        expand.Collapse();
        Assert.False(picker.IsOpen);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DateRangePicker.xaml"),
        });

        var picker = new NaviusDateRangePicker
        {
            Resources = scope,
            Style = (Style)scope[typeof(NaviusDateRangePicker)],
        };

        Assert.True(picker.ApplyTemplate());
    }

    private static AutomationPeer CreatePeer(NaviusDateRangePicker picker)
    {
        var peer = typeof(NaviusDateRangePicker)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(picker, null) as AutomationPeer;
        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Custom, peer!.GetAutomationControlType());
        return peer;
    }
}
