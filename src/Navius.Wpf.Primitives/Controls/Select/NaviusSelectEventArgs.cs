using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Select;

/// <summary>
/// Cancelable event args for the contract's NaviusSelectItem.OnSelect: mirrors the source's
/// <c>NaviusSelectEventArgs.PreventDefault()</c>, which keeps the listbox open and skips applying
/// the value. Deliberately distinct from the Menu family's identically-shaped
/// <c>Navius.Wpf.Primitives.Controls.Menus.NaviusSelectEventArgs</c> (a different namespace) so
/// the two families never collide; they are not shared because their owners commit differently
/// (a Menu closes the whole chain, a Select commits a value and only closes in single mode).
/// </summary>
public class NaviusSelectEventArgs : RoutedEventArgs
{
    public NaviusSelectEventArgs(RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
    }

    public bool IsDefaultPrevented { get; private set; }

    public void PreventDefault() => IsDefaultPrevented = true;
}
