using System.ComponentModel;

namespace Navius.Wpf.Primitives.Controls.TagInput;

/// <summary>
/// One committed chip. The WPF stand-in for the web's <c>NaviusTag</c> + cascaded
/// <c>TagValueContext</c>: the chip's text and index ride on the VM, the remove button passes the
/// whole VM as its command parameter, and the roving highlight is an INPC flag driven by the root
/// (same idiom as ComboboxRowVm, but the highlight here IS a real focus target per the contract's
/// roving-tabindex model, unlike the Combobox's virtual focus).
/// </summary>
public sealed class TagChipVm : INotifyPropertyChanged
{
    private bool _isHighlighted;

    public TagChipVm(string text, int index)
    {
        Text = text;
        Index = index;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The tag string.</summary>
    public string Text { get; }

    /// <summary>The chip's position in the committed list (the list is never filtered, so index removal is safe here).</summary>
    public int Index { get; }

    /// <summary>The remove button's accessible name (the web's default <c>aria-label="Remove {value}"</c>).</summary>
    public string RemoveName => $"Remove {Text}";

    /// <summary>True when this chip is the roving keyboard target (the web's data-highlighted + tabindex=0).</summary>
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
}
