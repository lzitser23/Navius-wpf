using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Ui.Internal;

namespace Navius.Wpf.Ui.Spinner;

/// <summary>
/// A rotating-arc loading indicator (indeterminate only; for a determinate progress bar use
/// Navius.Wpf.Primitives' NaviusProgress instead). ShouldAnimate is read once at construction
/// from ReducedMotion.AnimationsEnabled, same guard as NaviusSkeleton: when the OS "reduce
/// animations" preference is on, the arc renders statically instead of spinning forever.
/// </summary>
public class NaviusSpinner : Control
{
    private static readonly DependencyPropertyKey ShouldAnimatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShouldAnimate), typeof(bool), typeof(NaviusSpinner), new PropertyMetadata(true));

    public static readonly DependencyProperty ShouldAnimateProperty = ShouldAnimatePropertyKey.DependencyProperty;

    static NaviusSpinner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusSpinner), new FrameworkPropertyMetadata(typeof(NaviusSpinner)));
    }

    public NaviusSpinner()
    {
        SetValue(ShouldAnimatePropertyKey, ReducedMotion.AnimationsEnabled);
        AutomationProperties.SetName(this, "Loading");
    }

    /// <summary>True when the rotation should run, snapshotted from ReducedMotion.AnimationsEnabled at construction.</summary>
    public bool ShouldAnimate => (bool)GetValue(ShouldAnimateProperty);

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSpinnerAutomationPeer(this);
}

/// <summary>
/// A plain Control ships no automation peer, so the "Loading" name set on the spinner never reached
/// UIA and the control had no control type at all. This peer reports ProgressBar (the honest type
/// for an indeterminate loading indicator; NaviusProgress is the determinate sibling) and lets the
/// inherited GetName surface the AutomationProperties.Name ("Loading"). No RangeValue pattern is
/// exposed because the spin is indeterminate -- there is no meaningful min/now/max to report.
/// </summary>
internal sealed class NaviusSpinnerAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSpinnerAutomationPeer(NaviusSpinner owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    protected override string GetClassNameCore() => nameof(NaviusSpinner);
}
