using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier A: derives from the native MenuItem (leaf, no checkable/radio state). Roving focus,
/// typeahead, mnemonics, and submenu-nesting are inherited for free; see
/// NaviusMenuItemBase for the shared OnSelect/close-on-activate plumbing.
///
/// Role == SubmenuHeader (this item has child items) is special-cased: native click on a
/// header opens/closes the submenu rather than "selecting" and closing the whole menu, so
/// that path defers entirely to base.OnClick() instead of raising Select.
/// </summary>
public class NaviusMenuItem : NaviusMenuItemBase
{
    static NaviusMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenuItem)));
    }

    protected override void OnClick()
    {
        if (Role == MenuItemRole.SubmenuHeader)
        {
            base.OnClick();
            return;
        }

        var args = RaiseSelect();
        ExecuteCommand();

        if (!args.IsDefaultPrevented)
        {
            CloseOwningMenu(this);
        }
    }

    /// <summary>
    /// Mirrors native ICommandSource execution (CommandHelpers.CriticalExecuteCommandSource):
    /// a RoutedCommand's plain ICommand.CanExecute/Execute overloads implicitly target
    /// Keyboard.FocusedElement, which is null in a headless test (and not necessarily this
    /// item during a real click either), so a RoutedCommand is routed explicitly at
    /// CommandTarget ?? this instead.
    /// </summary>
    private void ExecuteCommand()
    {
        if (Command is null)
        {
            return;
        }

        if (Command is RoutedCommand routedCommand)
        {
            var target = CommandTarget ?? this;
            if (routedCommand.CanExecute(CommandParameter, target))
            {
                routedCommand.Execute(CommandParameter, target);
            }

            return;
        }

        if (Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }
}
