using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusSortable: a whole-row drag list with one disabled (keyboard-skipped) row, and
/// a handle-scoped list where drag starts only from the NaviusSortableItemHandle grip. Both lists
/// also support the APG keyboard grab-and-move model (Space to grab, arrows to move, Space to drop,
/// Escape to cancel).
/// </summary>
public partial class SortablePage : UserControl
{
    public SortablePage()
    {
        InitializeComponent();
    }
}
