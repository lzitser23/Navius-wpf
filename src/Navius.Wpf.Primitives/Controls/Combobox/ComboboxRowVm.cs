using System.ComponentModel;

namespace Navius.Wpf.Primitives.Controls.Combobox;

/// <summary>
/// One filtered popup row. Carries the boxed committed value, its display text, and its index within
/// the currently filtered list, plus the two transient visual flags the row template binds to.
///
/// This is the WPF stand-in for the web's <c>ComboboxItemData</c> + <c>ComboboxItemContext</c>: the
/// "highlighted" row is purely a data pointer (virtual focus), never a WPF focus target, so the flag
/// lives here and the row's highlight visual comes from a template trigger on it, not from real
/// keyboard focus or a native Selector's IsSelected. Implements INotifyPropertyChanged so moving the
/// highlight only pokes two rows' flags instead of rebuilding the whole list.
/// </summary>
public sealed class ComboboxRowVm : INotifyPropertyChanged
{
    private bool _isHighlighted;
    private bool _isSelected;

    public ComboboxRowVm(object value, string text, int index)
    {
        Value = value;
        Text = text;
        Index = index;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The boxed committed value this row represents (the item itself; items ARE the values).</summary>
    public object Value { get; }

    /// <summary>The display label, from the root's ItemToString.</summary>
    public string Text { get; }

    /// <summary>Index within the currently filtered list (not the full Items list).</summary>
    public int Index { get; }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value)
            {
                return;
            }

            _isHighlighted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}

/// <summary>
/// One selected-value chip (multi-select). The web's <c>ComboboxChipContext</c> cascades the value
/// down to the chip's remove button so it can remove BY VALUE; here the same value rides on the VM
/// and is passed as the <see cref="NaviusComboboxBase.RemoveChipCommand"/> parameter.
/// </summary>
public sealed class ComboboxChipVm
{
    public ComboboxChipVm(object value, string text)
    {
        Value = value;
        Text = text;
    }

    public object Value { get; }

    public string Text { get; }
}
