using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Select;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class SelectTests : IDisposable
{
    private sealed record NamedOption(string Name);

    private sealed record WrappedOption(NamedOption Inner);

    private sealed record DualOption(string Code, string Label);

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
    // style 0 = no WS_VISIBLE bit) is the lightest real one available headlessly. Lazily created
    // (not a static field initializer) and disposed per test instance -- it must not outlive the
    // STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusSelectTests", IntPtr.Zero);

    public void Dispose()
    {
        _testSource?.Dispose();
        TestCleanup.PumpDispatcher();
    }

    private void SimulateKey(NaviusSelectBase select, Key key)
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
    public void NonGenericSelect_LoadsFromXaml_AndDisplaysBoundItems()
    {
        var select = Assert.IsType<NaviusSelect>(XamlReader.Parse(
            "<select:NaviusSelect xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:select='clr-namespace:Navius.Wpf.Primitives.Controls.Select;assembly=Navius.Wpf.Primitives' />"));
        var options = new[] { new NamedOption("Apple"), new NamedOption("Banana") };
        select.Resources = CreateThemedScope();
        select.Style = (Style)select.Resources[typeof(NaviusSelectBase)];
        select.DisplayMemberPath = nameof(NamedOption.Name);
        select.ItemsSource = options;
        var window = new Window
        {
            Content = select,
            Width = 300,
            Height = 200,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            select.IsOpen = true;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            var option = Assert.IsType<NaviusSelectItem>(select.ItemContainerGenerator.ContainerFromIndex(1));

            Assert.Same(options[1], option.Value);
            Assert.Equal("Banana", option.DisplayText);

            option.RaiseSelectEvent();

            Assert.Same(options[1], select.Value);
            Assert.Equal("Banana", select.DisplayText);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NonGenericSelect_RendersDataBoundRows_ThroughItemTemplate()
    {
        var select = Assert.IsType<NaviusSelect>(XamlReader.Parse(
            "<select:NaviusSelect xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:select='clr-namespace:Navius.Wpf.Primitives.Controls.Select;assembly=Navius.Wpf.Primitives'>" +
            "<select:NaviusSelect.ItemTemplate><DataTemplate><TextBlock Text='{Binding Name}' /></DataTemplate></select:NaviusSelect.ItemTemplate>" +
            "</select:NaviusSelect>"));
        var options = new[] { new NamedOption("Apple"), new NamedOption("Banana") };
        select.Resources = CreateThemedScope();
        select.Style = (Style)select.Resources[typeof(NaviusSelectBase)];
        select.ItemsSource = options;
        var window = new Window
        {
            Content = select,
            Width = 300,
            Height = 200,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            select.IsOpen = true;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            var option = Assert.IsType<NaviusSelectItem>(select.ItemContainerGenerator.ContainerFromIndex(1));

            Assert.Same(select.ItemTemplate, option.ContentTemplate);
            Assert.Same(options[1], option.Content);
            Assert.NotNull(FindTextBlock(option, "Banana"));
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class NameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NameTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container) =>
            item is NamedOption ? NameTemplate : null;
    }

    [StaFact]
    public void NonGenericSelect_RendersDataBoundRows_ThroughItemTemplateSelector()
    {
        var template = (DataTemplate)XamlReader.Parse(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Name}' /></DataTemplate>");
        var selector = new NameTemplateSelector { NameTemplate = template };
        var select = Assert.IsType<NaviusSelect>(XamlReader.Parse(
            "<select:NaviusSelect xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:select='clr-namespace:Navius.Wpf.Primitives.Controls.Select;assembly=Navius.Wpf.Primitives' />"));
        var options = new[] { new NamedOption("Apple"), new NamedOption("Banana") };
        select.Resources = CreateThemedScope();
        select.Style = (Style)select.Resources[typeof(NaviusSelectBase)];
        select.ItemTemplateSelector = selector;
        select.ItemsSource = options;
        var window = new Window
        {
            Content = select,
            Width = 300,
            Height = 200,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            select.IsOpen = true;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            var option = Assert.IsType<NaviusSelectItem>(select.ItemContainerGenerator.ContainerFromIndex(1));

            Assert.Same(selector, option.ContentTemplateSelector);
            Assert.Same(options[1], option.Content);
            Assert.NotNull(FindTextBlock(option, "Banana"));
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void NonGenericSelect_ResolvesDottedDisplayMemberPath_ForTheTriggerLabel()
    {
        var options = new[] { new WrappedOption(new NamedOption("Apple")), new WrappedOption(new NamedOption("Banana")) };
        var select = new NaviusSelect { DisplayMemberPath = "Inner.Name", ItemsSource = options };

        select.Value = options[1];

        Assert.Equal("Banana", select.DisplayText);
    }

    [StaFact]
    public void NonGenericSelect_ReresolvesLabels_WhenDisplayMemberPathChanges()
    {
        var select = Assert.IsType<NaviusSelect>(XamlReader.Parse(
            "<select:NaviusSelect xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:select='clr-namespace:Navius.Wpf.Primitives.Controls.Select;assembly=Navius.Wpf.Primitives' />"));
        var options = new[] { new DualOption("A1", "Apple"), new DualOption("B1", "Banana") };
        select.Resources = CreateThemedScope();
        select.Style = (Style)select.Resources[typeof(NaviusSelectBase)];
        select.DisplayMemberPath = nameof(DualOption.Code);
        select.ItemsSource = options;
        var window = new Window
        {
            Content = select,
            Width = 300,
            Height = 200,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            select.IsOpen = true;
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            var option = Assert.IsType<NaviusSelectItem>(select.ItemContainerGenerator.ContainerFromIndex(1));
            select.Value = options[1];
            Assert.Equal("B1", option.DisplayText);
            Assert.Equal("B1", select.DisplayText);

            select.DisplayMemberPath = nameof(DualOption.Label);

            Assert.Equal("Banana", option.DisplayText);
            Assert.Equal("Banana", select.DisplayText);
        }
        finally
        {
            window.Close();
        }
    }

    private static TextBlock? FindTextBlock(DependencyObject root, string text)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock textBlock && textBlock.Text == text)
            {
                return textBlock;
            }

            if (FindTextBlock(child, text) is { } match)
            {
                return match;
            }
        }

        return null;
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

        try
        {
            var item = ItemAt(select, 1);
            item.Select += (_, e) => ((NaviusSelectEventArgs)e).PreventDefault();

            item.RaiseSelectEvent();

            Assert.Null(select.Value);
            Assert.False(item.IsSelectedValue);
            Assert.True(select.IsOpen);
        }
        finally
        {
            select.IsOpen = false;
        }
    }

    [StaFact]
    public void Multiple_TogglesInSet_AndKeepsPopupOpen()
    {
        var select = CreateSelect(multiple: true);
        select.IsOpen = true;

        try
        {
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
        finally
        {
            select.IsOpen = false;
        }
    }

    [StaFact]
    public void ClosedTrigger_ArrowDownOpens_HighlightsFirst()
    {
        var select = CreateSelect();

        try
        {
            SimulateKey(select, Key.Down);

            Assert.True(select.IsOpen);
            Assert.True(ItemAt(select, 0).IsHighlightedValue);
        }
        finally
        {
            select.IsOpen = false;
        }
    }

    [StaFact]
    public void ClosedTrigger_ArrowUpOpens_HighlightsLast()
    {
        var select = CreateSelect();

        try
        {
            SimulateKey(select, Key.Up);

            Assert.True(select.IsOpen);
            Assert.True(ItemAt(select, 2).IsHighlightedValue);
        }
        finally
        {
            select.IsOpen = false;
        }
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

        try
        {
            SimulateKey(select, Key.End);
            Assert.True(ItemAt(select, 2).IsHighlightedValue);

            SimulateKey(select, Key.Home);
            Assert.True(ItemAt(select, 0).IsHighlightedValue);
        }
        finally
        {
            select.IsOpen = false;
        }
    }

    [StaFact]
    public void ArrowDown_ClampsAtEnd_WhenLoopIsFalse()
    {
        var select = CreateSelect();
        SimulateKey(select, Key.Up); // open, highlight last (cherry, index 2)

        try
        {
            SimulateKey(select, Key.Down); // clamp (Loop=false)

            Assert.True(ItemAt(select, 2).IsHighlightedValue);
        }
        finally
        {
            select.IsOpen = false;
        }
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

        try
        {
            SimulateKey(select, Key.C); // jump to Cherry

            Assert.True(ItemAt(select, 2).IsHighlightedValue);
        }
        finally
        {
            select.IsOpen = false;
        }
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

    [StaFact]
    public void AutomationPeer_ExposesReadOnlyValuePattern_SurfacingDisplayText()
    {
        var select = CreateSelect();
        var rootPeer = typeof(NaviusSelectBase)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(select, null) as AutomationPeer;

        var provider = (IValueProvider)rootPeer!.GetPattern(PatternInterface.Value);
        Assert.NotNull(provider);
        Assert.True(provider.IsReadOnly);
        Assert.Equal("Pick", provider.Value); // placeholder while nothing is selected

        select.Value = "cherry";
        Assert.Equal("Cherry", provider.Value);
    }

    [StaFact]
    public void AutomationPeer_ExposesExpandCollapsePattern_TrackingOpenState()
    {
        var select = CreateSelect();
        var rootPeer = typeof(NaviusSelectBase)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(select, null) as AutomationPeer;

        var provider = (IExpandCollapseProvider)rootPeer!.GetPattern(PatternInterface.ExpandCollapse);
        Assert.NotNull(provider);
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);

        provider.Expand();
        Assert.True(select.IsOpen);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        provider.Collapse();
        Assert.False(select.IsOpen);
    }
}
