using System;
using System.Collections.Generic;
using Navius.Wpf.Primitives.Controls.Autocomplete;

namespace Navius.Wpf.Ui.CommandPalette;

/// <summary>
/// Pure filter/highlight/execute logic for NaviusCommandPalette, zero WPF-control dependency so it
/// is unit tested directly. Deliberately reuses Navius.Wpf.Primitives' AutocompleteEngine rather
/// than duplicating its filtering/highlight-movement math (both public static methods; consuming
/// the Autocomplete primitive, per the composite tier's brief, rather than reimplementing it) --
/// this class only supplies the CommandPaletteItem-shaped surface (matching on Label + Group) and
/// the execute step Autocomplete has no equivalent for.
/// </summary>
public static class CommandPaletteEngine
{
    public static IReadOnlyList<CommandPaletteItem> Filter(IReadOnlyList<CommandPaletteItem> items, string? query) =>
        AutocompleteEngine.Filter(items, query, ItemToSearchText, filter: null);

    public static int MoveHighlight(int current, int count, int delta, bool loop = false) =>
        AutocompleteEngine.MoveHighlight(current, count, delta, loop);

    /// <summary>Runs the item's command if present and executable. Returns true if it ran.</summary>
    public static bool Execute(CommandPaletteItem? item)
    {
        if (item?.Command is null)
        {
            return false;
        }

        if (!item.Command.CanExecute(item.CommandParameter))
        {
            return false;
        }

        item.Command.Execute(item.CommandParameter);
        return true;
    }

    private static string ItemToSearchText(CommandPaletteItem item) => $"{item.Label} {item.Group}";
}
