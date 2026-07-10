using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Menubar;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates the Menubar family: a File/Edit/View bar with a submenu, a separator, a
/// label + checkbox items (one tri-state/indeterminate), and a label + radio group.
/// </summary>
public partial class MenubarPage : UserControl
{
    public MenubarPage()
    {
        InitializeComponent();

        WordWrapItem.Select += (_, args) =>
            StatusText.Text = $"Word wrap: {WordWrapItem.Checked}";
        AutoSaveItem.Select += (_, args) =>
            StatusText.Text = $"Autosave: {AutoSaveItem.Checked}";
    }
}
