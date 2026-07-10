using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Tree;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusTree: a small multi-select fixture (with a disabled leaf) plus a
/// 10,000-node demo section for the milestone's virtualization perf gate. Self-contained, no
/// navigation wiring (see this family's HARD RULES in the task brief).
/// </summary>
public partial class TreePage : UserControl
{
    public TreePage()
    {
        InitializeComponent();

        Demo.RootNodes = BuildFruitFixture();
        Demo10k.RootNodes = Build10kNodes();
    }

    private static IReadOnlyList<NaviusTreeNode> BuildFruitFixture()
    {
        var fuji = new NaviusTreeNode("apple-fuji", "Fuji");
        var gala = new NaviusTreeNode("apple-gala", "Gala");
        var apple = new NaviusTreeNode("apple", "Apple", new[] { fuji, gala }) { IsExpanded = true };

        var plantain = new NaviusTreeNode("banana-plantain", "Plantain (disabled)", disabled: true);
        var banana = new NaviusTreeNode("banana", "Banana", new[] { plantain }) { IsExpanded = true };

        var cherry = new NaviusTreeNode("cherry", "Cherry");

        return new[] { apple, banana, cherry };
    }

    private static IReadOnlyList<NaviusTreeNode> Build10kNodes()
    {
        var groups = new List<NaviusTreeNode>(100);
        for (var g = 0; g < 100; g++)
        {
            var children = new List<NaviusTreeNode>(99);
            for (var c = 0; c < 99; c++)
            {
                children.Add(new NaviusTreeNode($"g{g}-c{c}", $"Item {g}-{c}"));
            }

            groups.Add(new NaviusTreeNode($"g{g}", $"Group {g}", children));
        }

        return groups;
    }

    private void OnDemoSelectionChanged(object? sender, TreeSelectionChangedEventArgs e)
    {
        SelectionSummary.Text = e.SelectedValues.Count == 0
            ? "Selection: (none)"
            : $"Selection: {string.Join(", ", e.SelectedValues)}";
    }
}
