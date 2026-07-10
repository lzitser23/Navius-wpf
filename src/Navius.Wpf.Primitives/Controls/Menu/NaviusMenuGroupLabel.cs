using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier B: a small lookless, non-focusable heading placed directly among a menu's
/// NaviusMenuItem siblings. Native Menu/ContextMenu keyboard roving only ever considers
/// MenuItem-typed containers as navigable, so a plain ContentControl here is safely skipped
/// by arrow-key navigation without any extra wiring - the same reason NaviusSeparator (also
/// reused as-is for menu separators, not resubclassed here) is safe to drop into an item
/// collection.
///
/// The contract's NaviusMenuGroup wrapper (a `role="group"` element pairing this label with
/// its items via aria-labelledby) is not reimplemented: like NaviusMenuRadioItem's GroupName,
/// a transparent non-MenuItem wrapper around a subset of items would break native item
/// roving for everything inside it. Grouping here is visual-only (this label plus ordinary
/// item ordering), not a distinct accessible group.
/// </summary>
public class NaviusMenuGroupLabel : ContentControl
{
    static NaviusMenuGroupLabel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuGroupLabel),
            new FrameworkPropertyMetadata(typeof(NaviusMenuGroupLabel)));
    }

    public NaviusMenuGroupLabel()
    {
        Focusable = false;
    }
}
