using System.Windows;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.ContextMenu;

/// <summary>
/// Tier B: derives from the native ContextMenu. Unlike NaviusMenuPopup (a Menu-family
/// trigger-anchored dropdown), this one is primarily opened via right-click through
/// NaviusContextMenuTrigger's native ContextMenu assignment - WPF's default placement for a
/// ContextMenu opened that way is already PlacementMode.MousePoint, which matches the
/// contract's cursor-anchored NaviusContextMenuPopup for free, so the constructor leaves
/// Placement untouched.
///
/// RequestOpenAt gives the contract's explicit `Open(point)` entry point (used e.g. for a
/// custom long-press gesture, which WPF's native ContextMenuService does not implement - see
/// docs/parity/context-menu.md "WPF implementation notes"): it switches to
/// PlacementMode.Custom for that one open and reuses the same PlacementMath-based Side/Align
/// mapping as NaviusMenuPopup, anchored at a zero-size rect at the given point instead of a
/// target element's bounds. Side/Align default to "right"/"start" per the contract's
/// NaviusContextMenuPositioner (submenus opening to the side of the item that triggered them).
/// </summary>
public class NaviusContextMenuPopup : System.Windows.Controls.ContextMenu
{
    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side),
        typeof(PlacementSide),
        typeof(NaviusContextMenuPopup),
        new PropertyMetadata(PlacementSide.Right));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align),
        typeof(PlacementAlign),
        typeof(NaviusContextMenuPopup),
        new PropertyMetadata(PlacementAlign.Start));

    static NaviusContextMenuPopup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusContextMenuPopup),
            new FrameworkPropertyMetadata(typeof(NaviusContextMenuPopup)));
    }

    public PlacementSide Side
    {
        get => (PlacementSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public PlacementAlign Align
    {
        get => (PlacementAlign)GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    /// <summary>Contract's explicit cursor-anchored Open(point) API, e.g. for a keyboard menu key or a custom gesture.</summary>
    public void RequestOpenAt(UIElement target, Point pointRelativeToTarget)
    {
        PlacementTarget = target;
        Placement = PlacementMode.Custom;
        CustomPopupPlacementCallback = (popupSize, _, _) =>
        {
            var anchor = new Rect(pointRelativeToTarget, new Size(0, 0));
            var options = new AnchoredPlacementOptions
            {
                Side = Side,
                Align = Align,
                FlipEnabled = false,
                ShiftEnabled = false,
            };

            var result = PlacementMath.Place(anchor, popupSize, Rect.Empty, options);
            return new[] { new CustomPopupPlacement(result.Origin, PopupPrimaryAxis.None) };
        };
        IsOpen = true;
    }
}
