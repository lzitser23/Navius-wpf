using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Navius.Wpf.Primitives.Controls.OverlaySurface;

namespace Navius.Wpf.Primitives.Controls.Drawer;

/// <summary>
/// Tier B (custom lookless control). WPF port of the web NaviusDrawer family: a modal sheet
/// docked to <see cref="Side"/> that slides in/out. Same dismiss semantics as
/// <see cref="Dialog.NaviusDialog"/> (Escape, optional outside click, Close-bound content).
///
/// Deferred for M2 (per the task brief): drag-to-dismiss / swipe gesture, snap points, and the
/// CSS-custom-property swipe-progress hooks the web contract's `createSheetSwipe` engine exposes
/// (data-swiping, --drawer-swipe-movement-x/y). This port is keyboard + button dismiss only; see
/// docs/parity/drawer.md's "## WPF implementation notes" for the full list.
///
/// The default template names the sliding panel "PART_Panel"; enter/exit both fade the whole
/// control (backdrop + panel, inherited from <see cref="NaviusOverlaySurfaceBase"/>) and
/// translate PART_Panel between <see cref="DrawerGeometry.GetOffscreenOffset"/> and (0, 0) over
/// 150ms (falls back to a default extent per side if the template panel has no explicit
/// Width/Height set for its offset axis).
/// </summary>
public class NaviusDrawer : NaviusOverlaySurfaceBase
{
    private const double DefaultExtent = 360d;

    public static readonly DependencyProperty ModalProperty = DependencyProperty.Register(
        nameof(Modal),
        typeof(bool),
        typeof(NaviusDrawer),
        new PropertyMetadata(true));

    public static readonly DependencyProperty CloseOnOutsideClickProperty = DependencyProperty.Register(
        nameof(CloseOnOutsideClick),
        typeof(bool),
        typeof(NaviusDrawer),
        new PropertyMetadata(true));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side),
        typeof(NaviusDrawerSide),
        typeof(NaviusDrawer),
        new PropertyMetadata(NaviusDrawerSide.Bottom));

    private FrameworkElement? _panel;
    private TranslateTransform? _panelTransform;

    static NaviusDrawer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusDrawer),
            new FrameworkPropertyMetadata(typeof(NaviusDrawer)));
    }

    public bool Modal
    {
        get => (bool)GetValue(ModalProperty);
        set => SetValue(ModalProperty, value);
    }

    public bool CloseOnOutsideClick
    {
        get => (bool)GetValue(CloseOnOutsideClickProperty);
        set => SetValue(CloseOnOutsideClickProperty, value);
    }

    public NaviusDrawerSide Side
    {
        get => (NaviusDrawerSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    protected override bool ModalEffective => Modal;

    protected override bool CloseOnOutsideClickEffective => CloseOnOutsideClick;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _panel = GetTemplateChild("PART_Panel") as FrameworkElement;
        if (_panel is null)
        {
            _panelTransform = null;
            return;
        }

        if (_panel.RenderTransform is TranslateTransform existing)
        {
            _panelTransform = existing;
        }
        else
        {
            _panelTransform = new TranslateTransform();
            _panel.RenderTransform = _panelTransform;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusDrawerAutomationPeer(this);

    protected override void PlayEnterAnimation()
    {
        base.PlayEnterAnimation();
        AnimatePanel(entering: true, EnterDuration);
    }

    protected override void PlayExitAnimation(Action onComplete)
    {
        AnimatePanel(entering: false, ExitDuration);
        base.PlayExitAnimation(onComplete);
    }

    private void AnimatePanel(bool entering, TimeSpan duration)
    {
        if (_panel is null || _panelTransform is null)
        {
            return;
        }

        var extentWidth = ResolveExtent(_panel.Width);
        var extentHeight = ResolveExtent(_panel.Height);
        var offset = DrawerGeometry.GetOffscreenOffset(Side, new Size(extentWidth, extentHeight));

        if (PresentationSource.FromVisual(this) is null)
        {
            _panelTransform.X = entering ? 0 : offset.X;
            _panelTransform.Y = entering ? 0 : offset.Y;
            return;
        }

        var fromX = entering ? offset.X : 0d;
        var toX = entering ? 0d : offset.X;
        var fromY = entering ? offset.Y : 0d;
        var toY = entering ? 0d : offset.Y;

        _panelTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(fromX, toX, duration));
        _panelTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(fromY, toY, duration));
    }

    private static double ResolveExtent(double explicitValue) =>
        double.IsNaN(explicitValue) || explicitValue <= 0 ? DefaultExtent : explicitValue;
}

/// <summary>
/// Same role mapping as NaviusDialog (see docs/parity/drawer.md's WPF strategy section): a
/// docked sheet is still, semantically, a dialog to assistive tech.
/// </summary>
internal sealed class NaviusDrawerAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusDrawerAutomationPeer(NaviusDrawer owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override string GetClassNameCore() => nameof(NaviusDrawer);

    protected override bool IsDialogCore() => true;
}
