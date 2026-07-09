using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Cancelable event args for the contract's OnSelect (Item/CheckboxItem/RadioItem): mirrors
/// the source's `NaviusSelectEventArgs.PreventDefault()`, which keeps the menu open instead
/// of letting the activation close it. Shared by both the Menu and ContextMenu families,
/// whose item-level parts are identical per their parity docs.
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
