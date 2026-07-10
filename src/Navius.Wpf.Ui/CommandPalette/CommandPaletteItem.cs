using System.Windows.Input;

namespace Navius.Wpf.Ui.CommandPalette;

/// <summary>A single runnable entry in a NaviusCommandPalette's list.</summary>
public sealed class CommandPaletteItem
{
    public CommandPaletteItem(string label, string group = "", ICommand? command = null, object? commandParameter = null, object? icon = null)
    {
        Label = label;
        Group = group;
        Command = command;
        CommandParameter = commandParameter;
        Icon = icon;
    }

    public string Label { get; }

    /// <summary>Section this item is displayed under (e.g. "Navigation", "Actions"). Empty string means ungrouped.</summary>
    public string Group { get; }

    public ICommand? Command { get; }

    public object? CommandParameter { get; }

    public object? Icon { get; }

    public override string ToString() => Label;
}
