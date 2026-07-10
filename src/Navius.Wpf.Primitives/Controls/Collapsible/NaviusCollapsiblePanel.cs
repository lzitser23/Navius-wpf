using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Collapsible;

/// <summary>
/// Tier B. The contract's enter/exit "starting-style"/"ending-style" CSS transition,
/// driven by a JS SizeObserver publishing natural size, has no WPF equivalent; it is
/// reimplemented with a measured-height DoubleAnimation via PanelHeightAnimator (see that
/// class for why animation completion is decoupled from logical open/closed state).
///
/// KeepMounted maps to whether Content is cached and cleared on close rather than left in
/// place: default (KeepMounted=false) caches and nulls out Content once the close
/// animation finishes, approximating the contract's DOM removal; KeepMounted=true leaves
/// Content in place and only toggles Visibility, matching "kept mounted while closed."
/// HiddenUntilFound is dropped (see NaviusCollapsible's remarks); it implies KeepMounted
/// with no WPF "find in page" affordance to hook a reveal callback into.
/// </summary>
public class NaviusCollapsiblePanel : ContentControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(NaviusCollapsiblePanel),
        new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty KeepMountedProperty = DependencyProperty.Register(
        nameof(KeepMounted),
        typeof(bool),
        typeof(NaviusCollapsiblePanel),
        new PropertyMetadata(false));

    private object? _cachedContent;

    static NaviusCollapsiblePanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCollapsiblePanel),
            new FrameworkPropertyMetadata(typeof(NaviusCollapsiblePanel)));
    }

    public NaviusCollapsiblePanel()
    {
        Visibility = Visibility.Collapsed;
    }

    /// <summary>Set by the ancestor NaviusCollapsible; mirrors the contract's data-open/data-closed.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool KeepMounted
    {
        get => (bool)GetValue(KeepMountedProperty);
        set => SetValue(KeepMountedProperty, value);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCollapsiblePanel)d).ApplyOpenState((bool)e.NewValue);

    private void UnmountContent()
    {
        _cachedContent = Content;
        Content = null;
    }

    private void ApplyOpenState(bool isOpen)
    {
        if (isOpen)
        {
            if (Content is null && _cachedContent is not null)
            {
                Content = _cachedContent;
                _cachedContent = null;
            }

            PanelHeightAnimator.Open(this);
        }
        else
        {
            PanelHeightAnimator.Close(this, onCollapsed: KeepMounted ? null : UnmountContent);
        }
    }
}
