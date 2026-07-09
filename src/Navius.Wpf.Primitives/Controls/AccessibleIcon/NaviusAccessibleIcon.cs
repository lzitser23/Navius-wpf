using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B (custom lookless control): wraps arbitrary icon content (typically a Path/Geometry or
/// vector Image) and supplies an accessible name for it. The contract renders the icon verbatim
/// plus a separate visually-hidden `&lt;span&gt;` carrying the name; WPF has no DOM text-node
/// equivalent to hide, so the correct port sets AutomationProperties.Name directly -- both on the
/// hosted content (mirroring "the name travels with the icon") and via the peer's GetNameCore, so
/// the name is discoverable regardless of how the icon element exposes its own automation peer
/// (see docs/parity/accessible-icon.md "WPF strategy").
/// </summary>
public class NaviusAccessibleIcon : ContentControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(NaviusAccessibleIcon), new PropertyMetadata(null, OnLabelChanged));

    static NaviusAccessibleIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccessibleIcon), new FrameworkPropertyMetadata(typeof(NaviusAccessibleIcon)));
    }

    /// <summary>The accessible name announced by screen readers. Required for the icon to surface in UIA at all.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusAccessibleIconAutomationPeer(this);

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        ApplyLabelToContent();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAccessibleIcon)d).ApplyLabelToContent();

    private void ApplyLabelToContent()
    {
        if (Content is not DependencyObject contentElement)
        {
            return;
        }

        AutomationProperties.SetName(contentElement, Label ?? string.Empty);
    }
}
