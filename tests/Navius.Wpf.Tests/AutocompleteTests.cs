using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.Autocomplete;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class AutocompleteTests
{
    // Deliberately NOT the precedent static-ctor pattern: this class mixes pure-engine [Fact]
    // tests, which xunit may run on an MTA worker thread, with [StaFact] WPF tests. A static ctor
    // touching Application/HwndSource on an MTA-first run throws (InputManager requires STA), so
    // WPF init is lazy and happens only inside the [StaFact] helpers below.
    private static void EnsureApplication()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch because xunit runs test classes in parallel on separate STA threads:
        // another test class can win the race.
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
        "Apple", "Apricot", "Banana", "Cherry", "Date", "Fig", "Grape",
    };

    private static readonly MethodInfo OnInputPreviewKeyDownMethod =
        typeof(NaviusAutocompleteBase).GetMethod("OnInputPreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown) is
    // the lightest real one available headlessly (same trick as RadioGroupTests). Lazy so it is
    // only created on an STA thread (see EnsureApplication's comment).
    private static PresentationSource? _testSource;

    private static PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusAutocompleteTests", IntPtr.Zero);

    /// <summary>Drives the input's private PreviewKeyDown handler directly with a real KeyEventArgs.</summary>
    private static KeyEventArgs SendKey(NaviusAutocompleteBase control, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        OnInputPreviewKeyDownMethod.Invoke(control, new object[] { control, args });
        return args;
    }

    private static NaviusAutocomplete<string> CreateWithItems() =>
        new() { Items = Fruits };

    private static ResourceDictionary CreateThemedScope()
    {
        EnsureApplication();

        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Autocomplete.xaml"),
        });

        return scope;
    }

    /// <summary>
    /// Builds a themed control the way XAML would: BeginInit/EndInit fires Initialized, which is
    /// where the control resolves its shared base-typed style (implicit style lookup cannot find a
    /// base-typed style for a closed generic, and Generic.xaml is deliberately untouched).
    /// </summary>
    private static NaviusAutocomplete<string> CreateThemed(bool withItems = false)
    {
        var control = new NaviusAutocomplete<string>();
        control.BeginInit();
        control.Resources = CreateThemedScope();
        if (withItems)
        {
            control.Items = Fruits;
        }

        control.EndInit();
        return control;
    }

    // ----- Pure engine: Filter -----

    [Fact]
    public void Filter_EmptyQuery_ReturnsAll()
    {
        var result = AutocompleteEngine.Filter(Fruits, string.Empty, x => x, null);
        Assert.Equal(Fruits.Length, result.Count);
    }

    [Fact]
    public void Filter_WhitespaceQuery_ReturnsAll()
    {
        var result = AutocompleteEngine.Filter(Fruits, "   ", x => x, null);
        Assert.Equal(Fruits.Length, result.Count);
    }

    [Fact]
    public void Filter_DefaultIsCaseInsensitiveContains()
    {
        var result = AutocompleteEngine.Filter(Fruits, "ap", x => x, null);
        Assert.Equal(new[] { "Apple", "Apricot", "Grape" }, result);
    }

    [Fact]
    public void Filter_UsesCustomPredicateWhenProvided()
    {
        var result = AutocompleteEngine.Filter(
            Fruits, "a", x => x, (item, q) => item.StartsWith(q, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Apple", "Apricot" }, result);
    }

    // ----- Pure engine: MoveHighlight -----

    [Fact]
    public void MoveHighlight_FromNone_DownGoesToFirst()
    {
        Assert.Equal(0, AutocompleteEngine.MoveHighlight(-1, 5, +1));
    }

    [Fact]
    public void MoveHighlight_FromNone_UpGoesToLast()
    {
        Assert.Equal(4, AutocompleteEngine.MoveHighlight(-1, 5, -1));
    }

    [Fact]
    public void MoveHighlight_ClampsAtEnd_WhenNotLooping()
    {
        Assert.Equal(4, AutocompleteEngine.MoveHighlight(4, 5, +1));
    }

    [Fact]
    public void MoveHighlight_ClampsAtStart_WhenNotLooping()
    {
        Assert.Equal(0, AutocompleteEngine.MoveHighlight(0, 5, -1));
    }

    [Fact]
    public void MoveHighlight_EmptyList_ReturnsMinusOne()
    {
        Assert.Equal(-1, AutocompleteEngine.MoveHighlight(-1, 0, +1));
    }

    [Fact]
    public void MoveHighlight_Loop_WrapsAround()
    {
        Assert.Equal(0, AutocompleteEngine.MoveHighlight(4, 5, +1, loop: true));
        Assert.Equal(4, AutocompleteEngine.MoveHighlight(0, 5, -1, loop: true));
    }

    // ----- Control state machine (no template needed) -----

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var control = new NaviusAutocomplete<string>();

        Assert.Equal(PlacementSide.Bottom, control.Side);
        Assert.Equal(PlacementAlign.Start, control.Align);
        Assert.Equal(-1, control.HighlightedIndex);
        Assert.False(control.IsOpen);
        Assert.Null(control.Value);
    }

    [StaFact]
    public void SettingItems_PopulatesFilteredRowsAndStatus()
    {
        var control = CreateWithItems();

        Assert.Equal(Fruits.Length, control.FilteredRows.Count);
        Assert.False(control.IsEmpty);
        Assert.Equal("7 results", control.StatusText);
    }

    [StaFact]
    public void SettingQuery_FiltersRows()
    {
        var control = CreateWithItems();

        control.Value = "ap";

        Assert.Equal(new[] { "Apple", "Apricot", "Grape" }, RowTexts(control));
    }

    [StaFact]
    public void QueryWithNoMatches_MarksEmpty_AndSingularStatus()
    {
        var control = CreateWithItems();

        control.Value = "zzz";
        Assert.True(control.IsEmpty);
        Assert.Equal("0 results", control.StatusText);

        control.Value = "Banana";
        Assert.Equal("1 result", control.StatusText);
    }

    [StaFact]
    public void HighlightedIndexChange_UpdatesRowFlags()
    {
        var control = CreateWithItems();

        control.HighlightedIndex = 2;

        Assert.False(control.FilteredRows[1].IsHighlighted);
        Assert.True(control.FilteredRows[2].IsHighlighted);
    }

    // ----- Keyboard (contract table) -----

    [StaFact]
    public void ArrowDown_OpensWhenClosed_WithoutHighlighting()
    {
        var control = CreateWithItems();

        var args = SendKey(control, Key.Down);

        Assert.True(control.IsOpen);
        Assert.Equal(-1, control.HighlightedIndex);
        Assert.True(args.Handled);
    }

    [StaFact]
    public void ArrowDown_MovesHighlightDown_WhenOpen()
    {
        var control = CreateWithItems();
        control.IsOpen = true;

        SendKey(control, Key.Down);
        Assert.Equal(0, control.HighlightedIndex);

        SendKey(control, Key.Down);
        Assert.Equal(1, control.HighlightedIndex);
    }

    [StaFact]
    public void ArrowUp_OpensAndHighlightsLast_WhenClosed()
    {
        var control = CreateWithItems();

        SendKey(control, Key.Up);

        Assert.True(control.IsOpen);
        Assert.Equal(Fruits.Length - 1, control.HighlightedIndex);
    }

    [StaFact]
    public void ArrowDown_DoesNotWrapPastEnd()
    {
        var control = CreateWithItems();
        control.IsOpen = true;
        control.HighlightedIndex = Fruits.Length - 1;

        SendKey(control, Key.Down);

        Assert.Equal(Fruits.Length - 1, control.HighlightedIndex);
    }

    [StaFact]
    public void Enter_CommitsHighlighted_SetsValueAndCloses()
    {
        var control = CreateWithItems();
        var raised = 0;
        control.IsOpen = true;
        control.HighlightedIndex = 2; // "Banana"

        DependencyPropertyDescriptor
            .FromProperty(NaviusAutocompleteBase.ValueProperty, typeof(NaviusAutocompleteBase))
            .AddValueChanged(control, (_, _) => raised++);

        SendKey(control, Key.Enter);

        Assert.Equal("Banana", control.Value);
        Assert.False(control.IsOpen);
        Assert.True(raised >= 1);
    }

    [StaFact]
    public void Escape_ClosesAndIsHandled()
    {
        var control = CreateWithItems();
        control.IsOpen = true;

        var args = SendKey(control, Key.Escape);

        Assert.False(control.IsOpen);
        Assert.True(args.Handled);
    }

    [StaFact]
    public void Tab_Closes_ButDoesNotHandleTheEvent()
    {
        var control = CreateWithItems();
        control.IsOpen = true;

        var args = SendKey(control, Key.Tab);

        Assert.False(control.IsOpen);
        Assert.False(args.Handled);
    }

    [StaFact]
    public void HomeAndEnd_JumpToFirstAndLast()
    {
        var control = CreateWithItems();
        control.IsOpen = true;

        SendKey(control, Key.End);
        Assert.Equal(Fruits.Length - 1, control.HighlightedIndex);

        SendKey(control, Key.Home);
        Assert.Equal(0, control.HighlightedIndex);
    }

    [StaFact]
    public void PageDownAndPageUp_JumpToLastAndFirst()
    {
        var control = CreateWithItems();
        control.IsOpen = true;

        SendKey(control, Key.PageDown);
        Assert.Equal(Fruits.Length - 1, control.HighlightedIndex);

        SendKey(control, Key.PageUp);
        Assert.Equal(0, control.HighlightedIndex);
    }

    // ----- Virtual focus (structural guarantee) + template + overlay wiring -----

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var control = CreateThemed();

        Assert.True(control.ApplyTemplate());
    }

    [StaFact]
    public void PopupListAndContent_AreNotFocusable_SoFocusNeverLeavesTheInput()
    {
        var control = CreateThemed();
        control.ApplyTemplate();

        var list = (ItemsControl)control.Template.FindName("PART_List", control)!;
        var popupContent = (FrameworkElement)control.Template.FindName("PART_PopupContent", control)!;
        var input = (TextBox)control.Template.FindName("PART_Input", control)!;

        // The rows/listbox never take real WPF focus: highlight is a data pointer only, so keyboard
        // focus stays on the TextBox at all times (the strict virtual-focus model).
        Assert.False(list.Focusable);
        Assert.False(popupContent.Focusable);
        Assert.True(input.Focusable);
    }

    [StaFact]
    public void Open_PushesANonFocusTrappingDismissableOverlaySession()
    {
        var control = CreateThemed(withItems: true);
        var window = new Window { Content = control };
        control.ApplyTemplate();

        control.IsOpen = true;

        var session = OverlayStack.GetFor(window).Topmost;
        Assert.NotNull(session);
        Assert.True(session!.Options.CloseOnEscape);
        Assert.True(session.Options.CloseOnOutsideClick);
        // Virtual focus: the overlay must NOT trap/move focus into the popup.
        Assert.False(session.Options.TrapFocus);
        Assert.False(session.Options.RestoreFocus);
    }

    [StaFact]
    public void Close_UnwindsTheOverlaySession()
    {
        var control = CreateThemed(withItems: true);
        var window = new Window { Content = control };
        control.ApplyTemplate();
        control.IsOpen = true;
        Assert.NotNull(OverlayStack.GetFor(window).Topmost);

        control.IsOpen = false;

        Assert.Null(OverlayStack.GetFor(window).Topmost);
    }

    [StaFact]
    public void AutomationPeer_ReportsComboBox_AndExposesValueAsName()
    {
        var control = CreateWithItems();
        control.Value = "Cherry";

        var peer = control.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(control, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.ComboBox, peer!.GetAutomationControlType());
        Assert.Equal("Cherry", peer.GetName());
    }

    private static IEnumerable<string> RowTexts(NaviusAutocompleteBase control)
    {
        var texts = new List<string>();
        foreach (var row in control.FilteredRows)
        {
            texts.Add(row.Text);
        }

        return texts;
    }
}
