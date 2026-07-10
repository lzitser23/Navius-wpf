using System.Windows.Input;

namespace Navius.Wpf.Ui.Sidebar;

/// <summary>
/// Pure logic for NaviusSidebar's two small state machines: which flattened nav item should take
/// focus next (roving focus across sections, no wrap, matching the AutocompleteEngine/
/// PaginationEngine precedent of zero WPF dependency so it is directly unit testable), and the
/// binary collapsed/expanded toggle. No wrap-around: ArrowDown from the last item, or ArrowUp from
/// the first, stays put rather than cycling, mirroring AutocompleteEngine.MoveHighlight's default.
/// </summary>
public static class SidebarNavigation
{
    /// <summary>
    /// Returns the next roving-focus index for <paramref name="key"/>, or -1 if the key is not a
    /// navigation key this sidebar handles or there are no items to focus.
    /// </summary>
    public static int MoveFocus(int currentIndex, int itemCount, Key key)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        return key switch
        {
            Key.Down => currentIndex < 0 ? 0 : System.Math.Min(itemCount - 1, currentIndex + 1),
            Key.Up => currentIndex < 0 ? itemCount - 1 : System.Math.Max(0, currentIndex - 1),
            Key.Home => 0,
            Key.End => itemCount - 1,
            _ => -1,
        };
    }

    public static bool ToggleCollapsed(bool isCollapsed) => !isCollapsed;
}
