using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.DatePicker;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class DatePickerTests
{
    static DatePickerTests()
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
        typeof(NaviusDatePicker).GetMethod("OnPickCommitted", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown,
    // style 0 = no WS_VISIBLE bit) is the lightest real one available headlessly.
    private static readonly PresentationSource TestSource =
        new HwndSource(0, 0, 0, 0, 0, "NaviusDatePickerTests", IntPtr.Zero);

    private static void SimulateKey(NaviusDatePickerBase picker, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        HandlePreviewKeyDownMethod.Invoke(picker, new object[] { picker, args });
    }

    private static void CommitPick(NaviusDatePicker picker, DateTime day) =>
        OnPickCommittedMethod.Invoke(picker, new object[] { day });

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DatePicker.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var picker = new NaviusDatePicker();

        Assert.False(picker.IsOpen);
        Assert.Null(picker.Value);
        Assert.False(picker.HasSelection);
        Assert.Equal(PlacementSide.Bottom, picker.Side);
        Assert.Equal(PlacementAlign.Start, picker.Align);
        Assert.Equal(6d, picker.SideOffset);
    }

    [StaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    [InlineData(Key.Down)]
    public void ClosedTrigger_OpenKeys_OpenThePopup(Key key)
    {
        var picker = new NaviusDatePicker();

        SimulateKey(picker, key);

        Assert.True(picker.IsOpen);
    }

    [StaFact]
    public void Escape_ClosesWithoutCommitting()
    {
        var picker = new NaviusDatePicker();
        SimulateKey(picker, Key.Enter);
        Assert.True(picker.IsOpen);

        SimulateKey(picker, Key.Escape);

        Assert.False(picker.IsOpen);
        Assert.Null(picker.Value);
    }

    [StaFact]
    public void PickCommit_SetsValue_RaisesChanged_AndCloses()
    {
        var picker = new NaviusDatePicker();
        var raised = 0;
        picker.ValueChanged += (_, _) => raised++;
        picker.IsOpen = true;
        var day = new DateTime(2026, 7, 9);

        CommitPick(picker, day);

        Assert.Equal(day, picker.Value);
        Assert.True(picker.HasSelection);
        Assert.Equal(1, raised);
        Assert.False(picker.IsOpen); // single-date commit closes
    }

    [StaFact]
    public void NoValue_DisplaysPlaceholder_ThenFormattedDate()
    {
        var picker = new NaviusDatePicker { Placeholder = "Pick a date" };

        Assert.Equal("Pick a date", picker.DisplayText);

        var day = new DateTime(2026, 7, 9);
        picker.Value = day;

        Assert.Equal(day.ToString("d", CultureInfo.CurrentCulture), picker.DisplayText);
    }

    [StaFact]
    public void ValuePattern_SurfacesFormattedDate_NeverThePlaceholder()
    {
        var picker = new NaviusDatePicker { Placeholder = "Pick a date" };
        var peer = CreatePeer(picker);
        var value = (IValueProvider)peer.GetPattern(PatternInterface.Value)!;

        Assert.True(value.IsReadOnly);
        Assert.Equal(string.Empty, value.Value); // empty while unset, not the placeholder

        var day = new DateTime(2026, 7, 9);
        picker.Value = day;

        Assert.Equal(day.ToString("d", CultureInfo.CurrentCulture), value.Value);
        Assert.Throws<InvalidOperationException>(() => value.SetValue("2026-01-01"));
    }

    [StaFact]
    public void ExpandCollapsePattern_TracksIsOpen()
    {
        var picker = new NaviusDatePicker();
        var peer = CreatePeer(picker);
        var expand = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;

        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);

        expand.Expand();
        Assert.True(picker.IsOpen);
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);

        expand.Collapse();
        Assert.False(picker.IsOpen);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var picker = new NaviusDatePicker
        {
            Resources = scope,
            Style = (Style)scope[typeof(NaviusDatePicker)],
        };

        Assert.True(picker.ApplyTemplate());
    }

    private static AutomationPeer CreatePeer(NaviusDatePicker picker)
    {
        var peer = typeof(NaviusDatePicker)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(picker, null) as AutomationPeer;
        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Custom, peer!.GetAutomationControlType());
        return peer;
    }
}
