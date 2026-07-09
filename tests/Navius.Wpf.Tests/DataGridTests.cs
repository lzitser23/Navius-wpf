using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Navius.Wpf.Primitives.Controls.DataGrid;

namespace Navius.Wpf.Tests;

public class DataGridTests
{
    static DataGridTests()
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

    private sealed record Person(string Name, int Age, string City);

    private static List<Person> SamplePeople() =>
    [
        new("Ada Lovelace", 36, "London"),
        new("Grace Hopper", 85, "New York"),
        new("Alan Turing", 41, "London"),
        new("Katherine Johnson", 101, "Hampton"),
    ];

    // --- Constructor defaults (the brief's re-template contract) ---

    [StaFact]
    public void Defaults_VirtualizationFlagsOn()
    {
        var grid = new NaviusDataGrid();

        Assert.True(grid.EnableRowVirtualization);
        Assert.True(grid.EnableColumnVirtualization);
    }

    [StaFact]
    public void Defaults_AutoGenerateColumnsOff()
    {
        Assert.False(new NaviusDataGrid().AutoGenerateColumns);
    }

    [StaFact]
    public void Defaults_HeadersVisibilityColumnOnly()
    {
        Assert.Equal(DataGridHeadersVisibility.Column, new NaviusDataGrid().HeadersVisibility);
    }

    [StaFact]
    public void Defaults_GridLinesNone()
    {
        Assert.Equal(DataGridGridLinesVisibility.None, new NaviusDataGrid().GridLinesVisibility);
    }

    [StaFact]
    public void Defaults_ReadOnlyRows()
    {
        var grid = new NaviusDataGrid();

        Assert.False(grid.CanUserAddRows);
        Assert.False(grid.CanUserDeleteRows);
    }

    [StaFact]
    public void Defaults_SelectionModeIsExtended()
    {
        // Documented default: closest parity to the web's set-based multi-key row selection.
        Assert.Equal(DataGridSelectionMode.Extended, new NaviusDataGrid().SelectionMode);
    }

    [StaFact]
    public void Defaults_NoFilterState()
    {
        var grid = new NaviusDataGrid();

        Assert.Null(grid.GlobalFilter);
        Assert.Null(grid.FilterFn);
        Assert.Null(grid.RowKeySelector);
    }

    // --- DefaultFilterMatch: reflection-based, stringified, case-insensitive Contains ---

    [StaFact]
    public void DefaultFilterMatch_MatchesStringProperty()
    {
        Assert.True(NaviusDataGrid.DefaultFilterMatch(new Person("Ada Lovelace", 36, "London"), "lovelace"));
    }

    [StaFact]
    public void DefaultFilterMatch_IsCaseInsensitive()
    {
        Assert.True(NaviusDataGrid.DefaultFilterMatch(new Person("Ada Lovelace", 36, "London"), "LONDON"));
    }

    [StaFact]
    public void DefaultFilterMatch_MatchesStringifiedNumericProperty()
    {
        Assert.True(NaviusDataGrid.DefaultFilterMatch(new Person("Ada Lovelace", 36, "London"), "36"));
    }

    [StaFact]
    public void DefaultFilterMatch_NoMatchReturnsFalse()
    {
        Assert.False(NaviusDataGrid.DefaultFilterMatch(new Person("Ada Lovelace", 36, "London"), "zzz"));
    }

    [StaFact]
    public void DefaultFilterMatch_EmptyFilterMatchesEverything()
    {
        var person = new Person("Ada Lovelace", 36, "London");

        Assert.True(NaviusDataGrid.DefaultFilterMatch(person, string.Empty));
    }

    // --- GlobalFilter wires a predicate onto the default ICollectionView ---

    [StaFact]
    public void GlobalFilter_FiltersDefaultView()
    {
        var grid = new NaviusDataGrid { ItemsSource = SamplePeople() };

        grid.GlobalFilter = "London";

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        var matches = view.Cast<Person>().ToList();

        Assert.Equal(2, matches.Count);
        Assert.All(matches, p => Assert.Equal("London", p.City));
    }

    [StaFact]
    public void GlobalFilter_ClearedShowsAllRows()
    {
        var grid = new NaviusDataGrid { ItemsSource = SamplePeople() };
        grid.GlobalFilter = "London";

        grid.GlobalFilter = null;

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        Assert.Equal(4, view.Cast<Person>().Count());
    }

    [StaFact]
    public void GlobalFilter_EmptyStringShowsAllRows()
    {
        var grid = new NaviusDataGrid { ItemsSource = SamplePeople() };
        grid.GlobalFilter = "London";

        grid.GlobalFilter = string.Empty;

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        Assert.Null(view.Filter);
        Assert.Equal(4, view.Cast<Person>().Count());
    }

    [StaFact]
    public void FilterFn_OverridesDefaultPredicate()
    {
        var grid = new NaviusDataGrid
        {
            ItemsSource = SamplePeople(),
            // Ignore the filter text entirely: keep only centenarians.
            FilterFn = (row, _) => row is Person { Age: >= 100 },
        };

        grid.GlobalFilter = "irrelevant";

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        var matches = view.Cast<Person>().ToList();

        Assert.Single(matches);
        Assert.Equal("Katherine Johnson", matches[0].Name);
    }

    [StaFact]
    public void FilterFn_SetAfterFilterReappliesImmediately()
    {
        var grid = new NaviusDataGrid { ItemsSource = SamplePeople() };
        grid.GlobalFilter = "London";

        // Changing FilterFn must re-run the predicate, not leave the stale default in place.
        grid.FilterFn = (row, _) => row is Person { City: "New York" };

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        var matches = view.Cast<Person>().ToList();

        Assert.Single(matches);
        Assert.Equal("Grace Hopper", matches[0].Name);
    }

    // --- RowKeySelector: the web's RowKey, falling back to the row itself ---

    [StaFact]
    public void GetRowKey_FallsBackToRowWhenSelectorNull()
    {
        var grid = new NaviusDataGrid();
        var row = new Person("Ada Lovelace", 36, "London");

        Assert.Same(row, grid.GetRowKey(row));
    }

    [StaFact]
    public void GetRowKey_UsesSelectorWhenSet()
    {
        var grid = new NaviusDataGrid { RowKeySelector = row => ((Person)row).Name };
        var row = new Person("Ada Lovelace", 36, "London");

        Assert.Equal("Ada Lovelace", grid.GetRowKey(row));
    }

    // --- SortDescriptionsSnapshot: read-only view of the native sort surface ---

    [StaFact]
    public void SortDescriptionsSnapshot_ReflectsItemsSortDescriptions()
    {
        var grid = new NaviusDataGrid { ItemsSource = SamplePeople() };
        grid.Items.SortDescriptions.Add(new SortDescription("Age", ListSortDirection.Descending));

        var snapshot = grid.SortDescriptionsSnapshot;

        Assert.Single(snapshot);
        Assert.Equal("Age", snapshot[0].PropertyName);
        Assert.Equal(ListSortDirection.Descending, snapshot[0].Direction);
    }

    [StaFact]
    public void SortDescriptionsSnapshot_IsEmptyByDefault()
    {
        Assert.Empty(new NaviusDataGrid().SortDescriptionsSnapshot);
    }

    // --- Perf guard: the re-template must not silently regress virtualization ---

    [StaFact]
    public void StyleApplication_PreservesVirtualization()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DataGrid.xaml",
                UriKind.Absolute),
        };

        var style = (Style)dictionary[typeof(NaviusDataGrid)];
        Assert.NotNull(style);

        // Exercise the real shipped path: a constructed NaviusDataGrid with the shipped Style applied
        // (assigning .Style runs the Setters without needing a live visual tree or .Show()).
        var grid = new NaviusDataGrid { Style = style };

        // Constructor defaults survive.
        Assert.True(grid.EnableRowVirtualization);
        Assert.True(grid.EnableColumnVirtualization);

        // Belt-and-suspenders attached properties the Style pins.
        Assert.True((bool)grid.GetValue(VirtualizingPanel.IsVirtualizingProperty));
        Assert.Equal(
            VirtualizationMode.Recycling,
            (VirtualizationMode)grid.GetValue(VirtualizingPanel.VirtualizationModeProperty));
    }

    [StaFact]
    public void Style_TargetsNaviusDataGrid()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/DataGrid.xaml",
                UriKind.Absolute),
        };

        var style = (Style)dictionary[typeof(NaviusDataGrid)];

        Assert.Equal(typeof(NaviusDataGrid), style.TargetType);
    }
}
