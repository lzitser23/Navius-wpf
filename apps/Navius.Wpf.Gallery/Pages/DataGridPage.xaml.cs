using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusDataGrid: a small filterable/sortable demo (DataGridDemo) plus a 10,000-row
/// demo (DataGrid10kDemo) that exercises native row virtualization, the milestone's perf gate.
/// </summary>
public partial class DataGridPage : UserControl
{
    private sealed record Person(string Name, int Age, string City, string Role);

    private sealed record Row(int Id, string Name, double Value);

    public DataGridPage()
    {
        InitializeComponent();

        DemoGrid.ItemsSource = new List<Person>
        {
            new("Ada Lovelace", 36, "London", "Mathematician"),
            new("Grace Hopper", 85, "New York", "Rear Admiral"),
            new("Alan Turing", 41, "London", "Cryptanalyst"),
            new("Katherine Johnson", 101, "Hampton", "Mathematician"),
            new("Margaret Hamilton", 88, "Paoli", "Software Engineer"),
            new("Barbara Liskov", 85, "Cambridge", "Computer Scientist"),
            new("Donald Knuth", 87, "Stanford", "Computer Scientist"),
            new("Edsger Dijkstra", 72, "Rotterdam", "Computer Scientist"),
            new("Dennis Ritchie", 70, "Bronxville", "Computer Scientist"),
            new("Frances Allen", 88, "Peru", "Computer Scientist"),
        };

        // The perf-gate row set: 10,000 rows served through the real, virtualized grid.
        var big = new List<Row>(10_000);
        for (var i = 1; i <= 10_000; i++)
        {
            big.Add(new Row(i, $"Item {i:D5}", (i * 1.6180339887) % 1000));
        }

        BigGrid.ItemsSource = big;
    }
}
