namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>Fired by NaviusTree.SelectedValuesChanged with the full current selection, mirroring the contract's OnSelectionChange.</summary>
public sealed class TreeSelectionChangedEventArgs : EventArgs
{
    public TreeSelectionChangedEventArgs(IReadOnlyList<object> selectedValues)
    {
        SelectedValues = selectedValues;
    }

    public IReadOnlyList<object> SelectedValues { get; }
}
