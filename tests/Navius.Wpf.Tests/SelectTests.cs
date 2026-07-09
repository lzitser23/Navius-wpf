using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.Select;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class SelectTests
{
    static SelectTests()
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
        typeof(NaviusSelectBase).GetMethod("HandlePreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown,
    // style 0 = no WS_VISIBLE bit) is the lightest real one available headlessly.
    private static readonly PresentationSource TestSource =
        new HwndSource(0, 0, 0, 0, 0, "NaviusSelectTests", IntPtr.Zero);

    private static void SimulateKey(NaviusSelectBase select, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        HandlePreviewKeyDownMethod.Invoke(select, new object[] { select, args });
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Select.xaml"),
        });

        return scope;
    }

    private static NaviusSelect<string> CreateSelect(bool multiple = false, params string[] values)
    {
        if (values.Length == 0)
        {
            values = new[] { "apple", "banana", "cherry" };
        }

        var select = new NaviusSelect<string> { Multiple = multiple, Placeholder = "Pick" };
        foreach (var value in values)
        {
            select.Items.Add(new NaviusSelectItem { Value = value, TextValue = char.ToUpperInvariant(value[0]) + value[1..] });
        }

        return select;
    }

    private static NaviusSelectItem ItemAt(NaviusSelectBase select, int index) =>
        (NaviusSelectItem)select.Items[index];

    // ---- Pure engine (no WPF / STA needed) --------------------------------------------------

    [Fact]
    public void MoveHighlight_ClampsAtEnds_WhenNotLooping()
    {
        Assert.Equal(2, SelectSelectionEngine.MoveHighlight(2, 3, 1, loop: false));
        Assert.Equal(0, SelectSelectionEngine.MoveHighlight(0, 3, -1, loop: false));
        Assert.Equal(1, SelectSelectionEngine.MoveHighlight(0, 3, 1, loop: false));
    }

    [Fact]
    public void MoveHighlight_WrapsAtEnds_WhenLooping()
    {
        Assert.Equal(0, SelectSelectionEngine.MoveHighlight(2, 3, 1, loop: true));
        Assert.Equal(2, SelectSelectionEngine.MoveHighlight(0, 3, -1, loop: true));
    }

    [Fact]
    public void MoveHighlight_FromNoHighlight_LandsFirstOrLast()
    {
        Assert.Equal(0, SelectSelectionEngine.MoveHighlight(-1, 3, 1, loop: false));
        Assert.Equal(2, SelectSelectionEngine.MoveHighlight(-1, 3, -1, loop: false));
        Assert.Equal(-1, SelectSelectionEngine.MoveHighlight(-1, 0, 1, loop: false));
    }

    [Fact]
    public void ToggleMultiple_AddsThenRemoves_WithoutMutatingInput()
    {
        var original = new List<string> { "a" };
        var added = SelectSelectionEngine.ToggleMultiple(original, "b");
        Assert.Equal(new[] { "a", "b" }, added);
        Assert.Equal(new[] { "a" }, original); // input untouched

        var removed = SelectSelectionEngine.ToggleMultiple(added, "a");
        Assert.Equal(new[] { "b" }, removed);
    }

    [Fact]
    public void ResolveSingleCommit_HonorsPreventDefault()
    {
        Assert.Equal("new", SelectSelectionEngine.ResolveSingleCommit("old", "new", prevented: false));
        Assert.Equal("old", SelectSelectionEngine.ResolveSingleCommit("old", "new", prevented: true));
    }

    [Fact]
    public void FindTypeaheadMatch_SearchesForwardAndWraps()
    {
        var texts = new[] { "Apple", "Banana", "Cherry", "Blueberry" };
        Assert.Equal(3, SelectSelectionEngine.FindTypeaheadMatch(texts, 1, 'b')); // after Banana -> Blueberry
        Assert.Equal(1, SelectSelectionEngine.FindTypeaheadMatch(texts, 3, 'b')); // wraps back to Banana
        Assert.Equal(0, SelectSelectionEngine.FindTypeaheadMatch(texts, -1, 'a'));
        Assert.Null(SelectSelectionEngine.FindTypeaheadMatch(texts, 0, 'z'));
    }

    // ---- StaFact control wiring -------------------------------------------------------------

    [StaFact]
    public void Defaults_MatchTheSelectContract()
    {
        var select = new NaviusSelect<string>();

        Assert.Equal(PlacementSide.Bottom, select.Side);
        Assert.Equal(PlacementAlign.Start, select.Align); // Select overrides DefaultAlign to Start
        Assert.False(select.Loop);
        Assert.False(select.Multiple);
        Assert.False(select.IsOpen);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var select = CreateSelect();
        select.Resources = scope;
        // The theme is an implicit style keyed by the non-generic base; WPF resolves implicit
        // styles by the element's closed generic runtime type, so assign the shared base style
        // explicitly (the same lookup NaviusSelect<T>'s ctor does via SetResourceReference).
        select.Style = (Style)scope[typeof(NaviusSelectBase)];

        Assert.True(select.ApplyTemplate());
    }

    [StaFact]
    public void ActivatingItem_CommitsValue_ClosesPopup_AndRaisesEvents()
    {
        var select = CreateSelect();
        select.IsOpen = true;
        var routed = 0;
        string? typed = null;
        select.ValueChanged += (_, _) => routed++;
        select.ValueSelected += (_, v) => typed = v;

        ItemAt(select, 1).RaiseSelectEvent();

        Assert.Equal("banana", select.Value);
        Assert.True(ItemAt(select, 1).IsSelectedValue);
        Assert.False(select.IsOpen); // single-select closes
        Assert.Equal(1, routed);
        Assert.Equal("banana", typed);
    }

    [StaFact]
    public void PreventDefault_KeepsValueUnchanged_AndStaysOpen()
    {
        var select = CreateSelect();
        select.IsOpen = true;
        var item = ItemAt(select, 1);
        item.Select += (_, e) => ((NaviusSelectEventArgs)e).PreventDefault();

        item.RaiseSelectEvent();

        Assert.Null(select.Value);
        Assert.False(item.IsSelectedValue);
        Assert.True(select.IsOpen);
    }

    [StaFact]
    public void Multiple_TogglesInSet_AndKeepsPopupOpen()
    {
        var select = CreateSelect(multiple: true);
        select.IsOpen = true;
        var raised = 0;
        select.ValuesSelected += (_, _) => raised++;

        ItemAt(select, 0).RaiseSelectEvent();
        ItemAt(select, 2).RaiseSelectEvent();

        Assert.Equal(new[] { "apple", "cherry" }, select.SelectedValues);
        Assert.True(select.IsOpen); // multi stays open
        Assert.Equal(2, raised);

        ItemAt(select, 0).RaiseSelectEvent(); // toggle off

        Assert.Equal(new[] { "cherry" }, select.SelectedValues);
        Assert.True(select.IsOpen);
    }

    [StaFact]
    public void ClosedTrigger_ArrowDownOpens_HighlightsFirst()
    {
        var select = CreateSelect();

        SimulateKey(select, Key.Down);

        Assert.True(select.IsOpen);
        Assert.True(ItemAt(select, 0).IsHighlightedValue);
    }

    [StaFact]
    public void ClosedTrigger_ArrowUpOpens_HighlightsLast()
    {
        var select = CreateSelect();

        SimulateKey(select, Key.Up);

        Assert.True(select.IsOpen);
        Assert.True(ItemAt(select, 2).IsHighlightedValue);
    }

    [StaFact]
    public void OpenListbox_ArrowKeysMoveHighlight_ThenEnterCommits()
    {
        var select = CreateSelect();

        SimulateKey(select, Key.Down); // open, highlight first (apple)
        SimulateKey(select, Key.Down); // banana
        Assert.True(ItemAt(select, 1).IsHighlightedValue);

        SimulateKey(select, Key.Enter); // commit banana

        Assert.Equal("banana", select.Value);
        Assert.False(select.IsOpen);
    }

    [StaFact]
    public void HomeAndEnd_JumpHighlightToFirstAndLast()
    {
        var select = CreateSelect();
        SimulateKey(select, Key.Down); // open, highlight first

        SimulateKey(select, Key.End);
        Assert.True(ItemAt(select, 2).IsHighlightedValue);

        SimulateKey(select, Key.Home);
        Assert.True(ItemAt(select, 0).IsHighlightedValue);
    }

    [StaFact]
    public void ArrowDown_ClampsAtEnd_WhenLoopIsFalse()
    {
        var select = CreateSelect();
        SimulateKey(select, Key.Up); // open, highlight last (cherry, index 2)

        SimulateKey(select, Key.Down); // clamp (Loop=false)

        Assert.True(ItemAt(select, 2).IsHighlightedValue);
    }

    [StaFact]
    public void Escape_ClosesThePopup()
    {
        var select = CreateSelect();
        SimulateKey(select, Key.Down);
        Assert.True(select.IsOpen);

        SimulateKey(select, Key.Escape);

        Assert.False(select.IsOpen);
    }

    [StaFact]
    public void Typeahead_HighlightsMatchingOption()
    {
        var select = CreateSelect(false, "apple", "banana", "cherry");
        SimulateKey(select, Key.Down); // open, highlight apple

        SimulateKey(select, Key.C); // jump to Cherry

        Assert.True(ItemAt(select, 2).IsHighlightedValue);
    }

    [StaFact]
    public void SettingValue_SyncsSelectedStateAndDisplayText()
    {
        var select = CreateSelect();

        select.Value = "cherry";

        Assert.True(select.HasSelection);
        Assert.True(ItemAt(select, 2).IsSelectedValue);
        Assert.Equal("Cherry", select.DisplayText);
    }

    [StaFact]
    public void NoSelection_DisplaysPlaceholder()
    {
        var select = CreateSelect();

        Assert.False(select.HasSelection);
        Assert.Equal("Pick", select.DisplayText);
    }

    [StaFact]
    public void AutomationPeer_RootReportsComboBox_ItemReportsListItem()
    {
        var select = CreateSelect();

        var rootPeer = typeof(NaviusSelectBase)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(select, null) as AutomationPeer;
        Assert.NotNull(rootPeer);
        Assert.Equal(AutomationControlType.ComboBox, rootPeer!.GetAutomationControlType());

        var item = ItemAt(select, 0);
        var itemPeer = typeof(NaviusSelectItem)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(item, null) as AutomationPeer;
        Assert.NotNull(itemPeer);
        Assert.Equal(AutomationControlType.ListItem, itemPeer!.GetAutomationControlType());
    }
}
