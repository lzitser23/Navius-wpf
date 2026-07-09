using System.ComponentModel;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Cancelable event args mirroring the contract's NaviusSelectEventArgs, used by
/// NaviusNavigationMenuLink's Select event.
/// </summary>
public class NaviusNavigationMenuSelectEventArgs : CancelEventArgs
{
    /// <summary>Alias for <see cref="CancelEventArgs.Cancel"/>, matching the web contract's naming.</summary>
    public void PreventDefault() => Cancel = true;
}
