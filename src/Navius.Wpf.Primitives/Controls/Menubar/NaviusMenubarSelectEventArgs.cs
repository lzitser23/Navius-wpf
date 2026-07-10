using System.ComponentModel;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Cancelable event args mirroring the contract's NaviusSelectEventArgs: raised when an
/// item/checkbox-item/radio-item is activated. Setting <see cref="CancelEventArgs.Cancel"/>
/// (via <c>PreventDefault</c>) keeps the owning (sub)menu open instead of the native default of
/// closing it.
/// </summary>
public class NaviusMenubarSelectEventArgs : CancelEventArgs
{
    /// <summary>Alias for <see cref="CancelEventArgs.Cancel"/>, matching the web contract's naming.</summary>
    public void PreventDefault() => Cancel = true;
}
