using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Navius.Wpf.Primitives.Controls.Combobox;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

/// <summary>
/// Pure engine / state-machine tests. Kept in a separate class with NO WPF static fields so they run
/// as plain [Fact]s on the thread pool: they must not drag in the STA-only Application/HwndSource
/// statics that the wiring class below needs.
/// </summary>
public class ComboboxEngineTests
{
    private static readonly string[] Fruits =
    {
        "Apple", "Apricot", "Banana", "Blackberry", "Cherry", "Mango", "Pineapple",
    };

    [Fact]
    public void Filter_EmptyQuery_ReturnsAllItems()
    {
        var result = ComboboxEngine.Filter(Fruits, string.Empty, x => x, null);
        Assert.Equal(Fruits.Length, result.Count);
    }

    [Fact]
    public void Filter_DefaultPredicate_IsCaseInsensitiveSubstring()
    {
        var result = ComboboxEngine.Filter(Fruits, "AP", x => x, null);

        // "ap" appears in Apple, Apricot, and Pineapple.
        Assert.Equal(new[] { "Apple", "Apricot", "Pineapple" }, result);
    }

    [Fact]
    public void Filter_CustomPredicate_IsHonored()
    {
        var result = ComboboxEngine.Filter(Fruits, "a", x => x, (item, _) => item.StartsWith("B", StringComparison.Ordinal));
        Assert.Equal(new[] { "Banana", "Blackberry" }, result);
    }

    [Fact]
    public void ToggleMultiple_AddsWhenAbsent_RemovesWhenPresent()
    {
        var added = ComboboxEngine.ToggleMultiple(new[] { "Apple" }, "Banana");
        Assert.Equal(new[] { "Apple", "Banana" }, added);

        var removed = ComboboxEngine.ToggleMultiple(added, "Apple");
        Assert.Equal(new[] { "Banana" }, removed);
    }

    [Fact]
    public void RemoveLast_DropsTheTrailingValue_AndIsSafeWhenEmpty()
    {
        Assert.Equal(new[] { "a", "b" }, ComboboxEngine.RemoveLast(new[] { "a", "b", "c" }));
        Assert.Empty(ComboboxEngine.RemoveLast(Array.Empty<string>()));
    }

    [Theory]
    [InlineData(-1, 3, 1, 0)]   // nothing highlighted, move down -> first
    [InlineData(-1, 3, -1, 2)]  // nothing highlighted, move up -> last
    [InlineData(0, 3, -1, 0)]   // at first, move up -> clamps (no wrap)
    [InlineData(2, 3, 1, 2)]    // at last, move down -> clamps (no wrap)
    [InlineData(1, 3, 1, 2)]    // middle, move down
    [InlineData(0, 0, 1, -1)]   // empty list -> none
    public void MoveHighlight_ClampsWithoutWrapping(int current, int count, int delta, int expected)
    {
        Assert.Equal(expected, ComboboxEngine.MoveHighlight(current, count, delta));
    }

    [Fact]
    public void RemoveValue_RemovesByValueIdentity_NotByDisplayedIndex()
    {
        // Regression guard for the historical web bug: a chip must be removed by VALUE, never by its
        // position in the currently filtered/displayed rows. Here the committed-values list holds the
        // items in a DIFFERENT order/subset than what a filter would render, so a positional removal
        // would delete the wrong value.
        var committedValues = new[] { "Cherry", "Apple", "Mango" };

        // Simulate the displayed (filtered) rows for query "a": Apple sits at index 0 there,
        // but in committedValues Apple sits at index 1.
        var displayedRows = ComboboxEngine.Filter(Fruits, "a", x => x, null);
        var appleDisplayedIndex = displayedRows.ToList().FindIndex(x => x == "Apple");
        var appleValuesIndex = committedValues.ToList().FindIndex(x => x == "Apple");
        Assert.NotEqual(appleDisplayedIndex, appleValuesIndex); // the indices genuinely diverge

        var afterRemove = ComboboxEngine.RemoveValue(committedValues, "Apple");

        // Apple is gone; Cherry and Mango (and their relative order) are untouched.
        Assert.Equal(new[] { "Cherry", "Mango" }, afterRemove);
    }
}

public class ComboboxTests : IDisposable
{
    private sealed record NamedOption(string Name);

    private sealed record WrappedOption(NamedOption Inner);

    private sealed record DualOption(string Code, string Label);

    static ComboboxTests()
    {
        // pack://application URIs only resolve once an Application exists in the process. Guarded
        // try/catch because xunit runs test classes in parallel on separate STA threads: another
        // class's static ctor can win the race to create the process-wide Application.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class already created the process-wide Application.
            }
        }
    }

    private static readonly string[] Fruits =
    {
        "Apple", "Apricot", "Banana", "Blackberry", "Cherry", "Mango", "Pineapple",
    };

    private static readonly MethodInfo OnInputKeyDownMethod =
        typeof(NaviusComboboxBase).GetMethod("OnInputPreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusComboboxTests", IntPtr.Zero);

    public void Dispose()
    {
        _testSource?.Dispose();
        TestCleanup.PumpDispatcher();
    }

    /// <summary>Drives the combobox's single private key handler directly with a real KeyEventArgs.</summary>
    private void SimulateKey(NaviusComboboxBase combobox, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        OnInputKeyDownMethod.Invoke(combobox, new object[] { combobox, args });
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Combobox.xaml"),
        });

        return scope;
    }

    /// <summary>
    /// Builds and templates a combobox parented to a (never-shown) Window whose Resources hold the
    /// themed scope. The scope is on the ANCESTOR (not the control's own Resources) so the base-typed
    /// theme style resolves via DefaultStyleKey, mirroring how the gallery page merges the dictionary.
    /// The Window also satisfies OpenCore's Window.GetWindow lookup.
    /// </summary>
    private static (NaviusCombobox<string> Combobox, Window Window) CreateApplied(bool multiple = false)
    {
        var combobox = new NaviusCombobox<string>
        {
            Multiple = multiple,
            Items = Fruits,
        };
        var window = new Window { Resources = CreateThemedScope(), Content = combobox };
        combobox.ApplyTemplate();
        return (combobox, window);
    }

    // ---------------------------------------------------------------------------------------------
    // StaFact wiring tests (template + overlay + keyboard)
    // ---------------------------------------------------------------------------------------------

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var combobox = new NaviusCombobox<string>();

        Assert.False(combobox.IsOpen);
        Assert.False(combobox.Multiple);
        Assert.Equal(-1, combobox.HighlightedIndex);
        Assert.Equal(string.Empty, combobox.Query);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        // The style is keyed to the non-generic base type and picked up through the constructor's
        // SetResourceReference(StyleProperty, typeof(NaviusComboboxBase)), so parenting into a themed
        // scope is all a closed generic instantiation needs.
        var combobox = new NaviusCombobox<string>();
        _ = new Window { Resources = CreateThemedScope(), Content = combobox };

        Assert.True(combobox.ApplyTemplate());
    }

    [StaFact]
    public void NonGenericCombobox_LoadsFromXaml_AndTracksBoundItems()
    {
        var combobox = Assert.IsType<NaviusCombobox>(XamlReader.Parse(
            "<combo:NaviusCombobox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:combo='clr-namespace:Navius.Wpf.Primitives.Controls.Combobox;assembly=Navius.Wpf.Primitives' />"));
        var options = new ObservableCollection<NamedOption> { new("Apple") };
        combobox.DisplayMemberPath = nameof(NamedOption.Name);
        combobox.ItemsSource = options;

        options.Add(new NamedOption("Banana"));
        Assert.Equal(new[] { "Apple", "Banana" }, combobox.FilteredRows!.Select(row => row.Text));

        combobox.Value = options[1];

        Assert.Equal("Banana", combobox.Query);
    }

    [StaFact]
    public void NonGenericCombobox_ResolvesDottedDisplayMemberPath()
    {
        var combobox = new NaviusCombobox { DisplayMemberPath = "Inner.Name" };
        combobox.ItemsSource = new[] { new WrappedOption(new NamedOption("Apple")), new WrappedOption(new NamedOption("Banana")) };

        Assert.Equal(new[] { "Apple", "Banana" }, combobox.FilteredRows!.Select(row => row.Text));
    }

    [StaFact]
    public void NonGenericCombobox_RelabelsRowsAndQuery_WhenDisplayMemberPathChanges()
    {
        var options = new[] { new DualOption("A1", "Apple"), new DualOption("B1", "Banana") };
        var combobox = new NaviusCombobox { DisplayMemberPath = nameof(DualOption.Code) };
        combobox.ItemsSource = options;
        Assert.Equal(new[] { "A1", "B1" }, combobox.FilteredRows!.Select(row => row.Text));

        // No committed value yet: the empty query leaves all rows visible for the relabel check.
        combobox.DisplayMemberPath = nameof(DualOption.Label);
        Assert.Equal(new[] { "Apple", "Banana" }, combobox.FilteredRows!.Select(row => row.Text));

        // A committed value writes its label into the query; a path change re-resolves it.
        combobox.Value = options[1];
        Assert.Equal("Banana", combobox.Query);

        combobox.DisplayMemberPath = nameof(DualOption.Code);
        Assert.Equal("B1", combobox.Query);
    }

    [StaFact]
    public void Typing_OpensPopup_AndFiltersRows()
    {
        var (combobox, _) = CreateApplied();

        try
        {
            combobox.Query = "ap";

            Assert.True(combobox.IsOpen);
            Assert.Equal(new[] { "Apple", "Apricot", "Pineapple" }, combobox.FilteredRows!.Select(r => r.Text));
            Assert.Equal(0, combobox.HighlightedIndex); // typing auto-highlights the first match
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void ArrowKeys_MoveHighlight_WithoutWrapping()
    {
        var (combobox, _) = CreateApplied();
        combobox.IsOpen = true; // all rows shown

        try
        {
            SimulateKey(combobox, Key.Down); // -1 -> 0
            Assert.Equal(0, combobox.HighlightedIndex);

            SimulateKey(combobox, Key.Up); // 0 -> clamps at 0
            Assert.Equal(0, combobox.HighlightedIndex);

            SimulateKey(combobox, Key.End); // -> last
            Assert.Equal(Fruits.Length - 1, combobox.HighlightedIndex);

            SimulateKey(combobox, Key.Down); // clamps at last
            Assert.Equal(Fruits.Length - 1, combobox.HighlightedIndex);
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void Enter_CommitsHighlightedRow_SingleSelect_AndCloses()
    {
        var (combobox, _) = CreateApplied();
        var raised = 0;
        combobox.ValueChanged += (_, _) => raised++;

        combobox.Query = "ap"; // opens, highlights Apple
        SimulateKey(combobox, Key.Enter);

        Assert.Equal("Apple", combobox.Value);
        Assert.Equal("Apple", combobox.Query); // filter text reverts to the committed label
        Assert.False(combobox.IsOpen);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void Escape_ClosesAndRevertsQueryToCommittedLabel()
    {
        var (combobox, _) = CreateApplied();
        combobox.Value = "Banana"; // committed label

        combobox.Query = "xyz"; // user edits the filter (opens)
        Assert.True(combobox.IsOpen);

        SimulateKey(combobox, Key.Escape);

        Assert.False(combobox.IsOpen);
        Assert.Equal("Banana", combobox.Query);
    }

    [StaFact]
    public void MultiSelect_ToggleViaEnter_AddsValue_ClearsQuery_StaysOpen()
    {
        var (combobox, _) = CreateApplied(multiple: true);
        var raised = 0;
        combobox.ValuesChanged += (_, _) => raised++;

        try
        {
            combobox.Query = "ba"; // Banana
            SimulateKey(combobox, Key.Enter);

            Assert.Contains("Banana", combobox.Values);
            Assert.Equal(string.Empty, combobox.Query); // query cleared after a multi commit
            Assert.True(combobox.IsOpen);               // popup stays open so chips can accumulate
            Assert.Equal(1, raised);
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void MultiSelect_BackspaceOnEmptyQuery_RemovesLastValue()
    {
        var (combobox, _) = CreateApplied(multiple: true);
        combobox.Values = new[] { "Apple", "Cherry" };

        SimulateKey(combobox, Key.Back); // empty query + has selection -> drop last

        Assert.Equal(new[] { "Apple" }, combobox.Values);
    }

    [StaFact]
    public void MultiSelect_BackspaceWithNonEmptyQuery_DoesNotRemove()
    {
        var (combobox, _) = CreateApplied(multiple: true);
        combobox.Values = new[] { "Apple", "Cherry" };

        try
        {
            combobox.Query = "z"; // non-empty filter: Backspace should edit text, not chips

            SimulateKey(combobox, Key.Back);

            Assert.Equal(new[] { "Apple", "Cherry" }, combobox.Values);
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void ChipRemoveCommand_RemovesByValue_RegardlessOfCurrentFilter()
    {
        var (combobox, _) = CreateApplied(multiple: true);
        combobox.Values = new[] { "Cherry", "Apple", "Mango" };

        try
        {
            // Narrow the displayed rows so the visible ordering does not match the Values ordering.
            combobox.Query = "a";

            NaviusComboboxBase.RemoveChipCommand.Execute("Apple", combobox);

            Assert.Equal(new[] { "Cherry", "Mango" }, combobox.Values);
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void Chips_ReflectCommittedValues_InMultiSelect()
    {
        var (combobox, _) = CreateApplied(multiple: true);
        combobox.Values = new[] { "Apple", "Mango" };

        Assert.Equal(new[] { "Apple", "Mango" }, combobox.SelectedChips!.Select(c => c.Text));
        Assert.True(combobox.HasSelection);
    }

    [StaFact]
    public void AutomationPeer_ReportsComboBox_WithExpandCollapse()
    {
        var (combobox, _) = CreateApplied();

        var peer = combobox.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(combobox, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.ComboBox, peer!.GetAutomationControlType());

        var expandCollapse = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        try
        {
            combobox.IsOpen = true;
            Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
        }
        finally
        {
            combobox.IsOpen = false;
        }
    }

    [StaFact]
    public void DisabledComboboxAutomationPeer_ReportsDisabled_AndExpandThrowsWithoutOpening()
    {
        // Regression (DEFECT 2): a Disabled combobox previously reported IsEnabled == true through
        // UIA and its ExpandCollapse.Expand opened the popup regardless. The peer must reflect the
        // Disabled flag and refuse to operate.
        var (combobox, _) = CreateApplied();
        combobox.Disabled = true;

        var peer = combobox.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(combobox, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.False(peer!.IsEnabled());

        var expandCollapse = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        Assert.Throws<ElementNotEnabledException>(() => expandCollapse.Expand());
        Assert.False(combobox.IsOpen);
    }
}
