using System.Windows;
using System.Windows.Media.Animation;

namespace Navius.Wpf.Primitives.Controls.Internal;

/// <summary>
/// Shared open/close height animation for the Collapsible and Accordion panel parts,
/// standing in for the web contract's CSS enter/exit transition (which measures natural
/// size via a JS SizeObserver and drives "starting-style"/"ending-style" CSS variables).
/// WPF has no ResizeObserver equivalent, so natural height is measured directly via
/// <see cref="UIElement.Measure"/>, and the whole "exit completes before collapse" rule
/// is reproduced with a plain <see cref="DoubleAnimation"/> whose Completed callback
/// performs the collapse (rather than collapsing immediately, which would clip the
/// closing animation).
///
/// Logical open/closed state (aria-expanded, automation peer state, data-open/data-closed
/// style triggers) is intentionally NOT gated on animation completion: callers flip that
/// state synchronously, independent of this class, so it stays correct and unit-testable
/// even in headless test hosts where no dispatcher pumps the animation clock to completion.
/// </summary>
internal static class PanelHeightAnimator
{
    public static readonly Duration Duration = new(TimeSpan.FromMilliseconds(180));

    /// <summary>Grows Height from 0 to the panel's measured natural height, then releases the animation back to Auto sizing.</summary>
    public static void Open(FrameworkElement panel)
    {
        panel.BeginAnimation(FrameworkElement.HeightProperty, null);
        panel.Visibility = Visibility.Visible;

        panel.Measure(new Size(double.IsNaN(panel.Width) ? double.PositiveInfinity : panel.Width, double.PositiveInfinity));
        var target = panel.DesiredSize.Height;

        var animation = new DoubleAnimation(0, target, Duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            // Hand height back to Auto sizing so subsequent content changes still measure
            // correctly, instead of leaving it pinned at the size measured at open-time.
            panel.BeginAnimation(FrameworkElement.HeightProperty, null);
            panel.Height = double.NaN;
        };

        panel.Height = 0;
        panel.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    /// <summary>
    /// Shrinks Height from the panel's current height to 0, collapsing it once the
    /// animation completes. <paramref name="onCollapsed"/> runs right after Visibility
    /// flips to Collapsed (e.g. to unmount content), so exit always completes before
    /// collapse, per the contract's transition-phase ordering.
    /// </summary>
    public static void Close(FrameworkElement panel, Action? onCollapsed = null)
    {
        panel.BeginAnimation(FrameworkElement.HeightProperty, null);
        var from = double.IsNaN(panel.Height) ? panel.ActualHeight : panel.Height;

        var animation = new DoubleAnimation(from, 0, Duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        animation.Completed += (_, _) =>
        {
            panel.Visibility = Visibility.Collapsed;
            onCollapsed?.Invoke();
        };

        panel.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }
}
