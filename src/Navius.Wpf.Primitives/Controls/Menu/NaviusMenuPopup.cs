using System.Windows;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier A: derives from the native ContextMenu, used here as a trigger-anchored dropdown
/// rather than a right-click menu (NaviusMenuTrigger sets PlacementTarget and IsOpen
/// directly instead of relying on ContextMenuService). Absorbs the contract's separate
/// NaviusMenuPortal (no-op: WPF popups already float in their own window layer, nothing to
/// teleport) and NaviusMenuPositioner (folded into these Side/Align/offset properties
/// directly on the popup, since WPF has no separate positioning-div concept) parts.
///
/// Side/Align map onto PlacementMode.Custom uniformly: WPF's built-in PlacementMode enum
/// (Top/Bottom/Left/Right/...) only expresses 4 single-edge placements with no independent
/// alignment axis, so it can't cover the contract's 4-side x 3-align = 12 combinations. The
/// CustomPopupPlacementCallback below computes the exact offset per (Side, Align) pair by
/// reusing PlacementMath.Place (the same pure math the Popover-family Tier B controls use),
/// with Flip/Shift disabled - native WPF Popup placement replaces the JS collision engine
/// per the parity doc's own WPF strategy note, so this stays a single fixed placement.
/// </summary>
public class NaviusMenuPopup : System.Windows.Controls.ContextMenu
{
    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side),
        typeof(PlacementSide),
        typeof(NaviusMenuPopup),
        new PropertyMetadata(PlacementSide.Bottom));

    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align),
        typeof(PlacementAlign),
        typeof(NaviusMenuPopup),
        new PropertyMetadata(PlacementAlign.Center));

    public static readonly DependencyProperty SideOffsetProperty = DependencyProperty.Register(
        nameof(SideOffset),
        typeof(double),
        typeof(NaviusMenuPopup),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty AlignOffsetProperty = DependencyProperty.Register(
        nameof(AlignOffset),
        typeof(double),
        typeof(NaviusMenuPopup),
        new PropertyMetadata(0d));

    /// <summary>
    /// Contract's roving-focus wrap-around toggle. Native MenuItem/ContextMenu arrow-key
    /// navigation has no public switch to disable wrap-around, so this is kept for
    /// parameter-surface parity but has no behavioral effect; see this repo's
    /// docs/parity/menu.md "WPF implementation notes" for detail.
    /// </summary>
    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop),
        typeof(bool),
        typeof(NaviusMenuPopup),
        new PropertyMetadata(false));

    static NaviusMenuPopup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuPopup),
            new FrameworkPropertyMetadata(typeof(NaviusMenuPopup)));
    }

    public NaviusMenuPopup()
    {
        Placement = PlacementMode.Custom;
        CustomPopupPlacementCallback = ComputePlacement;
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

    public double SideOffset
    {
        get => (double)GetValue(SideOffsetProperty);
        set => SetValue(SideOffsetProperty, value);
    }

    public double AlignOffset
    {
        get => (double)GetValue(AlignOffsetProperty);
        set => SetValue(AlignOffsetProperty, value);
    }

    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    private CustomPopupPlacement[] ComputePlacement(Size popupSize, Size targetSize, Point offset)
    {
        var anchor = new Rect(0, 0, targetSize.Width, targetSize.Height);
        var options = new AnchoredPlacementOptions
        {
            Side = Side,
            Align = Align,
            SideOffset = SideOffset,
            AlignOffset = AlignOffset,
            FlipEnabled = false,
            ShiftEnabled = false,
        };

        var result = PlacementMath.Place(anchor, popupSize, Rect.Empty, options);
        return new[] { new CustomPopupPlacement(result.Origin, PopupPrimaryAxis.None) };
    }
}
